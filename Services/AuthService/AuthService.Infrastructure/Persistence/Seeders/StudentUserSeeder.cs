using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Seeders;

public static class StudentUserSeeder
{
    private const string StudentRoleName = "student";
    private const string SeedPassword = "SeedUser123!";

    private static readonly StudentSeedRecord[] StudentSeeds =
    {
        new("Ali", "Mammadov", "000012345", "amammadov12345", "amammadov12345@ada.edu.az"),
        new("Vusal", "Aliyev", "000067891", "valiyev67891", "valiyev67891@ada.edu.az"),
        new("Elvin", "Huseynov", "000023456", "ehuseynov23456", "ehuseynov23456@ada.edu.az"),
        new("Rauf", "Guliyev", "000078912", "rguliyev78912", "rguliyev78912@ada.edu.az"),
        new("Orkhan", "Ismayilov", "000034567", "oismayilov34567", "oismayilov34567@ada.edu.az"),
        new("Murad", "Abdullayev", "000089123", "mabdullayev89123", "mabdullayev89123@ada.edu.az"),
        new("Tural", "Najafov", "000045678", "tnajafov45678", "tnajafov45678@ada.edu.az"),
        new("Nihat", "Rzayev", "000091234", "nrzayev91234", "nrzayev91234@ada.edu.az"),
        new("Kamran", "Mammadli", "000056789", "kmammadli56789", "kmammadli56789@ada.edu.az"),
        new("Javid", "Safarov", "000012398", "jsafarov12398", "jsafarov12398@ada.edu.az"),
        new("Farid", "Huseynli", "000067812", "fhuseynli67812", "fhuseynli67812@ada.edu.az"),
        new("Samir", "Aliyev", "000023498", "saliyev23498", "saliyev23498@ada.edu.az"),
        new("Elnur", "Mammadov", "000078345", "emammadov78345", "emammadov78345@ada.edu.az"),
        new("Agil", "Gurbanov", "000034129", "agurbanov34129", "agurbanov34129@ada.edu.az"),
        new("Rashad", "Abbasov", "000089567", "rabbasov89567", "rabbasov89567@ada.edu.az"),
        new("Emil", "Rzayev", "000045321", "erzayev45321", "erzayev45321@ada.edu.az"),
        new("Fuad", "Huseynov", "000091876", "fhuseynov91876", "fhuseynov91876@ada.edu.az"),
        new("Ilkin", "Mammadli", "000056432", "imammadli56432", "imammadli56432@ada.edu.az"),
        new("Zaur", "Aliyev", "000012987", "zaliyev12987", "zaliyev12987@ada.edu.az"),
        new("Nijat", "Guliyev", "000067543", "nguliyev67543", "nguliyev67543@ada.edu.az"),
        new("Ramin", "Huseynov", "000023765", "rhuseynov23765", "rhuseynov23765@ada.edu.az"),
        new("Teymur", "Mammadov", "000078654", "tmammadov78654", "tmammadov78654@ada.edu.az"),
        new("Shahin", "Aliyev", "000034876", "saliyev34876", "saliyev34876@ada.edu.az"),
        new("Elshan", "Gasimov", "000089345", "egasimov89345", "egasimov89345@ada.edu.az"),
        new("Vugar", "Rzayev", "000045987", "vrzayev45987", "vrzayev45987@ada.edu.az"),
        new("Faiq", "Huseynli", "000091654", "fhuseynli91654", "fhuseynli91654@ada.edu.az"),
        new("Sadiq", "Aliyev", "000056123", "saliyev56123", "saliyev56123@ada.edu.az"),
        new("Ziyad", "Mammadov", "000012564", "zmammadov12564", "zmammadov12564@ada.edu.az"),
        new("Ismayil", "Guliyev", "000067234", "iguliyev67234", "iguliyev67234@ada.edu.az"),
        new("Arif", "Huseynov", "000023999", "ahuseynov23999", "ahuseynov23999@ada.edu.az"),
        new("Ali", "Karimli", "000078111", "akarimli78111", "akarimli78111@ada.edu.az"),
        new("Rashid", "Mammadov", "000034222", "rmammadov34222", "rmammadov34222@ada.edu.az"),
        new("Parviz", "Aliyev", "000089333", "paliyev89333", "paliyev89333@ada.edu.az"),
        new("Nurlan", "Gasimov", "000045444", "ngasimov45444", "ngasimov45444@ada.edu.az"),
        new("Oruc", "Huseynli", "000091555", "ohuseynli91555", "ohuseynli91555@ada.edu.az"),
        new("Ahmad", "Mammadli", "000056666", "amammadli56666", "amammadli56666@ada.edu.az"),
        new("Farman", "Aliyev", "000012777", "faliyev12777", "faliyev12777@ada.edu.az"),
        new("Rauf", "Gurbanov", "000067888", "rgurbanov67888", "rgurbanov67888@ada.edu.az"),
        new("Ilgar", "Huseynov", "000023111", "ihuseynov23111", "ihuseynov23111@ada.edu.az"),
        new("Eldar", "Mammadov", "000078222", "emammadov78222", "emammadov78222@ada.edu.az"),
        new("Nihad", "Aliyev", "000034333", "naliyev34333", "naliyev34333@ada.edu.az"),
        new("Vidadi", "Gasimov", "000089444", "vgasimov89444", "vgasimov89444@ada.edu.az"),
        new("Aqshin", "Huseynli", "000045555", "ahuseynli45555", "ahuseynli45555@ada.edu.az"),
        new("Samit", "Mammadli", "000091666", "smammadli91666", "smammadli91666@ada.edu.az"),
        new("Rovshan", "Aliyev", "000056777", "raliyev56777", "raliyev56777@ada.edu.az"),
        new("Namiq", "Guliyev", "000012888", "nguliyev12888", "nguliyev12888@ada.edu.az"),
        new("Aliyar", "Huseynov", "000067999", "ahuseynov67999", "ahuseynov67999@ada.edu.az"),
        new("Tarlan", "Mammadov", "000023222", "tmammadov23222", "tmammadov23222@ada.edu.az"),
        new("Vugar", "Aliyev", "000078333", "valiyev78333", "valiyev78333@ada.edu.az"),
        new("Elvin", "Gasimov", "000034444", "egasimov34444", "egasimov34444@ada.edu.az"),
        new("Rashad", "Huseynli", "000089555", "rhuseynli89555", "rhuseynli89555@ada.edu.az"),
        new("Zaur", "Mammadli", "000045666", "zmammadli45666", "zmammadli45666@ada.edu.az"),
        new("Nihat", "Aliyev", "000091777", "naliyev91777", "naliyev91777@ada.edu.az"),
        new("Ilkin", "Guliyev", "000056888", "iguliyev56888", "iguliyev56888@ada.edu.az"),
        new("Kamran", "Huseynov", "000012999", "khuseynov12999", "khuseynov12999@ada.edu.az"),
        new("Murad", "Mammadov", "000067101", "mmammadov67101", "mmammadov67101@ada.edu.az"),
        new("Orkhan", "Aliyev", "000023202", "oaliyev23202", "oaliyev23202@ada.edu.az"),
        new("Ramin", "Gasimov", "000078303", "rgasimov78303", "rgasimov78303@ada.edu.az"),
        new("Emil", "Huseynli", "000034404", "ehuseynli34404", "ehuseynli34404@ada.edu.az"),
        new("Tural", "Mammadli", "000089505", "tmammadli89505", "tmammadli89505@ada.edu.az"),
        new("Fuad", "Aliyev", "000045606", "faliyev45606", "faliyev45606@ada.edu.az"),
        new("Javid", "Guliyev", "000091707", "jguliyev91707", "jguliyev91707@ada.edu.az"),
        new("Elnur", "Huseynov", "000056808", "ehuseynov56808", "ehuseynov56808@ada.edu.az"),
        new("Samir", "Mammadov", "000012909", "smammadov12909", "smammadov12909@ada.edu.az"),
        new("Farid", "Aliyev", "000067010", "faliyev67010", "faliyev67010@ada.edu.az"),
        new("Agil", "Gasimov", "000023121", "agasimov23121", "agasimov23121@ada.edu.az"),
        new("Rashid", "Huseynli", "000078232", "rhuseynli78232", "rhuseynli78232@ada.edu.az"),
        new("Parviz", "Mammadli", "000034343", "pmammadli34343", "pmammadli34343@ada.edu.az"),
        new("Nurlan", "Aliyev", "000089454", "naliyev89454", "naliyev89454@ada.edu.az"),
        new("Oruc", "Guliyev", "000045565", "oguliyev45565", "oguliyev45565@ada.edu.az"),
        new("Ahmad", "Huseynov", "000091676", "ahuseynov91676", "ahuseynov91676@ada.edu.az"),
        new("Farman", "Mammadov", "000056787", "fmammadov56787", "fmammadov56787@ada.edu.az"),
        new("Rauf", "Aliyev", "000012898", "raliyev12898", "raliyev12898@ada.edu.az"),
        new("Ilgar", "Gasimov", "000067909", "igasimov67909", "igasimov67909@ada.edu.az"),
        new("Eldar", "Huseynli", "000023010", "ehuseynli23010", "ehuseynli23010@ada.edu.az"),
        new("Nihad", "Mammadli", "000078121", "nmammadli78121", "nmammadli78121@ada.edu.az"),
        new("Vidadi", "Aliyev", "000034232", "valiyev34232", "valiyev34232@ada.edu.az"),
        new("Aqshin", "Guliyev", "000089343", "aguliyev89343", "aguliyev89343@ada.edu.az"),
        new("Samit", "Huseynov", "000045454", "shuseynov45454", "shuseynov45454@ada.edu.az"),
        new("Rovshan", "Mammadov", "000091565", "rmammadov91565", "rmammadov91565@ada.edu.az"),
        new("Namiq", "Aliyev", "000056676", "naliyev56676", "naliyev56676@ada.edu.az"),
        new("Aliyar", "Gasimov", "000012787", "agasimov12787", "agasimov12787@ada.edu.az"),
        new("Tarlan", "Huseynli", "000067898", "thuseynli67898", "thuseynli67898@ada.edu.az"),
        new("Vugar", "Mammadli", "000023909", "vmammadli23909", "vmammadli23909@ada.edu.az"),
        new("Elvin", "Aliyev", "000078010", "ealiyev78010", "ealiyev78010@ada.edu.az"),
        new("Rashad", "Guliyev", "000034121", "rguliyev34121", "rguliyev34121@ada.edu.az"),
        new("Zaur", "Huseynov", "000089232", "zhuseynov89232", "zhuseynov89232@ada.edu.az"),
        new("Nihat", "Mammadov", "000045343", "nmammadov45343", "nmammadov45343@ada.edu.az"),
        new("Ilkin", "Aliyev", "000091454", "ialiyev91454", "ialiyev91454@ada.edu.az"),
        new("Kamran", "Gasimov", "000056565", "kgasimov56565", "kgasimov56565@ada.edu.az"),
        new("Murad", "Huseynli", "000012676", "mhuseynli12676", "mhuseynli12676@ada.edu.az"),
        new("Orkhan", "Mammadli", "000067787", "omammadli67787", "omammadli67787@ada.edu.az"),
        new("Ramin", "Aliyev", "000023898", "raliyev23898", "raliyev23898@ada.edu.az"),
        new("Emil", "Guliyev", "000078909", "eguliyev78909", "eguliyev78909@ada.edu.az"),
        new("Tural", "Huseynov", "000034010", "thuseynov34010", "thuseynov34010@ada.edu.az"),
        new("Fuad", "Mammadov", "000089121", "fmammadov89121", "fmammadov89121@ada.edu.az"),
        new("Javid", "Aliyev", "000045232", "jaliyev45232", "jaliyev45232@ada.edu.az"),
        new("Elnur", "Gasimov", "000091343", "egasimov91343", "egasimov91343@ada.edu.az"),
        new("Samir", "Huseynli", "000056454", "shuseynli56454", "shuseynli56454@ada.edu.az"),
        new("Farid", "Mammadli", "000012565", "fmammadli12565", "fmammadli12565@ada.edu.az")
    };

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        if (!await roleManager.RoleExistsAsync(StudentRoleName))
        {
            throw new InvalidOperationException(
                $"Required role '{StudentRoleName}' does not exist before student seeding.");
        }

        foreach (var student in StudentSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await FindExistingUserAsync(userManager, student, cancellationToken);
            var isNewUser = user is null;

            if (isNewUser)
            {
                user = new ApplicationUser
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
            }
            else
            {
                user!.ModifiedAt = DateTime.UtcNow;
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
                var createResult = await userManager.CreateAsync(user, SeedPassword);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to create student '{student.Username}': {errors}");
                }
            }
            else
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to update student '{student.Username}': {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, StudentRoleName))
            {
                var roleResult = await userManager.AddToRoleAsync(user, StudentRoleName);
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
        StudentSeedRecord student,
        CancellationToken cancellationToken)
    {
        // Random GUIDs are generated only on first insert; reruns match by stable identity fields.
        var matches = await userManager.Users
            .Where(user =>
                user.OrganizationalId == student.OrganizationalId ||
                user.Email == student.Email ||
                user.UserName == student.Username)
            .ToListAsync(cancellationToken);

        var distinctMatches = matches
            .GroupBy(user => user.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (distinctMatches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Student seed data for '{student.Username}' matched multiple existing users. " +
                "Please resolve the conflicting auth records before rerunning the seeder.");
        }

        return distinctMatches.SingleOrDefault();
    }

    private sealed record StudentSeedRecord(
        string FirstName,
        string LastName,
        string OrganizationalId,
        string Username,
        string Email);
}
