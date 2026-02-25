using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AuthService.Infrastructure.Persistence.Seeders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Persistence.Context
{
    public class MyAdaDbContext:IdentityDbContext<ApplicationUser>
    {

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
