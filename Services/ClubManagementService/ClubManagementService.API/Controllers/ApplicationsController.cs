using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/applications")]
[ApiController]
[Authorize]
[Tags("Applications")]
public class ApplicationsController : ControllerBase
{
    [HttpGet("{applicationId}/interview-slots")]
    public IActionResult GetInterviewSlots(string applicationId) => Ok(new { items = Array.Empty<object>() });

    [HttpPost("{applicationId}/interview-slot")]
    public IActionResult SelectInterviewSlot(string applicationId, [FromBody] SelectInterviewSlotRequest request)
    {
        return Ok(new
        {
            applicationId,
            status = "Interview Scheduled",
            interviewSlotId = request.SlotId
        });
    }

    public sealed class SelectInterviewSlotRequest
    {
        public string SlotId { get; set; } = string.Empty;
    }
}
