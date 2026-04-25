using Microsoft.EntityFrameworkCore;

namespace CallService.Api.Persistence;

public sealed class CallHistoryDbContext(DbContextOptions<CallHistoryDbContext> options) : DbContext(options)
{
    public DbSet<CallHistoryRecord> CallHistory => Set<CallHistoryRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("call");

        var entity = modelBuilder.Entity<CallHistoryRecord>();
        entity.ToTable("CallHistory");
        entity.HasKey(x => x.Id);

        entity.Property(x => x.CallId).HasMaxLength(64).IsRequired();
        entity.Property(x => x.RoomId).HasMaxLength(96).IsRequired();
        entity.Property(x => x.CallerUserId).HasMaxLength(128).IsRequired();
        entity.Property(x => x.CallerDisplayName).HasMaxLength(256).IsRequired();
        entity.Property(x => x.DispatcherUserId).HasMaxLength(128).IsRequired();
        entity.Property(x => x.DispatcherDisplayName).HasMaxLength(256).IsRequired();
        entity.Property(x => x.CallerConnectionId).HasMaxLength(128).IsRequired();
        entity.Property(x => x.DispatcherConnectionId).HasMaxLength(128);
        entity.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        entity.Property(x => x.ResolvedByUserId).HasMaxLength(128);
        entity.Property(x => x.ResolvedByConnectionId).HasMaxLength(128);
        entity.Property(x => x.ResolveReason).HasMaxLength(512);
        entity.Property(x => x.EndedByUserId).HasMaxLength(128);
        entity.Property(x => x.EndedByConnectionId).HasMaxLength(128);
        entity.Property(x => x.EndReason).HasMaxLength(512);

        entity.HasIndex(x => x.CallId).IsUnique();
        entity.HasIndex(x => x.RoomId).IsUnique();
        entity.HasIndex(x => x.CallerUserId);
        entity.HasIndex(x => x.DispatcherUserId);
        entity.HasIndex(x => x.Status);
        entity.HasIndex(x => x.RequestedAtUtc);
    }
}
