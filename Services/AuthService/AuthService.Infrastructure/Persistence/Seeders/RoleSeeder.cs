using AuthService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AuthService.Infrastructure.Persistence.Seeders
{
    using Microsoft.AspNetCore.Identity;
    using Microsoft.EntityFrameworkCore;

    public static class IdentityRoleSeeder
    {
        public static void Seed(ModelBuilder builder)
        {
            builder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = "1", Name = "student", NormalizedName = "STUDENT" },
                new IdentityRole { Id = "2", Name = "club_rep", NormalizedName = "CLUB_REP" },
                new IdentityRole { Id = "3", Name = "student_services", NormalizedName = "STUDENT_SERVICES" },
                new IdentityRole { Id = "4", Name = "it_admin", NormalizedName = "IT_ADMIN" },
                new IdentityRole { Id = "5", Name = "it_staff", NormalizedName = "IT_STAFF" },
                new IdentityRole { Id = "6", Name = "tech_admin", NormalizedName = "TECH_ADMIN" },
                new IdentityRole { Id = "7", Name = "tech_staff", NormalizedName = "TECH_STAFF" },
                new IdentityRole { Id = "8", Name = "teacher", NormalizedName = "TEACHER" },
                new IdentityRole { Id = "9", Name = "course_reg_staff", NormalizedName = "COURSE_REG_STAFF" },
                new IdentityRole { Id = "10", Name = "lost_found_leader", NormalizedName = "LOST_FOUND_LEADER" },
                new IdentityRole { Id = "11", Name = "admin", NormalizedName = "ADMIN" }
            );
        }
    }

}

