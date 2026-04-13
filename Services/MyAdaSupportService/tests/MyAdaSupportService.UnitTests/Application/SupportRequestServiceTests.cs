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
        loc.Setup(l => l.ResolveLocationAsync(
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<bool>(),
                It.IsAny<int?>(),
                It.IsAny<string?>()))
            .ReturnsAsync("Resolved");
        return new SupportRequestService(repo.Object, TestMapperFactory.Create(), loc.Object);
    }

    private static Request RequestWithCategory(int id, int createdUserId, int? staffId = null) => new()
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
        var sut = new SupportRequestService(repo.Object, TestMapperFactory.Create(), loc.Object);
        var dto = new CreateSupportRequestDto
        {
            CategoryId = 1,
            Description = "Help",
            Area = "it",
            LocationType = "campus",
            Urgency = "standard"
        };

        var result = await sut.CreateRequestAsync(dto, memberId: 99);

        Assert.Equal(3, result.Id);
        Assert.Equal("Here", result.Location);
        repo.Verify(r => r.AddAsync(It.Is<Request>(x => x.CreatedUserId == 99 && x.Status == RequestStatus.New)), Times.Once);
    }

    [Fact]
    public async Task GetMyRequestsAsync_maps_each_item_with_location()
    {
        var repo = new Mock<IRequestRepository>();
        var requests = new List<Request> { RequestWithCategory(1, 5) };
        repo.Setup(r => r.GetRequestsByMemberIdAsync(5)).ReturnsAsync(requests);
        var sut = CreateSut(repo);

        var result = await sut.GetMyRequestsAsync(5);

        Assert.Single(result);
        Assert.Equal("Resolved", result[0].Location);
    }

    [Fact]
    public async Task GetAssignedRequestsAsync_returns_list()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetRequestsByStaffIdAsync(2)).ReturnsAsync(new List<Request> { RequestWithCategory(1, 9, 2) });
        var sut = CreateSut(repo);

        var result = await sut.GetAssignedRequestsAsync(2);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllRequestsAsync_applies_filter_and_orders()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Request, bool>>>(),
                It.IsAny<Func<IQueryable<Request>, IQueryable<Request>>?>(),
                It.IsAny<Func<IQueryable<Request>, IOrderedQueryable<Request>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new List<Request> { RequestWithCategory(1, 1) });
        var sut = CreateSut(repo);
        var filter = new RequestFilterDto { MemberId = 0, StaffId = 0 };

        var result = await sut.GetAllRequestsAsync(filter);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRequestDetailAsync_returns_detail_when_found()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(7)).ReturnsAsync(RequestWithCategory(7, 1));
        var sut = CreateSut(repo);

        var result = await sut.GetRequestDetailAsync(7);

        Assert.Equal(7, result.Id);
        Assert.Equal("Resolved", result.Location);
    }

    [Fact]
    public async Task GetRequestDetailAsync_throws_when_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(404)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.GetRequestDetailAsync(404));
    }

    [Fact]
    public async Task CancelRequestAsync_updates_status_when_owner_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 10);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.CancelRequestAsync(1, 10, new CancelRequestDto { Reason = "No longer needed" });

        Assert.Equal(RequestStatus.Cancelled, req.Status);
        Assert.Equal("No longer needed", req.CancelReason);
    }

    [Fact]
    public async Task CancelRequestAsync_throws_when_wrong_member()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(RequestWithCategory(1, 10));
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.CancelRequestAsync(1, 99, new CancelRequestDto { Reason = "x" }));
    }

    [Fact]
    public async Task CancelRequestAsync_throws_when_request_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(2)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.CancelRequestAsync(2, 1, new CancelRequestDto { Reason = "x" }));
    }

    [Fact]
    public async Task AssignRequestAsync_sets_staff_and_status()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.AssignRequestAsync(1, new AssignRequestDto { StaffId = 5 }, dispatcherId: 2);

        Assert.Equal(5, req.StaffId);
        Assert.Equal(RequestStatus.Assigned, req.Status);
    }

    [Fact]
    public async Task AssignRequestAsync_throws_when_request_missing()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(9)).ReturnsAsync((Request?)null);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.AssignRequestAsync(9, new AssignRequestDto { StaffId = 1 }, dispatcherId: 1));
    }

    [Fact]
    public async Task ReassignRequestAsync_updates_staff()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1, staffId: 3);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.ReassignRequestAsync(1, new AssignRequestDto { StaffId = 8 }, dispatcherId: 1);

        Assert.Equal(8, req.StaffId);
    }

    [Fact]
    public async Task AddDispatcherInstructionsAsync_persists_text()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.AddDispatcherInstructionsAsync(1, new DispatcherInstructionDto { DispatcherInstructions = "Go left" }, 9);

        Assert.Equal("Go left", req.DispatcherInstructions);
    }

    [Fact]
    public async Task MarkAsStartedAsync_requires_matching_staff()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1, staffId: 5);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.MarkAsStartedAsync(1, staffId: 99));
    }

    [Fact]
    public async Task MarkAsStartedAsync_sets_in_progress_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1, staffId: 5);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsStartedAsync(1, staffId: 5);

        Assert.Equal(RequestStatus.InProgress, req.Status);
    }

    [Fact]
    public async Task MarkAsInProgressAsync_throws_when_staff_mismatch()
    {
        var repo = new Mock<IRequestRepository>();
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(RequestWithCategory(1, 1, staffId: 5));
        var sut = CreateSut(repo);

        await Assert.ThrowsAsync<Exception>(() => sut.MarkAsInProgressAsync(1, staffId: 6));
    }

    [Fact]
    public async Task MarkAsInProgressAsync_sets_in_progress_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1, staffId: 5);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsInProgressAsync(1, staffId: 5);

        Assert.Equal(RequestStatus.InProgress, req.Status);
    }

    [Fact]
    public async Task MarkAsCompletedAsync_sets_completed_when_staff_matches()
    {
        var repo = new Mock<IRequestRepository>();
        var req = RequestWithCategory(1, 1, staffId: 5);
        repo.Setup(r => r.GetByIdOrDefaultAsync(1)).ReturnsAsync(req);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Request>())).ReturnsAsync((Request r) => r);
        var sut = CreateSut(repo);

        await sut.MarkAsCompletedAsync(1, staffId: 5);

        Assert.Equal(RequestStatus.Completed, req.Status);
    }
}
