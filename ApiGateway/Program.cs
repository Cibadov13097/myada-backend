var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Root and health – must be registered before MapReverseProxy so they are hit first
app.MapGet("/", () => Results.Text("ApiGateway is running.", "text/plain")).AllowAnonymous();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();

app.MapReverseProxy();

app.Run();