using ClubManagementService.Application.DTOs.JobApplication;
using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/vacancies")]
[ApiController]
[Authorize]
[Tags("Vacancies")]
public class JobVacanciesController : ControllerBase
{
    private readonly IJobVacancyService _vacancyService;
    private readonly IJobApplicationService _applicationService;

    public JobVacanciesController(IJobVacancyService vacancyService, IJobApplicationService applicationService)
    {
        _vacancyService = vacancyService;
        _applicationService = applicationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] string? category = null, [FromQuery] bool savedOnly = false, [FromQuery] int page = 1, [FromQuery] int limit = 12)
    {
        var vacancies = await _vacancyService.GetAllAsync();
        var filtered = vacancies.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
            filtered = filtered.Where(v => v.Title.Contains(search, StringComparison.OrdinalIgnoreCase));

        var total = filtered.Count();
        var items = filtered.Skip(Math.Max(page - 1, 0) * Math.Max(limit, 1)).Take(Math.Max(limit, 1));
        return Ok(new { items, total, page, limit });
    }

    [HttpGet("{vacancyId:int}")]
    public async Task<IActionResult> GetById(int vacancyId)
    {
        var dto = await _vacancyService.GetByIdAsync(vacancyId);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost("{vacancyId:int}/applications")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ApplyToVacancy(int vacancyId, [FromForm] VacancyApplicationRequest request)
    {
        var created = await _applicationService.CreateAsync(new CreateJobApplicationDto
        {
            JobVacancyId = vacancyId,
            ApplicantId = GetCurrentUserIdOrDefault(),
            ExperienceLinks = request.PurposeOfApplication,
            FileUrl = request.CvFile?.FileName ?? string.Empty
        });

        return Ok(new { applicationId = $"APP-{created.Id}", status = "Submitted" });
    }

    [HttpGet("by-club/{clubId:int}")]
    public async Task<IActionResult> GetByClubId(int clubId) => Ok(await _vacancyService.GetByClubIdAsync(clubId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobVacancyDto dto)
    {
        var created = await _vacancyService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { vacancyId = created.Id }, created);
    }

    [HttpPatch("{vacancyId:int}")]
    public async Task<IActionResult> Update(int vacancyId, [FromBody] UpdateJobVacancyDto dto)
    {
        var updated = await _vacancyService.UpdateAsync(vacancyId, dto);
        return Ok(updated);
    }

    [HttpDelete("{vacancyId:int}")]
    public async Task<IActionResult> Delete(int vacancyId)
    {
        await _vacancyService.DeleteAsync(vacancyId);
        return NoContent();
    }

    private Guid GetCurrentUserIdOrDefault()
    {
        var claim = User.FindFirst("sub")?.Value ?? User.FindFirst("user_id")?.Value;
        return Guid.TryParse(claim, out var value) ? value : Guid.Empty;
    }

    public sealed class VacancyApplicationRequest
    {
        public string PurposeOfApplication { get; set; } = string.Empty;
        public IFormFile? CvFile { get; set; }
    }
}
