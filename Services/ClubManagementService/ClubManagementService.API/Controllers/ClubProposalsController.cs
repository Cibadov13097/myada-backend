using Asp.Versioning;
using ClubManagementService.API.Models;
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
    private readonly IImageStorageService _imageStorageService;

    public ClubProposalsController(IProposalContractService proposalContract, IImageStorageService imageStorageService)
    {
        _proposalContract = proposalContract;
        _imageStorageService = imageStorageService;
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

    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] ClubProposalMultipartForm form, CancellationToken cancellationToken)
    {
        var body = form.ToSubmissionDto();

        var logoUrl = await UploadFileIfPresentAsync("club-proposals/logo", form.LogoFile, cancellationToken).ConfigureAwait(false);
        if (logoUrl == string.Empty)
            return BadRequest(new { message = "logoFile has an unsupported file type." });

        var constitutionUrl = await UploadFileIfPresentAsync("club-proposals/constitution", form.ConstitutionFile, cancellationToken).ConfigureAwait(false);
        if (constitutionUrl == string.Empty)
            return BadRequest(new { message = "constitutionFile has an unsupported file type." });

        body.LogoFile = logoUrl;
        body.ConstitutionFile = constitutionUrl;

        var result = await _proposalContract
            .SubmitClubProposalAsync(body.Id, body, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(result.ErrorBody);
        return Ok(result.Payload);
    }

    private async Task<string?> UploadFileIfPresentAsync(string directory, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return null;

        if (!_imageStorageService.IsSupportedImage(file.FileName))
            return string.Empty;

        await using var stream = file.OpenReadStream();
        return await _imageStorageService.UploadImageAsync(
            stream,
            file.FileName,
            file.ContentType,
            directory,
            cancellationToken).ConfigureAwait(false);
    }
}
