using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Moq;
using MyAdaSupportService.Api.Controllers;
using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.ServicesInterfaces;

namespace MyAdaSupportService.UnitTests.Api;

public class SupportRequestsControllerTests
{
    private static SupportRequestsController CreateController(
        Mock<ISupportRequestService>? support = null,
        Mock<IAssignmentService>? assign = null,
        Mock<IRequestStatusService>? status = null)
    {
        support ??= new Mock<ISupportRequestService>();
        assign ??= new Mock<IAssignmentService>();
        status ??= new Mock<IRequestStatusService>();
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(x => x.WebRootPath).Returns("wwwroot");
        env.SetupGet(x => x.ContentRootPath).Returns(".");
        return new SupportRequestsController(support.Object, assign.Object, status.Object, env.Object);
    }

    [Fact]
    public async Task CreateRequest_returns_Ok_with_detail()
    {
        var support = new Mock<ISupportRequestService>();
        var dto = new CreateSupportRequestDto { Description = "x", CategoryId = 1, Area = "it", LocationType = "campus", Urgency = "standard" };
        var detail = new SupportRequestDetailDto { Id = 1, Description = "x" };
        var memberId = Guid.NewGuid();
        support.Setup(s => s.CreateRequestAsync(dto, memberId)).ReturnsAsync(detail);
        var controller = CreateController(support);

        var result = await controller.CreateRequest(memberId, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public async Task GetMyRequests_returns_Ok()
    {
        var support = new Mock<ISupportRequestService>();
        var list = new List<SupportRequestListItemDto>();
        var memberId = Guid.NewGuid();
        support.Setup(s => s.GetMyRequestsAsync(memberId)).ReturnsAsync(list);
        var controller = CreateController(support);

        var result = await controller.GetMyRequests(memberId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAssignedRequests_returns_Ok()
    {
        var support = new Mock<ISupportRequestService>();
        var staffId = Guid.NewGuid();
        support.Setup(s => s.GetAssignedRequestsAsync(staffId)).ReturnsAsync(new List<SupportRequestListItemDto>());
        var controller = CreateController(support);

        var result = await controller.GetAssignedRequests(staffId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetRequestDetail_returns_Ok()
    {
        var support = new Mock<ISupportRequestService>();
        support.Setup(s => s.GetRequestDetailAsync(9)).ReturnsAsync(new SupportRequestDetailDto { Id = 9 });
        var controller = CreateController(support);

        var result = await controller.GetRequestDetail(9);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CancelRequest_returns_NoContent()
    {
        var support = new Mock<ISupportRequestService>();
        var memberId = Guid.NewGuid();
        support.Setup(s => s.CancelRequestAsync(1, memberId, It.IsAny<CancelRequestDto>())).Returns(Task.CompletedTask);
        var controller = CreateController(support);

        var result = await controller.CancelRequest(1, memberId, new CancelRequestDto { Reason = "r" });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AssignRequest_returns_NoContent()
    {
        var assign = new Mock<IAssignmentService>();
        var dispatcherId = Guid.NewGuid();
        assign.Setup(a => a.AssignRequestAsync(1, It.IsAny<AssignRequestDto>(), dispatcherId)).Returns(Task.CompletedTask);
        var controller = CreateController(assign: assign);

        var result = await controller.AssignRequest(1, dispatcherId, new AssignRequestDto { StaffId = Guid.NewGuid() });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task MarkAsCompleted_returns_NoContent()
    {
        var status = new Mock<IRequestStatusService>();
        var staffId = Guid.NewGuid();
        status.Setup(s => s.MarkAsCompletedAsync(1, staffId)).Returns(Task.CompletedTask);
        var controller = CreateController(status: status);

        var result = await controller.MarkAsCompleted(1, staffId);

        Assert.IsType<NoContentResult>(result);
    }
}
