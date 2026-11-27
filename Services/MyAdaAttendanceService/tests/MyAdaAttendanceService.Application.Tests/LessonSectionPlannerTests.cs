using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.Tests;

public class LessonSectionPlannerTests
{
    private static readonly Guid InstructorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid InstructorB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void DistributeCapacity_SplitsRemainderAcrossFirstSections()
    {
        var capacities = LessonSectionPlanner.DistributeCapacity(121, 4);

        Assert.Equal(new[] { 31, 30, 30, 30 }, capacities);
    }

    [Fact]
    public void NormalizeInstructorSlots_PadsMissingIdsWithNull_AndTreatsEmptyGuidAsUnassigned()
    {
        var slots = LessonSectionPlanner.NormalizeInstructorSlots(3, [InstructorA, Guid.Empty]);

        Assert.Equal(3, slots.Count);
        Assert.Equal(InstructorA, slots[0]);
        Assert.Null(slots[1]);
        Assert.Null(slots[2]);
    }

    [Fact]
    public void Plan_RoundRobinsInstructorSlotsAcrossSections()
    {
        var proposed = LessonSectionPlanner.Plan(
            courseId: 7,
            academicYear: 2026,
            semester: AcademicSemester.Spring,
            totalStudents: 90,
            sectionCount: 5,
            instructorSlots: [InstructorA, InstructorB, null]);

        Assert.Equal(5, proposed.Count);
        Assert.Equal(new Guid?[] { InstructorA, InstructorB, null, InstructorA, InstructorB }, proposed.Select(x => x.InstructorId).ToArray());
        Assert.Equal(new[] { 18, 18, 18, 18, 18 }, proposed.Select(x => x.MaxCapacity).ToArray());
        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, proposed.Select(x => x.SectionNumber).ToArray());
        Assert.All(proposed, x =>
        {
            Assert.Equal(7, x.CourseId);
            Assert.Equal(2026, x.AcademicYear);
            Assert.Equal(AcademicSemester.Spring, x.Semester);
            Assert.Null(x.RoomId);
        });
    }

    [Fact]
    public void Plan_LeavesInstructorsUnassigned_WhenNoSlots()
    {
        var proposed = LessonSectionPlanner.Plan(1, 2026, AcademicSemester.Fall, 10, 2, Array.Empty<Guid?>());

        Assert.All(proposed, x => Assert.Null(x.InstructorId));
        Assert.Equal(new[] { 5, 5 }, proposed.Select(x => x.MaxCapacity).ToArray());
    }

    [Fact]
    public void DistributeCapacity_ThrowsWhenMoreSectionsThanStudents()
    {
        var ex = Assert.Throws<ArgumentException>(() => LessonSectionPlanner.DistributeCapacity(3, 5));
        Assert.Equal("SectionCount cannot exceed TotalStudents.", ex.Message);
    }
}
