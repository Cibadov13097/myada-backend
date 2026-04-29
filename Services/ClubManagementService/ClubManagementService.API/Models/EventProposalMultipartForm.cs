using ClubManagementService.Application.DTOs.Contracts;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace ClubManagementService.API.Models;

public sealed class EventProposalMultipartForm
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string? Name { get; set; }
    public string? DateTime { get; set; }
    public string? Duration { get; set; }
    public string? Attendance { get; set; }
    public string? Venue { get; set; }
    public int? BuildingId { get; set; }
    public int? RoomId { get; set; }
    public string? Description { get; set; }
    public string? Objectives { get; set; }
    public string? ImageUrl { get; set; }
    public IFormFile? ImageFile { get; set; }
    public string? SubEventsJson { get; set; }
    public string? LogisticsJson { get; set; }
    public string? SubmittedByOrganization { get; set; }

    public EventProposalSubmitDto ToSubmissionDto() =>
        new()
        {
            Name = Name?.Trim() ?? string.Empty,
            DateTime = DateTime?.Trim() ?? string.Empty,
            Duration = Duration?.Trim() ?? string.Empty,
            Attendance = Attendance?.Trim() ?? string.Empty,
            Venue = Venue?.Trim() ?? string.Empty,
            BuildingId = BuildingId,
            RoomId = RoomId,
            Description = Description?.Trim() ?? string.Empty,
            Objectives = Objectives?.Trim() ?? string.Empty,
            ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim(),
            SubEvents = ParseSubEvents(SubEventsJson),
            Logistics = ParseLogistics(LogisticsJson),
            SubmittedByOrganization = string.IsNullOrWhiteSpace(SubmittedByOrganization)
                ? null
                : SubmittedByOrganization.Trim()
        };

    private static List<EventProposalSubEventDto> ParseSubEvents(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<EventProposalSubEventDto>();

        return JsonSerializer.Deserialize<List<EventProposalSubEventDto>>(json, JsonOptions)
            ?? new List<EventProposalSubEventDto>();
    }

    private static EventProposalLogisticsDto ParseLogistics(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new EventProposalLogisticsDto();

        return JsonSerializer.Deserialize<EventProposalLogisticsDto>(json, JsonOptions)
            ?? new EventProposalLogisticsDto();
    }
}
