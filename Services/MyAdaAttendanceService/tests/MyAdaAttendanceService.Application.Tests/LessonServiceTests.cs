using Moq;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Excel;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class LessonServiceTests
{
    private static readonly Guid InstructorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstructorB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static ICourseRepository DummyCourseRepository()
    {
        var m = new Mock<ICourseRepository>();
        return m.Object;
    }

    private static IExternalUserDirectoryService DummyUserDirectoryService()
    {
        var m = new Mock<IExternalUserDirectoryService>();
        m.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => new ExternalUserDirectoryDto
            {
                UserId = id,
                DisplayName = "User",
                Email = "user@example.com"
            });
        m.Setup(x => x.GetUsersByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) => (IReadOnlyDictionary<Guid, ExternalUserDirectoryDto>)ids
                .Distinct()
                .ToDictionary(
                    id => id,
                    id => new ExternalUserDirectoryDto
                    {
                        UserId = id,
                        DisplayName = "User",
                        Email = "user@example.com"
                    }));
        m.Setup(x => x.EnsureUserExistsInRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return m.Object;
    }

    [Fact]
    public async Task GetMyLessonByIdAsync_ThrowsUnauthorized_WhenInstructorDoesNotOwnLesson()
    {
        var lesson = new Lesson
        {
            Id = 9,
            InstructorId = InstructorA,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE310", Department = "CS", Name = "DB", Credits = 3, TimesPerWeek = 2 }
        };

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetByIdWithDetailsAsync(9)).ReturnsAsync(lesson);
        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMyLessonByIdAsync(InstructorB, 9));
    }

    [Fact]
    public async Task GetLessonByIdAsync_MapsAndSortsSessions()
    {
        var lesson = new Lesson
        {
            Id = 102,
            InstructorId = InstructorA,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            CRN = "20001",
            Course = new Course { Code = "CSE201", Department = "CS", Name = "Algorithms", Credits = 3, TimesPerWeek = 2 },
            Sessions = new List<LessonSession>
            {
                new() { Id = 2, Date = new DateOnly(2026, 4, 12), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), Topic = "B" },
                new() { Id = 1, Date = new DateOnly(2026, 4, 11), StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(10, 0), Topic = "A" }
            }
        };

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository
            .Setup(x => x.GetByIdWithDetailsAsync(102))
            .ReturnsAsync(lesson);

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());

        var result = await service.GetLessonByIdAsync(102);

        Assert.Equal(102, result.Id);
        Assert.NotNull(result.Sessions);
        Assert.Equal(new[] { 1, 2 }, result.Sessions!.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task GetAllLessonsAsync_ReturnsLessonsOrderedById()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Lesson, bool>>?>(),
                It.IsAny<Func<IQueryable<Lesson>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Lesson, object>>?>(),
                It.IsAny<Func<IQueryable<Lesson>, IOrderedQueryable<Lesson>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Lesson>
            {
                new()
                {
                    Id = 1,
                    InstructorId = InstructorA,
                    AcademicYear = 2026,
                    Semester = AcademicSemester.Spring,
                    CRN = "20001",
                    Course = new Course { Code = "A1", Department = "D", Name = "A", Credits = 3, TimesPerWeek = 1 }
                },
                new()
                {
                    Id = 2,
                    InstructorId = InstructorA,
                    AcademicYear = 2026,
                    Semester = AcademicSemester.Spring,
                    CRN = "20002",
                    Course = new Course { Code = "B1", Department = "D", Name = "B", Credits = 3, TimesPerWeek = 1 }
                }
            });

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());
        var result = (await service.GetAllLessonsAsync()).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task CreateLessonAsync_ThrowsWhenInstructorIdIsEmpty()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        var courseRepository = new Mock<ICourseRepository>();
        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 1,
            InstructorId = Guid.Empty
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLessonAsync(dto));
    }

    [Fact]
    public async Task CreateLessonAsync_AllowsNullInstructorId()
    {
        Lesson? passedToAdd = null;
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .Callback<Lesson>(l => passedToAdd = l)
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = 50;
                l.CRN = "20050";
                return l;
            });

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Course
        {
            Id = 7,
            Name = "Discrete Math",
            Department = "CS",
            Code = "CSE120",
            Credits = 3,
            TimesPerWeek = 2
        });

        var userDirectory = new Mock<IExternalUserDirectoryService>();
        userDirectory.Setup(x => x.EnsureUserExistsInRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        userDirectory.Setup(x => x.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExternalUserDirectoryDto?)null);

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, userDirectory.Object);
        var dto = new CreateLessonDto
        {
            CourseId = 7,
            InstructorId = null,
            RoomId = 1,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            MaxCapacity = 30
        };

        var created = await service.CreateLessonAsync(dto);

        Assert.Null(passedToAdd!.InstructorId);
        Assert.Null(created.InstructorId);
        Assert.Equal(string.Empty, created.InstructorDisplayName);
        userDirectory.Verify(
            x => x.EnsureUserExistsInRoleAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateLessonAsync_AllowsNullRoomId()
    {
        Lesson? passedToAdd = null;
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .Callback<Lesson>(l => passedToAdd = l)
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = 51;
                l.CRN = "20051";
                return l;
            });

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Course
        {
            Id = 7,
            Name = "Discrete Math",
            Department = "CS",
            Code = "CSE120",
            Credits = 3,
            TimesPerWeek = 2
        });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 7,
            InstructorId = null,
            RoomId = null,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            MaxCapacity = 30
        };

        var created = await service.CreateLessonAsync(dto);

        Assert.Null(passedToAdd!.RoomId);
        Assert.Null(created.RoomId);
    }

    [Fact]
    public async Task CreateLessonAsync_ThrowsWhenRoomIdIsNegative()
    {
        var service = new LessonService(
            new Mock<ILessonRepository>().Object,
            new Mock<ICourseRepository>().Object,
            DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 1,
            RoomId = -1,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            MaxCapacity = 30
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLessonAsync(dto));
    }

    [Fact]
    public async Task CreateLessonAsync_TrimsStringFieldsAndUsesGeneratedCrn()
    {
        Lesson? passedToAdd = null;
        string? crnWhenRepositoryReceived = null;
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .Callback<Lesson>(l =>
            {
                passedToAdd = l;
                crnWhenRepositoryReceived = l.CRN;
            })
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = 99;
                l.CRN = "20001";
                return l;
            });

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(42)).ReturnsAsync(new Course
        {
            Id = 42,
            Name = "Algorithms",
            Department = "CS",
            Code = "CSE201",
            Credits = 3,
            TimesPerWeek = 2
        });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var dto = new CreateLessonDto
        {
            CourseId = 42,
            InstructorId = InstructorA,
            RoomId = 3,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            MaxCapacity = 40
        };

        var created = await service.CreateLessonAsync(dto);

        courseRepository.Verify(x => x.GetByIdAsync(42), Times.Once);
        lessonRepository.Verify(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()), Times.Once);
        Assert.NotNull(passedToAdd);
        Assert.Equal(AcademicSemester.Spring, passedToAdd!.Semester);
        Assert.Equal(2026, passedToAdd.AcademicYear);
        Assert.Equal(42, passedToAdd.CourseId);
        Assert.Equal(string.Empty, crnWhenRepositoryReceived);
        Assert.Equal("Algorithms", created.Name);
        Assert.Equal("CSE201", created.Code);
        Assert.Equal("20001", created.CRN);
    }

    [Fact]
    public async Task GenerateBulkCreateTemplateAsync_IncludesCoursesAndInstructors()
    {
        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>?>(),
                It.IsAny<Func<IQueryable<Course>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Course, object>>?>(),
                It.IsAny<Func<IQueryable<Course>, IOrderedQueryable<Course>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Course>
            {
                new() { Id = 12, Code = "CSE201", Name = "Algorithms", Department = "CS", Credits = 3, TimesPerWeek = 2 }
            });

        var userDirectory = new Mock<IExternalUserDirectoryService>();
        userDirectory.Setup(x => x.GetUsersByRoleAsync("instructor", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalUserDirectoryDto>
            {
                new() { UserId = InstructorA, DisplayName = "Ada Instructor", Email = "a@example.com", UserName = "ada" }
            });

        var service = new LessonService(new Mock<ILessonRepository>().Object, courseRepository.Object, userDirectory.Object);
        var bytes = await service.GenerateBulkCreateTemplateAsync();

        Assert.True(bytes.Length > 0);
        using var stream = new MemoryStream(bytes);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        Assert.Equal(12, workbook.Worksheet("Courses").Cell(2, 1).GetValue<int>());
        Assert.Equal(InstructorA.ToString(), workbook.Worksheet("Instructors").Cell(2, 1).GetString());
    }

    [Fact]
    public async Task CreateLessonsFromExcelAsync_CreatesAllValidRows()
    {
        var createdLessons = new List<Lesson>();
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = createdLessons.Count + 1;
                l.CRN = $"2000{l.Id}";
                createdLessons.Add(l);
                return l;
            });

        var course = new Course { Id = 12, Name = "Algorithms", Department = "CS", Code = "CSE201", Credits = 3, TimesPerWeek = 2 };
        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>?>(),
                It.IsAny<Func<IQueryable<Course>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Course, object>>?>(),
                It.IsAny<Func<IQueryable<Course>, IOrderedQueryable<Course>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Course> { course });
        courseRepository.Setup(x => x.GetByIdAsync(12)).ReturnsAsync(course);

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var excel = WriteLessonExcel([12, "", "", 2026, "Spring", 35]);

        await using var stream = new MemoryStream(excel);
        var result = await service.CreateLessonsFromExcelAsync(stream);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Single(result.Created);
        Assert.Null(result.Created[0].InstructorId);
        Assert.Null(result.Created[0].RoomId);
        Assert.Equal("20001", result.Created[0].CRN);
        lessonRepository.Verify(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()), Times.Once);
    }

    [Fact]
    public async Task CreateLessonsFromExcelAsync_CreatesNothing_WhenAnyRowIsInvalid()
    {
        var lessonRepository = new Mock<ILessonRepository>();
        var course = new Course { Id = 12, Name = "Algorithms", Department = "CS", Code = "CSE201", Credits = 3, TimesPerWeek = 2 };
        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Course, bool>>?>(),
                It.IsAny<Func<IQueryable<Course>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Course, object>>?>(),
                It.IsAny<Func<IQueryable<Course>, IOrderedQueryable<Course>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Course> { course });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var excel = WriteLessonExcel(
            [12, "", 401, 2026, "Spring", 35],
            [99, "", 402, 2026, "Fall", 40]);

        await using var stream = new MemoryStream(excel);
        var result = await service.CreateLessonsFromExcelAsync(stream);

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.FailedCount);
        Assert.Contains(result.Errors, e => e.RowNumber == 3 && e.Message.Contains("Course 99"));
        lessonRepository.Verify(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()), Times.Never);
    }

    [Fact]
    public async Task CreateLessonsFromExcelAsync_Throws_WhenNoDataRows()
    {
        var service = new LessonService(
            new Mock<ILessonRepository>().Object,
            DummyCourseRepository(),
            DummyUserDirectoryService());
        var excel = LessonBulkExcelWorkbook.BuildTemplate(Array.Empty<Course>(), Array.Empty<ExternalUserDirectoryDto>());

        await using var stream = new MemoryStream(excel);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateLessonsFromExcelAsync(stream));
    }

    private static byte[] WriteLessonExcel(params object?[][] rows)
    {
        var template = LessonBulkExcelWorkbook.BuildTemplate(Array.Empty<Course>(), Array.Empty<ExternalUserDirectoryDto>());
        using var input = new MemoryStream(template);
        using var workbook = new ClosedXML.Excel.XLWorkbook(input);
        var sheet = workbook.Worksheet("Lessons");
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

    [Fact]
    public async Task GetLessonsForSchedulingAsync_DelegatesToRepository()
    {
        var rows = new List<LessonSchedulingRow>
        {
            new()
            {
                LessonId = 10,
                InstructorUserId = InstructorB,
                Enrollment = 12,
                MaxCapacity = 40,
                TimesPerWeek = 2,
                CourseCode = "MTH101",
                CourseTitle = "Calculus",
            }
        };
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository
            .Setup(x => x.GetLessonSchedulingRowsAsync(2026, AcademicSemester.Spring))
            .ReturnsAsync(rows);

        var service = new LessonService(lessonRepository.Object, DummyCourseRepository(), DummyUserDirectoryService());
        var result = await service.GetLessonsForSchedulingAsync(2026, AcademicSemester.Spring);

        Assert.Same(rows, result);
        lessonRepository.Verify(x => x.GetLessonSchedulingRowsAsync(2026, AcademicSemester.Spring), Times.Once);
    }

    [Fact]
    public async Task PreviewGeneratedLessonsAsync_DistributesCapacityAndInstructors()
    {
        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Course
        {
            Id = 7,
            Name = "Algorithms",
            Department = "CS",
            Code = "CSE201",
            Credits = 3,
            TimesPerWeek = 2
        });

        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.GetByCourseIdAsync(7)).ReturnsAsync(new List<Lesson>
        {
            new() { Id = 9, CourseId = 7, AcademicYear = 2026, Semester = AcademicSemester.Spring }
        });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var preview = await service.PreviewGeneratedLessonsAsync(7, new GenerateLessonsRequestDto
        {
            TotalStudents = 100,
            AcademicYear = 2026,
            Semester = AcademicSemester.Spring,
            SectionCount = 3,
            InstructorCount = 3,
            Instructors = [InstructorA, null, InstructorB]
        });

        Assert.Equal("CSE201", preview.CourseCode);
        Assert.Equal(100, preview.TotalSeats);
        Assert.Equal(1, preview.ExistingLessonCountForTerm);
        Assert.Equal(3, preview.ProposedLessons.Count);
        Assert.Equal(new[] { 34, 33, 33 }, preview.ProposedLessons.Select(x => x.MaxCapacity).ToArray());
        Assert.Equal(new Guid?[] { InstructorA, null, InstructorB }, preview.ProposedLessons.Select(x => x.InstructorId).ToArray());
        Assert.Equal("User", preview.ProposedLessons[0].InstructorDisplayName);
        Assert.Equal(string.Empty, preview.ProposedLessons[1].InstructorDisplayName);
    }

    [Fact]
    public async Task CreateGeneratedLessonsAsync_CreatesEachProposedSection()
    {
        var created = new List<Lesson>();
        var lessonRepository = new Mock<ILessonRepository>();
        lessonRepository.Setup(x => x.AddWithGeneratedCrnAsync(It.IsAny<Lesson>()))
            .Callback<Lesson>(l => created.Add(l))
            .ReturnsAsync((Lesson l) =>
            {
                l.Id = created.Count;
                l.CRN = $"2000{created.Count}";
                return l;
            });

        var courseRepository = new Mock<ICourseRepository>();
        courseRepository.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new Course
        {
            Id = 7,
            Name = "Algorithms",
            Department = "CS",
            Code = "CSE201",
            Credits = 3,
            TimesPerWeek = 2
        });

        var service = new LessonService(lessonRepository.Object, courseRepository.Object, DummyUserDirectoryService());
        var result = await service.CreateGeneratedLessonsAsync(7, new GenerateLessonsPreviewDto
        {
            CourseId = 7,
            ProposedLessons =
            [
                new() { SectionNumber = 1, CourseId = 7, AcademicYear = 2026, Semester = AcademicSemester.Spring, MaxCapacity = 34, InstructorId = InstructorA },
                new() { SectionNumber = 2, CourseId = 7, AcademicYear = 2026, Semester = AcademicSemester.Spring, MaxCapacity = 33, InstructorId = null }
            ]
        });

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(2, created.Count);
        Assert.Equal(InstructorA, created[0].InstructorId);
        Assert.Null(created[1].InstructorId);
        Assert.Equal(new[] { 34, 33 }, created.Select(x => x.MaxCapacity).ToArray());
    }

    [Fact]
    public async Task CreateGeneratedLessonsAsync_ThrowsWhenPreviewCourseDoesNotMatch()
    {
        var service = new LessonService(
            new Mock<ILessonRepository>().Object,
            DummyCourseRepository(),
            DummyUserDirectoryService());

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateGeneratedLessonsAsync(7, new GenerateLessonsPreviewDto
        {
            CourseId = 99,
            ProposedLessons = [new() { CourseId = 99, AcademicYear = 2026, Semester = AcademicSemester.Spring, MaxCapacity = 10 }]
        }));
    }
}
