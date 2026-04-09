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
        }
    }
}
