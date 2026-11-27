using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.DTOs.Query;
using MyAdaAttendanceService.Application.Query;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.Tests;

public class CollectionQueryTests
{
    private static readonly CourseDto[] Courses =
    [
        new() { Id = 2, Name = "Calculus I", Department = "Math", Code = "MATH101", Credits = 3, TimesPerWeek = 2 },
        new() { Id = 1, Name = "Algorithms", Department = "CSE", Code = "CSE201", Credits = 4, TimesPerWeek = 3 },
        new() { Id = 3, Name = "Linear Algebra", Department = "Math", Code = "MATH201", Credits = 3, TimesPerWeek = 2 }
    ];

    [Fact]
    public void Apply_SortsByIdAscending()
    {
        var result = CollectionQuery.Apply(Courses, sortBy: "id");

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Apply_SortsByNameDescending_CaseInsensitiveField()
    {
        var result = CollectionQuery.Apply(Courses, sortBy: "NAME", sortDir: "desc");

        Assert.Equal(new[] { "Linear Algebra", "Calculus I", "Algorithms" }, result.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void Apply_Throws_WhenSortFieldUnknown()
    {
        var ex = Assert.Throws<ArgumentException>(() => CollectionQuery.Apply(Courses, sortBy: "missing"));
        Assert.Contains("Unknown sort field", ex.Message);
        Assert.Contains("department", ex.Message);
    }

    [Fact]
    public void Apply_Throws_WhenSortDirInvalid()
    {
        var ex = Assert.Throws<ArgumentException>(() => CollectionQuery.Apply(Courses, sortBy: "id", sortDir: "sideways"));
        Assert.Equal("sortDir must be 'asc' or 'desc'.", ex.Message);
    }

    [Fact]
    public void Apply_SearchMatchesAnyScalarField()
    {
        var result = CollectionQuery.Apply(Courses, search: "math");

        Assert.Equal(new[] { 2, 3 }, result.Select(x => x.Id).ToArray());
    }

    [Fact]
    public void Apply_FilterAndSortTogether()
    {
        var result = CollectionQuery.Apply(
            Courses,
            sortBy: "code",
            sortDir: "asc",
            filter: new CourseFilterDto { Department = "Math", Credits = 3 });

        Assert.Equal(new[] { "MATH101", "MATH201" }, result.Select(x => x.Code).ToArray());
    }

    [Fact]
    public void Apply_FilterMatchesGuidAndEnum()
    {
        var instructor = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lessons = new LessonDto[]
        {
            new() { Id = 1, Name = "A", InstructorId = instructor, Semester = AcademicSemester.Spring, AcademicYear = 2026 },
            new() { Id = 2, Name = "B", InstructorId = Guid.NewGuid(), Semester = AcademicSemester.Fall, AcademicYear = 2026 }
        };

        var result = CollectionQuery.Apply(
            lessons,
            filter: new LessonFilterDto { InstructorId = instructor, Semester = AcademicSemester.Spring });

        var match = Assert.Single(result);
        Assert.Equal(1, match.Id);
    }

    [Fact]
    public void Apply_KeepsOriginalOrder_WhenSortOmitted()
    {
        var result = CollectionQuery.Apply(Courses);

        Assert.Equal(new[] { 2, 1, 3 }, result.Select(x => x.Id).ToArray());
    }
}
