using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs;

public class GenerateLessonsRequestDto
{
    /// <summary>How many students will take this course in the term.</summary>
    public int TotalStudents { get; set; }

    /// <summary>Calendar year for the term (e.g. 2026 for Spring 2026).</summary>
    public int AcademicYear { get; set; }

    public AcademicSemester Semester { get; set; }

    /// <summary>How many lesson sections to create.</summary>
    public int SectionCount { get; set; }

    /// <summary>
    /// How many instructors will teach this course over the term (example: 3).
    /// The <see cref="Instructors"/> list is padded or truncated to this length.
    /// </summary>
    public int InstructorCount { get; set; }

    /// <summary>
    /// Instructor auth user ids, one per instructor slot. Use <c>null</c> (or omit a trailing slot)
    /// when that instructor will be assigned later. Empty GUID is treated as unassigned.
    /// </summary>
    public IReadOnlyList<Guid?>? Instructors { get; set; }
}
