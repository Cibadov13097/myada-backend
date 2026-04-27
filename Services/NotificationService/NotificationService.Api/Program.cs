using MyAda.Observability;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Infrastructure.Email;
using NotificationService.Infrastructure.Messaging;
using NotificationService.Infrastructure.Persistence;
using NotificationService.Infrastructure.Persistence.Seed;
using NotificationAppService = NotificationService.Application.Services.NotificationService;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.AddMyAdaSerilog("notification-service");

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddScoped<INotificationService, NotificationAppService>();
builder.Services.AddHostedService<RabbitMqConsumer>();
builder.Services.AddDbContext<NotificationDbContext>(options=>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("Develop"));
});

builder.AddMyAdaOpenTelemetry("notification-service");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    await db.Database.MigrateAsync();
    await NotificationDbSeeder.SeedAsync(db);
}
// Configure the HTTP request pipeline.
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
app.UseMyAdaPrometheusScraping();
app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
