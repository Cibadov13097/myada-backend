using AuthService.Domain.Enums;
using AuthService.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence.Seeders;

public static class InstructorUserSeeder
{
    private const string InstructorRoleName = "instructor";
    private const string SeedPassword = "SeedUser123!";

    private static readonly InstructorSeedRecord[] InstructorSeeds =
    {
        new("Nuraddin", "Sadili", "000073421", "nsadili73421", "nsadili73421@ada.edu.az"),
        new("Azar", "Aliyev", "000015892", "aaliyev15892", "aaliyev15892@ada.edu.az"),
        new("Mykhailo", "Medvediev", "000098314", "mmedvediev98314", "mmedvediev98314@ada.edu.az"),
        new("Mahmud", "Ahmadov", "000042765", "mahmadov42765", "mahmadov42765@ada.edu.az"),
        new("Minura", "Hajisoy", "000087213", "mhajisoy87213", "mhajisoy87213@ada.edu.az"),
        new("Azar", "Hasanzada", "000034981", "ahasanzada34981", "ahasanzada34981@ada.edu.az"),
        new("Orkhan", "Mammadov", "000065472", "omammadov65472", "omammadov65472@ada.edu.az"),
        new("Lala", "Guluzada", "000019283", "lguluzada19283", "lguluzada19283@ada.edu.az"),
        new("John Thomas", "Burns", "000076145", "jburns76145", "jburns76145@ada.edu.az"),
        new("Araz", "Yusubov", "000023984", "ayusubov23984", "ayusubov23984@ada.edu.az"),
        new("Kamila", "Ismayilova", "000054672", "kismayilova54672", "kismayilova54672@ada.edu.az"),
        new("Samir", "Mammadov", "000081239", "smammadov81239", "smammadov81239@ada.edu.az"),
        new("Jamaladdin", "Hasanov", "000067845", "jhasanov67845", "jhasanov67845@ada.edu.az"),
        new("Nurlan", "Imanov", "000012938", "nimanov12938", "nimanov12938@ada.edu.az"),
        new("Fidan", "Guvvatli", "000094561", "fguvvatli94561", "fguvvatli94561@ada.edu.az"),
        new("Rashad", "Aliyev", "000037284", "raliyev37284", "raliyev37284@ada.edu.az"),
        new("Vugar", "Musayev", "000056193", "vmusayev56193", "vmusayev56193@ada.edu.az"),
        new("Umid", "Suleymanov", "000082734", "usuleymanov82734", "usuleymanov82734@ada.edu.az"),
        new("Gulzar", "Ismayilova", "000021657", "gismayilova21657", "gismayilova21657@ada.edu.az"),
        new("Elvin", "Allazov", "000073812", "eallazov73812", "eallazov73812@ada.edu.az"),
        new("Samir", "Hajizada", "000048239", "shajizada48239", "shajizada48239@ada.edu.az"),
        new("Damirli", "Huseyn", "000091284", "dhuseyn91284", "dhuseyn91284@ada.edu.az"),
        new("Alberto Levy", "Macedo", "000065981", "amacedo65981", "amacedo65981@ada.edu.az"),
        new("Azra", "Brankovic", "000032847", "abrankovic32847", "abrankovic32847@ada.edu.az"),
        new("Elnur", "Eyvazov", "000078213", "eeyvazov78213", "eeyvazov78213@ada.edu.az"),
        new("Ruslan", "Aliyev", "000014672", "raliyev14672", "raliyev14672@ada.edu.az"),
        new("Nicolas", "Hamelin", "000086451", "nhamelin86451", "nhamelin86451@ada.edu.az"),
        new("Vusal", "Hasanov", "000053892", "vhasanov53892", "vhasanov53892@ada.edu.az"),
        new("Ramil", "Maharramov", "000097231", "rmaharramov97231", "rmaharramov97231@ada.edu.az"),
        new("Wynand", "Bodewes", "000061284", "wbodewes61284", "wbodewes61284@ada.edu.az"),
        new("Kamil", "Mirishov", "000029384", "kmirishov29384", "kmirishov29384@ada.edu.az"),
        new("Nargiz", "Heydarova", "000084562", "nheydarova84562", "nheydarova84562@ada.edu.az"),
        new("Vusal", "Mammadrzayev", "000037951", "vmammadrzayev37951", "vmammadrzayev37951@ada.edu.az"),
        new("Kamala", "Babayeva", "000075213", "kbabayeva75213", "kbabayeva75213@ada.edu.az"),
        new("Aydan", "Bakhshaliyeva", "000018942", "abakhshaliyeva18942", "abakhshaliyeva18942@ada.edu.az"),
        new("Narmina", "Rustamova", "000092734", "nrustamova92734", "nrustamova92734@ada.edu.az"),
        new("Aysel", "Bandad", "000046281", "abandad46281", "abandad46281@ada.edu.az"),
        new("Amin", "Babazade", "000063712", "ababazade63712", "ababazade63712@ada.edu.az"),
        new("Muharrem", "Yeshilirmak", "000085439", "myeshilirmak85439", "myeshilirmak85439@ada.edu.az"),
        new("Ali", "Gunesh", "000027154", "agunesh27154", "agunesh27154@ada.edu.az"),
        new("Tural", "Yusifzada", "000073642", "tyusifzada73642", "tyusifzada73642@ada.edu.az"),
        new("Kerim", "Keskin", "000058213", "kkeskin58213", "kkeskin58213@ada.edu.az"),
        new("Azar", "Abizada", "000014829", "aabizada14829", "aabizada14829@ada.edu.az"),
        new("Elkin", "Nurmammadov", "000096312", "enurmammadov96312", "enurmammadov96312@ada.edu.az"),
        new("Kavus", "Abushov", "000039281", "aabushov39281", "aabushov39281@ada.edu.az"),
        new("Sarvar", "Gurbanov", "000087654", "sgurbanov87654", "sgurbanov87654@ada.edu.az"),
        new("Nijat", "Abbasli", "000052193", "nabbasli52193", "nabbasli52193@ada.edu.az")
    };

    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        CancellationToken cancellationToken = default)
    {
        if (!await roleManager.RoleExistsAsync(InstructorRoleName))
        {
            throw new InvalidOperationException(
                $"Required role '{InstructorRoleName}' does not exist before instructor seeding.");
        }

        foreach (var instructor in InstructorSeeds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var user = await FindExistingUserAsync(userManager, instructor, cancellationToken);
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

            user!.FirstName = instructor.FirstName;
            user.LastName = instructor.LastName;
            user.OrganizationalId = instructor.OrganizationalId;
            user.UserName = instructor.Username;
            user.Email = instructor.Email;
            user.EmailConfirmed = true;
            user.PhoneNumber = null;
            user.ProfileImage = null;
            user.UserType = UserType.Faculty;
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
                        $"Failed to create instructor '{instructor.Username}': {errors}");
                }
            }
            else
            {
                var updateResult = await userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to update instructor '{instructor.Username}': {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, InstructorRoleName))
            {
                var roleResult = await userManager.AddToRoleAsync(user, InstructorRoleName);
                if (!roleResult.Succeeded)
                {
                    var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                    throw new InvalidOperationException(
                        $"Failed to assign role '{InstructorRoleName}' to '{instructor.Username}': {errors}");
                }
            }
        }
    }

    private static async Task<ApplicationUser?> FindExistingUserAsync(
        UserManager<ApplicationUser> userManager,
        InstructorSeedRecord instructor,
        CancellationToken cancellationToken)
    {
        var matches = await userManager.Users
            .Where(user =>
                user.OrganizationalId == instructor.OrganizationalId ||
                user.Email == instructor.Email ||
                user.UserName == instructor.Username)
            .ToListAsync(cancellationToken);

        var distinctMatches = matches
            .GroupBy(user => user.Id, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();

        if (distinctMatches.Count > 1)
        {
            throw new InvalidOperationException(
                $"Instructor seed data for '{instructor.Username}' matched multiple existing users. " +
                "Please resolve the conflicting auth records before rerunning the seeder.");
        }

        return distinctMatches.SingleOrDefault();
    }

    private sealed record InstructorSeedRecord(
        string FirstName,
        string LastName,
        string OrganizationalId,
        string Username,
        string Email);
}
