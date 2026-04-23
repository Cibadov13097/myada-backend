using Asp.Versioning;
using ClubManagementService.API.Security;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/interviews")]
[ApiController]
[Authorize]
[Tags("Interviews")]
public sealed class InterviewsController : ControllerBase
{
    private readonly IInterviewSchedulingService _interviews;

    public InterviewsController(IInterviewSchedulingService interviews) => _interviews = interviews;

    /// <summary>
    /// List available interview slots for an interview.
    /// In this service, the "interviewId" maps to an existing JobApplication Id.
    /// Optional <paramref name="date"/> filters to slots starting on that calendar day (UTC).
    /// </summary>
    [HttpGet("{interviewId}/slots")]
    public async Task<IActionResult> ListSlots(
        string interviewId,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] DateOnly? date = null,
        [FromQuery] bool includeFull = false,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(interviewId, out var applicationId) || applicationId <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var result = await _interviews
            .ListSlotsAsync(applicationId, callerId, User.IsMyAdaAdministrator(), from, to, date, includeFull, cancellationToken)
            .ConfigureAwait(false);

        if (result.NotFound)
            return NotFound(new { message = result.Message ?? "Interview not found." });
        if (result.Forbidden)
            return Forbid();
        return Ok(new { interviewId, slots = result.Slots });
    }

    /// <summary>Book exactly one slot for this interview (JobApplication).</summary>
    [HttpPost("{interviewId}/bookings")]
    public async Task<IActionResult> BookSlot(string interviewId, [FromBody] BookSlotRequest request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(interviewId, out var applicationId) || applicationId <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var r = await _interviews
            .BookSlotAsync(applicationId, callerId, User.IsMyAdaAdministrator(), request?.SlotId ?? string.Empty, cancellationToken)
            .ConfigureAwait(false);

        if (r.Forbidden)
            return Forbid();
        if (r.NotFound)
            return NotFound(new { message = r.Message });
        if (r.BadRequest)
            return BadRequest(new { message = r.Message });
        if (r.Conflict)
            return Conflict(new { message = r.Message });

        if (r.IsIdempotentReplay)
            return Ok(new BookingResponse
            {
                BookingId = r.BookingId,
                InterviewId = r.InterviewId,
                SlotId = r.SlotId,
                Status = r.Status,
                StartAt = r.StartAt,
                EndAt = r.EndAt
            });

        return StatusCode(StatusCodes.Status201Created, new BookingResponse
        {
            BookingId = r.BookingId,
            InterviewId = r.InterviewId,
            SlotId = r.SlotId,
            Status = r.Status,
            StartAt = r.StartAt,
            EndAt = r.EndAt
        });
    }

    [HttpGet("{interviewId}/my-booking")]
    public async Task<IActionResult> GetMyBooking(string interviewId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(interviewId, out var applicationId) || applicationId <= 0)
            return NotFound();

        var callerId = GetCurrentUserIdOrDefault();
        if (callerId == Guid.Empty)
            return Unauthorized();

        var r = await _interviews
            .GetBookingAsync(applicationId, callerId, User.IsMyAdaAdministrator(), cancellationToken)
            .ConfigureAwait(false);

        if (r.Forbidden)
            return Forbid();
        if (r.NotFound)
            return NotFound(new { message = r.Message ?? "No booking found." });

        return Ok(new BookingResponse
        {
            BookingId = r.BookingId,
            InterviewId = r.InterviewId,
            SlotId = r.SlotId,
            Status = r.Status,
            StartAt = r.StartAt,
            EndAt = r.EndAt
        });
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class BookSlotRequest
    {
        public string SlotId { get; set; } = string.Empty;
    }

    public sealed class BookingResponse
    {
        public string BookingId { get; set; } = string.Empty;
        public string InterviewId { get; set; } = string.Empty;
        public string SlotId { get; set; } = string.Empty;
        public string Status { get; set; } = "CONFIRMED";
        public DateTimeOffset? StartAt { get; set; }
        public DateTimeOffset? EndAt { get; set; }
    }
}
