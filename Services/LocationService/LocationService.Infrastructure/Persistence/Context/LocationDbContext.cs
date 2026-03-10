using LocationService.Domain.Entities;
using LocationService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace LocationService.Infrastructure.Persistence.Context
{
    public class LocationDbContext : DbContext
    {
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Room> Rooms { get; set; }

        public LocationDbContext(DbContextOptions<LocationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new BuildingConfiguration());
            modelBuilder.ApplyConfiguration(new RoomConfiguration());

            modelBuilder.Entity<Building>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Room>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.HasDefaultSchema("location");
            base.OnModelCreating(modelBuilder);
        }
    }
}
