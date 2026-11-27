namespace MyAdaAttendanceService.Web.Models;

public class BulkLessonExcelUploadRequest
{
    /// <summary>Filled lesson bulk-create template (.xlsx) downloaded from GET /api/admin/lessons/bulk/template.</summary>
    public IFormFile File { get; set; } = null!;
}
