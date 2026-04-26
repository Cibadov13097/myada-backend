using global::ClubManagementService.Application.DTOs.JobVacancy;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Application.Interfaces.Services;
using global::ClubManagementService.Domain.Entities;
using FluentAssertions;
using Moq;
using ClubManagementService.Tests.Common;

namespace ClubManagementService.Tests.Services;

public class JobVacancyServiceTests
{
    private readonly Mock<IJobVacancyRepository> _repositoryMock = new();
    private readonly Mock<IClubNotificationService> _notificationServiceMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.JobVacancyService _service;

    public JobVacancyServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.JobVacancyService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create(),
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task GetByClubIdAsync_ShouldReturnVacancies()
    {
        _repositoryMock.Setup(x => x.GetByClubIdAsync(3, It.IsAny<bool>())).ReturnsAsync(new List<JobVacancy>
        {
            new() { Id = 1, ClubId = 3, Title = "Designer", Description = "Create assets", PostedBy = Guid.NewGuid() }
        });

        var result = await _service.GetByClubIdAsync(3);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Designer");
    }

    [Fact]
    public async Task UpdateAsync_WhenVacancyDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((JobVacancy?)null);

        Func<Task> action = async () => await _service.UpdateAsync(9, new UpdateJobVacancyDto { Title = "Lead", Description = "Desc" });

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("JobVacancy 9 not found.");
    }
}
