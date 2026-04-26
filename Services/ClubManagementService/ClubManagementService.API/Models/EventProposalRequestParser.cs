using ClubManagementService.Application.DTOs.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace ClubManagementService.API.Models;

internal static class EventProposalRequestParser
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static bool IsMultipartRequest(HttpRequest request) =>
        request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;

    public static bool IsJsonRequest(HttpRequest request)
    {
        var contentType = request.ContentType;
        if (string.IsNullOrWhiteSpace(contentType))
            return false;

        var mediaType = contentType.Split(';', 2)[0].Trim();
        return string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    public static EventProposalMultipartForm ToMultipartForm(IFormCollection form) =>
        new()
        {
            Name = form["name"],
            DateTime = form["dateTime"],
            Duration = form["duration"],
            Attendance = form["attendance"],
            Venue = form["venue"],
            Description = form["description"],
            Objectives = form["objectives"],
            ImageUrl = FirstNonEmpty(form["imageUrl"], form["posterUrl"]),
            ImageFile = form.Files.GetFile("imageFile") ?? form.Files.GetFile("posterFile"),
            SubEventsJson = FirstNonEmpty(form["subEventsJson"], form["subEvents"]),
            LogisticsJson = FirstNonEmpty(form["logisticsJson"], form["logistics"]),
            SubmittedByOrganization = form["submittedByOrganization"]
        };

    public static async Task<EventProposalSubmitDto> ReadJsonBodyAsync(HttpRequest request, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return new EventProposalSubmitDto();

        var dto = document.RootElement.Deserialize<EventProposalSubmitDto>(JsonOptions)
                  ?? new EventProposalSubmitDto();

        if (string.IsNullOrWhiteSpace(dto.ImageUrl)
            && document.RootElement.TryGetProperty("posterUrl", out var posterUrl)
            && posterUrl.ValueKind == JsonValueKind.String)
        {
            dto.ImageUrl = posterUrl.GetString();
        }

        return dto;
    }

    private static string? FirstNonEmpty(params StringValues[] values)
    {
        foreach (var value in values)
        {
            var trimmed = value.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return null;
    }
}
