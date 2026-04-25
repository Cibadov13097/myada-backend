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
        var legacyTeacherRole = await roleManager.FindByNameAsync("teacher");
        if (legacyTeacherRole is not null)
        {
            var instructorRole = await roleManager.FindByNameAsync("instructor");
            if (instructorRole is null)
            {
                legacyTeacherRole.Name = "instructor";
                legacyTeacherRole.NormalizedName = "INSTRUCTOR";

                var renameResult = await roleManager.UpdateAsync(legacyTeacherRole);
                if (!renameResult.Succeeded)
                {
                    var errors = string.Join("; ", renameResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to rename 'teacher' role to 'instructor': {errors}");
                }
            }
            else
            {
                var usersInTeacherRole = await userManager.GetUsersInRoleAsync("teacher");
                foreach (var user in usersInTeacherRole)
                {
                    if (!await userManager.IsInRoleAsync(user, "instructor"))
                    {
                        var addResult = await userManager.AddToRoleAsync(user, "instructor");
                        if (!addResult.Succeeded)
                        {
                            var errors = string.Join("; ", addResult.Errors.Select(e => e.Description));
                            throw new InvalidOperationException($"Failed to migrate '{user.UserName}' to 'instructor' role: {errors}");
                        }
                    }

                    var removeResult = await userManager.RemoveFromRoleAsync(user, "teacher");
                    if (!removeResult.Succeeded)
                    {
                        var errors = string.Join("; ", removeResult.Errors.Select(e => e.Description));
                        throw new InvalidOperationException($"Failed to remove legacy 'teacher' role from '{user.UserName}': {errors}");
                    }
                }

                var deleteResult = await roleManager.DeleteAsync(legacyTeacherRole);
                if (!deleteResult.Succeeded)
                {
                    var errors = string.Join("; ", deleteResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to delete legacy 'teacher' role: {errors}");
                }
            }
        }

        var roleMap = new Dictionary<int, string>
        {
            [1] = "student",
            [2] = "club_rep",
            [3] = "student_services",
            [8] = "instructor",
            [12] = "dispatcher",
            [11] = "admin"
        };

        foreach (var roleEntry in roleMap)
        {
            var roleId = roleEntry.Key.ToString();
            var roleName = roleEntry.Value;

            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var createRoleResult = await roleManager.CreateAsync(new IdentityRole
                {
                    Id = roleId,
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant()
                });

                if (!createRoleResult.Succeeded)
                {
                    var errors = string.Join("; ", createRoleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
                }
            }
        }

        var seedFirstNames = new[]
        {
            "Aydan", "Narmin", "Lamiya", "Rashad", "Elvin", "Sabina", "Tural", "Gunay", "Orkhan", "Nigar"
        };

        var seedLastNames = new[]
        {
            "Mammadova", "Hasanov", "Aliyeva", "Bayramov", "Ismayilova"
        };

        for (var i = 1; i <= 50; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var userGuid = BuildSeedUserGuid(i);
            var userId = userGuid.ToString();
            var userName = $"seed.user{i:00}";
            var email = $"{userName}@ada.edu.az";

            var existing = await userManager.FindByIdAsync(userId);
            if (existing is not null)
            {
                continue;
            }

            var userType = i <= 30 ? UserType.Student : i <= 40 ? UserType.Staff : UserType.Faculty;

            var nameIndex = i - 1;
            var firstName = seedFirstNames[nameIndex % seedFirstNames.Length];
            var lastName = seedLastNames[nameIndex / seedFirstNames.Length];

            var user = new ApplicationUser
            {
                Id = userId,
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FirstName = firstName,
                LastName = lastName,
                OrganizationalId = BuildSeedOrganizationalId(i),
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

        await StudentUserSeeder.SeedAsync(userManager, roleManager, cancellationToken);
    }

    private static Guid BuildSeedUserGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }

    private static string BuildSeedOrganizationalId(int index)
    {
        return (100000000 + index).ToString();
    }
}
