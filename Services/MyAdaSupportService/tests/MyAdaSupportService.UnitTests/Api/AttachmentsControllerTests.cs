using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaSupportService.Api.Controllers;
using MyAdaSupportService.Application.DTOs.Attcahment;
using MyAdaSupportService.Application.ServicesInterfaces;

namespace MyAdaSupportService.UnitTests.Api;

public class AttachmentsControllerTests
{
    [Fact]
    public async Task AddAttachment_returns_Ok()
    {
        var svc = new Mock<IAttachmentService>();
        svc.Setup(s => s.AddAttachmentAsync(It.IsAny<AttachmentCreateDto>()))
            .ReturnsAsync(new AttachmentDto { Id = 1 });
        var controller = new AttachmentsController(svc.Object);

        var result = await controller.AddAttachment(new AttachmentCreateDto { RequestId = 1, Url = "u" });

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRequestAttachments_returns_Ok()
    {
        var svc = new Mock<IAttachmentService>();
        svc.Setup(s => s.GetRequestAttachmentsAsync(7)).ReturnsAsync(new List<AttachmentDto>());
        var controller = new AttachmentsController(svc.Object);

        var result = await controller.GetRequestAttachments(7);

        Assert.IsType<OkObjectResult>(result.Result);
    }

}
