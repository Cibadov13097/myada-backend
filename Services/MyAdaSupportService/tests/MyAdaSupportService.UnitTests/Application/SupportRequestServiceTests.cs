using Moq;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Dispatcher;
using MyAdaSupportService.Application.DTOs.Filter;
using MyAdaSupportService.Application.DTOs.Request;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Application;

public class SupportRequestServiceTests
{
    private static SupportRequestService CreateSut(
        Mock<IRequestRepository> repo,
        Mock<ILocationService>? location = null)
    {
        var loc = location ?? new Mock<ILocationService>();
        var realtime = new Mock<ISupportRealtimeNotifier>();
        realtime.Setup(r => r.NotifySupportRequestChangedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        loc.Setup(l => l.ResolveLocationAsync(
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()))
            .ReturnsAsync("Resolved");
        return new SupportRequestService(repo.Object, TestMapperFactory.Create(), loc.Object, realtime.Object);
    }

    private static Request RequestWithCategory(int id, Guid createdUserId, Guid? staffId = null) => new()
    {
        Id = id,
        CreatedUserId = createdUserId,
        StaffId = staffId,
        Status = RequestStatus.New,
        Description = "d",
        Category = new Category { Name = "Cat", Module = SupportArea.IT }
    };

    [Fact]
    public async Task CreateRequestAsync_sets_member_status_and_resolves_location()
    {
        var repo = new Mock<IRequestRepository>();
        var loc = new Mock<ILocationService>();
        loc.Setup(l => l.ResolveLocationAsync(false, null, false, null, null)).ReturnsAsync("Here");
        repo.Setup(r => r.AddAsync(It.IsAny<Request>()))
            .ReturnsAsync((Request r) =>
            {
                r.Id = 3;
                r.Category = new Category { Name = "Cat", Module = SupportArea.IT };
                return r;
            });
        var realtime = new Mock<ISupportRealtimeNotifier>();
        realtime.Setup(r => r.NotifySupportRequestChangedAsync(It.IsAny<int>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var sut = new SupportRequestService(repo.Object, TestMapperFactory.Create(), loc.Object, realtime.Object);
        var dto = new CreateSupportRequestDto
        {
            CategoryId = 1,
            Description = "Help",
            Area = "it",
            LocationType = "campus",
            Urgency = "standard"
        };

        var memberId = Guid.NewGuid();
        var result = await sut.CreateRequestAsync(dto, memberId);

        Assert.Equal(3, result.Id);
        Assert.Equal("Here", result.Location);
        repo.Verify(r => r.AddAsync(It.Is<Request>(x => x.CreatedUserId == memberId && x.Status == RequestStatus.New)), Times.Once);
    }

    [Fact]
    public async Task GetMyRequestsAsync_maps_each_item_with_location()
    {
        var repo = new Mock<IRequestRepository>();
        var memberId = Guid.NewGuid();
        var requests = new List<Request> { RequestWithCategory(1, memberId) };
        repo.Setup(r => r.GetRequestsByMemberIdAsync(memberId)).ReturnsAsync(requests);
        var sut = CreateSut(repo);

        var result = await sut.GetMyRequestsAsync(memberId);

        Assert.Single(result);
        Assert.Equal("Resolved", result[0].Location);
    }

    [Fact]
    public async Task GetAssignedRequestsAsync_returns_list()
    {
        var repo = new Mock<IRequestRepository>();
        var staffId = Guid.NewGuid();
        repo.Setup(r => r.GetRequestsByStaffIdAsync(staffId))
            .ReturnsAsync(new List<Request> { RequestWithCategory(1, Guid.NewGuid(), staffId) });
        var sut = CreateSut(repo);

        var result = await sut.GetAssignedRequestsAsync(staffId);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllRequestsAsync_applies_filter_and_orders()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetAllWithDetailsAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Request, bool>>>()))
            .ReturnsAsync(new List<Request> { RequestWithCategory(1, Guid.NewGuid()) });
        var sut = CreateSut(repo);
        var filter = new RequestFilterDto { MemberId = null, StaffId = null };

        var result = await sut.GetAllRequestsAsync(filter);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRequestDetailAsync_returns_detail_when_found()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdWithDetailsOrDefaultAsync(7)).ReturnsAsync(RequestWithCategory(7, Guid.NewGuid()));
        var sut = CreateSut(repo);

        var result = await sut.GetRequestDetailAsync(7);

        Assert.Equal(7, result.Id);
        Assert.Equal("Resolved", result.Location);
    }

    [Fact]
    public async Task GetRequestDetailAsync_throws_when_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdWithDetailsOrDefaultAsync(404)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.GetRequestDetailAsync(404));
    }

    [Fact]
    public async Task CancelRequestAsync_updates_status_when_owner_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var memberId = Guid.NewGuid();
        var req = RequestWithCategory(1, memberId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.CancelRequestAsync(1, memberId, new CancelRequestDto { Reason = "No longer needed" });

        Assert.Equal(RequestStatus.Cancelled, req.Status);
        Assert.Equal("No longer needed", req.CancelReason);
    }

    [Fact]
    public async Task CancelRequestAsync_throws_when_wrong_member()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(RequestWithCategory(1, Guid.NewGuid()));
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.CancelRequestAsync(1, Guid.NewGuid(), new CancelRequestDto { Reason = "x" }));
    }

    [Fact]
    public async Task CancelRequestAsync_throws_when_request_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(2)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.CancelRequestAsync(2, Guid.NewGuid(), new CancelRequestDto { Reason = "x" }));
    }

    [Fact]
    public async Task AssignRequestAsync_sets_staff_and_status()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, Guid.NewGuid());
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        var assignedStaffId = Guid.NewGuid();
        await sut.AssignRequestAsync(1, new AssignRequestDto { StaffId = assignedStaffId }, dispatcherId: Guid.NewGuid());

        Assert.Equal(assignedStaffId, req.StaffId);
        Assert.Equal(RequestStatus.Assigned, req.Status);
    }

    [Fact]
    public async Task AssignRequestAsync_throws_when_request_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(9)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.AssignRequestAsync(9, new AssignRequestDto { StaffId = Guid.NewGuid() }, dispatcherId: Guid.NewGuid()));
    }

    [Fact]
    public async Task ReassignRequestAsync_updates_staff()
    {
        var repo = new Mock<IRequestRepository>();
        var originalStaffId = Guid.NewGuid();
        var req = RequestWithCategory(1, Guid.NewGuid(), staffId: originalStaffId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        var newStaffId = Guid.NewGuid();
        await sut.ReassignRequestAsync(1, new AssignRequestDto { StaffId = newStaffId }, dispatcherId: Guid.NewGuid());

        Assert.Equal(newStaffId, req.StaffId);
    }

    [Fact]
    public async Task AddDispatcherInstructionsAsync_persists_text()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, Guid.NewGuid());
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.AddDispatcherInstructionsAsync(1, new DispatcherInstructionDto { DispatcherInstructions = "Go left" }, Guid.NewGuid());

        Assert.Equal("Go left", req.DispatcherInstructions);
    }

    [Fact]
    public async Task MarkAsStartedAsync_requires_matching_staff()
    {
        var repo = new Mock<IRequestRepository>();
        var assignedStaffId = Guid.NewGuid();
        var req = RequestWithCategory(1, Guid.NewGuid(), staffId: assignedStaffId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.MarkAsStartedAsync(1, staffId: Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsStartedAsync_sets_in_progress_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var assignedStaffId = Guid.NewGuid();
        var req = RequestWithCategory(1, Guid.NewGuid(), staffId: assignedStaffId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsStartedAsync(1, staffId: assignedStaffId);

        Assert.Equal(RequestStatus.InProgress, req.Status);
    }

    [Fact]
    public async Task MarkAsInProgressAsync_throws_when_staff_mismatch()
    {
        var repo = new Mock<IRequestRepository>();
        var assignedStaffId = Guid.NewGuid();
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(RequestWithCategory(1, Guid.NewGuid(), staffId: assignedStaffId));
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.MarkAsInProgressAsync(1, staffId: Guid.NewGuid()));
    }

    [Fact]
    public async Task MarkAsInProgressAsync_sets_in_progress_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var assignedStaffId = Guid.NewGuid();
        var req = RequestWithCategory(1, Guid.NewGuid(), staffId: assignedStaffId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsInProgressAsync(1, staffId: assignedStaffId);

        Assert.Equal(RequestStatus.InProgress, req.Status);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_sets_completed_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var assignedStaffId = Guid.NewGuid();
        var req = RequestWithCategory(1, Guid.NewGuid(), staffId: assignedStaffId);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsCompletedAsync(1, staffId: assignedStaffId);

        Assert.Equal(RequestStatus.Completed, req.Status);
    }
}
