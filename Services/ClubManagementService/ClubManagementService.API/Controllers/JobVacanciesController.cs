using ClubManagementService.Application.DTOs.JobVacancy;
using ClubManagementService.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace ClubManagementService.API.Controllers;

[Route("api/v1/job-vacancies")]
[ApiController]
[Tags("Job Vacancies")]
public class JobVacanciesController : ControllerBase
{
    private readonly IJobVacancyService _service;

    public JobVacanciesController(IJobVacancyService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("by-club/{clubId:int}")]
    public async Task<IActionResult> GetByClubId(int clubId) =>
        Ok(await _service.GetByClubIdAsync(clubId));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var dto = await _service.GetByIdAsync(id);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobVacancyDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateJobVacancyDto dto)
    {
        var updated = await _service.UpdateAsync(id, dto);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
