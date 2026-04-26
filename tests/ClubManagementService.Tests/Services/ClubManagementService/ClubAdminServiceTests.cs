using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Entities;
using ClubManagementService.Infrastructure.Persistence.Context;
using ClubManagementService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace ClubManagementService.Tests.Services;

public class ClubAdminServiceTests : IDisposable
{
    private readonly Mock<IClubService> _clubService = new();
    private readonly Mock<IClubRepository> _clubRepository = new();
    private readonly Mock<IAuthUserLookupService> _authUserLookup = new();
    private readonly ClubManagementDbContext _dbContext;
    private readonly ClubAdminService _service;

    public ClubAdminServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        _dbContext = new ClubManagementDbContext(dbOptions);

        _service = new ClubAdminService(
            _clubService.Object,
            Mock.Of<IJobVacancyService>(),
            Mock.Of<IJobApplicationService>(),
            Mock.Of<IEventService>(),
            Mock.Of<IClubPositionRepository>(),
            new ClubAdminWorkspaceStore(),
            Mock.Of<IProposalContractService>(),
            _clubRepository.Object,
            Mock.Of<IClubMembershipRepository>(),
            _dbContext,
            _authUserLookup.Object,
            Mock.Of<ILogger<ClubAdminService>>());
    }

    [Fact]
    public async Task GetFilesAsync_ShouldBackfillMissingIdsAndPersist()
    {
        var club = CreateClub();
        club.ResourcesJson = "[{\"url\":\"https://example.com/guide.pdf\",\"title\":\"Guide\"}]";
        SetupClub(club);

        var result = await _service.GetFilesAsync("1");

        result.Should().ContainSingle();
        result[0].Id.Should().NotBeNullOrWhiteSpace();
        result[0].Title.Should().Be("Guide");
        ClubResourcesJson.ParseForRead(club.ResourcesJson).Single().Id.Should().NotBeNullOrWhiteSpace();
        _clubRepository.Verify(x => x.UpdateAsync(club), Times.Once);
        _clubRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateFileAsync_ShouldAppendNewFileAndPersist()
    {
        var club = CreateClub();
        SetupClub(club);

        var created = await _service.CreateFileAsync("1", new ClubAdminCreateFileInput
        {
            Url = "https://example.com/files/handbook.pdf",
            Title = "Handbook"
        });

        created.Should().NotBeNull();
        created!.Id.Should().NotBeNullOrWhiteSpace();
        created.Url.Should().Be("https://example.com/files/handbook.pdf");
        created.Title.Should().Be("Handbook");

        var stored = ClubResourcesJson.ParseForRead(club.ResourcesJson);
        stored.Should().ContainSingle();
        stored[0].Id.Should().Be(created.Id);
        stored[0].Url.Should().Be(created.Url);
        stored[0].Title.Should().Be(created.Title);
        _clubRepository.Verify(x => x.UpdateAsync(club), Times.Once);
        _clubRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateFileAsync_ShouldReplaceFileMetadata()
    {
        var club = CreateClub();
        club.ResourcesJson = ClubResourcesJson.SerializeCanonical(new[]
        {
            new ClubResourceItemDto
            {
                Id = "file-1",
                Url = "https://example.com/files/old.pdf",
                Title = "Old"
            }
        });
        SetupClub(club);

        var updated = await _service.UpdateFileAsync("1", "file-1", new ClubAdminUpdateFileInput
        {
            Url = "https://example.com/files/new.pdf",
            Title = "New"
        });

        updated.Should().NotBeNull();
        updated!.Id.Should().Be("file-1");
        updated.Url.Should().Be("https://example.com/files/new.pdf");
        updated.Title.Should().Be("New");

        var stored = ClubResourcesJson.ParseForRead(club.ResourcesJson);
        stored.Should().ContainSingle();
        stored[0].Url.Should().Be("https://example.com/files/new.pdf");
        stored[0].Title.Should().Be("New");
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldRemoveMatchingFile()
    {
        var club = CreateClub();
        club.ResourcesJson = ClubResourcesJson.SerializeCanonical(new[]
        {
            new ClubResourceItemDto
            {
                Id = "file-1",
                Url = "https://example.com/files/old.pdf",
                Title = "Old"
            },
            new ClubResourceItemDto
            {
                Id = "file-2",
                Url = "https://example.com/files/keep.pdf",
                Title = "Keep"
            }
        });
        SetupClub(club);

        var deleted = await _service.DeleteFileAsync("1", "file-1");

        deleted.Should().NotBeNull();
        deleted!.Id.Should().Be("file-1");
        var stored = ClubResourcesJson.ParseForRead(club.ResourcesJson);
        stored.Should().ContainSingle();
        stored[0].Id.Should().Be("file-2");
        stored[0].Title.Should().Be("Keep");
    }

    [Fact]
    public async Task PatchProfileAsync_ShouldPersistLogoAndBackgroundSeparately()
    {
        var club = CreateClub();
        SetupClub(club);

        var result = await _service.PatchProfileAsync(
            "1",
            socialLinks: null,
            focusAreasJson: null,
            email: null,
            resourcesJson: null,
            uploadHints: new ClubProfileUploadHintsDto
            {
                HasLogo = true,
                HasBackground = true,
                LogoUrl = "https://example.com/clubs/1/logo.png",
                BackgroundUrl = "https://example.com/clubs/1/background.png"
            });

        result.Updated.Should().BeTrue();
        result.ProfileImageUrl.Should().Be("https://example.com/clubs/1/logo.png");
        result.BackgroundImageUrl.Should().Be("https://example.com/clubs/1/background.png");
        club.ProfileImageUrl.Should().Be("https://example.com/clubs/1/logo.png");
        club.BackgroundImageUrl.Should().Be("https://example.com/clubs/1/background.png");
        _clubRepository.Verify(x => x.UpdateAsync(club), Times.Once);
        _clubRepository.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetEmployeesAsync_ShouldReturnEmployees_WhenAuthLookupFails()
    {
        var club = CreateClub();
        SetupClub(club);

        var userId = Guid.NewGuid();
        _dbContext.ClubEmployees.Add(new ClubEmployee
        {
            Id = 17,
            ClubId = club.Id,
            UserId = userId,
            PositionId = 4
        });
        _dbContext.ClubPositions.Add(new ClubPosition
        {
            Id = 4,
            ClubId = club.Id,
            Title = "Coordinator"
        });
        await _dbContext.SaveChangesAsync();

        _authUserLookup
            .Setup(x => x.GetUserNamesByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NpgsqlException("Auth DB unavailable"));

        var result = await _service.GetEmployeesAsync("1", 1, 50);

        result.Items.Should().ContainSingle();
        result.Items[0].Id.Should().Be("17");
        result.Items[0].StudentId.Should().Be(userId.ToString("D"));
        result.Items[0].EmployeeName.Should().BeNull();
        result.Items[0].PositionId.Should().Be("4");
        result.Items[0].PositionTitle.Should().Be("Coordinator");
        result.Items[0].PositionName.Should().Be("Coordinator");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    private void SetupClub(Club club)
    {
        _clubService.Setup(x => x.GetByIdAsync(club.Id)).ReturnsAsync(new ClubDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            CategoryId = club.CategoryId
        });
        _clubRepository.Setup(x => x.GetByIdWithCategoryAsync(club.Id)).ReturnsAsync(club);
        _clubRepository.Setup(x => x.UpdateAsync(club)).Returns(Task.CompletedTask);
        _clubRepository.Setup(x => x.SaveChangesAsync()).Returns(Task.CompletedTask);
    }

    private static Club CreateClub()
    {
        return new Club
        {
            Id = 1,
            Name = "Robotics Club",
            Description = "Club description",
            CategoryId = 3,
            Category = new Category { Id = 3, Name = "Technology" }
        };
    }
}
