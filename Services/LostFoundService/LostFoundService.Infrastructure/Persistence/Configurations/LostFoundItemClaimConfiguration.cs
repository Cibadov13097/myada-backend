using LostFoundService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostFoundService.Infrastructure.Persistence.Configurations;

public class LostFoundItemClaimConfiguration : IEntityTypeConfiguration<LostFoundItemClaim>
{
    public void Configure(EntityTypeBuilder<LostFoundItemClaim> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ClaimType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);
        builder.Property(x => x.Message).HasMaxLength(500);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(x => x.LostFoundItem)
            .WithMany(x => x.Claims)
            .HasForeignKey(x => x.LostFoundItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.LostFoundItemId, x.UserId, x.ClaimType }).IsUnique();
    }
}
