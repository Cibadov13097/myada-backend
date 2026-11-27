using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.DTOs.Query;
using MyAdaAttendanceService.Application.Query;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/instructors/{instructorId:guid}/lessons")]
public class InstructorLessonsController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ISessionService _sessionService;

    public InstructorLessonsController(
        ILessonService lessonService,
        ISessionService sessionService)
    {
        _lessonService = lessonService;
        _sessionService = sessionService;
    }

    [HttpGet]
    public Task<IActionResult> GetMyLessons(Guid instructorId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListSort(await _lessonService.GetMyLessonsAsync(instructorId), query);
        });

    [HttpGet("search")]
    public Task<IActionResult> SearchMyLessons(Guid instructorId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListSearch(await _lessonService.GetMyLessonsAsync(instructorId), query);
        });

    [HttpGet("filter")]
    public Task<IActionResult> FilterMyLessons(
        Guid instructorId,
        [FromQuery] LessonFilterDto filter,
        [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListFilter(await _lessonService.GetMyLessonsAsync(instructorId), query, filter);
        });

    [HttpGet("{lessonId:int}")]
    public Task<IActionResult> GetMyLesson(Guid instructorId, int lessonId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _lessonService.GetMyLessonByIdAsync(instructorId, lessonId);
        });

    [HttpGet("{lessonId:int}/sessions")]
    public Task<IActionResult> GetSessions(Guid instructorId, int lessonId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListSort(await _sessionService.GetSessionsByLessonAsync(instructorId, lessonId), query);
        });

    [HttpGet("{lessonId:int}/sessions/search")]
    public Task<IActionResult> SearchSessions(Guid instructorId, int lessonId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListSearch(await _sessionService.GetSessionsByLessonAsync(instructorId, lessonId), query);
        });

    [HttpGet("{lessonId:int}/sessions/filter")]
    public Task<IActionResult> FilterSessions(
        Guid instructorId,
        int lessonId,
        [FromQuery] SessionFilterDto filter,
        [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return ApplyListFilter(await _sessionService.GetSessionsByLessonAsync(instructorId, lessonId), query, filter);
        });

    [HttpGet("{lessonId:int}/sessions/{sessionId:int}")]
    public Task<IActionResult> GetSession(Guid instructorId, int lessonId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _sessionService.GetSessionByIdAsync(instructorId, sessionId);
        });

    [HttpPost("{lessonId:int}/sessions")]
    public Task<IActionResult> CreateSession(Guid instructorId, int lessonId, [FromBody] CreateSessionDto dto) =>
        HandleAsync(
            async () =>
            {
                EnsureRouteUserMatchesClaim(instructorId);
                return await _sessionService.CreateSessionAsync(instructorId, lessonId, dto);
            },
            session => CreatedAtAction(nameof(GetSession), new { instructorId, lessonId, sessionId = session.Id }, session));

    /// <summary>Generate sessions between two dates for each listed weekday and time window; existing same date/time rows are skipped.</summary>
    [HttpPost("{lessonId:int}/sessions/generate")]
    public Task<IActionResult> GenerateSessions(Guid instructorId, int lessonId, [FromBody] BulkGenerateSessionsDto dto) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _sessionService.BulkGenerateSessionsAsync(instructorId, lessonId, dto);
        });

    [HttpPatch("{lessonId:int}/sessions/{sessionId:int}")]
    public Task<IActionResult> UpdateSession(Guid instructorId, int lessonId, int sessionId, [FromBody] UpdateSessionDto dto) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            return await _sessionService.UpdateSessionAsync(instructorId, sessionId, dto);
        });

    [HttpDelete("{lessonId:int}/sessions/{sessionId:int}")]
    public Task<IActionResult> DeleteSession(Guid instructorId, int lessonId, int sessionId) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(instructorId);
            await _sessionService.DeleteSessionAsync(instructorId, sessionId);
        });
}
