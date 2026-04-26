using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;

namespace MyAda.Tests.Controllers.ClubManagementService;

public class ClubAdminControllerTests
{
    [Fact]
    public async Task CreateFile_ShouldUploadAndPersistFile()
    {
        var clubAdmin = new Mock<IClubAdminService>();
        clubAdmin.Setup(x => x.CreateFileAsync("1", It.Is<ClubAdminCreateFileInput>(input =>
                input.Url == "https://bucket.example/guide.pdf" && input.Title == "Guide"),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubResourceItemDto
            {
                Id = "file-1",
                Url = "https://bucket.example/guide.pdf",
                Title = "Guide"
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("guide.pdf")).Returns(true);
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "guide.pdf",
                "application/pdf",
                "clubs/1/files",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/guide.pdf");

        var controller = new ClubAdminController(clubAdmin.Object, storage.Object);
        var result = await controller.CreateFile("1", new ClubAdminController.ClubFileCreateRequest
        {
            Title = "Guide",
            File = CreateFormFile("file", "guide.pdf", "application/pdf")
        }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var body = ok.Value.Should().BeAssignableTo<ClubResourceItemDto>().Subject;
        body.Id.Should().Be("file-1");
        body.Url.Should().Be("https://bucket.example/guide.pdf");
        storage.VerifyAll();
        clubAdmin.VerifyAll();
    }

    [Fact]
    public async Task PatchFile_ShouldRejectUnsupportedFileTypes()
    {
        var clubAdmin = new Mock<IClubAdminService>();
        clubAdmin.Setup(x => x.GetFileAsync("1", "file-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubResourceItemDto
            {
                Id = "file-1",
                Url = "https://bucket.example/guide.pdf",
                Title = "Guide"
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("script.exe")).Returns(false);

        var controller = new ClubAdminController(clubAdmin.Object, storage.Object);
        var result = await controller.PatchFile("1", "file-1", new ClubAdminController.ClubFilePatchRequest
        {
            File = CreateFormFile("file", "script.exe", "application/octet-stream")
        }, CancellationToken.None);

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.Value.Should().NotBeNull();
        clubAdmin.Verify(x => x.UpdateFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ClubAdminUpdateFileInput>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteFile_ShouldDeleteStoredObjectAfterMetadataRemoval()
    {
        var clubAdmin = new Mock<IClubAdminService>();
        clubAdmin.Setup(x => x.DeleteFileAsync("1", "file-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubResourceItemDto
            {
                Id = "file-1",
                Url = "https://bucket.example/guide.pdf",
                Title = "Guide"
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.DeleteFileAsync("https://bucket.example/guide.pdf", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ClubAdminController(clubAdmin.Object, storage.Object);
        var result = await controller.DeleteFile("1", "file-1", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        storage.VerifyAll();
        clubAdmin.VerifyAll();
    }

    [Fact]
    public async Task PatchProfile_ShouldReturnSeparateImageFields()
    {
        var clubAdmin = new Mock<IClubAdminService>();
        clubAdmin.Setup(x => x.PatchProfileAsync(
                "1",
                null,
                null,
                null,
                null,
                It.Is<ClubProfileUploadHintsDto>(h =>
                    h.HasLogo &&
                    h.HasBackground &&
                    h.LogoUrl == "https://bucket.example/clubs/1/profile/logo.png" &&
                    h.BackgroundUrl == "https://bucket.example/clubs/1/profile/background.png"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfilePatchResultDto
            {
                ClubId = "1",
                Updated = true,
                PendingReview = false,
                ProfileImageUrl = "https://bucket.example/clubs/1/profile/logo.png",
                BackgroundImageUrl = "https://bucket.example/clubs/1/profile/background.png"
            });

        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("logo.png")).Returns(true);
        storage.Setup(x => x.IsSupportedImage("background.png")).Returns(true);
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "logo.png",
                "image/png",
                "clubs/1/profile/logo",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/clubs/1/profile/logo.png");
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "background.png",
                "image/png",
                "clubs/1/profile/background",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/clubs/1/profile/background.png");

        var controller = new ClubAdminController(clubAdmin.Object, storage.Object);
        var result = await controller.PatchProfile("1", new ClubAdminController.ClubProfilePatchRequest
        {
            LogoFile = CreateFormFile("logoFile", "logo.png", "image/png"),
            BackgroundFile = CreateFormFile("backgroundFile", "background.png", "image/png")
        }, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
        doc.RootElement.GetProperty("profileImageUrl").GetString().Should().Be("https://bucket.example/clubs/1/profile/logo.png");
        doc.RootElement.GetProperty("backgroundImageUrl").GetString().Should().Be("https://bucket.example/clubs/1/profile/background.png");
        clubAdmin.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task PatchProfile_ShouldDeleteUploadedLogo_WhenBackgroundIsUnsupported()
    {
        var storage = new Mock<IImageStorageService>();
        storage.Setup(x => x.IsSupportedImage("logo.png")).Returns(true);
        storage.Setup(x => x.IsSupportedImage("background.exe")).Returns(false);
        storage.Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "logo.png",
                "image/png",
                "clubs/1/profile/logo",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/clubs/1/profile/logo.png");
        storage.Setup(x => x.DeleteFileAsync("https://bucket.example/clubs/1/profile/logo.png", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var controller = new ClubAdminController(Mock.Of<IClubAdminService>(), storage.Object);
        var result = await controller.PatchProfile("1", new ClubAdminController.ClubProfilePatchRequest
        {
            LogoFile = CreateFormFile("logoFile", "logo.png", "image/png"),
            BackgroundFile = CreateFormFile("backgroundFile", "background.exe", "application/octet-stream")
        }, CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
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
}
