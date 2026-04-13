using Microsoft.AspNetCore.Mvc;
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
        return new SupportRequestsController(support.Object, assign.Object, status.Object);
    }

    [Fact]
    public async Task CreateRequest_returns_Ok_with_detail()
    {
        var support = new Mock<ISupportRequestService>();
        var dto = new CreateSupportRequestDto { Description = "x", CategoryId = 1, Area = "it", LocationType = "campus", Urgency = "standard" };
        var detail = new SupportRequestDetailDto { Id = 1, Description = "x" };
        support.Setup(s => s.CreateRequestAsync(dto, 5)).ReturnsAsync(detail);
        var controller = CreateController(support);

        var result = await controller.CreateRequest(5, dto);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(detail, ok.Value);
    }

    [Fact]
    public async Task GetMyRequests_returns_Ok()
    {
        var support = new Mock<ISupportRequestService>();
        var list = new List<SupportRequestListItemDto>();
        support.Setup(s => s.GetMyRequestsAsync(3)).ReturnsAsync(list);
        var controller = CreateController(support);

        var result = await controller.GetMyRequests(3);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetAssignedRequests_returns_Ok()
    {
        var support = new Mock<ISupportRequestService>();
        support.Setup(s => s.GetAssignedRequestsAsync(2)).ReturnsAsync(new List<SupportRequestListItemDto>());
        var controller = CreateController(support);

        var result = await controller.GetAssignedRequests(2);

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
        support.Setup(s => s.CancelRequestAsync(1, 2, It.IsAny<CancelRequestDto>())).Returns(Task.CompletedTask);
        var controller = CreateController(support);

        var result = await controller.CancelRequest(1, 2, new CancelRequestDto { Reason = "r" });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task AssignRequest_returns_NoContent()
    {
        var assign = new Mock<IAssignmentService>();
        assign.Setup(a => a.AssignRequestAsync(1, It.IsAny<AssignRequestDto>(), 9)).Returns(Task.CompletedTask);
        var controller = CreateController(assign: assign);

        var result = await controller.AssignRequest(1, 9, new AssignRequestDto { StaffId = 4 });

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task MarkAsCompleted_returns_NoContent()
    {
        var status = new Mock<IRequestStatusService>();
        status.Setup(s => s.MarkAsCompletedAsync(1, 5)).Returns(Task.CompletedTask);
        var controller = CreateController(status: status);

        var result = await controller.MarkAsCompleted(1, 5);

        Assert.IsType<NoContentResult>(result);
    }
}
