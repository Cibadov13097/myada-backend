using System.Security.Claims;
using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Infrastructure.Persistence.Context;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Moq;

namespace ClubManagementService.Tests.Controllers;

public class StudentServicesControllerTests
{
    [Fact]
    public async Task CreateStaffEventProposal_ShouldUploadAttachedImageFromImageAlias()
    {
        var proposalContract = new Mock<IProposalContractService>();
        proposalContract.Setup(x => x.SubmitStaffEventProposalAsync(
                It.Is<EventProposalSubmitDto>(dto =>
                    dto.Name == "Campus Fair"
                    && dto.ImageUrl == "https://bucket.example/student-services/event-proposals/poster.png"),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EventProposalSubmitResult
            {
                Success = true,
                Payload = new
                {
                    proposalId = "11",
                    id = "11",
                    status = "pending_review",
                    imageUrl = "https://bucket.example/student-services/event-proposals/poster.png"
                }
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("poster.png")).Returns(true);
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "poster.png",
                "image/png",
                "student-services/event-proposals",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/student-services/event-proposals/poster.png");

        var dbOptions = new DbContextOptionsBuilder<ClubManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var dbContext = new ClubManagementDbContext(dbOptions);

        var controller = new StudentServicesController(
            Mock.Of<IClubService>(),
            Mock.Of<IEventService>(),
            Mock.Of<IClubNotificationService>(),
            proposalContract.Object,
            storage.Object,
            dbContext);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString("D")),
                new Claim("sub", Guid.NewGuid().ToString("D")),
                new Claim(ClaimTypes.Role, "admin")
            },
            authenticationType: "TestAuth"));

        var form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["name"] = "Campus Fair",
            ["dateTime"] = "2026-04-26T14:30:00",
            ["duration"] = "2",
            ["attendance"] = "150",
            ["venue"] = "Auditorium",
            ["description"] = "Event description",
            ["objectives"] = "Event objectives"
        }, new FormFileCollection
        {
            CreateFormFile("image", "poster.png", "image/png")
        });

        httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        httpContext.Features.Set<IFormFeature>(new TestFormFeature(form));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.CreateStaffEventProposal(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        proposalContract.VerifyAll();
        storage.VerifyAll();
    }

    private static IFormFile CreateFormFile(string fieldName, string fileName, string contentType)
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, fieldName, fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TestFormFeature(IFormCollection form) : IFormFeature
    {
        public bool HasFormContentType => true;
        public IFormCollection? Form { get; set; } = form;
        public IFormCollection ReadForm() => Form ?? new FormCollection(new Dictionary<string, StringValues>());
        public Task<IFormCollection> ReadFormAsync(CancellationToken cancellationToken) => Task.FromResult(ReadForm());
    }
}
