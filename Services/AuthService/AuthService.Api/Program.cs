using AuthService.Application.Options;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;

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


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

// 5. Controllers/
builder.Services.AddControllers();

// 6. Swagger + JWT Auth 
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

// 7. Swagger


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MyAdaDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();

app.Run();
