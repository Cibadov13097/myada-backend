using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using MyAdaSupportService.Api.Controllers;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.ServicesInterfaces;
using System.Security.Claims;

namespace MyAdaSupportService.UnitTests.Api;

public class SupportStaffStatusesControllerTests
{
    private static SupportStaffStatusesController CreateController(
        ISupportStaffStatusService service,
        Guid? authorizedUserId = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalAuth:ProvisioningKey"] = "test-key"
            })
            .Build();

        var controller = new SupportStaffStatusesController(service, configuration);
        var claims = new List<Claim>();
        if (authorizedUserId.HasValue)
            claims.Add(new Claim("sub", authorizedUserId.Value.ToString()));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
            }
        };

        return controller;
    }

    [Fact]
    public async Task Create_returns_CreatedAtAction()
    {
        var memberId = Guid.NewGuid();
        var svc = new Mock<ISupportStaffStatusService>();
        var createDto = new CreateSupportStaffStatusDto { MemberId = memberId, Status = "Online" };
        var createdDto = new SupportStaffStatusDto { Id = 1, MemberId = memberId, Status = "Online" };
        svc.Setup(s => s.CreateStatusAsync(It.Is<CreateSupportStaffStatusDto>(d => d.MemberId == memberId && d.Status == "Online")))
            .ReturnsAsync(createdDto);
        var controller = CreateController(svc.Object, memberId);

        var result = await controller.Create(createDto);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(SupportStaffStatusesController.GetByMemberId), created.ActionName);
        var payload = Assert.IsType<SupportStaffStatusDto>(created.Value);
        Assert.Equal(memberId, payload.MemberId);
    }

    [Fact]
    public async Task Create_returns_Forbid_when_member_does_not_match_authorized_user()
    {
        var authorizedUserId = Guid.NewGuid();
        var anotherMemberId = Guid.NewGuid();
        var svc = new Mock<ISupportStaffStatusService>();
        var controller = CreateController(svc.Object, authorizedUserId);

        var result = await controller.Create(new CreateSupportStaffStatusDto
        {
            MemberId = anotherMemberId,
            Status = "Online"
        });

        Assert.IsType<ForbidResult>(result.Result);
        svc.Verify(s => s.CreateStatusAsync(It.IsAny<CreateSupportStaffStatusDto>()), Times.Never);
    }

    [Fact]
    public async Task GetByMemberId_returns_Ok_when_found()
    {
        var memberId = Guid.NewGuid();
        var svc = new Mock<ISupportStaffStatusService>();
        svc.Setup(s => s.GetOrCreateByMemberIdAsync(memberId))
            .ReturnsAsync(new SupportStaffStatusDto { MemberId = memberId, Status = "Online" });
        var controller = CreateController(svc.Object, memberId);

        var result = await controller.GetByMemberId(memberId);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetByMemberId_returns_Forbid_when_member_does_not_match_authorized_user()
    {
        var authorizedUserId = Guid.NewGuid();
        var anotherMemberId = Guid.NewGuid();
        var svc = new Mock<ISupportStaffStatusService>();
        var controller = CreateController(svc.Object, authorizedUserId);

        var result = await controller.GetByMemberId(anotherMemberId);

        Assert.IsType<ForbidResult>(result.Result);
        svc.Verify(s => s.GetOrCreateByMemberIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetByStatus_returns_Ok()
    {
        var svc = new Mock<ISupportStaffStatusService>();
        svc.Setup(s => s.GetByStatusAsync("Online")).ReturnsAsync(new List<SupportStaffStatusDto>());
        var controller = CreateController(svc.Object);

        var result = await controller.GetByStatus("Online");

        Assert.IsType<OkObjectResult>(result.Result);
    }
}
