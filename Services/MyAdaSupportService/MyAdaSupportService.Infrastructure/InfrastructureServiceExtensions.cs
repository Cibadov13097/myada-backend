using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Application.ServicesInterfaces;
using MyAdaSupportService.Application.ServicesInterfaces.CrudServiceInterface;
using MyAdaSupportService.Infrastructure.Repositories;
using MyAdaSupportService.Infrastructure.Services;

namespace MyAdaSupportService.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));

        services.AddScoped<IAttachmentRepository, AttachmentRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<ISupportStaffStatusRepository, SupportStaffStatusRepository>();

        services.AddScoped<IAttachmentCrudService, AttachmentService>();
        services.AddScoped<IAttachmentService, AttachmentService>();

        services.AddScoped<ICategoryCrudService, CategoryService>();
        services.AddScoped<ICategoryService, CategoryService>();

        services.AddScoped<ISupportStaffStatusCrudService, SupportStaffStatusService>();
        services.AddScoped<ISupportStaffStatusService, SupportStaffStatusService>();

        services.AddScoped<IRequestCrudService, SupportRequestService>();
        services.AddScoped<ISupportRequestService, SupportRequestService>();
        services.AddScoped<IRequestStatusService, SupportRequestService>();
        services.AddScoped<IAssignmentService, SupportRequestService>();

        services.AddScoped<ILocationService, LocationService>();
        return services;
    }
}
