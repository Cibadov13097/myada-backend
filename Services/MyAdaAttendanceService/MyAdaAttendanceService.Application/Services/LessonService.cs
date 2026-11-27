using Microsoft.EntityFrameworkCore;
using MyAdaAttendanceService.Application.DTOs;
using MyAdaAttendanceService.Application.Excel;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Services;

public class LessonService : ILessonService
{
    private readonly ILessonRepository _lessonRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly IExternalUserDirectoryService _userDirectoryService;

    public LessonService(
        ILessonRepository lessonRepository,
        ICourseRepository courseRepository,
        IExternalUserDirectoryService userDirectoryService)
    {
        _lessonRepository = lessonRepository;
        _courseRepository = courseRepository;
        _userDirectoryService = userDirectoryService;
    }

    private static void ValidateAcademicYear(int academicYear)
    {
        if (academicYear is < 2000 or > 2100)
            throw new ArgumentException("Academic year must be between 2000 and 2100.");
    }

    private static void ValidateSemester(AcademicSemester semester)
    {
        if (!Enum.IsDefined(semester))
            throw new ArgumentException("Semester must be Fall, Spring, or Summer.");
    }

    private static void ValidateInstructorId(Guid? instructorId)
    {
        if (instructorId == Guid.Empty)
            throw new ArgumentException("InstructorId cannot be empty. Omit or set null if unassigned.");
    }

    private static void ValidateRoomId(int? roomId)
    {
        if (roomId.HasValue && roomId.Value < 0)
            throw new ArgumentException("RoomId cannot be negative. Omit or set null if unassigned.");
    }

    private async Task EnsureInstructorAssignedAsync(Guid? instructorId)
    {
        ValidateInstructorId(instructorId);
        if (instructorId.HasValue)
            await _userDirectoryService.EnsureUserExistsInRoleAsync(instructorId.Value, "instructor");
    }

    private static IEnumerable<Guid> NonNullInstructorIds(IEnumerable<Lesson> lessons) =>
        lessons.Where(x => x.InstructorId.HasValue).Select(x => x.InstructorId!.Value);

    private static LessonDto MapLessonSummary(Lesson l, ExternalUserDirectoryDto? instructor = null) => new()
    {
        Id = l.Id,
        Name = l.Course!.Name,
        Code = l.Course.Code,
        InstructorId = l.InstructorId,
        InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
        InstructorEmail = instructor?.Email ?? string.Empty,
        RoomId = l.RoomId,
        AcademicYear = l.AcademicYear,
        Semester = l.Semester,
        CRN = l.CRN,
        MaxCapacity = l.MaxCapacity,
        TimesPerWeek = l.Course.TimesPerWeek
    };

    private async Task<ExternalUserDirectoryDto?> ResolveInstructorAsync(Guid? instructorId)
    {
        if (!instructorId.HasValue)
            return null;
        return await _userDirectoryService.GetUserByIdAsync(instructorId.Value);
    }

    public async Task<IEnumerable<LessonDto>> GetMyLessonsAsync(Guid instructorId)
    {
        var lessons = await _lessonRepository.GetByInstructorIdAsync(instructorId);
        var users = await _userDirectoryService.GetUsersByIdsAsync(NonNullInstructorIds(lessons));
        return lessons.Select(x => MapLessonSummary(
            x,
            x.InstructorId.HasValue ? users.GetValueOrDefault(x.InstructorId.Value) : null));
    }

    public async Task<LessonDto> GetMyLessonByIdAsync(Guid instructorId, int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        if (lesson.InstructorId != instructorId)
            throw new UnauthorizedAccessException("You do not own this lesson.");

        var instructor = await ResolveInstructorAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Course!.Name,
            Code = lesson.Course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            RoomId = lesson.RoomId,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity,
            TimesPerWeek = lesson.Course.TimesPerWeek,
            Sessions = lesson.Sessions
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Topic = s.Topic,
                    IsAttendanceActive = s.IsAttendanceActive,
                    RoundNo = s.RoundNo
                }).ToList()
        };
    }

    public async Task<IEnumerable<LessonDto>> GetAllLessonsAsync()
    {
        var lessons = await _lessonRepository.GetAllAsync(
            include: q => q.Include(l => l.Course),
            orderBy: q => q.OrderBy(l => l.Id));

        var users = await _userDirectoryService.GetUsersByIdsAsync(NonNullInstructorIds(lessons));
        return lessons.Select(x => MapLessonSummary(
            x,
            x.InstructorId.HasValue ? users.GetValueOrDefault(x.InstructorId.Value) : null));
    }

    public async Task<LessonDto> GetLessonByIdAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdWithDetailsAsync(lessonId)
            ?? throw new KeyNotFoundException($"Lesson {lessonId} not found.");

        var instructor = await ResolveInstructorAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = lesson.Course!.Name,
            Code = lesson.Course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            RoomId = lesson.RoomId,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity,
            TimesPerWeek = lesson.Course.TimesPerWeek,
            Sessions = lesson.Sessions
                .OrderBy(s => s.Date).ThenBy(s => s.StartTime)
                .Select(s => new SessionShortDto
                {
                    Id = s.Id,
                    Date = s.Date,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    Topic = s.Topic,
                    IsAttendanceActive = s.IsAttendanceActive,
                    RoundNo = s.RoundNo
                }).ToList()
        };
    }

    public async Task<IEnumerable<LessonDto>> GetLessonsByCourseIdAsync(int courseId)
    {
        _ = await _courseRepository.GetByIdAsync(courseId);

        var lessons = await _lessonRepository.GetByCourseIdAsync(courseId);

        var users = await _userDirectoryService.GetUsersByIdsAsync(NonNullInstructorIds(lessons));
        return lessons.Select(x => MapLessonSummary(
            x,
            x.InstructorId.HasValue ? users.GetValueOrDefault(x.InstructorId.Value) : null));
    }

    public async Task<IEnumerable<LessonDto>> GetLessonsByAcademicTermAsync(int academicYear, AcademicSemester semester)
    {
        ValidateAcademicYear(academicYear);
        ValidateSemester(semester);

        var lessons = await _lessonRepository.GetByAcademicTermAsync(academicYear, semester);
        var users = await _userDirectoryService.GetUsersByIdsAsync(NonNullInstructorIds(lessons));
        return lessons.Select(x => MapLessonSummary(
            x,
            x.InstructorId.HasValue ? users.GetValueOrDefault(x.InstructorId.Value) : null));
    }

    public async Task<LessonDto> CreateLessonAsync(CreateLessonDto dto)
    {
        if (dto.CourseId <= 0)
            throw new ArgumentException("CourseId is required and must be positive.");
        if (dto.MaxCapacity < 0)
            throw new ArgumentException("Max capacity cannot be negative.");

        ValidateAcademicYear(dto.AcademicYear);
        ValidateSemester(dto.Semester);
        ValidateRoomId(dto.RoomId);
        await EnsureInstructorAssignedAsync(dto.InstructorId);

        var course = await _courseRepository.GetByIdAsync(dto.CourseId);

        var lesson = new Lesson
        {
            InstructorId = dto.InstructorId,
            RoomId = dto.RoomId,
            CourseId = course.Id,
            AcademicYear = dto.AcademicYear,
            Semester = dto.Semester,
            CRN = string.Empty,
            MaxCapacity = dto.MaxCapacity
        };

        await _lessonRepository.AddWithGeneratedCrnAsync(lesson);

        var instructor = await ResolveInstructorAsync(lesson.InstructorId);
        return new LessonDto
        {
            Id = lesson.Id,
            Name = course.Name,
            Code = course.Code,
            InstructorId = lesson.InstructorId,
            InstructorDisplayName = instructor?.DisplayName ?? string.Empty,
            InstructorEmail = instructor?.Email ?? string.Empty,
            RoomId = lesson.RoomId,
            AcademicYear = lesson.AcademicYear,
            Semester = lesson.Semester,
            CRN = lesson.CRN,
            MaxCapacity = lesson.MaxCapacity,
            TimesPerWeek = course.TimesPerWeek
        };
    }

    public async Task<byte[]> GenerateBulkCreateTemplateAsync()
    {
        var courses = await _courseRepository.GetAllAsync(
            orderBy: q => q.OrderBy(c => c.Department).ThenBy(c => c.Code));
        var instructors = await _userDirectoryService.GetUsersByRoleAsync("instructor");
        return LessonBulkExcelWorkbook.BuildTemplate(courses, instructors);
    }

    public async Task<BulkCreateLessonsResultDto> CreateLessonsFromExcelAsync(Stream excelStream)
    {
        ArgumentNullException.ThrowIfNull(excelStream);

        var parsedRows = LessonBulkExcelWorkbook.Parse(excelStream);
        if (parsedRows.Count == 0)
            throw new ArgumentException("No lesson rows found. Fill at least one row on the Lessons sheet.");

        var errors = parsedRows
            .Where(row => !string.IsNullOrWhiteSpace(row.Error))
            .Select(row => new BulkLessonRowErrorDto { RowNumber = row.RowNumber, Message = row.Error! })
            .ToList();

        var validRows = parsedRows.Where(row => row.Dto is not null).ToList();
        if (validRows.Count > 0)
        {
            var courses = (await _courseRepository.GetAllAsync())
                .ToDictionary(c => c.Id);

            var uniqueInstructorIds = validRows
                .Select(row => row.Dto!.InstructorId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var instructorErrors = new Dictionary<Guid, string>();
            foreach (var instructorId in uniqueInstructorIds)
            {
                try
                {
                    await EnsureInstructorAssignedAsync(instructorId);
                }
                catch (Exception ex) when (ex is ArgumentException or KeyNotFoundException or InvalidOperationException)
                {
                    instructorErrors[instructorId] = ex.Message;
                }
            }

            foreach (var row in validRows)
            {
                var dto = row.Dto!;
                if (!courses.ContainsKey(dto.CourseId))
                {
                    errors.Add(new BulkLessonRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = $"Course {dto.CourseId} was not found. Use an Id from the Courses sheet."
                    });
                }

                if (dto.InstructorId.HasValue && instructorErrors.TryGetValue(dto.InstructorId.Value, out var instructorError))
                {
                    errors.Add(new BulkLessonRowErrorDto
                    {
                        RowNumber = row.RowNumber,
                        Message = instructorError
                    });
                }
            }
        }

        if (errors.Count > 0)
        {
            return new BulkCreateLessonsResultDto
            {
                TotalRows = parsedRows.Count,
                CreatedCount = 0,
                FailedCount = errors.Select(e => e.RowNumber).Distinct().Count(),
                Errors = errors.OrderBy(e => e.RowNumber).ToList()
            };
        }

        var created = new List<LessonDto>(validRows.Count);
        foreach (var row in validRows)
            created.Add(await CreateLessonAsync(row.Dto!));

        return new BulkCreateLessonsResultDto
        {
            TotalRows = parsedRows.Count,
            CreatedCount = created.Count,
            FailedCount = 0,
            Created = created
        };
    }

    public async Task<LessonDto> UpdateLessonAsync(int lessonId, UpdateLessonDto dto)
    {
        if (dto.CourseId <= 0)
            throw new ArgumentException("CourseId is required and must be positive.");
        if (dto.MaxCapacity < 0)
            throw new ArgumentException("Max capacity cannot be negative.");

        ValidateAcademicYear(dto.AcademicYear);
        ValidateSemester(dto.Semester);
        ValidateRoomId(dto.RoomId);
        await EnsureInstructorAssignedAsync(dto.InstructorId);

        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        _ = await _courseRepository.GetByIdAsync(dto.CourseId);

        var termChanged = lesson.AcademicYear != dto.AcademicYear || lesson.Semester != dto.Semester;

        await _lessonRepository.UpdateLessonWithAutoCrnAsync(
            lesson,
            dto.AcademicYear,
            dto.Semester,
            dto.CourseId,
            termChanged,
            dto.InstructorId,
            dto.RoomId,
            dto.MaxCapacity);

        return await GetLessonByIdAsync(lessonId);
    }

    public async Task DeleteLessonAsync(int lessonId)
    {
        var lesson = await _lessonRepository.GetByIdAsync(lessonId);
        await _lessonRepository.RemoveAsync(lesson);
    }

    public Task<IReadOnlyList<LessonSchedulingRow>> GetLessonsForSchedulingAsync(
        int academicYear,
        AcademicSemester semester)
    {
        ValidateAcademicYear(academicYear);
        ValidateSemester(semester);
        return _lessonRepository.GetLessonSchedulingRowsAsync(academicYear, semester);
    }

    public async Task<GenerateLessonsPreviewDto> PreviewGeneratedLessonsAsync(int courseId, GenerateLessonsRequestDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var course = await _courseRepository.GetByIdAsync(courseId);
        ValidateAcademicYear(dto.AcademicYear);
        ValidateSemester(dto.Semester);

        var instructorCount = dto.InstructorCount;
        if (instructorCount == 0 && dto.Instructors is { Count: > 0 })
            instructorCount = dto.Instructors.Count;

        var instructorSlots = LessonSectionPlanner.NormalizeInstructorSlots(instructorCount, dto.Instructors);
        await EnsureInstructorSlotsExistAsync(instructorSlots);

        var proposed = LessonSectionPlanner.Plan(
            courseId,
            dto.AcademicYear,
            dto.Semester,
            dto.TotalStudents,
            dto.SectionCount,
            instructorSlots);

        var users = await _userDirectoryService.GetUsersByIdsAsync(
            instructorSlots.Where(id => id.HasValue).Select(id => id!.Value));

        foreach (var lesson in proposed)
        {
            if (lesson.InstructorId.HasValue && users.TryGetValue(lesson.InstructorId.Value, out var instructor))
            {
                lesson.InstructorDisplayName = instructor.DisplayName;
                lesson.InstructorEmail = instructor.Email;
            }
        }

        var existing = await _lessonRepository.GetByCourseIdAsync(courseId);
        var existingForTerm = existing.Count(l => l.AcademicYear == dto.AcademicYear && l.Semester == dto.Semester);

        return new GenerateLessonsPreviewDto
        {
            CourseId = course.Id,
            CourseCode = course.Code,
            CourseName = course.Name,
            AcademicYear = dto.AcademicYear,
            Semester = dto.Semester,
            TotalStudents = dto.TotalStudents,
            SectionCount = dto.SectionCount,
            InstructorCount = instructorSlots.Count,
            TotalSeats = proposed.Sum(x => x.MaxCapacity),
            ExistingLessonCountForTerm = existingForTerm,
            InstructorSlots = instructorSlots,
            ProposedLessons = proposed
        };
    }

    public async Task<BulkCreateLessonsResultDto> CreateGeneratedLessonsAsync(int courseId, GenerateLessonsPreviewDto preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (preview.ProposedLessons is null || preview.ProposedLessons.Count == 0)
            throw new ArgumentException("ProposedLessons is required and must contain at least one section.");
        if (preview.ProposedLessons.Count > LessonSectionPlanner.MaxSectionCount)
            throw new ArgumentException($"Cannot create more than {LessonSectionPlanner.MaxSectionCount} sections in one request.");
        if (preview.CourseId != 0 && preview.CourseId != courseId)
            throw new ArgumentException("Preview courseId does not match the course in the URL.");

        _ = await _courseRepository.GetByIdAsync(courseId);

        foreach (var proposed in preview.ProposedLessons)
        {
            if (proposed.CourseId != 0 && proposed.CourseId != courseId)
                throw new ArgumentException($"Proposed section {proposed.SectionNumber} has a different courseId.");
            if (proposed.MaxCapacity < 0)
                throw new ArgumentException($"Proposed section {proposed.SectionNumber} has a negative maxCapacity.");
            ValidateAcademicYear(proposed.AcademicYear);
            ValidateSemester(proposed.Semester);
            ValidateRoomId(proposed.RoomId);
            ValidateInstructorId(proposed.InstructorId);
        }

        await EnsureInstructorSlotsExistAsync(
            preview.ProposedLessons.Select(x => x.InstructorId).ToList());

        var created = new List<LessonDto>(preview.ProposedLessons.Count);
        foreach (var proposed in preview.ProposedLessons)
        {
            created.Add(await CreateLessonAsync(new CreateLessonDto
            {
                CourseId = courseId,
                InstructorId = proposed.InstructorId == Guid.Empty ? null : proposed.InstructorId,
                RoomId = proposed.RoomId,
                AcademicYear = proposed.AcademicYear,
                Semester = proposed.Semester,
                MaxCapacity = proposed.MaxCapacity
            }));
        }

        return new BulkCreateLessonsResultDto
        {
            TotalRows = created.Count,
            CreatedCount = created.Count,
            FailedCount = 0,
            Created = created
        };
    }

    private async Task EnsureInstructorSlotsExistAsync(IReadOnlyList<Guid?> instructorSlots)
    {
        var uniqueIds = instructorSlots
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        foreach (var instructorId in uniqueIds)
            await EnsureInstructorAssignedAsync(instructorId);
    }
}
