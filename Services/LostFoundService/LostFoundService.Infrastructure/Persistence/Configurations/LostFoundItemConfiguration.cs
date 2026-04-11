using LostFoundService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostFoundService.Infrastructure.Persistence.Configurations;

public class LostFoundItemConfiguration : IEntityTypeConfiguration<LostFoundItem>
{
    public void Configure(EntityTypeBuilder<LostFoundItem> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(10);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(250);
        builder.Property(x => x.Location).HasMaxLength(400);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);
        builder.Property(x => x.AdminStatus)
            .HasConversion<string>()
            .HasMaxLength(50);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.ReferenceNumber).HasMaxLength(50);
        builder.Property(x => x.PrimaryImageUrl).HasMaxLength(500);
        builder.Property(x => x.PostedByUserId).HasMaxLength(100);

        builder.Property(x => x.LocationType).HasMaxLength(30);
        builder.Property(x => x.BuildingCode).HasMaxLength(50);
        builder.Property(x => x.IsRoom).HasMaxLength(10);
        builder.Property(x => x.RoomOrArea).HasMaxLength(200);
        builder.Property(x => x.CampusLocation).HasMaxLength(200);
        builder.Property(x => x.Building).HasMaxLength(100);
        builder.Property(x => x.Floor).HasMaxLength(50);
        builder.Property(x => x.RoomArea).HasMaxLength(200);
        builder.Property(x => x.LastKnownLocation).HasMaxLength(400);
        builder.Property(x => x.TimeLost).HasMaxLength(10);
        builder.Property(x => x.CollectionPlace).HasMaxLength(200);
        builder.Property(x => x.ContactName).HasMaxLength(150);
        builder.Property(x => x.ContactPhone).HasMaxLength(50);

        builder.Property(x => x.StorageBinId).HasMaxLength(100);
        builder.Property(x => x.Condition)
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(x => x.AdminNotes).HasMaxLength(1000);
        builder.Property(x => x.ReportedColor).HasMaxLength(100);
        builder.Property(x => x.IntakePhotoUrl).HasMaxLength(500);

        builder.Property(x => x.ClaimantName).HasMaxLength(150);
        builder.Property(x => x.ClaimantStudentId).HasMaxLength(100);
        builder.Property(x => x.HandoverProofUrl).HasMaxLength(500);

        builder.HasIndex(x => x.ReferenceNumber).IsUnique();
    }
}
