using Microsoft.AspNetCore.Http;

namespace ClubManagementService.API.Extensions;

public static class HttpContextApiVersionExtensions
{
    /// <summary>Reads the <c>version</c> route segment from <c>/api/v{version}/...</c>.</summary>
    public static int GetClubApiMajorVersion(this HttpContext http)
    {
        if (http.GetRouteData()?.Values.TryGetValue("version", out var v) == true && int.TryParse(v?.ToString(), out var major))
            return major;
        return 1;
    }
}
