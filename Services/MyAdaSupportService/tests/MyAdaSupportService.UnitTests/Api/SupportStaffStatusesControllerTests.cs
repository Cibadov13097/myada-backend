using Microsoft.AspNetCore.Mvc;
using Moq;
using MyAdaSupportService.Api.Controllers;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;

namespace MyAdaSupportService.UnitTests.Api;

public class SupportStaffStatusesControllerTests
{
    [Fact]
    public async Task GetByMemberId_returns_Ok_when_found()
    {
        var svc = new Mock<ISupportStaffStatusService>();
        svc.Setup(s => s.GetByMemberIdAsync(1)).ReturnsAsync(new SupportStaffStatusDto { MemberId = 1, Status = "Online" });
        var controller = new SupportStaffStatusesController(svc.Object);

        var result = await controller.GetByMemberId(1);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByMemberId_returns_NotFound_on_KeyNotFound()
    {
        var svc = new Mock<ISupportStaffStatusService>();
        svc.Setup(s => s.GetByMemberIdAsync(2)).ThrowsAsync(new KeyNotFoundException("none"));
        var controller = new SupportStaffStatusesController(svc.Object);

        var result = await controller.GetByMemberId(2);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByStatus_returns_Ok()
    {
        var svc = new Mock<ISupportStaffStatusService>();
        svc.Setup(s => s.GetByStatusAsync("Online")).ReturnsAsync(new List<SupportStaffStatusDto>());
        var controller = new SupportStaffStatusesController(svc.Object);

        var result = await controller.GetByStatus("Online");

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
