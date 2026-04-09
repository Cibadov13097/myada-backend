using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Infrastructure.Persistence.Seeders;

public static class IdentityUserSeeder
{
    private const string SeedPassword = "SeedUser123!";

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        var roleMap = new Dictionary<int, string>
        {
            [1] = "student",
            [2] = "club_rep",
            [3] = "student_services",
            [8] = "teacher",
            [11] = "admin"
        };

        foreach (var roleName in roleMap.Values.Distinct())
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        for (var i = 1; i <= 50; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userGuid = BuildSeedUserGuid(i);
            var userId = userGuid.ToString();
            var userName = $"seed.user{i:00}";
            var email = $"{userName}@myada.local";

            var existing = await userManager.FindByIdAsync(userId);
            if (existing is not null)
            {
                continue;
            }

            var userType = i <= 30 ? UserType.Student : i <= 40 ? UserType.Staff : UserType.Faculty;

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FirstName = $"SeedFirst{i}",
                LastName = $"SeedLast{i}",
                UserType = userType,
                Status = Status.Active
            };

            var createResult = await userManager.CreateAsync(user, SeedPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create seed user '{userName}': {errors}");
            }

            var roleName = i <= 30
                ? roleMap[1]
                : i <= 40
                    ? roleMap[3]
                    : i <= 45
                        ? roleMap[8]
                        : i <= 48
                            ? roleMap[2]
                            : roleMap[11];

            var roleResult = await userManager.AddToRoleAsync(user, roleName);
            if (!roleResult.Succeeded)
            {
                var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to assign role '{roleName}' to '{userName}': {errors}");
            }
        }
    }

    private static Guid BuildSeedUserGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }
}
