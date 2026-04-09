using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Application.Mappings;
using ClubManagementService.Infrastructure.Persistence.Context;
using ClubManagementService.Infrastructure.Persistence.Repositories;
using ClubManagementService.Infrastructure.Persistence.Seed;
using ClubManagementService.Infrastructure.Services;
using ClubManagementService.API.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;

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
    await JwksProvider.InitializeAsync(jwksUrl);

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
builder.Services.AddScoped<IJobApplicationRepository, JobApplicationRepository>();
builder.Services.AddScoped<ISubeventRepository, SubeventRepository>();

// Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IClubService, ClubService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IEventRegistrationService, EventRegistrationService>();
builder.Services.AddScoped<IJobVacancyService, JobVacancyService>();
builder.Services.AddScoped<IJobApplicationService, JobApplicationService>();
builder.Services.AddScoped<ISubeventService, SubeventService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Controllers & Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Club Management Service API",
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

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClubManagementDbContext>();
    db.Database.Migrate();
    await ClubManagementSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll"); 
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
