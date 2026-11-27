using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.Services.Interfaces;

public interface ILessonService
{
    Task<IEnumerable<LessonDto>> GetMyLessonsAsync(Guid instructorId);

    Task<LessonDto> GetMyLessonByIdAsync(Guid instructorId, int lessonId);

    Task<IEnumerable<LessonDto>> GetAllLessonsAsync();

    Task<LessonDto> GetLessonByIdAsync(int lessonId);

    Task<IEnumerable<LessonDto>> GetLessonsByCourseIdAsync(int courseId);

    Task<IEnumerable<LessonDto>> GetLessonsByAcademicTermAsync(int academicYear, AcademicSemester semester);

    /// <summary>Creates a lesson for an existing course; <see cref="CreateLessonDto.CourseId"/> is required. <see cref="CreateLessonDto.InstructorId"/> and <see cref="CreateLessonDto.RoomId"/> are optional (null = unassigned).</summary>
    Task<LessonDto> CreateLessonAsync(CreateLessonDto dto);

    /// <summary>Excel workbook users fill in and upload to create lessons in bulk.</summary>
    Task<byte[]> GenerateBulkCreateTemplateAsync();

    /// <summary>
    /// Creates lessons from a filled bulk template. If any row is invalid, nothing is created
    /// and <see cref="BulkCreateLessonsResultDto.Errors"/> lists every problem.
    /// </summary>
    Task<BulkCreateLessonsResultDto> CreateLessonsFromExcelAsync(Stream excelStream);

    Task<LessonDto> UpdateLessonAsync(int lessonId, UpdateLessonDto dto);

    Task DeleteLessonAsync(int lessonId);

    /// <summary>Lessons with scheduling fields for a term (enrollment count, instructor user id, display metadata).</summary>
    Task<IReadOnlyList<LessonSchedulingRow>> GetLessonsForSchedulingAsync(int academicYear, AcademicSemester semester);

    /// <summary>Suggests lesson sections for a course/term from student count, section count, and instructor slots. Does not persist.</summary>
    Task<GenerateLessonsPreviewDto> PreviewGeneratedLessonsAsync(int courseId, GenerateLessonsRequestDto dto);

    /// <summary>Creates the proposed lesson sections after the caller confirms (and optionally edits) a preview.</summary>
    Task<BulkCreateLessonsResultDto> CreateGeneratedLessonsAsync(int courseId, GenerateLessonsPreviewDto preview);
}