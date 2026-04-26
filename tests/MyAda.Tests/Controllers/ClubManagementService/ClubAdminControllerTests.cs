using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Club;
using ClubManagementService.Application.DTOs.ClubAdmin;
using ClubManagementService.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

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
