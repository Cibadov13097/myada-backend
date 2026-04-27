using MyAda.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.AddMyAdaSerilog("api-gateway");

var corsSection = builder.Configuration.GetSection("Cors");
var allowedOrigins = corsSection.GetSection("AllowedOrigins").Get<string[]>() ?? [];
var allowCredentials = corsSection.GetValue("AllowCredentials", false);

builder.Services.AddCors(options =>
{
    options.AddPolicy("SpaCors", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
            if (allowCredentials)
                policy.AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.WithOrigins("http://localhost:3000");
        }
        else
        {
            throw new InvalidOperationException(
                "Configure Cors:AllowedOrigins (e.g. [\"https://your-spa.example\"]) for non-Development environments.");
        }

        policy
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowAnyHeader()
            .SetPreflightMaxAge(TimeSpan.FromHours(24));
    });
});

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.AddMyAdaOpenTelemetry("api-gateway");

var app = builder.Build();

// CORS first: OPTIONS preflight must get 204 + CORS headers before auth/routing/YARP.
// (JWT [Authorize] would return 401 for OPTIONS without a token if CORS did not short-circuit.)
app.UseCors("SpaCors");
app.UseMyAdaPrometheusScraping();

// Root and health – must be registered before MapReverseProxy so they are hit first
app.MapGet("/", () => Results.Text("ApiGateway is running.", "text/plain")).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapReverseProxy();

app.Run();

public partial class Program;
