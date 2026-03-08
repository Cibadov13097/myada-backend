using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LostFoundService.Infrastructure.Persistence.Context
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LostFoundDbContext>
    {
        public LostFoundDbContext CreateDbContext(string[] args)
        {
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Develop")
                ?? "Host=localhost;Port=5432;Database=MyAdaDb;Username=postgres;Password=123";
            var optionsBuilder = new DbContextOptionsBuilder<LostFoundDbContext>();
            optionsBuilder.UseNpgsql(connectionString);
            return new LostFoundDbContext(optionsBuilder.Options);
        }
    }
}
