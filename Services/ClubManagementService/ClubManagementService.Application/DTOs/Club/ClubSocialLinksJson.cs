using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace ClubManagementService.Application.DTOs.Club;

public static class ClubSocialLinksJson
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static ClubSocialLinksDto? ParseForRead(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonObject o)
                return null;

            string? S(string key) => o[key]?.GetValue<string>()?.Trim();

            var x = S("x");
            if (string.IsNullOrEmpty(x))
                x = S("twitter");

            return new ClubSocialLinksDto
            {
                Website = S("website"),
                Instagram = S("instagram"),
                X = x,
                Tiktok = S("tiktok")
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Merge-patch: keys present in <paramref name="patchRaw"/> update or clear that field;
    /// keys absent leave existing values unchanged. Empty string / null value clears the field.
    /// <c>twitter</c> is an alias for <c>x</c> (persisted as <c>x</c> only).
    /// </summary>
    public static (bool Ok, string? ErrorMessage, string? CanonicalJson) MergePatch(string? existingJson, string patchRaw)
    {
        if (string.IsNullOrWhiteSpace(patchRaw))
            return (false, "socialLinks must be a valid JSON object when provided", null);

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(patchRaw);
        }
        catch (JsonException)
        {
            return (false, "socialLinks must be a valid JSON object when provided", null);
        }

        if (root is not JsonObject patch)
            return (false, "socialLinks must be a valid JSON object when provided", null);

        var merged = ToMutableMap(ParseForRead(existingJson));

        foreach (var prop in patch)
        {
            var slot = MapIncomingKey(prop.Key);
            if (slot == null)
                continue;

            if (prop.Value is null || IsEmptyJsonValue(prop.Value))
            {
                merged.Remove(slot);
                continue;
            }

            if (prop.Value is not JsonValue jv)
                return (false, $"socialLinks.{prop.Key} must be a string", null);

            string raw;
            try
            {
                raw = jv.GetValue<string>()?.Trim() ?? string.Empty;
            }
            catch (InvalidOperationException)
            {
                return (false, $"socialLinks.{prop.Key} must be a string", null);
            }
            if (raw.Length == 0)
            {
                merged.Remove(slot);
                continue;
            }

            if (!ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(raw, out var norm, out var err))
                return (false, $"{prop.Key}: {err}", null);

            merged[slot] = norm;
        }

        var dto = MapToDto(merged);
        var canonical = JsonSerializer.Serialize(dto, WriteOptions);
        return (true, null, canonical);
    }

    private static Dictionary<string, string> ToMutableMap(ClubSocialLinksDto? dto)
    {
        var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (dto == null)
            return d;
        if (!string.IsNullOrEmpty(dto.Website))
            d["website"] = dto.Website;
        if (!string.IsNullOrEmpty(dto.Instagram))
            d["instagram"] = dto.Instagram;
        if (!string.IsNullOrEmpty(dto.X))
            d["x"] = dto.X;
        if (!string.IsNullOrEmpty(dto.Tiktok))
            d["tiktok"] = dto.Tiktok;
        return d;
    }

    private static ClubSocialLinksDto MapToDto(Dictionary<string, string> m)
    {
        m.TryGetValue("website", out var w);
        m.TryGetValue("instagram", out var ig);
        m.TryGetValue("x", out var x);
        m.TryGetValue("tiktok", out var tt);
        return new ClubSocialLinksDto
        {
            Website = w,
            Instagram = ig,
            X = x,
            Tiktok = tt
        };
    }

    private static string? MapIncomingKey(string rawKey)
    {
        return rawKey.Trim().ToLowerInvariant() switch
        {
            "website" => "website",
            "instagram" => "instagram",
            "x" or "twitter" => "x",
            "tiktok" => "tiktok",
            _ => null
        };
    }

    private static bool IsEmptyJsonValue(JsonNode n)
    {
        if (n is JsonValue v)
        {
            if (v.TryGetValue<string>(out var s))
                return string.IsNullOrWhiteSpace(s);
        }

        return false;
    }
}
