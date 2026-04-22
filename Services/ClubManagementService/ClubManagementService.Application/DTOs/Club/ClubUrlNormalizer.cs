namespace ClubManagementService.Application.DTOs.Club;

public static class ClubUrlNormalizer
{
    public const int MaxUrlLength = 2048;

    /// <summary>Normalizes social / resource URLs: https preferred, scheme-less hosts get https://, relative paths kept as-is.</summary>
    public static bool TryNormalizeSocialOrResourceUrl(string input, out string normalized, out string? error)
    {
        normalized = input.Trim();
        error = null;

        if (normalized.Length > MaxUrlLength)
        {
            error = "URL exceeds maximum length.";
            return false;
        }

        if (normalized.Contains('<', StringComparison.Ordinal) || normalized.Contains('>', StringComparison.Ordinal))
        {
            error = "Invalid characters in URL.";
            return false;
        }

        if (normalized.Length == 0)
        {
            error = "URL is empty.";
            return false;
        }

        if (normalized.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            if (!Uri.TryCreate(normalized, UriKind.Absolute, out var mail) || mail.Scheme != "mailto")
            {
                error = "Invalid mailto URL.";
                return false;
            }

            return true;
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal))
            return true;

        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            if (normalized.StartsWith("//", StringComparison.Ordinal))
                normalized = "https:" + normalized;
            else
                normalized = "https://" + normalized.TrimStart('/');
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            error = "Invalid URL.";
            return false;
        }

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            error = "URL must use http or https.";
            return false;
        }

        if (uri.Scheme == Uri.UriSchemeHttp)
        {
            var builder = new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = uri.IsDefaultPort || uri.Port == 80 ? -1 : uri.Port };
            normalized = builder.Uri.AbsoluteUri;
        }
        else
        {
            normalized = uri.AbsoluteUri;
        }

        return true;
    }
}
