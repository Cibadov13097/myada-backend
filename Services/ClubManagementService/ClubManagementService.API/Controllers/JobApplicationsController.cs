using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/job-applications")]
[ApiController]
[Tags("Job Applications")]
public class JobApplicationsController : ControllerBase
{
    private readonly IJobApplicationService _service;
    private readonly IImageStorageService _imageStorageService;

    public JobApplicationsController(IJobApplicationService service, IImageStorageService imageStorageService)
    {
        _service = service;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("by-vacancy/{jobVacancyId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByJobVacancyId(int jobVacancyId) =>
        Ok(await _service.GetByJobVacancyIdAsync(jobVacancyId));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateJobApplicationRequest request)
    {
        var applicantId = GetCurrentUserIdOrDefault();
        if (applicantId == Guid.Empty)
            return Unauthorized();

        var uploadedCvUrl = await UploadCvIfPresentAsync(request.JobVacancyId, request.CvFile);
        if (uploadedCvUrl == string.Empty)
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Unsupported CV file type." });
        if (string.IsNullOrWhiteSpace(uploadedCvUrl))
            return BadRequest(new { error = "VALIDATION_ERROR", message = "cvFile is required." });

        var dto = new CreateJobApplicationDto
        {
            JobVacancyId = request.JobVacancyId,
            ApplicantId = applicantId,
            ExperienceLinks = request.ExperienceLinks,
            FileUrl = uploadedCvUrl!
        };

        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), id = created.Id }, created);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateJobApplicationStatusDto dto)
    {
        var updated = await _service.UpdateStatusAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    private async Task<string?> UploadCvIfPresentAsync(int vacancyId, IFormFile? file)
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
            $"vacancies/{vacancyId}/applications/cv",
            HttpContext.RequestAborted);
    }

    public sealed class CreateJobApplicationRequest
    {
        public int JobVacancyId { get; set; }
        public string ExperienceLinks { get; set; } = string.Empty;
        public IFormFile? CvFile { get; set; }
    }
}
