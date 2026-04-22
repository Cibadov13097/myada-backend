using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClubManagementService.Application.DTOs.Club;

/// <summary>
/// Validates and normalizes focusAreasJson for PATCH (Option A: immediate publish).
/// Unknown icons are rejected with HTTP 400 semantics via error message (caller maps to ArgumentException).
/// </summary>
public static class ClubFocusAreasJson
{
    public const int MaxItems = 6;
    public const int MaxTitleLength = 40;
    public const int MaxDescriptionLength = 180;

    public static readonly HashSet<string> AllowedIcons = new(StringComparer.Ordinal)
    {
        "target", "star", "award", "sparkles", "flag", "compass", "layers", "grid", "circle", "badge", "book",
        "book-open", "graduation-cap", "lightbulb", "brain", "notebook", "pencil", "microscope", "atom", "calculator",
        "palette", "brush", "music", "mic", "guitar", "camera", "video", "film", "clapperboard", "headphones", "laptop",
        "code", "cpu", "settings", "database", "server", "wifi", "cloud", "users", "user-plus", "heart", "handshake",
        "message-circle", "megaphone", "globe", "map", "briefcase", "bar-chart", "calendar", "ticket", "rocket",
        "trending-up", "clipboard", "check-circle"
    };

    /// <summary>Parse stored JSON for GET responses (legacy string entries supported).</summary>
    public static List<ClubFocusAreaItemDto> ParseForRead(string? focusAreasJson)
    {
        if (string.IsNullOrWhiteSpace(focusAreasJson))
            return new List<ClubFocusAreaItemDto>();

        try
        {
            var node = JsonNode.Parse(focusAreasJson);
            if (node is not JsonArray arr)
                return new List<ClubFocusAreaItemDto>();

            var list = new List<ClubFocusAreaItemDto>();
            foreach (var item in arr)
            {
                if (item is JsonValue jv && jv.TryGetValue<string>(out var s))
                {
                    list.Add(new ClubFocusAreaItemDto { Icon = "target", Title = s.Trim(), Description = string.Empty });
                    continue;
                }

                if (item is JsonObject o)
                {
                    var icon = o["icon"]?.GetValue<string>()?.Trim() ?? "target";
                    if (!AllowedIcons.Contains(icon))
                        icon = "target";
                    list.Add(new ClubFocusAreaItemDto
                    {
                        Icon = icon,
                        Title = o["title"]?.GetValue<string>()?.Trim() ?? string.Empty,
                        Description = o["description"]?.GetValue<string>()?.Trim() ?? string.Empty
                    });
                }
            }

            return list;
        }
        catch
        {
            return new List<ClubFocusAreaItemDto>();
        }
    }

    /// <summary>Validate multipart focusAreasJson; returns canonical JSON for persistence or an error message.</summary>
    public static (bool Ok, string? ErrorMessage, string? SerializedJson, List<ClubFocusAreaItemDto> Areas) ValidateAndNormalizeForWrite(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (false, "focusAreasJson must be a valid JSON array", null, new List<ClubFocusAreaItemDto>());

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return (false, "focusAreasJson must be a valid JSON array", null, new List<ClubFocusAreaItemDto>());
        }

        if (node is not JsonArray arr)
            return (false, "focusAreasJson must be an array", null, new List<ClubFocusAreaItemDto>());

        var normalized = new List<ClubFocusAreaItemDto>();
        for (var i = 0; i < arr.Count; i++)
        {
            var item = arr[i];
            if (item is JsonValue)
                return (false, "Each focus area must be an object with icon, title, and description", null, new List<ClubFocusAreaItemDto>());

            if (item is not JsonObject o)
                return (false, "Each focus area must be an object with icon, title, and description", null, new List<ClubFocusAreaItemDto>());

            var icon = o["icon"]?.GetValue<string>()?.Trim();
            if (string.IsNullOrEmpty(icon))
                icon = "target";

            if (!AllowedIcons.Contains(icon))
                return (false, $"Invalid icon for focus area at index {i}", null, new List<ClubFocusAreaItemDto>());

            var title = o["title"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var description = o["description"]?.GetValue<string>()?.Trim() ?? string.Empty;

            if (ContainsHtml(title) || ContainsHtml(description))
                return (false, $"HTML is not allowed in focus area at index {i}", null, new List<ClubFocusAreaItemDto>());

            if (title.Length > MaxTitleLength)
                return (false, $"title exceeds 40 characters at index {i}", null, new List<ClubFocusAreaItemDto>());

            if (description.Length > MaxDescriptionLength)
                return (false, $"description exceeds 180 characters at index {i}", null, new List<ClubFocusAreaItemDto>());

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(description))
                continue;

            normalized.Add(new ClubFocusAreaItemDto { Icon = icon, Title = title, Description = description });
        }

        if (normalized.Count > MaxItems)
            return (false, "At most 6 focus areas are allowed", null, new List<ClubFocusAreaItemDto>());

        var serialized = JsonSerializer.Serialize(normalized, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return (true, null, serialized, normalized);
    }

    private static bool ContainsHtml(string s) => s.Contains('<', StringComparison.Ordinal) || s.Contains('>', StringComparison.Ordinal);
}
