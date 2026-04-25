using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Seeders;

public static class ClubRosterStudentSeeder
{
    private const string StudentRoleName = "student";
    private const string SeedPassword = "SeedUser123!";
    private const int ClubRosterSeedStart = 1000;

    private static readonly string[] ClubRosterFullNames =
    {
        "Rauf Məmmədov",
        "Nihat Əliyev",
        "Orxan Hüseynov",
        "Elvin Quliyev",
        "Kamran Abbasov",
        "Aysel Əliyeva",
        "Nigar Həsənova",
        "Fidan Quliyeva",
        "Ləman Məmmədova",
        "Günay Əliyeva",
        "Tural İbrahimov",
        "Murad Əliyev",
        "Samir Həsənov",
        "Vüqar Rzayev",
        "Elşən Məmmədov",
        "Emil Qasımov",
        "Tural Əliyev",
        "Rəşad Qurbanov",
        "Nicat Hüseynov",
        "Zaur Məmmədov",
        "Nərmin Həsənova",
        "Şəbnəm Quliyeva",
        "Səidə Əliyeva",
        "İlkin Məmmədli",
        "Kənan Əliyev",
        "Elnur Abbasov",
        "Emin Əliyev",
        "Vüsal Quliyev",
        "Cavid Hüseynov",
        "Anar Məmmədov",
        "Elvin Məmmədli",
        "Murad Rzayev",
        "Kamal Əliyev",
        "Fərid Quliyev",
        "Ramin Hüseynov",
        "Aytac Əliyeva",
        "Ləman Qasımova",
        "Günel Məmmədova",
        "Səbinə Əliyeva",
        "Nərgiz Hüseynova",
        "Rauf Əliyev",
        "Samir Qasımov",
        "Tural Hüseynov",
        "Vüqar Məmmədov",
        "Elşad Əliyev",
        "Nurlan Əliyev",
        "Elnur Quliyev",
        "Samir Hüseynov",
        "Orxan Məmmədov",
        "Kənan Rzayev",
        "Günay Quliyeva",
        "Aysu Məmmədova",
        "Nigar Əliyeva",
        "Ləman Hüseynova",
        "Fidan Əliyeva",
        "Rəşad Hüseynov",
        "Zaur Quliyev",
        "Nicat Məmmədov",
        "Emin Hüseynov",
        "Tural Rzayev",
        "Rəşad Əliyev",
        "Vüsal Məmmədov",
        "Anar Hüseynov",
        "Cavid Quliyev",
        "Elşən Əliyev",
        "Nərmin Əliyeva",
        "Şəbnəm Məmmədova",
        "Səbinə Hüseynova",
        "Günel Əliyeva",
        "Ləman Quliyeva",
        "İlkin Hüseynov",
        "Kamran Əliyev",
        "Murad Quliyev",
        "Samir Məmmədov",
        "Vüqar Əliyev"
    };

    private static readonly ClubRosterStudentSeedRecord[] ClubRosterStudentSeeds = BuildClubRosterStudentSeeds();

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        if (!await roleManager.RoleExistsAsync(StudentRoleName).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"Required role '{StudentRoleName}' does not exist before club roster seeding.");
        }

        foreach (var student in ClubRosterStudentSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await FindExistingUserAsync(userManager, student, cancellationToken).ConfigureAwait(false);
            var isNewUser = user is null;

            if (isNewUser)
            {
                user = new ApplicationUser
                {
                    Id = student.Id,
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                if (!string.Equals(user!.Id, student.Id, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Club roster seed '{student.Username}' matched an existing user with id '{user.Id}', " +
                        $"but the club tables expect '{student.Id}'.");
                }

                user.ModifiedAt = DateTime.UtcNow;
            }

            user!.FirstName = student.FirstName;
            user.LastName = student.LastName;
            user.OrganizationalId = student.OrganizationalId;
            user.UserName = student.Username;
            user.Email = student.Email;
            user.EmailConfirmed = true;
            user.PhoneNumber = null;
            user.ProfileImage = null;
            user.UserType = UserType.Student;
            user.Status = Status.Active;
            user.IsDeleted = false;
            user.DeletedAt = null;
            user.DeletedByUserId = null;

            if (isNewUser)
            {
                var createResult = await userManager.CreateAsync(user, SeedPassword).ConfigureAwait(false);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to create club roster user '{student.Username}': {errors}");
                }
            }
            else
            {
                var updateResult = await userManager.UpdateAsync(user).ConfigureAwait(false);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to update club roster user '{student.Username}': {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, StudentRoleName).ConfigureAwait(false))
            {
                var roleResult = await userManager.AddToRoleAsync(user, StudentRoleName).ConfigureAwait(false);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to assign role '{StudentRoleName}' to '{student.Username}': {errors}");
                }
            }
        }
    }

    private static async Task<ApplicationUser?> FindExistingUserAsync(
        UserManager<ApplicationUser> userManager,
        ClubRosterStudentSeedRecord student,
        CancellationToken cancellationToken)
    {
        var matches = await userManager.Users
            .Where(user =>
                user.Id == student.Id ||
                user.OrganizationalId == student.OrganizationalId ||
                user.Email == student.Email ||
                user.UserName == student.Username)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var distinctMatches = matches
            .GroupBy(user => user.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (distinctMatches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Club roster seed data for '{student.Username}' matched multiple existing users. " +
                "Please resolve the conflicting auth records before rerunning the seeder.");
        }

        return distinctMatches.SingleOrDefault();
    }

    private static ClubRosterStudentSeedRecord[] BuildClubRosterStudentSeeds()
    {
        return ClubRosterFullNames
            .Select((fullName, offset) =>
            {
                var seedIndex = ClubRosterSeedStart + offset;
                var (firstName, lastName) = SplitName(fullName);
                return new ClubRosterStudentSeedRecord(
                    BuildSeedUserGuid(seedIndex).ToString("D"),
                    firstName,
                    lastName,
                    BuildSeedOrganizationalId(offset),
                    $"club.seed{seedIndex}",
                    $"club.seed{seedIndex}@ada.edu.az");
            })
            .ToArray();
    }

    private static (string FirstName, string LastName) SplitName(string fullName)
    {
        var parts = fullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            throw new InvalidOperationException($"Club roster name '{fullName}' must include first and last name.");

        return (parts[0], parts[1]);
    }

    private static Guid BuildSeedUserGuid(int index)
    {
        return Guid.Parse($"00000000-0000-0000-0000-{index:000000000000}");
    }

    private static string BuildSeedOrganizationalId(int offset)
    {
        return (200000000 + offset).ToString();
    }

    private sealed record ClubRosterStudentSeedRecord(
        string Id,
        string FirstName,
        string LastName,
        string OrganizationalId,
        string Username,
        string Email);
}
