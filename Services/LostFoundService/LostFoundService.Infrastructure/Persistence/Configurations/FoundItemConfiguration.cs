using LostFoundService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LostFoundService.Infrastructure.Persistence.Configurations
{
    public class FoundItemConfiguration : IEntityTypeConfiguration<FoundItem>
    {
        public void Configure(EntityTypeBuilder<FoundItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ImageUrl)
                   .IsRequired(false)
                   .HasMaxLength(500);

            builder.Property(x => x.Description)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(x => x.BuildingId).IsRequired(false);
            builder.Property(x => x.RoomId).IsRequired(false);

            builder.Property(x => x.FoundByUserId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.Property(x => x.CategoryId).IsRequired();
            builder.HasOne(x => x.Category)
                   .WithMany()
                   .HasForeignKey(x => x.CategoryId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false);
        }
    }
}
