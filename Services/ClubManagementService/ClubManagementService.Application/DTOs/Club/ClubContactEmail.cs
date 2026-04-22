using System.Net.Mail;

namespace ClubManagementService.Application.DTOs.Club;

public static class ClubContactEmail
{
    public const int MaxLength = 320;

    /// <summary>Clears when <paramref name="input"/> is null or whitespace after trim.</summary>
    public static bool TryNormalize(string? input, out string? normalized, out string? error)
    {
        normalized = null;
        error = null;
        if (string.IsNullOrWhiteSpace(input))
            return true;

        var t = input.Trim();
        if (t.Length > MaxLength)
        {
            error = "Email exceeds maximum length.";
            return false;
        }

        try
        {
            _ = new MailAddress(t);
            normalized = t;
            return true;
        }
        catch
        {
            error = "Invalid email address.";
            return false;
        }
    }
}
