using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs;

public class CreateLessonDto
{
    /// <summary>Existing course this lesson belongs to (create the course via <c>POST /api/admin/courses</c> first).</summary>
    public int CourseId { get; set; }

    /// <summary>Assigned instructor for this lesson (optional; null means unassigned). Not derived from the caller.</summary>
    public Guid? InstructorId { get; set; }

    /// <summary>Assigned room (optional; null means unassigned).</summary>
    public int? RoomId { get; set; }

    /// <summary>Calendar year for this term (e.g. 2026 for Spring 2026).</summary>
    public int AcademicYear { get; set; }

    public AcademicSemester Semester { get; set; }

    /// <summary>Maximum enrollments allowed for this lesson.</summary>
    public int MaxCapacity { get; set; }
}
