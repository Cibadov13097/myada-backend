using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence.Context;

/// <summary>
/// Used by EF Core tools (e.g. migrations) at design time. Avoids needing to run the full app host and JWT key setup.
/// </summary>
public class MyAdaDbContextFactory : IDesignTimeDbContextFactory<MyAdaDbContext>
{
    public MyAdaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyAdaDbContext>();

        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Develop")
            ?? "Host=localhost;Port=5432;Database=MyAdaDb;Username=postgres;Password=123";

        optionsBuilder.UseNpgsql(connectionString);

        return new MyAdaDbContext(optionsBuilder.Options);
    }
}
