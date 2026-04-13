using Microsoft.EntityFrameworkCore;
using MyAdaSupportService.Domain.Entities.RequestAggregate;

namespace MyAdaSupportService.Infrastructure;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Request> Requests { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<SupportStaffStatus> SupportStaffStatuses { get; set; }

}
