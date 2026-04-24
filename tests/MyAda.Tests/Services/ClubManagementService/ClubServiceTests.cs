using global::ClubManagementService.Application.DTOs.Club;
using global::ClubManagementService.Application.Interfaces.Services;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Domain.Entities;
using global::ClubManagementService.Domain.Enums;
using FluentAssertions;
using Moq;
using MyAda.Tests.Common;

namespace MyAda.Tests.Services.ClubManagementService;

public class ClubServiceTests
{
    private readonly Mock<IClubRepository> _repositoryMock = new();
    private readonly Mock<IClubNotificationService> _notificationServiceMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.ClubService _service;

    public ClubServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.ClubService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create(),
            _notificationServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetPendingStatus()
    {
        var request = new CreateClubDto
        {
            Name = "Robotics Club",
            Description = "Build robots",
            ApprovedBy = Guid.NewGuid(),
            CategoryId = 3
        };
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<Club>())).Callback<Club>(x => x.Id = 8).Returns(Task.CompletedTask);
        _repositoryMock.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _service.CreateAsync(request);

        result.Id.Should().Be(8);
        result.Status.Should().Be(ClubStatus.Pending);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<Club>(c => c.Status == ClubStatus.Pending && c.CategoryId == request.CategoryId)), Times.Once);
    }

    [Fact]
    public async Task GetByCategoryIdAsync_ShouldReturnMappedClubs()
    {
        _repositoryMock.Setup(x => x.GetByCategoryIdAsync(3)).ReturnsAsync(new List<Club>
        {
            new() { Id = 1, Name = "Robotics", Description = "Desc", CategoryId = 3, Category = new Category { Name = "Tech" } }
        });

        var result = await _service.GetByCategoryIdAsync(3);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Robotics");
    }

    [Fact]
    public async Task DeleteAsync_WhenClubDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(40)).ReturnsAsync((Club?)null);

        Func<Task> action = async () => await _service.DeleteAsync(40);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Club 40 not found.");
    }
}
