using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.API.Security;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.DTOs.EventRegistration;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/events")]
[Route("api/events")]
[ApiController]
[Authorize]
[Tags("Events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IEventRegistrationService _registrationService;
    private readonly IClubMembershipRepository _clubMembershipRepository;
    private readonly IImageStorageService _imageStorageService;

    public EventsController(
        IEventService eventService,
        IEventRegistrationService registrationService,
        IClubMembershipRepository clubMembershipRepository,
        IImageStorageService imageStorageService)
    {
        _eventService = eventService;
        _registrationService = registrationService;
        _clubMembershipRepository = clubMembershipRepository;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? clubId = null,
        [FromQuery] bool myClubsOnly = false,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 0)
    {
        if (limit <= 0)
            limit = GetDefaultEventPageSize();

        var events = await _eventService.GetAllAsync();
        var filtered = events.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(e => e.Name.Contains(search, StringComparison.OrdinalIgnoreCase));

        if (clubId is > 0)
            filtered = filtered.Where(e => e.ClubId == clubId);

        if (myClubsOnly && !User.IsMyAdaAdministrator())
        {
            var userId = GetCurrentUserIdOrDefault();
            if (userId == Guid.Empty)
                return Unauthorized();

            var myClubIds = await _clubMembershipRepository.GetClubIdsByUserAsync(userId, HttpContext.RequestAborted).ConfigureAwait(false);
            if (myClubIds.Count == 0)
            {
                return Ok(new { items = Array.Empty<object>(), total = 0, page, limit });
            }

            var set = myClubIds.ToHashSet();
            filtered = filtered.Where(e => set.Contains(e.ClubId));
        }

        var total = filtered.Count();
        var items = filtered
            .Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1))
            .Take(Math.Max(limit, 1));
        return Ok(new { items, total, page, limit });
    }

    [HttpGet("{eventId:int}")]
    public async Task<IActionResult> GetById(int eventId)
    {
        var dto = await _eventService.GetByIdAsync(eventId);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("{eventId:int}/registrations")]
    public async Task<IActionResult> Register(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var evt = await _eventService.GetByIdAsync(eventId);
        if (evt == null)
            return NotFound();

        EventRegistrationDto reg;
        try
        {
            reg = await _registrationService.RegisterAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return Conflict(new { message = "This event has reached its seat limit." });
        }

        var registeredCount = (await _registrationService.GetByEventIdAsync(eventId)).Count;

        return Ok(new
        {
            ticketId = reg.TicketId,
            eventId = eventId.ToString(),
            userId = userId.ToString("D"),
            jwt = reg.RegistrationJwt,
            attendeeId = userId.ToString("D"),
            @event = new
            {
                id = evt.Id.ToString(),
                name = evt.Name,
                date = evt.StartTime.ToString("yyyy-MM-dd"),
                startTime = evt.StartTime.ToString("HH:mm"),
                endTime = evt.EndTime.ToString("HH:mm"),
                location = evt.Location,
                clubName = (string?)null,
                imageUrl = evt.ImageUrl,
                seatLimit = evt.SeatLimit,
                registeredCount
            }
        });
    }

    [HttpDelete("{eventId:int}/registrations")]
    public async Task<IActionResult> CancelRegistration(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        try
        {
            await _registrationService.DeleteByEventAndUserAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }

        return NoContent();
    }

    [HttpGet("{eventId:int}/ticket")]
    public async Task<IActionResult> GetTicket(int eventId)
    {
        var userId = GetCurrentUserIdOrDefault();
        if (userId == Guid.Empty)
            return Unauthorized();

        var reg = await _registrationService.GetTicketAsync(eventId, userId, HttpContext.RequestAborted).ConfigureAwait(false);
        if (reg == null)
            return NotFound();

        return Ok(new
        {
            ticketId = reg.TicketId,
            eventId = eventId.ToString(),
            userId = userId.ToString("D"),
            jwt = reg.RegistrationJwt,
            attendeeId = userId.ToString("D"),
            eventImageUrl = reg.EventImageUrlSnapshot
        });
    }

    [HttpPost("{eventId:int}/check-in")]
    [Authorize(Roles = "admin,Admin,staff,Staff")]
    public async Task<IActionResult> CheckIn(int eventId, [FromBody] CheckInRequest body, CancellationToken cancellationToken)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Jwt))
            return BadRequest(new { message = "jwt is required." });

        var staffId = GetCurrentUserIdOrDefault();
        if (staffId == Guid.Empty)
            return Unauthorized();

        var result = await _registrationService.CheckInAsync(eventId, body.Jwt, staffId, cancellationToken).ConfigureAwait(false);

        if (result.Success)
        {
            return Ok(new
            {
                success = true,
                status = result.Status,
                message = result.Message ?? "Entry allowed",
                ticketId = result.TicketId,
                attendeeId = result.AttendeeId.ToString("D"),
                eventId = result.EventId.ToString(),
                checkedInAt = result.CheckedInAt?.ToString("O")
            });
        }

        return Ok(new
        {
            success = false,
            status = result.Status,
            message = result.Message,
            ticketId = result.TicketId,
            attendeeId = result.AttendeeId == Guid.Empty ? null : result.AttendeeId.ToString("D"),
            eventId = result.EventId.ToString(),
            checkedInAt = result.CheckedInAt?.ToString("O")
        });
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateEventFormRequest request, CancellationToken cancellationToken)
    {
        var approvedBy = GetCurrentUserIdOrDefault();
        if (approvedBy == Guid.Empty)
            return Unauthorized();

        var (form, errorResult) = await TryReadMultipartFormAsync(cancellationToken).ConfigureAwait(false);
        if (errorResult != null)
            return errorResult;

        if (ContainsDisallowedImageUrlFields(form!))
        {
            return BadRequest(new
            {
                message = "Attach the event image with imageFile. imageUrl and imgUrl are not accepted on this route."
            });
        }

        var uploadedImageUrl = await UploadEventImageIfPresentAsync(request.ClubId, form!, request.ImageFile, cancellationToken).ConfigureAwait(false);
        if (uploadedImageUrl == string.Empty)
            return BadRequest(new { message = "imageFile has an unsupported file type." });

        try
        {
            var dto = request.ToCreateDto(approvedBy, uploadedImageUrl);
            var created = await _eventService.CreateAsync(dto).ConfigureAwait(false);
            var v = HttpContext.GetClubApiMajorVersion();
            return CreatedAtAction(nameof(GetById), new { version = v, eventId = created.Id }, created);
        }
        catch
        {
            await TryDeleteStoredFileAsync(uploadedImageUrl, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    [HttpPatch("{eventId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int eventId, [FromForm] UpdateEventFormRequest request, CancellationToken cancellationToken)
    {
        var (form, errorResult) = await TryReadMultipartFormAsync(cancellationToken).ConfigureAwait(false);
        if (errorResult != null)
            return errorResult;

        if (ContainsDisallowedImageUrlFields(form!))
        {
            return BadRequest(new
            {
                message = "Attach the event image with imageFile. imageUrl and imgUrl are not accepted on this route."
            });
        }

        var existing = await _eventService.GetByIdAsync(eventId).ConfigureAwait(false);
        if (existing == null)
            return NotFound();

        var uploadedImageUrl = await UploadEventImageIfPresentAsync(existing.ClubId, form!, request.ImageFile, cancellationToken).ConfigureAwait(false);
        if (uploadedImageUrl == string.Empty)
            return BadRequest(new { message = "imageFile has an unsupported file type." });

        var nextImageUrl = string.IsNullOrWhiteSpace(uploadedImageUrl) ? existing.ImageUrl : uploadedImageUrl;
        try
        {
            var updated = await _eventService.UpdateAsync(eventId, request.ToUpdateDto(nextImageUrl)).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(uploadedImageUrl)
                && !string.IsNullOrWhiteSpace(existing.ImageUrl)
                && !string.Equals(existing.ImageUrl, uploadedImageUrl, StringComparison.Ordinal))
            {
                await TryDeleteStoredFileAsync(existing.ImageUrl, cancellationToken).ConfigureAwait(false);
            }

            return Ok(updated);
        }
        catch
        {
            await TryDeleteStoredFileAsync(uploadedImageUrl, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    [HttpDelete("{eventId:int}")]
    public async Task<IActionResult> Delete(int eventId)
    {
        await _eventService.DeleteAsync(eventId);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private async Task<(IFormCollection? Form, IActionResult? ErrorResult)> TryReadMultipartFormAsync(CancellationToken cancellationToken)
    {
        if (Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) != true)
        {
            return (null, StatusCode(StatusCodes.Status415UnsupportedMediaType, new
            {
                message = "Unsupported content type. Use multipart/form-data and attach the image as imageFile."
            }));
        }

        try
        {
            return (await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false), null);
        }
        catch (InvalidDataException)
        {
            return (null, BadRequest(new
            {
                message = "Invalid multipart form data. Use multipart/form-data for file uploads and let the client set the boundary automatically."
            }));
        }
    }

    private static bool ContainsDisallowedImageUrlFields(IFormCollection form)
    {
        return form.Keys.Any(key =>
            string.Equals(key, "imageUrl", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "imgUrl", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> UploadEventImageIfPresentAsync(
        int clubId,
        IFormCollection form,
        IFormFile? imageFile,
        CancellationToken cancellationToken)
    {
        var file = ResolveEventImageFile(form, imageFile);
        if (file == null || file.Length <= 0)
            return null;

        if (!_imageStorageService.IsSupportedImage(file.FileName))
            return string.Empty;

        await using var stream = file.OpenReadStream();
        return await _imageStorageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            $"events/{clubId}",
            cancellationToken).ConfigureAwait(false);
    }

    private static IFormFile? ResolveEventImageFile(IFormCollection form, IFormFile? imageFile)
    {
        return imageFile
            ?? form.Files.GetFile("imageFile")
            ?? form.Files.GetFile("image")
            ?? form.Files.GetFile("posterFile");
    }

    private async Task TryDeleteStoredFileAsync(string? fileUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        try
        {
            await _imageStorageService.DeleteFileAsync(fileUrl, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Event metadata updates should still succeed if cleanup of an old stored file fails.
        }
    }

    private int GetDefaultEventPageSize() => HttpContext.GetClubApiMajorVersion() >= 2 ? 24 : 12;

    public sealed class CheckInRequest
    {
        public string Jwt { get; set; } = string.Empty;
    }

    public sealed class CreateEventFormRequest : EventFormRequestBase
    {
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

    public abstract class EventFormRequestBase
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? BuildingId { get; set; }
        public int? RoomId { get; set; }
        public int SeatLimit { get; set; }
        public string Requirements { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public IFormFile? ImageFile { get; set; }

        protected static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
