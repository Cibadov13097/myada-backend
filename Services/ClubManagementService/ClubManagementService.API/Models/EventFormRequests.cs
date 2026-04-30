using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Models;

public abstract class EventFormRequestBase
{
    [FromForm(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [FromForm(Name = "description")]
    public string Description { get; set; } = string.Empty;

    [FromForm(Name = "startTime")]
    public DateTime StartTime { get; set; }

    [FromForm(Name = "endTime")]
    public DateTime EndTime { get; set; }

    [FromForm(Name = "location")]
    public string Location { get; set; } = string.Empty;

    [FromForm(Name = "buildingId")]
    public int? BuildingId { get; set; }

    [FromForm(Name = "roomId")]
    public int? RoomId { get; set; }

    [FromForm(Name = "seatLimit")]
    public int SeatLimit { get; set; }

    [FromForm(Name = "requirements")]
    public string Requirements { get; set; } = string.Empty;

    [FromForm(Name = "prerequisites")]
    public string Prerequisites { get; set; } = string.Empty;

    [FromForm(Name = "notes")]
    public string Notes { get; set; } = string.Empty;

    [FromForm(Name = "imageFile")]
    public IFormFile? ImageFile { get; set; }

    protected static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}

public sealed class CreateEventFormRequest : EventFormRequestBase
{
    [FromForm(Name = "clubId")]
    public int ClubId { get; set; }

    public CreateEventDto ToCreateDto(Guid approvedBy, string? imageUrl)
    {
        return new CreateEventDto
        {
            ClubId = ClubId,
            Name = Name,
            Description = Description,
            ImageUrl = NormalizeOptionalText(imageUrl),
            StartTime = StartTime,
            EndTime = EndTime,
            Location = Location,
            BuildingId = BuildingId,
            RoomId = RoomId,
            SeatLimit = SeatLimit,
            Requirements = Requirements,
            Prerequisites = Prerequisites,
            Notes = Notes,
            ApprovedBy = approvedBy
        };
    }
}

public sealed class UpdateEventFormRequest : EventFormRequestBase
{
    [FromForm(Name = "status")]
    public ClubStatus Status { get; set; }

    public UpdateEventDto ToUpdateDto(string? imageUrl)
    {
        return new UpdateEventDto
        {
            Name = Name,
            Description = Description,
            ImageUrl = NormalizeOptionalText(imageUrl),
            StartTime = StartTime,
            EndTime = EndTime,
            Location = Location,
            BuildingId = BuildingId,
            RoomId = RoomId,
            SeatLimit = SeatLimit,
            Requirements = Requirements,
            Prerequisites = Prerequisites,
            Notes = Notes,
            Status = Status
        };
    }
}
