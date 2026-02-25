using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ClubManagementService.Infrastructure.Persistence.Context
{
    public class ClubManagementDbContextFactory : IDesignTimeDbContextFactory<ClubManagementDbContext>
    {
        public ClubManagementDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ClubManagementDbContext>();

            var environment =
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            // Reuse the API project's configuration so the connection string stays in one place
            var basePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "ClubManagementService.API");

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("Develop");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Connection string 'Develop' was not found for ClubManagementDbContext design-time creation.");
            }

            optionsBuilder.UseNpgsql(connectionString);

            return new ClubManagementDbContext(optionsBuilder.Options);
        }
    }
}

