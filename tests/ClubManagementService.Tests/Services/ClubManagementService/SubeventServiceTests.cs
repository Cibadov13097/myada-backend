using global::ClubManagementService.Application.DTOs.Subevent;
using global::ClubManagementService.Application.Interfaces.Repositories;
using global::ClubManagementService.Domain.Entities;
using FluentAssertions;
using Moq;
using ClubManagementService.Tests.Common;

namespace ClubManagementService.Tests.Services;

public class SubeventServiceTests
{
    private readonly Mock<ISubeventRepository> _repositoryMock = new();
    private readonly global::ClubManagementService.Infrastructure.Services.SubeventService _service;

    public SubeventServiceTests()
    {
        _service = new global::ClubManagementService.Infrastructure.Services.SubeventService(
            _repositoryMock.Object,
            ClubManagementMapperFactory.Create());
    }

    [Fact]
    public async Task GetByMainEventIdAsync_ShouldReturnSubevents()
    {
        _repositoryMock.Setup(x => x.GetByMainEventIdAsync(12)).ReturnsAsync(new List<Subevent>
        {
            new() { Id = 1, MainEventId = 12, Name = "Workshop", StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) }
        });

        var result = await _service.GetByMainEventIdAsync(12);

        result.Should().ContainSingle();
        result[0].MainEventId.Should().Be(12);
    }

    [Fact]
    public async Task DeleteAsync_WhenSubeventDoesNotExist_ShouldThrowKeyNotFoundException()
    {
        _repositoryMock.Setup(x => x.GetByIdAsync(6)).ReturnsAsync((Subevent?)null);

        Func<Task> action = async () => await _service.DeleteAsync(6);

        await action.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Subevent 6 not found.");
    }
}
