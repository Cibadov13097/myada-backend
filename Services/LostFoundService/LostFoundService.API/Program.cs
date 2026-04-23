using AutoMapper;
using LostFoundService.Api.Middlewares;
using LostFoundService.Api.Security;
using LostFoundService.Application.Interfaces;
using LostFoundService.Application.Interfaces.Repositories;
using LostFoundService.Application.Interfaces.Services;
using LostFoundService.Application.Mappings;
using LostFoundService.Application.Services;
using LostFoundService.Infrastructure.Options;
using LostFoundService.Infrastructure.Services;
using LostFoundService.Infrastructure.Persistence.Context;
using LostFoundService.Infrastructure.Persistence.Seed;
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
{
    jwksUrl = "http://authservice:8080/.well-known/jwks.json";
}

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
    c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    c.CustomOperationIds(apiDescription =>
    {
        var controller = apiDescription.ActionDescriptor.RouteValues["controller"] ?? "UnknownController";
        var action = apiDescription.ActionDescriptor.RouteValues["action"] ?? "UnknownAction";
        var method = apiDescription.HttpMethod ?? "UNKNOWN";
        var path = apiDescription.RelativePath
            ?.Replace("/", "_")
            .Replace("{", string.Empty)
            .Replace("}", string.Empty)
            .Replace(":", "_")
            ?? "root";

        return $"{controller}_{action}_{method}_{path}";
    });

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
builder.Services.Configure<S3StorageOptions>(builder.Configuration.GetSection(S3StorageOptions.SectionName));
builder.Services.AddSingleton<IImageStorageService, S3ImageStorageService>();

var locationBaseUrl = builder.Configuration["LocationBaseUrl:Url"];
if (string.IsNullOrWhiteSpace(locationBaseUrl))
{
    locationBaseUrl = "http://locationservice:8080/";
}

if (!Uri.TryCreate(locationBaseUrl, UriKind.Absolute, out var locationBaseUri))
{
    throw new InvalidOperationException("LocationBaseUrl:Url must be a valid absolute URL.");
}

builder.Services.AddHttpClient<ILocationServiceClient, LocationServiceClient>(client =>
{
    client.BaseAddress = locationBaseUri;
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
    await db.Database.MigrateAsync();
    await LostFoundDbSeeder.SeedAsync(db);
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

app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
