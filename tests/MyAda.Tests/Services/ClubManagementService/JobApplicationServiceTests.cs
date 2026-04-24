using global::ClubManagementService.Application.DTOs.JobApplication;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Application.Interfaces.Services;
using global::ClubManagementService.Domain.Entities;
using global::ClubManagementService.Domain.Enums;
using FluentAssertions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.ClubManagementService;

public class JobApplicationServiceTests
{
    private readonly Mock<IJobApplicationRepository> _repositoryMock = new();
    private readonly Mock<IClubNotificationService> _notificationServiceMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.JobApplicationService _service;

    public JobApplicationServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.JobApplicationService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create(),
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetPendingStatus()
    {
        var request = new CreateJobApplicationDto
        {
            JobVacancyId = 6,
            ApplicantId = Guid.NewGuid(),
            ExperienceLinks = "portfolio",
            FileUrl = "cv.pdf"
        };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<JobApplication>())).Callback<JobApplication>(x => x.Id = 4).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Id.Should().Be(4);
        result.Status.Should().Be(JobApplicationStatus.Pending);
    }

    [Fact]
    public async Task UpdateStatusAsync_ShouldPersistNewStatus()
    {
        var entity = new JobApplication { Id = 9, Status = JobApplicationStatus.Pending };
        _repositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync(entity);
        _repositoryMock.Setup(x => x.UpdateAsync(entity)).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.UpdateStatusAsync(9, new UpdateJobApplicationStatusDto { Status = JobApplicationStatus.Approved });

        result.Status.Should().Be(JobApplicationStatus.Approved);
        _repositoryMock.Verify(x => x.UpdateAsync(It.Is<JobApplication>(a => a.Status == JobApplicationStatus.Approved)), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WhenApplicationDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(17)).ReturnsAsync((JobApplication?)null);

        Func<Task> action = async () => await _service.DeleteAsync(17);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("JobApplication 17 not found.");
    }
}
