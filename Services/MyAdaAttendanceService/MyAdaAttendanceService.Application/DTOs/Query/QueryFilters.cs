using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.DTOs.Query;

public class CourseFilterDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Department { get; set; }
    public string? Code { get; set; }
    public int? Credits { get; set; }
    public int? TimesPerWeek { get; set; }
}

public class LessonFilterDto
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Code { get; set; }
    public Guid? InstructorId { get; set; }
    public string? InstructorDisplayName { get; set; }
    public string? InstructorEmail { get; set; }
    public int? RoomId { get; set; }
    public int? AcademicYear { get; set; }
    public AcademicSemester? Semester { get; set; }
    public string? CRN { get; set; }
    public int? MaxCapacity { get; set; }
    public int? TimesPerWeek { get; set; }
}

public class LessonSchedulingFilterDto
{
    public int? LessonId { get; set; }
    public Guid? InstructorUserId { get; set; }
    public int? Enrollment { get; set; }
    public int? MaxCapacity { get; set; }
    public int? TimesPerWeek { get; set; }
    public string? CourseCode { get; set; }
    public string? CourseTitle { get; set; }
}

public class EnrollmentFilterDto
{
    public int? Id { get; set; }
    public int? LessonId { get; set; }
    public Guid? StudentId { get; set; }
    public string? StudentFullName { get; set; }
    public string? StudentCode { get; set; }
    public string? StudentEmail { get; set; }
}

public class SessionFilterDto
{
    public int? Id { get; set; }
    public int? LessonId { get; set; }
    public string? LessonName { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Topic { get; set; }
    public bool? IsAttendanceActive { get; set; }
    public byte? RoundNo { get; set; }
    public DateTime? AttendanceActivatedAt { get; set; }
    public DateTime? AttendanceDeactivatedAt { get; set; }
    public int? TotalStudents { get; set; }
    public int? PresentCount { get; set; }
    public int? LateCount { get; set; }
    public int? AbsentCount { get; set; }
    public int? ExcusedCount { get; set; }
}

public class AttendanceFilterDto
{
    public int? Id { get; set; }
    public int? SessionId { get; set; }
    public int? LessonId { get; set; }
    public Guid? StudentId { get; set; }
    public string? StudentFullName { get; set; }
    public string? StudentCode { get; set; }
    public string? Status { get; set; }
    public DateTime? MarkedAt { get; set; }
    public string? MarkedSource { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
    public DateTime? FirstScanAt { get; set; }
    public DateTime? LastScanAt { get; set; }
    public bool? IsManuallyAdjusted { get; set; }
    public string? InstructorNote { get; set; }
}

public class ExternalUserDirectoryFilterDto
{
    public Guid? UserId { get; set; }
    public string? Role { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? UserType { get; set; }
    public string? Status { get; set; }
    public DateTime? SyncedAtUtc { get; set; }
}

public class StudentLessonFilterDto
{
    public int? LessonId { get; set; }
    public string? LessonName { get; set; }
    public string? LessonCode { get; set; }
    public int? TotalSessions { get; set; }
    public int? PresentCount { get; set; }
    public int? LateCount { get; set; }
    public int? AbsentCount { get; set; }
    public int? ExcusedCount { get; set; }
}

public class StudentAttendanceFilterDto
{
    public int? AttendanceId { get; set; }
    public int? SessionId { get; set; }
    public DateTime? SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public string? LessonName { get; set; }
    public string? LessonCode { get; set; }
    public string? Status { get; set; }
    public DateTime? FirstScanAt { get; set; }
    public DateTime? LastScanAt { get; set; }
    public bool? IsManuallyAdjusted { get; set; }
    public string? InstructorNote { get; set; }
}
