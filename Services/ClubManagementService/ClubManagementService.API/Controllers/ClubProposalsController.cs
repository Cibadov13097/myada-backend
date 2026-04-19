using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-proposals")]
[ApiController]
[Authorize]
[Tags("Club Proposals")]
public class ClubProposalsController : ControllerBase
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    public IActionResult Create([FromForm] ClubProposalRequest request)
    {
        return Ok(new
        {
            proposalId = $"PROP-{DateTime.UtcNow.Year}-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            status = "pending_review"
        });
    }

    public sealed class ClubProposalRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public IFormFile? LogoFile { get; set; }
        public IFormFile? ConstitutionFile { get; set; }
    }
}
