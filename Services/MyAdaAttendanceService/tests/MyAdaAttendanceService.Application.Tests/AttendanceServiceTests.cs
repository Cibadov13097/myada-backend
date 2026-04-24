using Microsoft.Extensions.Configuration;
using Moq;
using MyAdaAttendanceService.Application.Services;
using MyAdaAttendanceService.Application.Services.Interfaces;
using MyAdaAttendanceService.Core.Entities;
using MyAdaAttendanceService.Core.Interfaces;

namespace MyAdaAttendanceService.Application.Tests;

public class AttendanceServiceTests
{
    private static readonly Guid InstructorId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StudentA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid StudentB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static AttendanceService CreateService(
        Mock<ISessionAttendanceRepository> attendanceRepo,
        Mock<ILessonSessionRepository> sessionRepo,
        Mock<ILessonEnrollmentRepository> enrollmentRepo,
        Mock<IAttendanceActivationRepository> activationRepo,
        Mock<IAttendanceScanLogRepository> scanLogRepo)
    {
        var userDirectory = new Mock<IExternalUserDirectoryService>();
        var config = new Mock<IConfiguration>();
        config.Setup(x => x["QrToken:Secret"])
            .Returns("this_is_a_test_secret_that_is_long_enough_12345");

        return new AttendanceService(
            attendanceRepo.Object,
            sessionRepo.Object,
            enrollmentRepo.Object,
            activationRepo.Object,
            scanLogRepo.Object,
            userDirectory.Object,
            config.Object);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_MarksSingleScanAsPresent_WhenRoundTwoNeverActivated()
    {
        var session = new LessonSession
        {
            Id = 10,
            LessonId = 100,
            Lesson = new Lesson { Id = 100, InstructorId = InstructorId }
        };
        var existingAttendance = new SessionAttendance
        {
            Id = 1,
            SessionId = 10,
            StudentId = StudentA,
            Status = AttendanceStatus.Absent,
            IsManuallyAdjusted = false
        };

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(10)).ReturnsAsync(session);

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(100)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { LessonId = 100, StudentId = StudentA },
            new() { LessonId = 100, StudentId = StudentB }
        });

        var activationRepo = new Mock<IAttendanceActivationRepository>();
        activationRepo.Setup(x => x.GetActiveBySessionIdAsync(10)).ReturnsAsync((AttendanceActivation?)null);
        activationRepo.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AttendanceActivation, bool>>>(),
                null,
                null,
                true))
            .ReturnsAsync(new List<AttendanceActivation>());

        var scanLogRepo = new Mock<IAttendanceScanLogRepository>();
        scanLogRepo.Setup(x => x.GetAcceptedBySessionIdAsync(10)).ReturnsAsync(new List<AttendanceScanLog>
        {
            new() { SessionId = 10, StudentId = StudentA, Round = 1, Accepted = true, TokenJti = "j1" }
        });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetBySessionAndStudentAsync(10, StudentA)).ReturnsAsync(existingAttendance);
        attendanceRepo.Setup(x => x.GetBySessionAndStudentAsync(10, StudentB)).ReturnsAsync((SessionAttendance?)null);
        attendanceRepo.Setup(x => x.UpdateAsync(existingAttendance)).ReturnsAsync(existingAttendance);
        attendanceRepo.Setup(x => x.AddAsync(It.IsAny<SessionAttendance>())).ReturnsAsync((SessionAttendance a) => a);

        var service = CreateService(attendanceRepo, sessionRepo, enrollmentRepo, activationRepo, scanLogRepo);

        await service.FinalizeAttendanceAsync(InstructorId, 10);

        attendanceRepo.Verify(
            x => x.UpdateAsync(It.Is<SessionAttendance>(a => a.StudentId == StudentA && a.Status == AttendanceStatus.Present)),
            Times.Once);
        attendanceRepo.Verify(
            x => x.AddAsync(It.Is<SessionAttendance>(a => a.StudentId == StudentB && a.Status == AttendanceStatus.Absent)),
            Times.Once);
    }

    [Fact]
    public async Task FinalizeAttendanceAsync_MarksSingleScanAsLate_WhenRoundTwoWasActivated()
    {
        var session = new LessonSession
        {
            Id = 11,
            LessonId = 101,
            Lesson = new Lesson { Id = 101, InstructorId = InstructorId }
        };

        var sessionRepo = new Mock<ILessonSessionRepository>();
        sessionRepo.Setup(x => x.GetByIdWithLessonAsync(11)).ReturnsAsync(session);

        var enrollmentRepo = new Mock<ILessonEnrollmentRepository>();
        enrollmentRepo.Setup(x => x.GetByLessonIdAsync(101)).ReturnsAsync(new List<LessonEnrollment>
        {
            new() { LessonId = 101, StudentId = StudentA }
        });

        var activationRepo = new Mock<IAttendanceActivationRepository>();
        activationRepo.Setup(x => x.GetActiveBySessionIdAsync(11)).ReturnsAsync((AttendanceActivation?)null);
        activationRepo.Setup(x => x.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<AttendanceActivation, bool>>>(),
                null,
                null,
                true))
            .ReturnsAsync(new List<AttendanceActivation>
            {
                new() { SessionId = 11, Round = 2, IsActive = false }
            });

        var scanLogRepo = new Mock<IAttendanceScanLogRepository>();
        scanLogRepo.Setup(x => x.GetAcceptedBySessionIdAsync(11)).ReturnsAsync(new List<AttendanceScanLog>
        {
            new() { SessionId = 11, StudentId = StudentA, Round = 1, Accepted = true, TokenJti = "j2" }
        });

        var attendanceRepo = new Mock<ISessionAttendanceRepository>();
        attendanceRepo.Setup(x => x.GetBySessionAndStudentAsync(11, StudentA)).ReturnsAsync((SessionAttendance?)null);
        attendanceRepo.Setup(x => x.AddAsync(It.IsAny<SessionAttendance>())).ReturnsAsync((SessionAttendance a) => a);

        var service = CreateService(attendanceRepo, sessionRepo, enrollmentRepo, activationRepo, scanLogRepo);

        await service.FinalizeAttendanceAsync(InstructorId, 11);

        attendanceRepo.Verify(
            x => x.AddAsync(It.Is<SessionAttendance>(a => a.StudentId == StudentA && a.Status == AttendanceStatus.Late)),
            Times.Once);
    }
}
