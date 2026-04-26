using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;

namespace MyAda.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddMyAdaOpenTelemetry(this WebApplicationBuilder builder, string serviceName)
    {
        var name = string.IsNullOrWhiteSpace(serviceName)
            ? builder.Environment.ApplicationName
            : serviceName.Trim();

        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(name))
            .WithMetrics(m => m
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddPrometheusExporter());

        return builder;
    }

    public static WebApplication UseMyAdaPrometheusScraping(this WebApplication app)
    {
        app.UseOpenTelemetryPrometheusScrapingEndpoint();
        return app;
    }
}
