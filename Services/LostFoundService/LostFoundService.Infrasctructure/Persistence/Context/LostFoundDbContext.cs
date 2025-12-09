
using LostFoundService.Domain.Entities;
using LostFoundService.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LostFoundService.Infrastructure.Persistence.Context
{
    public class LostFoundDbContext:DbContext
    {
       public DbSet<FoundItem> FoundItems { get; set; }
        public  DbSet<LostItem> LostItems { get; set; }
        public LostFoundDbContext(DbContextOptions<LostFoundDbContext> options):base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new LostItemConfiguration());
            modelBuilder.ApplyConfiguration(new FoundItemConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}
