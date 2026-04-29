using System.Text;
using AutoMapper;
using FluentAssertions;
using global::LostFoundService.Api.Controllers;
using global::LostFoundService.Api.Mappings;
using global::LostFoundService.Api.Models.LostAndFound.Responses;
using global::LostFoundService.Application.DTOs.LostAndFound;
using global::LostFoundService.Application.Interfaces.Services;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace LostFoundService.Tests.Layers.Api;

public sealed class LostAndFoundApiLayerTests
{
    [Fact]
    public async Task CreateFoundReport_returns_bad_request_when_json_uses_image_url_field()
    {
        var controller = BuildController();
        var httpContext = new DefaultHttpContext();
        var payload = """
            {
              "type": "found",
              "itemName": "Phone",
              "categoryId": 3,
              "description": "Black phone",
              "location": "Library",
              "imageUrl": "https://example.com/phone.png"
            }
            """;

        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(payload));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.CreateFoundReport();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequest.Value.Should().BeOfType<ErrorResponse>().Subject;
        error.Message.Should().Be("Found reports accept image attachments as files, not body URL fields.");
        error.Details.Should().ContainKey("unsupportedFields");
        error.Details!["unsupportedFields"].Should().ContainSingle(message => message.Contains("'imageUrl'"));
    }

    [Fact]
    public async Task CreateFoundReport_returns_bad_request_when_form_uses_image_url_field()
    {
        var controller = BuildController();
        var httpContext = new DefaultHttpContext();
        var form = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["type"] = "found",
                ["itemName"] = "Wallet",
                ["categoryId"] = "4",
                ["description"] = "Brown leather wallet",
                ["location"] = "Cafeteria",
                ["imageUrl"] = "https://example.com/wallet.png"
            },
            new FormFileCollection());

        httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        httpContext.Features.Set<IFormFeature>(new FormFeature(form));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.CreateFoundReport();

        var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        var error = badRequest.Value.Should().BeOfType<ErrorResponse>().Subject;
        error.Details.Should().ContainKey("unsupportedFields");
        error.Details!["unsupportedFields"].Should().ContainSingle(message => message.Contains("'imageUrl'"));
    }

    [Fact]
    public async Task CreateFoundReport_uploads_attached_files_from_multipart_form()
    {
        var lostAndFoundService = new Mock<ILostAndFoundService>();
        var imageStorageService = new Mock<IImageStorageService>();
        CreateLostFoundItemCommand? capturedCommand = null;

        imageStorageService
            .Setup(storage => storage.IsSupportedImage("phone.png"))
            .Returns(true);
        imageStorageService
            .Setup(storage => storage.UploadImageAsync(
                It.IsAny<Stream>(),
                "phone.png",
                "image/png",
                "items",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://cdn.example.com/items/phone.png");
        lostAndFoundService
            .Setup(service => service.CreateItemAsync(It.IsAny<CreateLostFoundItemCommand>()))
            .Callback<CreateLostFoundItemCommand>(command => capturedCommand = command)
            .ReturnsAsync(new LostFoundItem
            {
                Id = 42,
                ReferenceNumber = "LF-42",
                Type = LostFoundItemType.Found,
                Status = LostFoundPublicStatus.PendingVerification,
                PostedAt = DateTime.UtcNow
            });

        var controller = BuildController(lostAndFoundService.Object, imageStorageService.Object);
        var httpContext = new DefaultHttpContext();
        var fileContent = new MemoryStream(Encoding.UTF8.GetBytes("png"));
        var file = new FormFile(fileContent, 0, fileContent.Length, "files", "phone.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
        var form = new FormCollection(
            new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                ["type"] = "found",
                ["itemName"] = "Phone",
                ["categoryId"] = "8",
                ["description"] = "Black smartphone",
                ["location"] = "Auditorium"
            },
            new FormFileCollection { file });

        httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        httpContext.Features.Set<IFormFeature>(new FormFeature(form));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var result = await controller.CreateFoundReport();

        var created = result.Result.Should().BeOfType<ObjectResult>().Subject;
        created.StatusCode.Should().Be(StatusCodes.Status201Created);
        capturedCommand.Should().NotBeNull();
        capturedCommand!.ImageUrls.Should().ContainSingle("https://cdn.example.com/items/phone.png");
    }

    private static LostAndFoundController BuildController(
        ILostAndFoundService? lostAndFoundService = null,
        IImageStorageService? imageStorageService = null)
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<LostAndFoundApiProfile>(), NullLoggerFactory.Instance)
            .CreateMapper();

        return new LostAndFoundController(
            lostAndFoundService ?? Mock.Of<ILostAndFoundService>(),
            imageStorageService ?? Mock.Of<IImageStorageService>(),
            mapper);
    }
}
