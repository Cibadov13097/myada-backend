using AutoWrapper.Filters;
using Microsoft.AspNetCore.Mvc;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.DTOs.Query;
using MyAdaAttendanceService.Application.Excel;
using MyAdaAttendanceService.Application.Query;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Web.Models;

namespace MyAdaAttendanceService.Web.Controllers;

[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    private readonly ILessonService _lessonService;
    private readonly ICourseService _courseService;
    private readonly ISessionService _sessionService;
    private readonly IAttendanceService _attendanceService;
    private readonly IAdminAttendanceService _adminAttendanceService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly IExternalUserDirectoryService _externalUserDirectoryService;

    public AdminController(
        ILessonService lessonService,
        ICourseService courseService,
        ISessionService sessionService,
        IAttendanceService attendanceService,
        IAdminAttendanceService adminAttendanceService,
        IEnrollmentService enrollmentService,
        IExternalUserDirectoryService externalUserDirectoryService)
    {
        _lessonService = lessonService;
        _courseService = courseService;
        _sessionService = sessionService;
        _attendanceService = attendanceService;
        _adminAttendanceService = adminAttendanceService;
        _enrollmentService = enrollmentService;
        _externalUserDirectoryService = externalUserDirectoryService;
    }

    [HttpPost("users/sync")]
    public async Task<IActionResult> SyncUsers([FromQuery] string[]? roles)
    {
        return await HandleAsync(() => _externalUserDirectoryService.SyncUsersAsync(roles));
    }

    [HttpGet("users/roles/{role}")]
    public async Task<IActionResult> GetUsersByRole(string role, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _externalUserDirectoryService.GetUsersByRoleAsync(role), query));
    }

    [HttpGet("users/roles/{role}/search")]
    public async Task<IActionResult> SearchUsersByRole(string role, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _externalUserDirectoryService.GetUsersByRoleAsync(role), query));
    }

    [HttpGet("users/roles/{role}/filter")]
    public async Task<IActionResult> FilterUsersByRole(
        string role,
        [FromQuery] ExternalUserDirectoryFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _externalUserDirectoryService.GetUsersByRoleAsync(role), query, filter));
    }

    [HttpGet("courses")]
    public async Task<IActionResult> GetAllCourses([FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _courseService.GetAllAsync(), query));
    }

    [HttpGet("courses/search")]
    public async Task<IActionResult> SearchCourses([FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _courseService.GetAllAsync(), query));
    }

    [HttpGet("courses/filter")]
    public async Task<IActionResult> FilterCourses(
        [FromQuery] CourseFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _courseService.GetAllAsync(), query, filter));
    }

    [HttpGet("courses/{courseId:int}")]
    public async Task<IActionResult> GetCourseById(int courseId)
    {
        return await HandleAsync(() => _courseService.GetByIdAsync(courseId));
    }

    [HttpPost("courses")]
    public async Task<IActionResult> CreateCourse([FromBody] CreateCourseDto dto)
    {
        return await HandleAsync(
            () => _courseService.CreateAsync(dto),
            course => Created($"/api/admin/courses/{course.Id}", course));
    }

    [HttpPut("courses/{courseId:int}")]
    public async Task<IActionResult> UpdateCourse(int courseId, [FromBody] UpdateCourseDto dto)
    {
        return await HandleAsync(() => _courseService.UpdateAsync(courseId, dto));
    }

    [HttpDelete("courses/{courseId:int}")]
    public async Task<IActionResult> DeleteCourse(int courseId)
    {
        return await HandleAsync(() => _courseService.DeleteAsync(courseId));
    }

    [HttpGet("courses/{courseId:int}/lessons")]
    public async Task<IActionResult> GetLessonsByCourse(int courseId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _lessonService.GetLessonsByCourseIdAsync(courseId), query));
    }

    [HttpGet("courses/{courseId:int}/lessons/search")]
    public async Task<IActionResult> SearchLessonsByCourse(int courseId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _lessonService.GetLessonsByCourseIdAsync(courseId), query));
    }

    [HttpGet("courses/{courseId:int}/lessons/filter")]
    public async Task<IActionResult> FilterLessonsByCourse(
        int courseId,
        [FromQuery] LessonFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _lessonService.GetLessonsByCourseIdAsync(courseId), query, filter));
    }

    /// <summary>
    /// Suggests lesson sections from student headcount, section count, and instructor slots.
    /// Does not create lessons. POST the returned payload to generate to persist after confirmation.
    /// </summary>
    [HttpPost("courses/{courseId:int}/lessons/generate/preview")]
    public async Task<IActionResult> PreviewGeneratedLessons(int courseId, [FromBody] GenerateLessonsRequestDto dto)
    {
        return await HandleAsync(() => _lessonService.PreviewGeneratedLessonsAsync(courseId, dto));
    }

    /// <summary>
    /// Creates the proposed lesson sections after the caller confirms a preview (fields may be edited).
    /// </summary>
    [HttpPost("courses/{courseId:int}/lessons/generate")]
    public async Task<IActionResult> CreateGeneratedLessons(int courseId, [FromBody] GenerateLessonsPreviewDto preview)
    {
        return await HandleAsync(
            () => _lessonService.CreateGeneratedLessonsAsync(courseId, preview),
            result => StatusCode(StatusCodes.Status201Created, result));
    }

    [HttpGet("instructors/{instructorId:guid}/lessons")]
    public async Task<IActionResult> GetLessonsByInstructor(Guid instructorId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _lessonService.GetMyLessonsAsync(instructorId), query));
    }

    [HttpGet("instructors/{instructorId:guid}/lessons/search")]
    public async Task<IActionResult> SearchLessonsByInstructor(Guid instructorId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _lessonService.GetMyLessonsAsync(instructorId), query));
    }

    [HttpGet("instructors/{instructorId:guid}/lessons/filter")]
    public async Task<IActionResult> FilterLessonsByInstructor(
        Guid instructorId,
        [FromQuery] LessonFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _lessonService.GetMyLessonsAsync(instructorId), query, filter));
    }

    [HttpGet("lessons")]
    public async Task<IActionResult> GetAllLessons([FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _lessonService.GetAllLessonsAsync(), query));
    }

    [HttpGet("lessons/search")]
    public async Task<IActionResult> SearchLessons([FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _lessonService.GetAllLessonsAsync(), query));
    }

    [HttpGet("lessons/filter")]
    public async Task<IActionResult> FilterLessons(
        [FromQuery] LessonFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _lessonService.GetAllLessonsAsync(), query, filter));
    }

    [HttpGet("lessons/by-term")]
    public async Task<IActionResult> GetLessonsByTerm(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _lessonService.GetLessonsByAcademicTermAsync(academicYear, semester), query));
    }

    [HttpGet("lessons/by-term/search")]
    public async Task<IActionResult> SearchLessonsByTerm(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _lessonService.GetLessonsByAcademicTermAsync(academicYear, semester), query));
    }

    [HttpGet("lessons/by-term/filter")]
    public async Task<IActionResult> FilterLessonsByTerm(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] LessonFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _lessonService.GetLessonsByAcademicTermAsync(academicYear, semester), query, filter));
    }

    /// <summary>Scheduling service: lesson id, instructor user id, enrollment count, times per week, and course code/title for one academic term.</summary>
    [HttpGet("lessons/scheduling")]
    public async Task<IActionResult> GetLessonsForScheduling(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _lessonService.GetLessonsForSchedulingAsync(academicYear, semester), query));
    }

    [HttpGet("lessons/scheduling/search")]
    public async Task<IActionResult> SearchLessonsForScheduling(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _lessonService.GetLessonsForSchedulingAsync(academicYear, semester), query));
    }

    [HttpGet("lessons/scheduling/filter")]
    public async Task<IActionResult> FilterLessonsForScheduling(
        [FromQuery] int academicYear,
        [FromQuery] AcademicSemester semester,
        [FromQuery] LessonSchedulingFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _lessonService.GetLessonsForSchedulingAsync(academicYear, semester), query, filter));
    }

    [HttpGet("lessons/{lessonId:int}")]
    public async Task<IActionResult> GetLessonById(int lessonId)
    {
        return await HandleAsync(() => _lessonService.GetLessonByIdAsync(lessonId));
    }

    /// <summary>Download an Excel template for bulk lesson create. Fill the Lessons sheet and upload it to POST /api/admin/lessons/bulk.</summary>
    [HttpGet("lessons/bulk/template")]
    [AutoWrapIgnore]
    [Produces(LessonBulkExcelWorkbook.ContentType)]
    public async Task<IActionResult> DownloadLessonBulkTemplate()
    {
        try
        {
            var bytes = await _lessonService.GenerateBulkCreateTemplateAsync();
            return File(bytes, LessonBulkExcelWorkbook.ContentType, LessonBulkExcelWorkbook.FileDownloadName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Upload a filled lesson bulk template (.xlsx). If any row is invalid, no lessons are created.</summary>
    [HttpPost("lessons/bulk")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5 * 1024 * 1024)]
    public async Task<IActionResult> BulkCreateLessons([FromForm] BulkLessonExcelUploadRequest request)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "An Excel file (.xlsx) is required. Download the template, fill it, and upload that file." });

        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = "File must be an .xlsx Excel workbook. Download the template and keep that format." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { message = "The Excel file must be 5 MB or smaller." });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _lessonService.CreateLessonsFromExcelAsync(stream);
            if (result.FailedCount > 0)
                return BadRequest(result);

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Office/registrar: create a lesson for an existing course (<see cref="CreateLessonDto.CourseId"/>). <see cref="CreateLessonDto.InstructorId"/> and <see cref="CreateLessonDto.RoomId"/> are optional (null = unassigned).</summary>
    [HttpPost("lessons")]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonDto dto)
    {
        return await HandleAsync(
            () => _lessonService.CreateLessonAsync(dto),
            lesson => Created($"/api/admin/lessons/{lesson.Id}", lesson));
    }

    [HttpPut("lessons/{lessonId:int}")]
    public async Task<IActionResult> UpdateLesson(int lessonId, [FromBody] UpdateLessonDto dto)
    {
        return await HandleAsync(() => _lessonService.UpdateLessonAsync(lessonId, dto));
    }

    [HttpDelete("lessons/{lessonId:int}")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        return await HandleAsync(() => _lessonService.DeleteLessonAsync(lessonId));
    }

    [HttpGet("lessons/{lessonId:int}/enrollments")]
    public async Task<IActionResult> GetEnrollmentsByLesson(int lessonId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _enrollmentService.GetEnrollmentsByLessonAsync(lessonId), query));
    }

    [HttpGet("lessons/{lessonId:int}/enrollments/search")]
    public async Task<IActionResult> SearchEnrollmentsByLesson(int lessonId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _enrollmentService.GetEnrollmentsByLessonAsync(lessonId), query));
    }

    [HttpGet("lessons/{lessonId:int}/enrollments/filter")]
    public async Task<IActionResult> FilterEnrollmentsByLesson(
        int lessonId,
        [FromQuery] EnrollmentFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _enrollmentService.GetEnrollmentsByLessonAsync(lessonId), query, filter));
    }

    [HttpPost("lessons/{lessonId:int}/enrollments")]
    public async Task<IActionResult> CreateEnrollment(int lessonId, [FromBody] CreateEnrollmentDto dto)
    {
        return await HandleAsync(
            () => _enrollmentService.CreateEnrollmentAsync(lessonId, dto),
            enrollment => Created($"/api/admin/lessons/{lessonId}/enrollments/{enrollment.Id}", enrollment));
    }

    [HttpGet("lessons/{lessonId:int}/sessions")]
    public async Task<IActionResult> GetSessionsByLesson(int lessonId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _sessionService.GetSessionsByLessonAdminAsync(lessonId), query));
    }

    [HttpGet("lessons/{lessonId:int}/sessions/search")]
    public async Task<IActionResult> SearchSessionsByLesson(int lessonId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _sessionService.GetSessionsByLessonAdminAsync(lessonId), query));
    }

    [HttpGet("lessons/{lessonId:int}/sessions/filter")]
    public async Task<IActionResult> FilterSessionsByLesson(
        int lessonId,
        [FromQuery] SessionFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _sessionService.GetSessionsByLessonAdminAsync(lessonId), query, filter));
    }

    /// <summary>Generate sessions between two dates from a weekly day/time pattern; skips duplicates.</summary>
    [HttpPost("lessons/{lessonId:int}/sessions/generate")]
    public async Task<IActionResult> GenerateSessionsForLesson(int lessonId, [FromBody] BulkGenerateSessionsDto dto)
    {
        return await HandleAsync(() => _sessionService.BulkGenerateSessionsAdminAsync(lessonId, dto));
    }

    [HttpGet("sessions/{sessionId:int}/attendance")]
    public async Task<IActionResult> GetSessionAttendance(int sessionId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSort(await _attendanceService.GetSessionAttendanceAdminAsync(sessionId), query));
    }

    [HttpGet("sessions/{sessionId:int}/attendance/search")]
    public async Task<IActionResult> SearchSessionAttendance(int sessionId, [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListSearch(await _attendanceService.GetSessionAttendanceAdminAsync(sessionId), query));
    }

    [HttpGet("sessions/{sessionId:int}/attendance/filter")]
    public async Task<IActionResult> FilterSessionAttendance(
        int sessionId,
        [FromQuery] AttendanceFilterDto filter,
        [FromQuery] ListQuery? query = null)
    {
        return await HandleAsync(async () =>
            ApplyListFilter(await _attendanceService.GetSessionAttendanceAdminAsync(sessionId), query, filter));
    }

    [HttpPatch("sessions/{sessionId:int}/attendance/{attendanceId:int}")]
    public async Task<IActionResult> CorrectAttendance(int sessionId, int attendanceId, [FromBody] AdminAttendanceCorrectionDto dto)
    {
        return await HandleAsync(() => _adminAttendanceService.FixAttendanceAsync(attendanceId, dto));
    }

    [HttpDelete("sessions/{sessionId:int}/attendance/{attendanceId:int}")]
    public async Task<IActionResult> DeleteAttendance(int sessionId, int attendanceId)
    {
        return await HandleAsync(() => _adminAttendanceService.DeleteAttendanceAsync(attendanceId));
    }
}
