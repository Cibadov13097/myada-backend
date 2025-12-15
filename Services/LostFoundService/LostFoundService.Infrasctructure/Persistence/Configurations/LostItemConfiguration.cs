using LostFoundService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Persistence.Configurations
{
    public class LostItemConfiguration:IEntityTypeConfiguration<LostItem>
    {
        public void Configure(EntityTypeBuilder<LostItem> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                   .IsRequired()
                   .HasMaxLength(200);

            builder.Property(x => x.Description)
                   .IsRequired()
                   .HasMaxLength(1000);

            builder.Property(x => x.ImageUrl)
                   .IsRequired(false)
                   .HasMaxLength(500);

            builder.Property(x => x.PostedByUserId)
                   .IsRequired()
                   .HasMaxLength(100);

            // AuditableEntity fields
            builder.Property(x => x.CreatedAt)
                   .IsRequired();

            builder.Property(x => x.IsDeleted)
                   .HasDefaultValue(false);
        }
    }
}
