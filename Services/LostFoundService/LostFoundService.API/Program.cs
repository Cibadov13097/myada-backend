using AutoMapper;
using LostFoundService.Api.Middlewares;
using LostFoundService.Api.Security;
using LostFoundService.Application.Interfaces;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Application.Mappings;
using LostFoundService.Application.Services;
using LostFoundService.Infrastructure.Services;
using LostFoundService.Infrastructure.Persistence.Context;
using LostFoundService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;
using Shared.Shared.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// 1. JWT: JWKS endpoint only (no local public key files)
// =====================================================

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
if (string.IsNullOrWhiteSpace(jwksUrl))
    throw new InvalidOperationException(
        "Auth:JwksUrl is required for JWT validation. Set to AuthService JWKS URL, e.g. https://AUTH_SERVICE_URL/.well-known/jwks.json");

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
    opt.AddProfile(new CategoryProfile());
}); 

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IFoundItemService, FoundItemService>();
builder.Services.AddScoped<IFoundItemRepository, FoundItemRepository>();

builder.Services.AddScoped<IFoundItemAdminService, FoundItemAdminService>();
builder.Services.AddScoped<ILostItemAdminService, LostItemAdminService>();

builder.Services.AddScoped<ILostItemService, LostItemService>();
builder.Services.AddScoped<ILostItemRepository,LostItemRepository>();

builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

builder.Services.AddSingleton<IRabbitMqPublisher, RabbitMqPublisher>();

builder.Services.AddHttpClient<ILocationServiceClient, LocationServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["LocationBaseUrl:Url"]);
});

builder.Services.AddControllers(options =>
{
    options.ModelBinderProviders.Insert(0, new LostFoundService.Api.ModelBinding.NullableIntModelBinderProvider());
});

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

app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
