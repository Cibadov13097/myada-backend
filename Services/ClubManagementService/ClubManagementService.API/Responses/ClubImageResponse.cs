namespace ClubManagementService.API.Responses;

internal static class ClubImageResponse
{
    internal const string DefaultImage = "/clubs/default.png";

    internal static string? Normalize(string? imageUrl)
        => string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();

    internal static string ResolveDisplayImage(string? imageUrl)
        => Normalize(imageUrl) ?? DefaultImage;
}
