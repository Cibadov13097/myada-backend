using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using AutoWrapper;
using ClubManagementService.API.Filters;
using ClubManagementService.API.Security;
using ClubManagementService.API.Swagger;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Application.Mappings;
using ClubManagementService.Infrastructure.Persistence.Context;
using ClubManagementService.Infrastructure.Persistence.Repositories;
using ClubManagementService.Infrastructure.Persistence.Seed;
using ClubManagementService.Infrastructure.Messaging;
using ClubManagementService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// ==========================
// 1. JWT via AuthService JWKS
// ==========================

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Bearer";
    options.DefaultChallengeScheme = "Bearer";
});

if (!string.IsNullOrWhiteSpace(jwksUrl))
{
    // Auth may still be starting in Docker; JWKS must succeed before JWT bearer is wired.
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
        var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
        var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";

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
                IssuerSigningKeyResolver = (token, securityToken, kid, parameters) => JwksProvider.GetKeys()
            };
        });
    }
    else if (isDevelopment)
    {
        Console.WriteLine(
            "[ClubManagementService] JWKS unavailable at '{0}' after {1} attempts: {2}. Using Development bearer bypass.",
            jwksUrl,
            jwksMaxAttempts,
            jwksLastError.Message);
        authBuilder.AddScheme<AuthenticationSchemeOptions, DevBypassAuthHandler>("Bearer", _ => { });
    }
    else
    {
        throw new InvalidOperationException(
            $"Could not load JWKS from '{jwksUrl}'. Ensure AuthService is healthy and GET /.well-known/jwks.json returns 200 (e.g. Jwt:PublicKeyPath and PEM mounts in Docker).",
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

// DbContext
builder.Services.AddDbContext<ClubManagementDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

// AutoMapper
builder.Services.AddAutoMapper(typeof(ClubManagementProfile));

// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IClubRepository, ClubRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();
builder.Services.AddScoped<IJobVacancyRepository, JobVacancyRepository>();
builder.Services.AddScoped<IClubPositionRepository, ClubPositionRepository>();
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<ISubeventRepository, SubeventRepository>();
builder.Services.AddScoped<IClubMembershipRepository, ClubMembershipRepository>();

// Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IJobVacancyService, JobVacancyService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<ISubeventService, SubeventService>();
builder.Services.AddSingleton<IClubNotificationService, ClubNotificationService>();
builder.Services.AddSingleton<ClubAdminWorkspaceStore>();
builder.Services.AddScoped<IClubAdminService, ClubAdminService>();

// SPA CORS: explicit origins (required when combining with credentials; avoids invalid AllowAnyOrigin+WithOrigins).
// OPTIONS preflight must succeed before JWT — UseCors is registered first in the pipeline below.
var corsSection = builder.Configuration.GetSection("Cors");
var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? [];
var allowCredentials = corsSection.GetValue("AllowCredentials", false);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
            if (allowCredentials)
                policy.AllowCredentials();
        }
        else if (isDevelopment)
        {
            policy.WithOrigins("http://localhost:3000");
        }
        else
        {
            throw new InvalidOperationException(
                "Configure Cors:AllowedOrigins (e.g. [\"https://your-spa.example\"]) for non-Development environments.");
        }

        policy
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowAnyHeader()
            .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

// API v1 only (URL segment /api/v1/...)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = false;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
}).AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();

builder.Services.AddControllers(options => options.Filters.Add<HttpExceptionFilter>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));

    // One OpenAPI document per API group from the version explorer.
    c.DocInclusionPredicate((documentName, apiDescription) =>
        string.Equals(apiDescription.GroupName, documentName, StringComparison.OrdinalIgnoreCase));

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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClubManagementDbContext>();
    db.Database.Migrate();
    await ClubManagementSeeder.SeedAsync(db);
}

app.UseCors("SpaCors");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions.OrderByDescending(d => d.ApiVersion))
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", $"Club {description.GroupName}");
    });
}

app.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions
{
    ShowApiVersion = false,
    ShowStatusCode = true,
    IsApiOnly = true,
    UseCustomSchema = false,
    EnableExceptionLogging = true
});

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
