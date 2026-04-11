using LostFoundService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostFoundService.Infrastructure.Persistence.Configurations;

public class LostFoundItemImageConfiguration : IEntityTypeConfiguration<LostFoundItemImage>
{
    public void Configure(EntityTypeBuilder<LostFoundItemImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);

        builder.HasOne(x => x.LostFoundItem)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.LostFoundItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
