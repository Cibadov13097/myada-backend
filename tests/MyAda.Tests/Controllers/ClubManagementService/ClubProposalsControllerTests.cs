using ClubManagementService.API.Controllers;
using ClubManagementService.API.Models;
using ClubManagementService.Application.DTOs.Contracts;
using ClubManagementService.Application.Interfaces.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace MyAda.Tests.Controllers.ClubManagementService;

public class ClubProposalsControllerTests
{
    [Fact]
    public async Task CreateMultipart_ShouldUploadFilesAndSubmitUserList()
    {
        var userId1 = Guid.NewGuid().ToString("D");
        var userId2 = Guid.NewGuid().ToString("D");

        var proposalContract = new Mock<IProposalContractService>();
        proposalContract
            .Setup(x => x.SubmitClubProposalAsync(
                "submitter-1",
                It.Is<ClubProposalSubmissionDto>(dto =>
                    dto.LogoFile == "https://bucket.example/logo.png"
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

        var result = await controller.Create(new ClubProposalMultipartForm
        {
            Id = "submitter-1",
            Name = "Robotics Club",
            Description = "Club proposal description",
            LogoFile = CreateFormFile("logoFile", "logo.png", "image/png"),
            ConstitutionFile = CreateFormFile("constitutionFile", "constitution.pdf", "application/pdf"),
            OtherMembersJson = new List<string> { userId1, userId2 }
        }, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        proposalContract.VerifyAll();
        imageStorage.VerifyAll();
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
