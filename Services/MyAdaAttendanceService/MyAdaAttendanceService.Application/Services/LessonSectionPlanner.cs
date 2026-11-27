using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Core;

namespace MyAdaAttendanceService.Application.Services;

public static class LessonSectionPlanner
{
    public const int MaxSectionCount = 200;
    public const int MaxInstructorCount = 200;

    public static IReadOnlyList<Guid?> NormalizeInstructorSlots(int instructorCount, IReadOnlyList<Guid?>? instructors)
    {
        if (instructorCount < 0)
            throw new ArgumentException("InstructorCount cannot be negative.");
        if (instructorCount > MaxInstructorCount)
            throw new ArgumentException($"InstructorCount cannot exceed {MaxInstructorCount}.");

        var provided = instructors ?? Array.Empty<Guid?>();
        if (provided.Count > instructorCount)
            throw new ArgumentException("Instructors list cannot be longer than InstructorCount.");

        var slots = new Guid?[instructorCount];
        for (var i = 0; i < instructorCount; i++)
        {
            var id = i < provided.Count ? provided[i] : null;
            slots[i] = id == Guid.Empty ? null : id;
        }

        return slots;
    }

    public static IReadOnlyList<int> DistributeCapacity(int totalStudents, int sectionCount)
    {
        if (totalStudents < 1)
            throw new ArgumentException("TotalStudents must be at least 1.");
        if (sectionCount < 1)
            throw new ArgumentException("SectionCount must be at least 1.");
        if (sectionCount > MaxSectionCount)
            throw new ArgumentException($"SectionCount cannot exceed {MaxSectionCount}.");
        if (sectionCount > totalStudents)
            throw new ArgumentException("SectionCount cannot exceed TotalStudents.");

        var baseCapacity = totalStudents / sectionCount;
        var remainder = totalStudents % sectionCount;
        var capacities = new int[sectionCount];
        for (var i = 0; i < sectionCount; i++)
            capacities[i] = baseCapacity + (i < remainder ? 1 : 0);

        return capacities;
    }

    public static IReadOnlyList<ProposedLessonDto> Plan(
        int courseId,
        int academicYear,
        AcademicSemester semester,
        int totalStudents,
        int sectionCount,
        IReadOnlyList<Guid?> instructorSlots)
    {
        var capacities = DistributeCapacity(totalStudents, sectionCount);
        var proposed = new ProposedLessonDto[sectionCount];

        for (var i = 0; i < sectionCount; i++)
        {
            Guid? instructorId = null;
            if (instructorSlots.Count > 0)
                instructorId = instructorSlots[i % instructorSlots.Count];

            proposed[i] = new ProposedLessonDto
            {
                SectionNumber = i + 1,
                CourseId = courseId,
                InstructorId = instructorId,
                RoomId = null,
                AcademicYear = academicYear,
                Semester = semester,
                MaxCapacity = capacities[i]
            };
        }

        return proposed;
    }
}
