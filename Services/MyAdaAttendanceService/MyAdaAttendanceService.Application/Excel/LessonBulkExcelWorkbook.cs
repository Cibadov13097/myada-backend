using System.Globalization;
using ClosedXML.Excel;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.Excel;

public static class LessonBulkExcelWorkbook
{
    public const string LessonsSheetName = "Lessons";
    public const string InstructionsSheetName = "Instructions";
    public const string CoursesSheetName = "Courses";
    public const string InstructorsSheetName = "Instructors";
    public const int MaxDataRows = 500;
    public const string FileDownloadName = "lesson-bulk-create-template.xlsx";
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static readonly string[] Headers =
    [
        "CourseId",
        "InstructorId",
        "RoomId",
        "AcademicYear",
        "Semester",
        "MaxCapacity"
    ];

    public static byte[] BuildTemplate(
        IEnumerable<Course> courses,
        IEnumerable<ExternalUserDirectoryDto> instructors)
    {
        using var workbook = new XLWorkbook();
        AddLessonsSheet(workbook);
        AddInstructionsSheet(workbook);
        AddCoursesSheet(workbook, courses);
        AddInstructorsSheet(workbook, instructors);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static IReadOnlyList<ParsedLessonExcelRow> Parse(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault(w =>
                            string.Equals(w.Name, LessonsSheetName, StringComparison.OrdinalIgnoreCase))
                        ?? throw new ArgumentException(
                            $"The uploaded file is not a valid lesson bulk template. Expected a '{LessonsSheetName}' sheet.");

            var headerRow = sheet.Row(1);
            var columnByHeader = MapHeaderColumns(headerRow);

            var lastRow = Math.Max(sheet.LastRowUsed()?.RowNumber() ?? 1, 1);
            if (lastRow - 1 > MaxDataRows)
                throw new ArgumentException($"A maximum of {MaxDataRows} lessons can be created per upload.");

            var rows = new List<ParsedLessonExcelRow>();
            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var parsed = ParseDataRow(sheet.Row(rowNumber), columnByHeader);
                if (parsed is not null)
                    rows.Add(parsed);
            }

            return rows;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new ArgumentException(
                "The uploaded file could not be read as an Excel workbook (.xlsx). Download the template and upload that file.");
        }
    }

    private static void AddLessonsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(LessonsSheetName);
        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];

        var headerRange = sheet.Range(1, 1, 1, Headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        headerRange.Style.Font.FontColor = XLColor.White;
        sheet.SheetView.FreezeRows(1);
        headerRange.SetAutoFilter();

        sheet.Column(1).Width = 14;
        sheet.Column(2).Width = 42;
        sheet.Column(3).Width = 12;
        sheet.Column(4).Width = 16;
        sheet.Column(5).Width = 14;
        sheet.Column(6).Width = 16;

        sheet.Column(1).Style.NumberFormat.Format = "0";
        sheet.Column(2).Style.NumberFormat.Format = "@";
        sheet.Column(3).Style.NumberFormat.Format = "0";
        sheet.Column(4).Style.NumberFormat.Format = "0";
        sheet.Column(6).Style.NumberFormat.Format = "0";

        sheet.Cell(1, 1).GetComment().AddText("Required. Use an Id from the Courses sheet.");
        sheet.Cell(1, 2).GetComment().AddText("Optional. Leave blank if unassigned. Use a UserId from the Instructors sheet.");
        sheet.Cell(1, 3).GetComment().AddText("Optional. Leave blank if unassigned. Integer 0 or greater when set.");
        sheet.Cell(1, 4).GetComment().AddText("Required. Calendar year for the term, e.g. 2026. Must be between 2000 and 2100.");
        sheet.Cell(1, 5).GetComment().AddText("Required. Fall, Spring, or Summer.");
        sheet.Cell(1, 6).GetComment().AddText("Required. Maximum enrollments allowed (0 or greater).");

        var semesterRange = sheet.Range(2, 5, MaxDataRows + 1, 5);
        var validation = semesterRange.CreateDataValidation();
        validation.AllowedValues = XLAllowedValues.List;
        validation.List("\"Fall,Spring,Summer\"", true);
        validation.IgnoreBlanks = true;
        validation.ShowErrorMessage = true;
        validation.ErrorTitle = "Invalid semester";
        validation.ErrorMessage = "Semester must be Fall, Spring, or Summer.";
    }

    private static void AddInstructionsSheet(XLWorkbook workbook)
    {
        var sheet = workbook.AddWorksheet(InstructionsSheetName);
        sheet.Column(1).Width = 110;
        sheet.Cell(1, 1).Value = "How to bulk-create lessons";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        var lines = new[]
        {
            "1. Keep the Lessons sheet headers unchanged (CourseId, InstructorId, RoomId, AcademicYear, Semester, MaxCapacity).",
            "2. Fill one lesson per row starting at row 2. Completely empty rows are ignored.",
            "3. CourseId must match an existing course. Copy the Id from the Courses sheet.",
            "4. InstructorId is optional. Leave the cell blank to create an unassigned lesson. Do not use an empty GUID.",
            "5. RoomId is optional. Leave the cell blank if no room is assigned yet. When set, it must be an integer 0 or greater.",
            "6. When assigning an instructor, paste the UserId from the Instructors sheet (keep the cell as text).",
            "7. Semester must be Fall, Spring, or Summer (a dropdown is provided). Numeric 1/2/3 is also accepted.",
            "8. AcademicYear is the calendar year of the term (for example 2026 for Spring 2026) and must be between 2000 and 2100.",
            "9. MaxCapacity must be an integer greater than or equal to 0.",
            "10. Do not send CRN. The server generates it after a successful upload.",
            $"11. Upload at most {MaxDataRows} lesson rows per file.",
            "12. If any row is invalid, no lessons are created. Fix the reported rows and upload again.",
            "13. Save as .xlsx (Excel workbook). CSV and older .xls files are not accepted.",
            string.Empty,
            "Example row:",
            "CourseId=12 | InstructorId=(blank or a GUID) | RoomId=(blank or 401) | AcademicYear=2026 | Semester=Spring | MaxCapacity=35"
        };

        for (var i = 0; i < lines.Length; i++)
            sheet.Cell(i + 3, 1).Value = lines[i];
    }

    private static void AddCoursesSheet(XLWorkbook workbook, IEnumerable<Course> courses)
    {
        var sheet = workbook.AddWorksheet(CoursesSheetName);
        WriteHeader(sheet, ["Id", "Code", "Name", "Department", "Credits", "TimesPerWeek"]);
        var row = 2;
        foreach (var course in courses)
        {
            sheet.Cell(row, 1).Value = course.Id;
            sheet.Cell(row, 2).Value = course.Code;
            sheet.Cell(row, 3).Value = course.Name;
            sheet.Cell(row, 4).Value = course.Department;
            sheet.Cell(row, 5).Value = course.Credits;
            sheet.Cell(row, 6).Value = course.TimesPerWeek;
            row++;
        }

        sheet.Columns().AdjustToContents(1, 40);
        sheet.SheetView.FreezeRows(1);
    }

    private static void AddInstructorsSheet(XLWorkbook workbook, IEnumerable<ExternalUserDirectoryDto> instructors)
    {
        var sheet = workbook.AddWorksheet(InstructorsSheetName);
        WriteHeader(sheet, ["UserId", "DisplayName", "Email", "UserName"]);
        var row = 2;
        foreach (var instructor in instructors)
        {
            sheet.Cell(row, 1).Value = instructor.UserId.ToString();
            sheet.Cell(row, 1).Style.NumberFormat.Format = "@";
            sheet.Cell(row, 2).Value = instructor.DisplayName;
            sheet.Cell(row, 3).Value = instructor.Email;
            sheet.Cell(row, 4).Value = instructor.UserName;
            row++;
        }

        sheet.Column(1).Width = 42;
        sheet.Column(2).Width = 28;
        sheet.Column(3).Width = 32;
        sheet.Column(4).Width = 24;
        sheet.SheetView.FreezeRows(1);
    }

    private static void WriteHeader(IXLWorksheet sheet, string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRange = sheet.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
        headerRange.Style.Font.FontColor = XLColor.White;
        headerRange.SetAutoFilter();
    }

    private static Dictionary<string, int> MapHeaderColumns(IXLRow headerRow)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var lastColumn = Math.Max(headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0, Headers.Length);
        for (var column = 1; column <= lastColumn; column++)
        {
            var header = NormalizeHeader(GetCellText(headerRow.Cell(column)));
            if (header.Length == 0)
                continue;
            map[header] = column;
        }

        var missing = Headers.Where(h => !map.ContainsKey(h)).ToArray();
        if (missing.Length > 0)
        {
            throw new ArgumentException(
                "The Lessons sheet is missing required header(s): "
                + string.Join(", ", missing)
                + ". Download a fresh template and keep the header row unchanged.");
        }

        return map;
    }

    private static ParsedLessonExcelRow? ParseDataRow(IXLRow row, IReadOnlyDictionary<string, int> columns)
    {
        var courseRaw = GetCellText(row.Cell(columns["CourseId"]));
        var instructorRaw = GetCellText(row.Cell(columns["InstructorId"]));
        var roomRaw = GetCellText(row.Cell(columns["RoomId"]));
        var yearRaw = GetCellText(row.Cell(columns["AcademicYear"]));
        var semesterRaw = GetCellText(row.Cell(columns["Semester"]));
        var capacityRaw = GetCellText(row.Cell(columns["MaxCapacity"]));

        if (IsBlank(courseRaw, instructorRaw, roomRaw, yearRaw, semesterRaw, capacityRaw))
            return null;

        var errors = new List<string>();
        var dto = new CreateLessonDto();

        if (!TryParseInt(courseRaw, out var courseId) || courseId <= 0)
            errors.Add("CourseId is required and must be a positive integer.");
        else
            dto.CourseId = courseId;

        if (string.IsNullOrWhiteSpace(instructorRaw))
        {
            dto.InstructorId = null;
        }
        else if (!Guid.TryParse(instructorRaw, out var instructorId))
        {
            errors.Add("InstructorId must be a valid GUID or left blank.");
        }
        else if (instructorId == Guid.Empty)
        {
            errors.Add("InstructorId cannot be empty. Leave the cell blank if unassigned.");
        }
        else
        {
            dto.InstructorId = instructorId;
        }

        if (string.IsNullOrWhiteSpace(roomRaw))
        {
            dto.RoomId = null;
        }
        else if (!TryParseInt(roomRaw, out var roomId) || roomId < 0)
        {
            errors.Add("RoomId must be an integer greater than or equal to 0, or left blank.");
        }
        else
        {
            dto.RoomId = roomId;
        }

        if (!TryParseInt(yearRaw, out var academicYear))
            errors.Add("AcademicYear is required and must be an integer between 2000 and 2100.");
        else if (academicYear is < 2000 or > 2100)
            errors.Add("Academic year must be between 2000 and 2100.");
        else
            dto.AcademicYear = academicYear;

        if (!TryParseSemester(semesterRaw, out var semester))
            errors.Add("Semester must be Fall, Spring, or Summer.");
        else
            dto.Semester = semester;

        if (!TryParseInt(capacityRaw, out var maxCapacity) || maxCapacity < 0)
            errors.Add("MaxCapacity is required and must be an integer greater than or equal to 0.");
        else
            dto.MaxCapacity = maxCapacity;

        if (errors.Count > 0)
        {
            return new ParsedLessonExcelRow
            {
                RowNumber = row.RowNumber(),
                Error = string.Join(" ", errors)
            };
        }

        return new ParsedLessonExcelRow
        {
            RowNumber = row.RowNumber(),
            Dto = dto
        };
    }

    private static string NormalizeHeader(string value) =>
        value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

    private static bool IsBlank(params string[] values) =>
        values.All(string.IsNullOrWhiteSpace);

    private static bool TryParseInt(string raw, out int value)
    {
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            && Math.Abs(number - Math.Truncate(number)) < double.Epsilon
            && number >= int.MinValue
            && number <= int.MaxValue)
        {
            value = (int)number;
            return true;
        }

        value = 0;
        return false;
    }

    private static bool TryParseSemester(string raw, out AcademicSemester semester)
    {
        semester = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        if (Enum.TryParse(raw, ignoreCase: true, out semester) && Enum.IsDefined(semester))
            return true;

        if (TryParseInt(raw, out var numeric) && Enum.IsDefined(typeof(AcademicSemester), numeric))
        {
            semester = (AcademicSemester)numeric;
            return true;
        }

        return false;
    }

    private static string GetCellText(IXLCell cell)
    {
        if (cell.IsEmpty())
            return string.Empty;

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString("G15", CultureInfo.InvariantCulture),
            XLDataType.Boolean => cell.GetBoolean() ? "true" : "false",
            XLDataType.DateTime => cell.GetDateTime().ToString("O", CultureInfo.InvariantCulture),
            _ => cell.GetFormattedString().Trim()
        };
    }
}

public sealed class ParsedLessonExcelRow
{
    public int RowNumber { get; init; }

    public CreateLessonDto? Dto { get; init; }

    public string? Error { get; init; }
}
