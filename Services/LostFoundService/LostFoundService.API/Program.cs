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
// 1. GET PUBLIC KEY FROM AUTH SERVICE (JWKS)
// =====================================================

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
await JwksProvider.InitializeAsync(jwksUrl);

// ==========================
// 2. JWT AUTHENTICATION 
// ==========================

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "myada-auth",

            ValidateAudience = true,
            ValidAudience = "myada-api",

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,


            IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
            {
                return JwksProvider.GetKeys();
            }
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
