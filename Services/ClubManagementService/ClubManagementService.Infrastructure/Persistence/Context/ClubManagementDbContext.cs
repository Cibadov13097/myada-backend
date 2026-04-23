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
        public DbSet<EventProposal> EventProposals => Set<EventProposal>();
        public DbSet<EventRegistration> EventRegistrations => Set<EventRegistration>();
        public DbSet<JobVacancy> JobVacancies => Set<JobVacancy>();
        public DbSet<JobApplication> JobApplications => Set<JobApplication>();
        public DbSet<InterviewSlot> InterviewSlots => Set<InterviewSlot>();
        public DbSet<Subevent> Subevents => Set<Subevent>();
        public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();
        public DbSet<ClubEmployee> ClubEmployees => Set<ClubEmployee>();
        public DbSet<ClubPosition> ClubPositions => Set<ClubPosition>();
        public DbSet<ClubPositionRequirement> ClubPositionRequirements => Set<ClubPositionRequirement>();
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
            modelBuilder.Entity<InterviewSlot>()
                .HasQueryFilter(x => !x.IsDeleted);
            modelBuilder.Entity<Subevent>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<EventRegistration>()
                .HasOne(r => r.Event)
                .WithMany()
                .HasForeignKey(r => r.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(r => new { r.EventId, r.UserId })
                .IsUnique();

            modelBuilder.Entity<EventRegistration>()
                .HasIndex(r => r.TicketId)
                .IsUnique();

            modelBuilder.Entity<ClubMembership>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<ClubMembership>()
                .HasOne(m => m.Club)
                .WithMany()
                .HasForeignKey(m => m.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClubMembership>()
                .HasIndex(m => new { m.ClubId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<ClubEmployee>()
                .HasIndex(e => new { e.ClubId, e.UserId })
                .IsUnique();

            modelBuilder.Entity<EventProposal>()
                .HasIndex(p => p.Status);

            modelBuilder.Entity<ClubPosition>()
                .HasQueryFilter(x => !x.IsDeleted);

            modelBuilder.Entity<ClubPosition>()
                .HasOne(p => p.Club)
                .WithMany()
                .HasForeignKey(p => p.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClubPosition>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClubPositionRequirement>()
                .HasQueryFilter(r => r.ClubPosition != null && !r.ClubPosition.IsDeleted);

            modelBuilder.Entity<ClubPositionRequirement>()
                .HasOne(r => r.ClubPosition)
                .WithMany(p => p.Requirements)
                .HasForeignKey(r => r.ClubPositionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<JobVacancy>()
                .HasOne(v => v.ClubPosition)
                .WithMany()
                .HasForeignKey(v => v.ClubPositionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<InterviewSlot>()
                .HasAlternateKey(x => x.SlotId);

            modelBuilder.Entity<InterviewSlot>()
                .HasIndex(x => new { x.ClubId, x.StartsAt });

            modelBuilder.Entity<InterviewSlot>()
                .HasOne(s => s.Club)
                .WithMany()
                .HasForeignKey(s => s.ClubId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<InterviewSlot>()
                .HasOne(s => s.JobVacancy)
                .WithMany()
                .HasForeignKey(s => s.JobVacancyId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<JobApplication>()
                .HasOne(a => a.InterviewSlot)
                .WithMany()
                .HasForeignKey(a => a.InterviewSlotId)
                .HasPrincipalKey(s => s.SlotId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.HasDefaultSchema("club");
            base.OnModelCreating(modelBuilder);
        }
    }
}

