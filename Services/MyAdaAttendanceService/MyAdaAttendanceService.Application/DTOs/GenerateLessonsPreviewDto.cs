using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs;

public class ProposedLessonDto
{
    /// <summary>1-based section number in this generated plan. Not stored on the lesson entity.</summary>
    public int SectionNumber { get; set; }

    public int CourseId { get; set; }
    public Guid? InstructorId { get; set; }
    public string InstructorDisplayName { get; set; } = string.Empty;
    public string InstructorEmail { get; set; } = string.Empty;
    public int? RoomId { get; set; }
    public int AcademicYear { get; set; }
    public AcademicSemester Semester { get; set; }
    public int MaxCapacity { get; set; }
}

public class GenerateLessonsPreviewDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int AcademicYear { get; set; }
    public AcademicSemester Semester { get; set; }
    public int TotalStudents { get; set; }
    public int SectionCount { get; set; }
    public int InstructorCount { get; set; }
    public int TotalSeats { get; set; }
    public int ExistingLessonCountForTerm { get; set; }
    public IReadOnlyList<Guid?> InstructorSlots { get; set; } = Array.Empty<Guid?>();
    public IReadOnlyList<ProposedLessonDto> ProposedLessons { get; set; } = Array.Empty<ProposedLessonDto>();
}
