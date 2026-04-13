using AutoWrapper;
using MyAdaSupportService.Api.Hubs;
using MyAdaSupportService.Api.Realtime;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.Mapping;
using MyAdaSupportService.Infrastructure;

namespace MyAdaSupportService.Api;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddInfrastructure(builder.Configuration);

        builder.Services.AddControllers();
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddAutoMapper(typeof(MappingProfile).Assembly);
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<ISupportRealtimeNotifier, SupportRealtimeNotifier>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            AppDbContextSeed.SeedAsync(dbContext).GetAwaiter().GetResult();
        }

        app.UseSwagger();
        app.UseSwaggerUI();
        app.MapGet("/", () => Results.Redirect("/swagger"));

        app.UseApiResponseAndExceptionWrapper(new AutoWrapperOptions
        {
            ShowApiVersion = false,
            ShowStatusCode = true,
            IsApiOnly = true,
            UseCustomSchema = false,
            EnableExceptionLogging = true
        });
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseCors();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<SupportUpdatesHub>("/hubs/support-updates");
        app.Run();
    }
}
