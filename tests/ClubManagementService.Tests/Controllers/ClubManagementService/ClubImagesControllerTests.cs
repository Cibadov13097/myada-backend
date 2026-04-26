using System.Security.Claims;
using System.Text.Json;
using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using ClubManagementService.Infrastructure.Persistence.Context;
using ClubManagementService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace ClubManagementService.Tests.Controllers;

public class ClubImagesControllerTests
{
    [Fact]
    public async Task ClubsController_GetAll_ShouldReturnSeparateProfileAndBackgroundImages()
    {
        var clubService = new Mock<IClubService>();
        clubService.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ClubDto>
        {
            new()
            {
                Id = 1,
                Name = "Robotics Club",
                Description = "Build and learn",
                Status = ClubStatus.Approved,
                CategoryName = "Technology",
                ProfileImageUrl = "https://bucket.example/clubs/robotics/logo.png",
                BackgroundImageUrl = "https://bucket.example/clubs/robotics/background.png"
            }
        });

        var controller = new ClubsController(
            clubService.Object,
            Mock.Of<IClubNotificationService>(),
            Mock.Of<IImageStorageService>(),
            new ClubAdminWorkspaceStore(),
            Mock.Of<global::ClubManagementService.Application.Interfaces.Repositories.IClubMembershipRepository>(),
            null!);

        var result = await controller.GetAll();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var item = doc.RootElement.GetProperty("items")[0];
        item.GetProperty("profileImageUrl").GetString().Should().Be("https://bucket.example/clubs/robotics/logo.png");
        item.GetProperty("backgroundImageUrl").GetString().Should().Be("https://bucket.example/clubs/robotics/background.png");
        item.GetProperty("backgroundImage").GetString().Should().Be("https://bucket.example/clubs/robotics/background.png");
        item.GetProperty("image").GetString().Should().Be("https://bucket.example/clubs/robotics/background.png");
    }

    [Fact]
    public async Task StudentServicesController_Clubs_ShouldReturnSeparateProfileAndBackgroundImages()
    {
        var clubService = new Mock<IClubService>();
        clubService.Setup(x => x.GetAllAsync()).ReturnsAsync(new List<ClubDto>
        {
            new()
            {
                Id = 5,
                Name = "Design Club",
                Description = "Create together",
                Status = ClubStatus.Approved,
                CategoryName = "Arts",
                ProfileImageUrl = "https://bucket.example/clubs/design/logo.webp",
                BackgroundImageUrl = "https://bucket.example/clubs/design/background.webp",
                ProposedProfileImageUrl = "/uploads/clubs/5/logo-proposed.png"
            }
        });

        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new ClubManagementDbContext(dbOptions);
        await dbContext.Database.EnsureCreatedAsync();

        var controller = new StudentServicesController(
            clubService.Object,
            Mock.Of<IEventService>(),
            Mock.Of<IClubNotificationService>(),
            Mock.Of<IProposalContractService>(),
            Mock.Of<IImageStorageService>(),
            dbContext);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.Role, "admin") },
                    "test"))
            }
        };

        var result = await controller.Clubs();

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        var item = doc.RootElement.GetProperty("items")[0];
        item.GetProperty("profileImageUrl").GetString().Should().Be("https://bucket.example/clubs/design/logo.webp");
        item.GetProperty("backgroundImageUrl").GetString().Should().Be("https://bucket.example/clubs/design/background.webp");
        item.GetProperty("backgroundImage").GetString().Should().Be("https://bucket.example/clubs/design/background.webp");
        item.GetProperty("image").GetString().Should().Be("https://bucket.example/clubs/design/background.webp");
        item.GetProperty("proposedProfileImageUrl").GetString().Should().Be("/uploads/clubs/5/logo-proposed.png");
    }
}
