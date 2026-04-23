using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Application.Services;
using AuthService.API.Integrations;
using AuthService.API.Security;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Options;
using AuthService.Infrastructure.Persistence.Context;
using AuthService.Infrastructure.Persistence.Repositories;
using AuthService.Infrastructure.Persistence.Seeders;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 1. Database
builder.Services.AddDbContext<MyAdaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

// 2. Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyAdaDbContext>()
.AddDefaultTokenProviders();

// 3. JWT options (issuer, audience, lifetime)
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddSingleton(jwtOptions);

// 4. RSA private key (path from config or default)
var keyPath = builder.Configuration["Jwt:PrivateKeyPath"] ?? "Keys/private.pem";
var fullPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, keyPath));

if (builder.Environment.IsDevelopment() && !File.Exists(fullPath))
{
    var contentRoot = builder.Environment.ContentRootPath;
    var fallbacks = new[]
    {
        Path.Combine(contentRoot, "Keys", "private.pem"),
        Path.Combine(contentRoot, "..", "..", "..", "Keys", "private.pem"),
        Path.Combine(contentRoot, "..", "..", "..", "keys", "private.pem")
    };
    foreach (var path in fallbacks)
    {
        var resolved = Path.GetFullPath(path);
        if (File.Exists(resolved))
        {
            fullPath = resolved;
            break;
        }
    }
}

var privateKeyPem = File.ReadAllText(fullPath);
var rsa = RSA.Create();
rsa.ImportFromPem(privateKeyPem);

builder.Services.AddSingleton<RSA>(rsa);
builder.Services.AddScoped<TokenService>();

// 5. Refresh token services
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IRefreshTokenService, RefreshTokenService>();
builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection(S3StorageOptions.SectionName));
builder.Services.AddScoped<IProfileImageStorageService, S3ProfileImageStorageService>();
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false";
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<IAccessTokenRevocationStore, RedisAccessTokenRevocationStore>();
builder.Services.AddHttpClient<SupportStaffProvisioningClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["SupportService:BaseUrl"] ?? "http://localhost:5008";
    if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        baseUrl += "/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(10);
});
builder.Services.AddHttpClient<NotificationEmailClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["NotificationService:BaseUrl"] ?? "http://localhost:5004";
    if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
        baseUrl += "/";

    var timeout = 10;
    var configuredTimeout = configuration["NotificationService:RequestTimeoutSeconds"];
    if (int.TryParse(configuredTimeout, out var parsedTimeout) && parsedTimeout > 0)
        timeout = parsedTimeout;

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeout);
});

// 6. JWT Bearer authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var rsaPublic = RSA.Create();
    rsaPublic.ImportParameters(rsa.ExportParameters(false));

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new RsaSecurityKey(rsaPublic),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var principal = context.Principal;
            var jti = principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value
                      ?? principal?.FindFirst("jti")?.Value;

            if (string.IsNullOrWhiteSpace(jti))
                return;

            var revocationStore = context.HttpContext.RequestServices.GetRequiredService<IAccessTokenRevocationStore>();
            if (await revocationStore.IsRevokedAsync(jti, context.HttpContext.RequestAborted))
                context.Fail("Token has been revoked.");
        }
    };
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

// 7. Controllers
builder.Services.AddControllers();

// 8. Swagger + JWT Auth
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Auth API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "JWT Bearer token. Format: Bearer {token}",
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
    var db = scope.ServiceProvider.GetRequiredService<MyAdaDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    db.Database.Migrate();
    await IdentityUserSeeder.SeedAsync(userManager, roleManager);
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
