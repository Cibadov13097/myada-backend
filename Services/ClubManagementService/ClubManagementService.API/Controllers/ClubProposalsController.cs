using Asp.Versioning;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-proposals")]
[ApiController]
[Tags("Club Proposals")]
public class ClubProposalsController : ControllerBase
{
    private readonly IProposalContractService _proposalContract;

    public ClubProposalsController(IProposalContractService proposalContract)
    {
        _proposalContract = proposalContract;
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json")]
    public async Task<IActionResult> Create([FromBody] ClubProposalSubmissionDto body, CancellationToken cancellationToken)
    {
        var result = await _proposalContract
            .SubmitClubProposalAsync(body.Id, body, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(result.ErrorBody);
        return Ok(result.Payload);
    }
}
