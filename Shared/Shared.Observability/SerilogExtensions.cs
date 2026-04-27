using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Events;

namespace MyAda.Observability;

public static class SerilogExtensions
{
    private const string SlnName = "MyAda.sln";

    /// <summary>
    /// Writes the same app logs to console and to a rolling file so Promtail can pick them up:
    /// <list type="bullet">
    /// <item><b>Local (no Docker)</b> — <c>{repo}/logs/{serviceName}/</c> when a parent folder contains <see cref="SlnName" /> (else under content root <c>logs</c>).</item>
    /// <item><b>Docker (AWS, etc.)</b> — set <c>MYADA_LOG_DIR=/app/logs</c> and a shared volume so Promtail tails <c>/var/log/myada/…</c>.</item>
    /// </list>
    /// Call as early as possible after <see cref="WebApplication.CreateBuilder" />.
    /// </summary>
    public static WebApplicationBuilder AddMyAdaSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        var name = SanitizeServiceName(serviceName);
        var logDir = ResolveServiceLogDirectory(builder, name);
        var logDirFromEnv = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(LogDirectoryEnvVar));

        Directory.CreateDirectory(logDir);

        builder.Host.UseSerilog((_, config) =>
        {
            const string template = "[{Timestamp:O}] [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}";

            config
                .Enrich.FromLogContext()
                .Enrich.WithProperty("service", name)
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .WriteTo.File(
                    path: Path.Combine(logDir, "app-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: template);

            if (!logDirFromEnv) config.WriteTo.Console(outputTemplate: template);
        }, preserveStaticLogger: false);

        return builder;
    }

    private static string SanitizeServiceName(string serviceName)
    {
        var t = (serviceName ?? string.Empty).Trim();
        if (t.Length == 0) t = "app";
        return Regex.Replace(t, @"[^A-Za-z0-9\-\.]", "-");
    }

    /// <summary>When this env var is set (e.g. in docker-compose for apps), we write logs to the file only. Promtail reads that volume as <c>myada_file_logs</c> and does not double-ingest the same lines from the Docker <c>stdout</c> job.</summary>
    public const string LogDirectoryEnvVar = "MYADA_LOG_DIR";

    private static string ResolveServiceLogDirectory(WebApplicationBuilder builder, string serviceName)
    {
        var fromEnv = Environment.GetEnvironmentVariable(LogDirectoryEnvVar);
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return Path.GetFullPath(Path.Combine(fromEnv.Trim(), serviceName));

        var fromSln = FindParentDirectoryWithFile(builder.Environment.ContentRootPath, SlnName);
        if (fromSln is not null)
            return Path.GetFullPath(Path.Combine(fromSln, "logs", serviceName));

        return Path.GetFullPath(
            Path.Combine(builder.Environment.ContentRootPath, "logs", serviceName));
    }

    private static string? FindParentDirectoryWithFile(string start, string fileName)
    {
        for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, fileName)))
                return dir.FullName;
        }
        return null;
    }
}
