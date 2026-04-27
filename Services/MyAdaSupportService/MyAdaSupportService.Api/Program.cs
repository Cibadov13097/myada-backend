using MyAda.Observability;
using AutoWrapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MyAdaSupportService.Api.Hubs;
using MyAdaSupportService.Api.Realtime;
using MyAdaSupportService.Api.Security;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.Mapping;
using MyAdaSupportService.Infrastructure;

namespace MyAdaSupportService.Api;

public partial class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddMyAdaSerilog("myada-support-service");

        var jwksUrl = builder.Configuration["Auth:JwksUrl"];
        if (string.IsNullOrWhiteSpace(jwksUrl))
            throw new InvalidOperationException(
                "Auth:JwksUrl is required for JWT validation. Set to AuthService JWKS URL, e.g. http://localhost:5001/.well-known/jwks.json");

        await JwksProvider.InitializeAsync(jwksUrl);

        var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
        var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";

        builder.Services.AddHostedService<JwksRefreshHostedService>();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.RequireHttpsMetadata = false;
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = validIssuer,
                    ValidateAudience = true,
                    ValidAudience = validAudience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.Zero,
                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) => JwksProvider.GetKeys()
                };
            });

        builder.Services.AddAuthorization();

        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MyAda Support Service API",
                Version = "v1"
            });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Description = "Enter JWT Bearer token: Bearer {token}",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = JwtBearerDefaults.AuthenticationScheme
                }
            };

            c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });
        });
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ISupportRealtimeNotifier, SupportRealtimeNotifier>();

        builder.AddMyAdaOpenTelemetry("myada-support-service");

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppDbContextSeed.SeedAsync(dbContext).GetAwaiter().GetResult();
        }

        app.UseSwagger(c =>
        {
            c.PreSerializeFilters.Add((swagger, req) =>
            {
                var forwardedPrefix = req.Headers["X-Forwarded-Prefix"].FirstOrDefault();
                var basePath = string.IsNullOrWhiteSpace(forwardedPrefix)
                    ? "/"
                    : $"/{forwardedPrefix.Trim().Trim('/')}";

                swagger.Servers = new List<OpenApiServer>
                {
                    new() { Url = basePath }
                };
            });
        });
        app.UseSwaggerUI();
        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions
        {
            ShowApiVersion = false,
            ShowStatusCode = true,
            IsApiOnly = true,
            UseCustomSchema = false,
            EnableExceptionLogging = true
        });
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors();
        app.UseMyAdaPrometheusScraping();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<SupportUpdatesHub>("/hubs/support-updates").RequireAuthorization();
        app.Run();
    }
}
