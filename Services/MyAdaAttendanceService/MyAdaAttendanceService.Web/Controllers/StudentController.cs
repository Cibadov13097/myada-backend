using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.DTOs.Query;
using MyAdaAttendanceService.Application.Query;
using MyAdaAttendanceService.Application.Services.Interfaces;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/students/{studentId:guid}")]
public class StudentController : ApiControllerBase
{
    private readonly IStudentAttendanceService _studentAttendanceService;
    private readonly IAttendanceService _attendanceService;

    public StudentController(
        IStudentAttendanceService studentAttendanceService,
        IAttendanceService attendanceService)
    {
        _studentAttendanceService = studentAttendanceService;
        _attendanceService = attendanceService;
    }

    [HttpGet("enrollments")]
    public Task<IActionResult> GetMyEnrollments(Guid studentId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListSort(await _studentAttendanceService.GetMyLessonsAsync(studentId), query);
        });

    [HttpGet("enrollments/search")]
    public Task<IActionResult> SearchMyEnrollments(Guid studentId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListSearch(await _studentAttendanceService.GetMyLessonsAsync(studentId), query);
        });

    [HttpGet("enrollments/filter")]
    public Task<IActionResult> FilterMyEnrollments(
        Guid studentId,
        [FromQuery] StudentLessonFilterDto filter,
        [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListFilter(await _studentAttendanceService.GetMyLessonsAsync(studentId), query, filter);
        });

    [HttpGet("lessons/{lessonId:int}/attendance")]
    public Task<IActionResult> GetMyAttendanceByLesson(Guid studentId, int lessonId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListSort(await _studentAttendanceService.GetMyAttendanceByLessonAsync(studentId, lessonId), query);
        });

    [HttpGet("lessons/{lessonId:int}/attendance/search")]
    public Task<IActionResult> SearchMyAttendanceByLesson(Guid studentId, int lessonId, [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListSearch(await _studentAttendanceService.GetMyAttendanceByLessonAsync(studentId, lessonId), query);
        });

    [HttpGet("lessons/{lessonId:int}/attendance/filter")]
    public Task<IActionResult> FilterMyAttendanceByLesson(
        Guid studentId,
        int lessonId,
        [FromQuery] StudentAttendanceFilterDto filter,
        [FromQuery] ListQuery? query = null) =>
        HandleAsync(async () =>
        {
            EnsureRouteUserMatchesClaim(studentId);
            return ApplyListFilter(await _studentAttendanceService.GetMyAttendanceByLessonAsync(studentId, lessonId), query, filter);
        });

    [HttpPost("attendance/scan")]
    public Task<IActionResult> ScanAttendance(Guid studentId, [FromBody] QrScanRequestDto dto) =>
        HandleAsync(async () =>
        {
            if (dto.StudentId == Guid.Empty)
                dto.StudentId = studentId;
            else if (dto.StudentId != studentId)
                throw new ArgumentException("Route student id and payload student id must match.");

            EnsureAuthenticatedUserMatches(dto.StudentId);
            return await _attendanceService.MarkAttendanceByQrAsync(dto);
        });

    // Alias for clients still using /attendance/qr/scan
    [HttpPost("attendance/qr/scan")]
    public Task<IActionResult> ScanAttendanceQr(Guid studentId, [FromBody] QrScanRequestDto dto) =>
        ScanAttendance(studentId, dto);
}
