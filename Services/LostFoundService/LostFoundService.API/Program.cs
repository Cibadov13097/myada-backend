using AutoMapper;
using LostFoundService.Api.Middlewares;
using LostFoundService.Api.Security;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Application.Mappings;
using LostFoundService.Infrastructure.Persistence.Context;
using LostFoundService.Infrastructure.Persistence.Repositories;
using LostFoundService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. SIGNING KEY: JWKS URL or local public key file
// =====================================================

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
var publicKeyPath = builder.Configuration["Auth:PublicKeyPath"];

SecurityKey? signingKey = null;

// 1) Lokal public key varsa istifadə et (AuthService asılılığı yoxdur)
if (!string.IsNullOrWhiteSpace(publicKeyPath) && File.Exists(publicKeyPath))
{
    var publicKeyPem = await File.ReadAllTextAsync(publicKeyPath);
    var rsa = System.Security.Cryptography.RSA.Create();
    rsa.ImportFromPem(publicKeyPem);
    signingKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);
}
// 2) JWKS sınanır; uğursuz olsa və PublicKeyPath varsa fallback
else if (!string.IsNullOrWhiteSpace(jwksUrl))
{
    try
    {
        await JwksProvider.InitializeAsync(jwksUrl);
    }
    catch (HttpRequestException)
    {
        if (!string.IsNullOrWhiteSpace(publicKeyPath) && File.Exists(publicKeyPath))
        {
            var publicKeyPem = await File.ReadAllTextAsync(publicKeyPath);
            var rsa = System.Security.Cryptography.RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            signingKey = new Microsoft.IdentityModel.Tokens.RsaSecurityKey(rsa);
        }
        else
            throw new InvalidOperationException(
                "Auth:JwksUrl is unreachable and Auth:PublicKeyPath is not set or file not found. Start AuthService or set Auth:PublicKeyPath to public_key.pem.");
    }
}
else if (string.IsNullOrWhiteSpace(publicKeyPath) || !File.Exists(publicKeyPath))
{
    throw new InvalidOperationException(
        "Configure Auth:PublicKeyPath (path to public_key.pem) or Auth:JwksUrl for JWT validation.");
}

var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";

// ==========================
// 2. JWT AUTHENTICATION 
// ==========================

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false; // keep "sub" as "sub" in ClaimsPrincipal

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = validIssuer,

            ValidateAudience = true,
            ValidAudience = validAudience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Expired token -> 401. No grace period (strict).
            ClockSkew = TimeSpan.Zero,

            // Either local public key (no JWKS) or JWKS resolver
            IssuerSigningKey = signingKey,
            IssuerSigningKeyResolver = signingKey == null
                ? (token, securityToken, kid, parameters) => JwksProvider.GetKeys()
                : null
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
        Title = "LostFound Service API",
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


builder.Services.AddAutoMapper(opt =>
{
    opt.AddProfile(new LostItemProfile());
    opt.AddProfile(new FoundItemProfile());
}); 

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IFoundItemService, FoundItemService>();
builder.Services.AddScoped<IFoundItemRepository, FoundItemRepository>();

builder.Services.AddScoped<IFoundItemAdminService, FoundItemAdminService>();
builder.Services.AddScoped<ILostItemAdminService, LostItemAdminService>();

builder.Services.AddScoped<ILostItemService, LostItemService>();
builder.Services.AddScoped<ILostItemRepository,LostItemRepository>();

builder.Services.AddControllers();

// ==========================
// 5. DATABASE
// ==========================

builder.Services.AddDbContext<LostFoundDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

// ==========================
// 6. BUILD APP
// ==========================

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LostFoundDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
