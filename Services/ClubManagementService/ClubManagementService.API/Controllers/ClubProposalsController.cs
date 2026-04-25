using Asp.Versioning;
using ClubManagementService.API.Models;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using System.Text.Json;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/club-proposals")]
[ApiController]
[Tags("Club Proposals")]
public class ClubProposalsController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IProposalContractService _proposalContract;
    private readonly IImageStorageService _imageStorageService;

    public ClubProposalsController(IProposalContractService proposalContract, IImageStorageService imageStorageService)
    {
        _proposalContract = proposalContract;
        _imageStorageService = imageStorageService;
    }

    [HttpPost]
    [AllowAnonymous]
    [Consumes("application/json", "multipart/form-data")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ClubProposalSubmissionDto body;
        if (IsMultipartRequest())
        {
            var form = HttpContext.Features.Get<IFormFeature>()?.Form
                       ?? await Request.ReadFormAsync(cancellationToken).ConfigureAwait(false);
            var multipart = ToMultipartForm(form);
            body = multipart.ToSubmissionDto();

            var logoUrl = await UploadFileIfPresentAsync("club-proposals/logo", multipart.LogoFile, cancellationToken).ConfigureAwait(false);
            if (logoUrl == string.Empty)
                return BadRequest(new { message = "logoFile has an unsupported file type." });

            var constitutionUrl = await UploadFileIfPresentAsync("club-proposals/constitution", multipart.ConstitutionFile, cancellationToken).ConfigureAwait(false);
            if (constitutionUrl == string.Empty)
                return BadRequest(new { message = "constitutionFile has an unsupported file type." });

            body.LogoFile = logoUrl;
            body.ConstitutionFile = constitutionUrl;
        }
        else
        {
            body = await ReadJsonBodyAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = await _proposalContract
            .SubmitClubProposalAsync(body.OrganizationId, body, cancellationToken)
            .ConfigureAwait(false);
        if (!result.Success)
            return BadRequest(result.ErrorBody);
        return Ok(result.Payload);
    }

    private bool IsMultipartRequest() =>
        Request.ContentType?.StartsWith("multipart/form-data", StringComparison.OrdinalIgnoreCase) == true;

    private static ClubProposalMultipartForm ToMultipartForm(IFormCollection form) =>
        new()
        {
            OrganizationId = FirstNonEmpty(form["organizationId"], form["id"]),
            Name = form["name"],
            Description = form["description"],
            LogoFile = form.Files.GetFile("logoFile"),
            ConstitutionFile = form.Files.GetFile("constitutionFile"),
            PresidentStudentId = form["presidentStudentId"],
            VicePresidentStudentId = form["vicePresidentStudentId"],
            OtherMembersJson = form["otherMembersJson"].Where(x => !string.IsNullOrWhiteSpace(x)).ToList()!,
            Commitment = form["commitment"],
            ShortDesc = form["shortDesc"],
            UniqueDesc = form["uniqueDesc"],
            Goals = form["goals"],
            Activities = form["activities"],
            Alignment = form["alignment"],
            Vision = form["vision"]
        };

    private async Task<ClubProposalSubmissionDto> ReadJsonBodyAsync(CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(Request.Body, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return new ClubProposalSubmissionDto();

        var dto = document.RootElement.Deserialize<ClubProposalSubmissionDto>(JsonOptions)
                  ?? new ClubProposalSubmissionDto();

        if (string.IsNullOrWhiteSpace(dto.OrganizationId)
            && document.RootElement.TryGetProperty("id", out var legacyId)
            && legacyId.ValueKind == JsonValueKind.String)
        {
            dto.OrganizationId = legacyId.GetString();
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
