using ClubManagementService.API.Controllers;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using System.Text;

namespace MyAda.Tests.Controllers.ClubManagementService;

public class ClubProposalsControllerTests
{
    [Fact]
    public async Task CreateMultipart_ShouldUploadFilesAndSubmitUserList()
    {
        const string organizationId = "123456789";
        const string userId1 = "123456781";
        const string userId2 = "123456782";

        var proposalContract = new Mock<IProposalContractService>();
        proposalContract
            .Setup(x => x.SubmitClubProposalAsync(
                organizationId,
                It.Is<ClubProposalSubmissionDto>(dto =>
                    dto.OrganizationId == organizationId
                    && dto.LogoFile == "https://bucket.example/logo.png"
                    && dto.ConstitutionFile == "https://bucket.example/constitution.pdf"
                    && dto.LogoFilePart != null
                    && dto.LogoFilePart.FileName == "logo.png"
                    && dto.ConstitutionFilePart != null
                    && dto.ConstitutionFilePart.FileName == "constitution.pdf"
                    && dto.OtherMembersJson.SequenceEqual(new[] { userId1, userId2 })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ClubProposalSubmitResult
            {
                Success = true,
                Payload = new { proposalId = "PROP-2026-001", status = "pending_review" }
            });

        var imageStorage = new Mock<IImageStorageService>();
        imageStorage.Setup(x => x.IsSupportedImage("logo.png")).Returns(true);
        imageStorage.Setup(x => x.IsSupportedImage("constitution.pdf")).Returns(true);
        imageStorage
            .Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "logo.png",
                "image/png",
                "club-proposals/logo",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/logo.png");
        imageStorage
            .Setup(x => x.UploadImageAsync(
                It.IsAny<Stream>(),
                "constitution.pdf",
                "application/pdf",
                "club-proposals/constitution",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("https://bucket.example/constitution.pdf");

        var controller = new ClubProposalsController(proposalContract.Object, imageStorage.Object);
        var httpContext = new DefaultHttpContext();
        var files = new FormFileCollection
        {
            CreateFormFile("logoFile", "logo.png", "image/png"),
            CreateFormFile("constitutionFile", "constitution.pdf", "application/pdf")
        };
        var form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["organizationId"] = organizationId,
            ["name"] = "Robotics Club",
            ["description"] = "Club proposal description",
            ["otherMembersJson"] = new StringValues(new[] { userId1, userId2 })
        }, files);
        httpContext.Request.ContentType = "multipart/form-data; boundary=test";
        httpContext.Features.Set<IFormFeature>(new TestFormFeature(form));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        proposalContract.VerifyAll();
        imageStorage.VerifyAll();
    }

    [Fact]
    public async Task Create_WithPdfContentType_ShouldReturnUnsupportedMediaType()
    {
        var controller = new ClubProposalsController(
            Mock.Of<IProposalContractService>(),
            Mock.Of<IImageStorageService>());

        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/pdf";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(CancellationToken.None);

        var unsupported = result.Should().BeOfType<ObjectResult>().Subject;
        unsupported.StatusCode.Should().Be(StatusCodes.Status415UnsupportedMediaType);
    }

    [Fact]
    public async Task Create_WithInvalidJsonBody_ShouldReturnBadRequest()
    {
        var proposalContract = new Mock<IProposalContractService>(MockBehavior.Strict);
        var imageStorage = new Mock<IImageStorageService>(MockBehavior.Strict);

        var controller = new ClubProposalsController(proposalContract.Object, imageStorage.Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.ContentType = "application/json";
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.7"));
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var result = await controller.Create(CancellationToken.None);

        result.Should().BeOfType<BadRequestObjectResult>();
        proposalContract.VerifyNoOtherCalls();
        imageStorage.VerifyNoOtherCalls();
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
