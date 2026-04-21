using Asp.Versioning;
using ClubManagementService.API.Extensions;
using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[ApiVersion(1)]
[Route("api/v{version:apiVersion}/vacancies")]
[ApiController]
[Authorize]
[Tags("Vacancies")]
public class JobVacanciesController : ControllerBase
{
    private readonly IJobVacancyService _vacancyService;
    private readonly IJobApplicationService _applicationService;
    private readonly IImageStorageService _imageStorageService;

    public JobVacanciesController(
        IJobVacancyService vacancyService,
        IJobApplicationService applicationService,
        IImageStorageService imageStorageService)
    {
        _vacancyService = vacancyService;
        _applicationService = applicationService;
        _imageStorageService = imageStorageService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] bool savedOnly = false, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        _ = category;
        _ = savedOnly;
        var vacancies = await _vacancyService.GetAllAsync();
        var filtered = vacancies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(v => v.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        var materialized = filtered.ToList();
        var total = materialized.Count;
        var items = materialized.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1));
        return Ok(new { items, total, page, limit });
    }

    [HttpGet("{vacancyId}")]
    public async Task<IActionResult> GetById([FromRoute] string vacancyId)
    {
        if (!int.TryParse(vacancyId, out var id) || id <= 0)
            return BadRequest();

        var dto = await _vacancyService.GetByIdAsync(id);
        if (dto == null || dto.IsDraft || !dto.IsActive)
            return NotFound();

        return Ok(dto);
    }

    [HttpPost("{vacancyId}/applications")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ApplyToVacancy([FromRoute] string vacancyId, [FromForm] VacancyApplicationRequest request)
    {
        if (!int.TryParse(vacancyId, out var id) || id <= 0)
            return BadRequest();

        if (string.IsNullOrWhiteSpace(request.PurposeOfApplication))
            return BadRequest(new { error = "purposeOfApplication is required." });

        var vacancy = await _vacancyService.GetByIdAsync(id);
        if (vacancy == null || !vacancy.IsActive)
            return NotFound();

        var cvUrl = await UploadCvIfPresentAsync(id, request.CvFile);
        if (cvUrl == string.Empty)
            return BadRequest(new { error = "VALIDATION_ERROR", message = "Unsupported CV file type." });

        var created = await _applicationService.CreateAsync(new CreateJobApplicationDto
        {
            JobVacancyId = id,
            ApplicantId = GetCurrentUserIdOrDefault(),
            ExperienceLinks = request.PurposeOfApplication.Trim(),
            FileUrl = cvUrl ?? string.Empty
        });

        return Ok(new { applicationId = $"job:{created.Id}", status = "Submitted", cvUrl });
    }

    [HttpGet("by-club/{clubId:int}")]
    public async Task<IActionResult> GetByClubId(int clubId, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var vacancies = await _vacancyService.GetByClubIdAsync(clubId, activeOnly: true);
        var total = vacancies.Count;
        var items = vacancies.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1));
        return Ok(new { items, total, page, limit });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobVacancyDto dto)
    {
        var created = await _vacancyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { version = HttpContext.GetClubApiMajorVersion(), vacancyId = created.Id.ToString() }, created);
    }

    [HttpPatch("{vacancyId}")]
    public async Task<IActionResult> Update([FromRoute] string vacancyId, [FromBody] UpdateJobVacancyDto dto)
    {
        if (!int.TryParse(vacancyId, out var id) || id <= 0)
            return BadRequest();

        var updated = await _vacancyService.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{vacancyId}")]
    public async Task<IActionResult> Delete([FromRoute] string vacancyId)
    {
        if (!int.TryParse(vacancyId, out var id) || id <= 0)
            return BadRequest();

        await _vacancyService.DeleteAsync(id);
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

    public sealed class VacancyApplicationRequest
    {
        public string PurposeOfApplication { get; set; } = string.Empty;
        public IFormFile? CvFile { get; set; }
    }
}
