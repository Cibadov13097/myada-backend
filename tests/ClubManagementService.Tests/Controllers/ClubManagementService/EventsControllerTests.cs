using System.Security.Claims;
using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Event;
using ClubManagementService.Application.Interfaces.Repositories;
using ClubManagementService.Application.Interfaces.Services;
using ClubManagementService.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;

namespace ClubManagementService.Tests.Controllers;

public class EventsControllerTests
{
    [Fact]
    public async Task Create_ShouldUploadAttachedImageAndPassStoredUrlToService()
    {
        var eventService = new Mock<IEventService>();
        eventService.Setup(x => x.CreateAsync(It.Is<CreateEventDto>(dto =>
                dto.ClubId == 3
                && dto.Name == "Hackathon"
                && dto.ImageUrl == "https://bucket.example/events/3/poster.png")))
            .ReturnsAsync(new EventDto
            {
                Id = 15,
                ClubId = 3,
                Name = "Hackathon",
                Description = "24 hours",
                ImageUrl = "https://bucket.example/events/3/poster.png",
                StartTime = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc),
                EndTime = new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
                Location = "Main Hall",
                SeatLimit = 120,
                Requirements = "Laptop",
                Prerequisites = "None",
                Notes = "Bring ID",
                ApprovedBy = TestUserId,
                Status = ClubStatus.Pending
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("poster.png")).Returns(true);
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "poster.png",
                "image/png",
                "events/3",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/events/3/poster.png");

        var controller = CreateController(eventService.Object, storage.Object);
        var httpContext = BuildMultipartHttpContext(
            new FormCollection(new Dictionary<string, StringValues>(), new FormFileCollection
            {
                CreateFormFile("imageFile", "poster.png", "image/png")
            }));

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(new EventsController.CreateEventFormRequest
        {
            ClubId = 3,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
            Location = "Main Hall",
            SeatLimit = 120,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID"
        }, CancellationToken.None);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        var body = created.Value.Should().BeAssignableTo<EventDto>().Subject;
        body.ImageUrl.Should().Be("https://bucket.example/events/3/poster.png");
        eventService.VerifyAll();
        storage.VerifyAll();
    }

    [Theory]
    [InlineData("imageUrl")]
    [InlineData("imgUrl")]
    public async Task Create_ShouldRejectDisallowedImageUrlFields(string fieldName)
    {
        var eventService = new Mock<IEventService>();
        var controller = CreateController(eventService.Object, Mock.Of<IImageStorageService>());
        var httpContext = BuildMultipartHttpContext(
            new FormCollection(new Dictionary<string, StringValues>
            {
                [fieldName] = "https://bucket.example/poster.png"
            }));

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(new EventsController.CreateEventFormRequest
        {
            ClubId = 3,
            Name = "Hackathon",
            Description = "24 hours",
            StartTime = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
            Location = "Main Hall",
            SeatLimit = 120,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID"
        }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        eventService.Verify(x => x.CreateAsync(It.IsAny<CreateEventDto>()), Times.Never);
    }

    [Fact]
    public async Task Update_ShouldKeepExistingImageWhenNoNewFileIsSent()
    {
        var existingEvent = new EventDto
        {
            Id = 15,
            ClubId = 3,
            Name = "Hackathon",
            Description = "24 hours",
            ImageUrl = "https://bucket.example/events/3/existing.png",
            StartTime = new DateTime(2026, 4, 30, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2026, 4, 30, 18, 0, 0, DateTimeKind.Utc),
            Location = "Main Hall",
            SeatLimit = 120,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            ApprovedBy = TestUserId,
            Status = ClubStatus.Pending
        };

        var eventService = new Mock<IEventService>();
        eventService.Setup(x => x.GetByIdAsync(15)).ReturnsAsync(existingEvent);
        eventService.Setup(x => x.UpdateAsync(15, It.Is<UpdateEventDto>(dto =>
                dto.ImageUrl == "https://bucket.example/events/3/existing.png"
                && dto.Status == ClubStatus.Approved)))
            .ReturnsAsync(new EventDto
            {
                Id = 15,
                ClubId = 3,
                Name = "Hackathon",
                Description = "Updated description",
                ImageUrl = "https://bucket.example/events/3/existing.png",
                StartTime = existingEvent.StartTime,
                EndTime = existingEvent.EndTime,
                Location = "Main Hall",
                SeatLimit = 120,
                Requirements = "Laptop",
                Prerequisites = "None",
                Notes = "Bring ID",
                ApprovedBy = TestUserId,
                Status = ClubStatus.Approved
            });

        var controller = CreateController(eventService.Object, Mock.Of<IImageStorageService>());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = BuildMultipartHttpContext(new FormCollection(new Dictionary<string, StringValues>()))
        };

        var result = await controller.Update(15, new EventsController.UpdateEventFormRequest
        {
            Name = "Hackathon",
            Description = "Updated description",
            StartTime = existingEvent.StartTime,
            EndTime = existingEvent.EndTime,
            Location = "Main Hall",
            SeatLimit = 120,
            Requirements = "Laptop",
            Prerequisites = "None",
            Notes = "Bring ID",
            Status = ClubStatus.Approved
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        eventService.VerifyAll();
    }

    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static EventsController CreateController(IEventService eventService, IImageStorageService imageStorageService)
    {
        return new EventsController(
            eventService,
            Mock.Of<IEventRegistrationService>(),
            Mock.Of<IClubMembershipRepository>(),
            Mock.Of<IClubNotificationService>(),
            imageStorageService);
    }

    private static DefaultHttpContext BuildMultipartHttpContext(IFormCollection form)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("sub", TestUserId.ToString("D")) },
                "test"))
        };

        httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        httpContext.Features.Set<IFormFeature>(new TestFormFeature(form));
        return httpContext;
    }

    private static IFormFile CreateFormFile(string fieldName, string fileName, string contentType)
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3, 4 });
        return new FormFile(stream, 0, stream.Length, fieldName, fileName)
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
