using MyAda.Observability;
using LocationService.Api.Middlewares;
using LocationService.Api.Security;
using LocationService.Application.Interfaces.Repositories;
using LocationService.Application.Interfaces.Services;
using LocationService.Application.Services;
using LocationService.Infrastructure.Persistence.Context;
using LocationService.Infrastructure.Persistence.Seed;
using LocationService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. JWT: JWKS endpoint only (no local public key files)
// =====================================================

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
if (string.IsNullOrWhiteSpace(jwksUrl))
    throw new InvalidOperationException(
        "Auth:JwksUrl is required for JWT validation. Set to AuthService JWKS URL, e.g. http://localhost:5001/.well-known/jwks.json");

await JwksProvider.InitializeAsync(jwksUrl);

var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";

builder.Services.AddHostedService<JwksRefreshHostedService>();

// ==========================
// 2. JWT AUTHENTICATION (RS256, issuer/audience/lifetime/signature)
// ==========================

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
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

// ==========================
// 3. SWAGGER + JWT AUTH
// ==========================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Location Service API",
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
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// ==========================
// 4. SERVICE REGISTRATION
// ==========================

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ILocationService, LocationService.Application.Services.LocationService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IRoomService, RoomService>();

builder.Services.AddControllers();

// ==========================
// 5. DATABASE
// ==========================

var connectionString = builder.Configuration.GetConnectionString("LocationService")
    ?? "Host=MyAda;Port=5432;Database=MyAdaLocationDb;Username=postgres;Password=123";
builder.Services.AddDbContext<LocationDbContext>(options =>
    options.UseNpgsql(connectionString));

// ==========================
// 6. BUILD APP
// ==========================

builder.AddMyAdaOpenTelemetry("location-service");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LocationDbContext>();
    await db.Database.MigrateAsync();
    await LocationDbSeeder.SeedAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swagger, req) =>
        {
            swagger.Servers = new List<OpenApiServer>
            {
                new() { Url = "/location" }
            };
        });
    });
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseMyAdaPrometheusScraping();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
