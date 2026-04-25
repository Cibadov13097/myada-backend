using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CallService.Api.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CallHistoryDbContext>
{
    public CallHistoryDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CallService")
            ?? "Host=localhost;Port=5432;Database=CallDb;Username=postgres;Password=123";

        var optionsBuilder = new DbContextOptionsBuilder<CallHistoryDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new CallHistoryDbContext(optionsBuilder.Options);
    }
}
