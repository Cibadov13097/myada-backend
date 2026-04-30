using MyAda.Observability;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NotificationService.Api.Security;
using NotificationService.Api.Hubs;
using NotificationService.Api.Realtime;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Seed;
using NotificationAppService = NotificationService.Application.Services.NotificationService;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);
builder.AddMyAdaSerilog("notification-service");

var isDevelopment = builder.Environment.IsDevelopment();
var jwksUrl = builder.Configuration["Auth:JwksUrl"];
var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
});

if (!string.IsNullOrWhiteSpace(jwksUrl))
{
    const int jwksMaxAttempts = 15;
    var jwksDelay = TimeSpan.FromSeconds(2);
    Exception? jwksLastError = null;

    for (var attempt = 1; attempt <= jwksMaxAttempts; attempt++)
    {
        try
        {
            await JwksProvider.InitializeAsync(jwksUrl);
            jwksLastError = null;
            break;
        }
        catch (Exception ex)
        {
            jwksLastError = ex;
            if (attempt < jwksMaxAttempts)
                await Task.Delay(jwksDelay);
        }
    }

    if (jwksLastError is null)
    {
        builder.Services.AddHostedService<JwksRefreshHostedService>();

        authBuilder.AddJwtBearer("Bearer", options =>
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
                IssuerSigningKeyResolver = (_, _, _, _) => JwksProvider.GetKeys()
            };
            options.Events = CreateSignalRJwtEvents();
        });
    }
    else if (isDevelopment)
    {
        Console.WriteLine(
            "[NotificationService] JWKS unavailable at '{0}' after {1} attempts: {2}. Using Development bearer bypass.",
            jwksUrl,
            jwksMaxAttempts,
            jwksLastError.Message);
        authBuilder.AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("Bearer", _ => { });
    }
    else
    {
        throw new InvalidOperationException(
            $"Could not load JWKS from '{jwksUrl}'. Ensure AuthService is healthy and GET /.well-known/jwks.json returns 200.",
            jwksLastError);
    }
}
else if (isDevelopment)
{
    authBuilder.AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("Bearer", _ => { });
}
else
{
    throw new InvalidOperationException(
        "Auth:JwksUrl is required for JWT validation. Set to AuthService JWKS URL, e.g. http://localhost:5001/.well-known/jwks.json");
}

builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Notification Service API",
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
            Id = "Bearer"
        }
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationService, NotificationAppService>();
builder.Services.AddSingleton<INotificationRealtimeNotifier, NotificationRealtimeNotifier>();
builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddDbContext<NotificationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

builder.AddMyAdaOpenTelemetry("notification-service");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
    await NotificationDbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
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
}

app.UseCors("AllowAll");
app.UseMyAdaPrometheusScraping();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

app.Run();

static JwtBearerEvents CreateSignalRJwtEvents()
{
    return new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrWhiteSpace(accessToken) &&
                path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            if (context.Principal?.Identity is not ClaimsIdentity identity)
                return Task.CompletedTask;

            var sub = identity.FindFirst("sub")?.Value;
            if (!string.IsNullOrWhiteSpace(sub) &&
                !identity.HasClaim(claim =>
                    claim.Type == ClaimTypes.NameIdentifier &&
                    string.Equals(claim.Value, sub, StringComparison.Ordinal)))
            {
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            return Task.CompletedTask;
        }
    };
}
