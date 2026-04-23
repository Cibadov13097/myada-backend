using AuthService.Domain.Entities;
using AuthService.Infrastructure.Identity;
using AuthService.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Persistence.Context
{
    public class MyAdaDbContext:IdentityDbContext<ApplicationUser>
    {

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public MyAdaDbContext(DbContextOptions<MyAdaDbContext> options):base(options) 
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {

            base.OnModelCreating(builder);

            builder.HasDefaultSchema("auth");
            IdentityRoleSeeder.Seed(builder);

            builder.Entity<RefreshToken>()
                .Property(x => x.UserId)
                .HasMaxLength(450);

            var applicationUserBuilder = builder.Entity<ApplicationUser>();
            applicationUserBuilder.Property(x => x.OrganizationalId)
                .HasMaxLength(9)
                .IsRequired();

            applicationUserBuilder.HasIndex(x => x.OrganizationalId)
                .IsUnique()
                .HasDatabaseName("IX_AspNetUsers_OrganizationalId");

            applicationUserBuilder.ToTable(table =>
            {
                table.HasCheckConstraint(
                    "CK_AspNetUsers_OrganizationalId_9Digits",
                    "\"OrganizationalId\" ~ '^[0-9]{9}$'");
            });
        }
    }
}
