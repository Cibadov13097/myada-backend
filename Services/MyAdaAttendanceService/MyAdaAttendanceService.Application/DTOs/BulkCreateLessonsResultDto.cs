namespace MyAdaAttendanceService.Application.DTOs;

public class BulkCreateLessonsResultDto
{
    public int TotalRows { get; set; }

    public int CreatedCount { get; set; }

    public int FailedCount { get; set; }

    public IReadOnlyList<LessonDto> Created { get; set; } = Array.Empty<LessonDto>();

    public IReadOnlyList<BulkLessonRowErrorDto> Errors { get; set; } = Array.Empty<BulkLessonRowErrorDto>();
}

public class BulkLessonRowErrorDto
{
    /// <summary>Excel row number (1-based, including the header row). 0 means a file-level error.</summary>
    public int RowNumber { get; set; }

    public string Message { get; set; } = string.Empty;
}
