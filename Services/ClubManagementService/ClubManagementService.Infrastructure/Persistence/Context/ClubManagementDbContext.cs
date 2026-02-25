using ClubManagementService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClubManagementService.Infrastructure.Persistence.Context
{
    public class ClubManagementDbContext : DbContext
    {
        public ClubManagementDbContext(DbContextOptions<ClubManagementDbContext> options)
            : base(options)
        {
        }
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Club> Clubs => Set<Club>();
        public DbSet<Event> Events => Set<Event>();
        public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
        public DbSet<JobVacancy> JobVacancies => Set<JobVacancy>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<Subevent> Subevents => Set<Subevent>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Soft-delete filters
            modelBuilder.Entity<Category>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Club>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Event>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<EventRegistration>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<JobVacancy>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<JobApplication>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Subevent>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.HasDefaultSchema("club");
            base.OnModelCreating(modelBuilder);
        }
    }
}

