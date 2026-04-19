using System.Globalization;

namespace ClubManagementService.Application.ClubAdmin;

public static class ClubRouteId
{
    /// <summary>Parses club route keys. Accepts invariant integer strings (positive); extend for GUID correlation later.</summary>
    public static bool TryParseClubKey(string clubId, out int numericId)
    {
        numericId = 0;
        if (string.IsNullOrWhiteSpace(clubId))
            return false;

        return int.TryParse(clubId.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out numericId)
               && numericId > 0;
    }
}
