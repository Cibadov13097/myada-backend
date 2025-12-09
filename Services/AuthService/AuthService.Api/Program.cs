using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Context;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Security.Cryptography;

var builder = WebApplication.CreateBuilder(args);

// 1?? Database
builder.Services.AddDbContext<MyAdaDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

// 2? Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MyAdaDbContext>()
.AddDefaultTokenProviders();

// 3? RSA PRIVATE KEY 
var privateKeyPem = File.ReadAllText("keys/private_key.pem");
var rsa = RSA.Create();
rsa.ImportFromPem(privateKeyPem);

builder.Services.AddSingleton<RSA>(rsa);
builder.Services.AddScoped<TokenService>();

// 4? Controllers
builder.Services.AddControllers();

// 5? Swagger + JWT Auth
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

// 6? Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
