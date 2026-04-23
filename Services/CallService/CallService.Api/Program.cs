using System.Security.Claims;
using System.Text.Json;
using CallService.Api.Hubs;
using CallService.Api.Realtime;
using CallService.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var jwksUrl = builder.Configuration["Auth:JwksUrl"];
if (string.IsNullOrWhiteSpace(jwksUrl))
{
    throw new InvalidOperationException(
        "Auth:JwksUrl is required for JWT validation. Set it to AuthService JWKS URL, e.g. http://localhost:5001/.well-known/jwks.json");
}

await JwksProvider.InitializeAsync(jwksUrl);

var validIssuer = builder.Configuration["Auth:Issuer"] ?? "myada-auth";
var validAudience = builder.Configuration["Auth:Audience"] ?? "myada-api";
var redisConnectionString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379";
var redisChannelPrefix = builder.Configuration["Redis:ChannelPrefix"] ?? "myada:call";

builder.Services.Configure<CallRuntimeOptions>(builder.Configuration.GetSection("Call"));
builder.Services.Configure<TurnOptions>(builder.Configuration.GetSection("Turn"));
builder.Services.AddHostedService<JwksRefreshHostedService>();

builder.Services.AddSingleton<IUserIdProvider, SubClaimUserIdProvider>();
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<ICallStateStore, RedisCallStateStore>();
builder.Services.AddSingleton<IIceConfigurationProvider, CoturnIceConfigurationProvider>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
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
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role,
            IssuerSigningKeyResolver = (_, _, _, _) => JwksProvider.GetKeys()
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrWhiteSpace(accessToken) &&
                    path.StartsWithSegments("/hub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                if (context.Principal?.Identity is not ClaimsIdentity identity)
                    return Task.CompletedTask;

                var normalizedRoles = identity.Claims
                    .Where(static claim =>
                        claim.Type == ClaimTypes.Role ||
                        string.Equals(claim.Type, "role", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(claim.Type, "roles", StringComparison.OrdinalIgnoreCase))
                    .SelectMany(static claim => ExpandRoleValues(claim.Value))
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                foreach (var role in normalizedRoles)
                {
                    var exists = identity.Claims.Any(claim =>
                        claim.Type == ClaimTypes.Role &&
                        string.Equals(claim.Value, role, StringComparison.OrdinalIgnoreCase));

                    if (!exists)
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddSignalR()
    .AddStackExchangeRedis(redisConnectionString, options =>
    {
        options.Configuration ??= ConfigurationOptions.Parse(redisConnectionString);
        options.Configuration.ChannelPrefix = RedisChannel.Literal(redisChannelPrefix);
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Call Service API",
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
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
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

var app = builder.Build();

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

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new
{
    service = "CallService",
    status = "running"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

app.MapGet("/webrtc/ice-servers", GetIceServers).RequireAuthorization();
app.MapGet("/call/webrtc/ice-servers", GetIceServers).RequireAuthorization();

app.MapHub<CallHub>("/hub").RequireAuthorization();

app.Run();

static IResult GetIceServers(ClaimsPrincipal user, IIceConfigurationProvider iceConfigurationProvider)
{
    var userId = user.FindFirst("sub")?.Value
                 ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrWhiteSpace(userId))
        return Results.Unauthorized();

    var iceConfiguration = iceConfigurationProvider.GetForUser(userId);
    return Results.Ok(iceConfiguration);
}

static IEnumerable<string> ExpandRoleValues(string rawRoleValue)
{
    if (string.IsNullOrWhiteSpace(rawRoleValue))
        return Array.Empty<string>();

    var trimmed = rawRoleValue.Trim();

    if (trimmed.StartsWith("[", StringComparison.Ordinal) &&
        trimmed.EndsWith("]", StringComparison.Ordinal))
    {
        try
        {
            using var document = JsonDocument.Parse(trimmed);
            if (document.RootElement.ValueKind == JsonValueKind.Array)
            {
                return document.RootElement
                    .EnumerateArray()
                    .Where(static element => element.ValueKind == JsonValueKind.String)
                    .Select(static element => element.GetString())
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Cast<string>()
                    .ToArray();
            }
        }
        catch (JsonException)
        {
            // Fallback to plain string parsing when claim is not valid JSON.
        }
    }

    if (trimmed.Contains(',', StringComparison.Ordinal))
    {
        return trimmed
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    return new[] { trimmed };
}

