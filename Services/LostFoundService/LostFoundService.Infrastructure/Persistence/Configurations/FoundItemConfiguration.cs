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

            builder.Property(x => x.LocationFound)
                   .IsRequired()
                   .HasMaxLength(150);

            builder.Property(x => x.FoundByUserId)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.Status)
                   .IsRequired();

            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false);
        }
    }
}
