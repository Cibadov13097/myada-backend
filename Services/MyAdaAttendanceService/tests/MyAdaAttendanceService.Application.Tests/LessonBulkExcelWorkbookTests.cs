using ClosedXML.Excel;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Excel;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;

namespace MyAdaAttendanceService.Application.Tests;

public class LessonBulkExcelWorkbookTests
{
    [Fact]
    public void BuildTemplate_WritesExpectedSheetsAndHeaders()
    {
        var bytes = LessonBulkExcelWorkbook.BuildTemplate(
            [
                new Course { Id = 12, Code = "CSE201", Name = "Algorithms", Department = "CS", Credits = 3, TimesPerWeek = 2 }
            ],
            [
                new ExternalUserDirectoryDto
                {
                    UserId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    DisplayName = "Ada Instructor",
                    Email = "instructor@example.com",
                    UserName = "ada"
                }
            ]);

        using var stream = new MemoryStream(bytes);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(
            new[] { "Lessons", "Instructions", "Courses", "Instructors" },
            workbook.Worksheets.Select(w => w.Name).ToArray());

        var lessons = workbook.Worksheet("Lessons");
        Assert.Equal(LessonBulkExcelWorkbook.Headers, Enumerable.Range(1, 6).Select(c => lessons.Cell(1, c).GetString()).ToArray());

        var courses = workbook.Worksheet("Courses");
        Assert.Equal(12, courses.Cell(2, 1).GetValue<int>());
        Assert.Equal("CSE201", courses.Cell(2, 2).GetString());

        var instructors = workbook.Worksheet("Instructors");
        Assert.Equal("11111111-1111-1111-1111-111111111111", instructors.Cell(2, 1).GetString());
    }

    [Fact]
    public void Parse_ReadsFilledRowsAndSkipsBlankRows()
    {
        var instructorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var template = LessonBulkExcelWorkbook.BuildTemplate(Array.Empty<Course>(), Array.Empty<ExternalUserDirectoryDto>());
        var filled = WriteLessonRows(template,
            [12, "", "", 2026, "Spring", 35],
            [13, instructorId.ToString(), 402, 2026, "Fall", 40],
            ["", "", "", "", "", ""]);

        using var stream = new MemoryStream(filled);
        var rows = LessonBulkExcelWorkbook.Parse(stream);

        Assert.Equal(2, rows.Count);
        Assert.Null(rows[0].Error);
        Assert.Equal(12, rows[0].Dto!.CourseId);
        Assert.Null(rows[0].Dto.InstructorId);
        Assert.Null(rows[0].Dto.RoomId);
        Assert.Equal(AcademicSemester.Spring, rows[0].Dto.Semester);

        Assert.Null(rows[1].Error);
        Assert.Equal(instructorId, rows[1].Dto!.InstructorId);
        Assert.Equal(402, rows[1].Dto!.RoomId);
    }

    [Fact]
    public void Parse_ReturnsRowError_WhenSemesterIsInvalid()
    {
        var template = LessonBulkExcelWorkbook.BuildTemplate(Array.Empty<Course>(), Array.Empty<ExternalUserDirectoryDto>());
        var filled = WriteLessonRows(template, [12, "", 401, 2026, "Winter", 35]);

        using var stream = new MemoryStream(filled);
        var rows = LessonBulkExcelWorkbook.Parse(stream);

        Assert.Single(rows);
        Assert.Null(rows[0].Dto);
        Assert.Contains("Semester", rows[0].Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_Throws_WhenLessonsSheetIsMissing()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Other");
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var ex = Assert.Throws<ArgumentException>(() => LessonBulkExcelWorkbook.Parse(stream));
        Assert.Contains("Lessons", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] WriteLessonRows(byte[] template, params object?[][] rows)
    {
        using var input = new MemoryStream(template);
        using var workbook = new XLWorkbook(input);
        var sheet = workbook.Worksheet(LessonBulkExcelWorkbook.LessonsSheetName);
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var value = rows[r][c];
                if (value is null)
                    continue;
                sheet.Cell(r + 2, c + 1).Value = value.ToString();
            }
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return output.ToArray();
    }
}
