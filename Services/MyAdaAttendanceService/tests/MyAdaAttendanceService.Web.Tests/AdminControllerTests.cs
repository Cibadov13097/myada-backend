using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.DTOs.Query;
using MyAdaAttendanceService.Application.Excel;
using MyAdaAttendanceService.Application.Query;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Web.Controllers;
using MyAdaAttendanceService.Web.Models;

namespace MyAdaAttendanceService.Web.Tests;

public class AdminControllerTests
{
    private static readonly Guid InstructorId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public async Task GetAllLessons_ReturnsOkWithLessons()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetAllLessonsAsync())
            .ReturnsAsync(new List<LessonDto>
            {
                new() { Id = 1, Name = "Algorithms", Code = "CSE201", InstructorId = InstructorId }
            });

        var sessionService = new Mock<ISessionService>();
        var attendanceService = new Mock<IAttendanceService>();
        var adminAttendanceService = new Mock<IAdminAttendanceService>();
        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            sessionService.Object,
            attendanceService.Object,
            adminAttendanceService.Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetAllLessons();

        var ok = Assert.IsType<OkObjectResult>(response);
        var lessons = Assert.IsAssignableFrom<IEnumerable<LessonDto>>(ok.Value);
        Assert.Single(lessons);
    }

    [Fact]
    public async Task GetLessonById_ReturnsNotFound_WhenServiceThrowsKeyNotFound()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetLessonByIdAsync(404))
            .ThrowsAsync(new KeyNotFoundException("Lesson 404 not found."));

        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetLessonById(404);

        Assert.IsType<NotFoundObjectResult>(response);
    }

    [Fact]
    public async Task GetSessionsByLesson_ReturnsOk_WhenServiceSucceeds()
    {
        var sessionService = new Mock<ISessionService>();
        sessionService
            .Setup(x => x.GetSessionsByLessonAdminAsync(102))
            .ReturnsAsync(new List<SessionDto>
            {
                new() { Id = 1, LessonId = 102, LessonName = "Algorithms", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) }
            });

        var controller = new AdminController(
            new Mock<ILessonService>().Object,
            new Mock<ICourseService>().Object,
            sessionService.Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetSessionsByLesson(102);

        var ok = Assert.IsType<OkObjectResult>(response);
        var sessions = Assert.IsAssignableFrom<IEnumerable<SessionDto>>(ok.Value);
        Assert.Single(sessions);
    }

    [Fact]
    public async Task GetLessonsForScheduling_ReturnsOkWithRows()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetLessonsForSchedulingAsync(2026, AcademicSemester.Spring))
            .ReturnsAsync(new List<LessonSchedulingRow>
            {
                new()
                {
                    LessonId = 1,
                    InstructorUserId = InstructorId,
                    Enrollment = 0,
                    MaxCapacity = 35,
                    TimesPerWeek = 2,
                    CourseCode = "X",
                    CourseTitle = "Y"
                }
            });

        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetLessonsForScheduling(2026, AcademicSemester.Spring);

        var ok = Assert.IsType<OkObjectResult>(response);
        var rows = Assert.IsAssignableFrom<IEnumerable<LessonSchedulingRow>>(ok.Value);
        Assert.Single(rows);
        lessonService.Verify(x => x.GetLessonsForSchedulingAsync(2026, AcademicSemester.Spring), Times.Once);
    }

    [Fact]
    public async Task DownloadLessonBulkTemplate_ReturnsExcelFile()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GenerateBulkCreateTemplateAsync())
            .ReturnsAsync([1, 2, 3]);

        var controller = CreateController(lessonService.Object);
        var response = await controller.DownloadLessonBulkTemplate();

        var file = Assert.IsType<FileContentResult>(response);
        Assert.Equal(LessonBulkExcelWorkbook.ContentType, file.ContentType);
        Assert.Equal(LessonBulkExcelWorkbook.FileDownloadName, file.FileDownloadName);
        Assert.Equal(new byte[] { 1, 2, 3 }, file.FileContents);
    }

    [Fact]
    public async Task BulkCreateLessons_ReturnsBadRequest_WhenFileMissing()
    {
        var controller = CreateController(new Mock<ILessonService>().Object);
        var response = await controller.BulkCreateLessons(new BulkLessonExcelUploadRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task BulkCreateLessons_ReturnsCreated_WhenAllRowsSucceed()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.CreateLessonsFromExcelAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new BulkCreateLessonsResultDto
            {
                TotalRows = 1,
                CreatedCount = 1,
                FailedCount = 0,
                Created = [new LessonDto { Id = 7, Code = "CSE201" }]
            });

        var controller = CreateController(lessonService.Object);
        using var content = new MemoryStream([1, 2, 3, 4]);
        var file = new FormFile(content, 0, content.Length, "file", "lessons.xlsx")
        {
            Headers = new HeaderDictionary(),
            ContentType = LessonBulkExcelWorkbook.ContentType
        };

        var response = await controller.BulkCreateLessons(new BulkLessonExcelUploadRequest { File = file });

        var created = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<BulkCreateLessonsResultDto>(created.Value);
        Assert.Equal(1, body.CreatedCount);
    }

    [Fact]
    public async Task BulkCreateLessons_ReturnsBadRequest_WhenRowsFail()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.CreateLessonsFromExcelAsync(It.IsAny<Stream>()))
            .ReturnsAsync(new BulkCreateLessonsResultDto
            {
                TotalRows = 1,
                CreatedCount = 0,
                FailedCount = 1,
                Errors = [new BulkLessonRowErrorDto { RowNumber = 2, Message = "Course 99 was not found." }]
            });

        var controller = CreateController(lessonService.Object);
        using var content = new MemoryStream([1, 2, 3, 4]);
        var file = new FormFile(content, 0, content.Length, "file", "lessons.xlsx");

        var response = await controller.BulkCreateLessons(new BulkLessonExcelUploadRequest { File = file });

        var badRequest = Assert.IsType<BadRequestObjectResult>(response);
        var body = Assert.IsType<BulkCreateLessonsResultDto>(badRequest.Value);
        Assert.Equal(1, body.FailedCount);
        lessonService.Verify(x => x.CreateLessonsFromExcelAsync(It.IsAny<Stream>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCourses_SortsByRequestedField()
    {
        var courseService = new Mock<ICourseService>();
        courseService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CourseDto>
            {
                new() { Id = 2, Name = "B", Department = "Z", Code = "Z1" },
                new() { Id = 1, Name = "A", Department = "A", Code = "A1" }
            });

        var controller = new AdminController(
            new Mock<ILessonService>().Object,
            courseService.Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetAllCourses(new ListQuery { SortBy = "id" });

        var ok = Assert.IsType<OkObjectResult>(response);
        var courses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(ok.Value).ToList();
        Assert.Equal(new[] { 1, 2 }, courses.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task SearchCourses_FiltersByQueryTerm()
    {
        var courseService = new Mock<ICourseService>();
        courseService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CourseDto>
            {
                new() { Id = 1, Name = "Algorithms", Department = "CSE", Code = "CSE201" },
                new() { Id = 2, Name = "Calculus", Department = "Math", Code = "MATH101" }
            });

        var controller = new AdminController(
            new Mock<ILessonService>().Object,
            courseService.Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.SearchCourses(new ListQuery { Q = "math" });

        var ok = Assert.IsType<OkObjectResult>(response);
        var courses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(ok.Value).ToList();
        var match = Assert.Single(courses);
        Assert.Equal(2, match.Id);
    }

    [Fact]
    public async Task FilterCourses_AppliesFieldFilters()
    {
        var courseService = new Mock<ICourseService>();
        courseService
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<CourseDto>
            {
                new() { Id = 1, Name = "Algorithms", Department = "CSE", Code = "CSE201", Credits = 4 },
                new() { Id = 2, Name = "Calculus", Department = "Math", Code = "MATH101", Credits = 3 },
                new() { Id = 3, Name = "Linear Algebra", Department = "Math", Code = "MATH201", Credits = 3 }
            });

        var controller = new AdminController(
            new Mock<ILessonService>().Object,
            courseService.Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.FilterCourses(
            new CourseFilterDto { Department = "Math", Credits = 3 },
            new ListQuery { SortBy = "code" });

        var ok = Assert.IsType<OkObjectResult>(response);
        var courses = Assert.IsAssignableFrom<IEnumerable<CourseDto>>(ok.Value).ToList();
        Assert.Equal(new[] { "MATH101", "MATH201" }, courses.Select(x => x.Code).ToArray());
    }

    [Fact]
    public async Task GetAllLessons_ReturnsBadRequest_WhenSortFieldUnknown()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.GetAllLessonsAsync())
            .ReturnsAsync(new List<LessonDto> { new() { Id = 1, Name = "Algorithms" } });

        var controller = new AdminController(
            lessonService.Object,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);

        var response = await controller.GetAllLessons(new ListQuery { SortBy = "notAField" });

        Assert.IsType<BadRequestObjectResult>(response);
    }

    [Fact]
    public async Task PreviewGeneratedLessons_ReturnsOk()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.PreviewGeneratedLessonsAsync(7, It.IsAny<GenerateLessonsRequestDto>()))
            .ReturnsAsync(new GenerateLessonsPreviewDto
            {
                CourseId = 7,
                SectionCount = 2,
                ProposedLessons =
                [
                    new() { SectionNumber = 1, CourseId = 7, MaxCapacity = 20 },
                    new() { SectionNumber = 2, CourseId = 7, MaxCapacity = 20 }
                ]
            });

        var controller = CreateController(lessonService.Object);
        var response = await controller.PreviewGeneratedLessons(7, new GenerateLessonsRequestDto
        {
            TotalStudents = 40,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            SectionCount = 2,
            InstructorCount = 1,
            Instructors = [null]
        });

        var ok = Assert.IsType<OkObjectResult>(response);
        var preview = Assert.IsType<GenerateLessonsPreviewDto>(ok.Value);
        Assert.Equal(2, preview.ProposedLessons.Count);
    }

    [Fact]
    public async Task CreateGeneratedLessons_ReturnsCreated()
    {
        var lessonService = new Mock<ILessonService>();
        lessonService
            .Setup(x => x.CreateGeneratedLessonsAsync(7, It.IsAny<GenerateLessonsPreviewDto>()))
            .ReturnsAsync(new BulkCreateLessonsResultDto
            {
                TotalRows = 2,
                CreatedCount = 2,
                Created = [new LessonDto { Id = 1 }, new LessonDto { Id = 2 }]
            });

        var controller = CreateController(lessonService.Object);
        var response = await controller.CreateGeneratedLessons(7, new GenerateLessonsPreviewDto
        {
            CourseId = 7,
            ProposedLessons = [new() { SectionNumber = 1, MaxCapacity = 20 }]
        });

        var created = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        var body = Assert.IsType<BulkCreateLessonsResultDto>(created.Value);
        Assert.Equal(2, body.CreatedCount);
    }

    private static AdminController CreateController(ILessonService lessonService) =>
        new(
            lessonService,
            new Mock<ICourseService>().Object,
            new Mock<ISessionService>().Object,
            new Mock<IAttendanceService>().Object,
            new Mock<IAdminAttendanceService>().Object,
            new Mock<IEnrollmentService>().Object,
            new Mock<IExternalUserDirectoryService>().Object);
}
