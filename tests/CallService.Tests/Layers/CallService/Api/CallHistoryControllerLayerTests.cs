using System.Security.Claims;
using CallService.Api.Controllers;
using CallService.Api.Persistence;
using CallService.Api.Realtime;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CallService.Tests.Layers.Api;

/// <summary>Small service — presentation / API layer guideline ~5–10 tests.</summary>
public sealed class CallHistoryControllerLayerTests
{
    private static CallHistoryController CreateController(ICallHistoryService svc, ClaimsPrincipal? user = null)
    {
        var controller = new CallHistoryController(svc)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user ?? new ClaimsPrincipal() }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetHistory_returns_unauthorized_without_sub()
    {
        var mock = new Mock<ICallHistoryService>();
        var controller = CreateController(mock.Object);

        var result = await controller.GetHistory(null, 50, CancellationToken.None);

        result.Result.Should().BeOfType<UnauthorizedResult>();
        mock.Verify(
            m => m.GetHistoryForUserAsync(It.IsAny<string>(), It.IsAny<CallRequestStatus?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetHistory_returns_bad_request_for_invalid_status()
    {
        var mock = new Mock<ICallHistoryService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }, "t"));
        var controller = CreateController(mock.Object, user);

        var result = await controller.GetHistory("not-a-status", 50, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetHistory_clamps_limit_to_max()
    {
        var mock = new Mock<ICallHistoryService>();
        mock.Setup(m => m.GetHistoryForUserAsync("user-1", null, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CallHistoryRecord>());
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }, "t"));
        var controller = CreateController(mock.Object, user);

        _ = await controller.GetHistory(null, 9999, CancellationToken.None);

        mock.Verify(m => m.GetHistoryForUserAsync("user-1", null, 200, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCallId_returns_bad_request_for_empty_id()
    {
        var mock = new Mock<ICallHistoryService>();
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }, "t"));
        var controller = CreateController(mock.Object, user);

        var result = await controller.GetByCallId("   ", CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetByCallId_returns_not_found_when_service_returns_null()
    {
        var mock = new Mock<ICallHistoryService>();
        mock.Setup(m => m.GetCallForUserAsync("abc", "user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CallHistoryRecord?)null);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("sub", "user-1") }, "t"));
        var controller = CreateController(mock.Object, user);

        var result = await controller.GetByCallId("abc", CancellationToken.None);

        result.Result.Should().BeOfType<NotFoundResult>();
    }
}
