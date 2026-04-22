using System.Text.Json;
using System.Text.Json.Nodes;

namespace ClubManagementService.Application.DTOs.Club;

public static class ClubResourcesJson
{
    public const int MaxItems = 50;

    public static List<ClubResourceItemDto> ParseForRead(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<ClubResourceItemDto>();

        try
        {
            var node = JsonNode.Parse(json);
            if (node is not JsonArray arr)
                return new List<ClubResourceItemDto>();

            var list = new List<ClubResourceItemDto>();
            foreach (var item in arr)
            {
                if (item is JsonValue jv && jv.TryGetValue<string>(out var s))
                {
                    var u = s.Trim();
                    if (u.Length > 0)
                        list.Add(new ClubResourceItemDto { Url = u });
                    continue;
                }

                if (item is JsonObject o)
                {
                    var url = FirstNonEmpty(o, "url", "href", "fileUrl", "link");
                    if (string.IsNullOrEmpty(url))
                        continue;
                    list.Add(new ClubResourceItemDto
                    {
                        Id = FirstNonEmpty(o, "id"),
                        Url = url.Trim(),
                        Title = FirstNonEmpty(o, "title", "name", "fileName")
                    });
                }
            }

            return list;
        }
        catch
        {
            return new List<ClubResourceItemDto>();
        }
    }

    public static (bool Ok, string? ErrorMessage, string? CanonicalJson) ValidateAndReplace(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (false, "resourcesJson must be a valid JSON array when provided", null);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(raw);
        }
        catch (JsonException)
        {
            return (false, "resourcesJson must be a valid JSON array when provided", null);
        }

        if (node is not JsonArray arr)
            return (false, "resourcesJson must be an array", null);

        if (arr.Count > MaxItems)
            return (false, $"At most {MaxItems} resource items are allowed", null);

        var normalized = new List<ClubResourceItemDto>();
        for (var i = 0; i < arr.Count; i++)
        {
            var item = arr[i];
            if (item is JsonValue jv && jv.TryGetValue<string>(out var s))
            {
                var u = s.Trim();
                if (u.Length == 0)
                    continue;
                if (!ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(u, out var nu, out var err))
                    return (false, $"resources[{i}]: {err}", null);
                normalized.Add(new ClubResourceItemDto { Url = nu });
                continue;
            }

            if (item is not JsonObject o)
                return (false, $"resources[{i}] must be a string URL or an object with url/href/fileUrl/link", null);

            var url = FirstNonEmpty(o, "url", "href", "fileUrl", "link");
            if (string.IsNullOrEmpty(url))
                return (false, $"resources[{i}] is missing a URL (url, href, fileUrl, or link)", null);

            if (!ClubUrlNormalizer.TryNormalizeSocialOrResourceUrl(url, out var nurl, out var err2))
                return (false, $"resources[{i}]: {err2}", null);

            normalized.Add(new ClubResourceItemDto
            {
                Id = FirstNonEmpty(o, "id"),
                Url = nurl,
                Title = FirstNonEmpty(o, "title", "name", "fileName")
            });
        }

        var canonical = JsonSerializer.Serialize(normalized, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
        return (true, null, canonical);
    }

    private static string? FirstNonEmpty(JsonObject o, params string[] keys)
    {
        foreach (var key in keys)
        {
            var v = o[key]?.GetValue<string>()?.Trim();
            if (!string.IsNullOrEmpty(v))
                return v;
        }

        return null;
    }
}
