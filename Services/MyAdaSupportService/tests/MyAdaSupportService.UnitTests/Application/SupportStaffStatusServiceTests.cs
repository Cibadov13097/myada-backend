using System.Linq.Expressions;
using AutoMapper;
using Moq;
using MyAdaSupportService.Application.Abstractions;
using MyAdaSupportService.Application.DTOs.Staff;
using MyAdaSupportService.Application.Services;
using MyAdaSupportService.Domain.Entities.RequestAggregate;
using MyAdaSupportService.Domain.Enums;

namespace MyAdaSupportService.UnitTests.Application;

public class SupportStaffStatusServiceTests
{
    private static void SetupGetAllByStatus(Mock<ISupportStaffStatusRepository> repo, List<SupportStaffStatus> data)
    {
        repo.Setup(r => r.GetAllAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IOrderedQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(data);
    }

    [Fact]
    public async Task CreateStatusAsync_adds_entity_when_member_status_does_not_exist()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .Returns(Task.FromResult<SupportStaffStatus>(null!));

        SupportStaffStatus? addedEntity = null;
        repo.Setup(r => r.AddAsync(It.IsAny<SupportStaffStatus>()))
            .Callback<SupportStaffStatus>(entity => addedEntity = entity)
            .ReturnsAsync((SupportStaffStatus entity) => entity);

        mapper.Setup(m => m.Map<SupportStaffStatusDto>(It.IsAny<SupportStaffStatus>()))
            .Returns((SupportStaffStatus entity) => new SupportStaffStatusDto
            {
                MemberId = entity.MemberId,
                Status = entity.Status.ToString()
            });

        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.CreateStatusAsync(new CreateSupportStaffStatusDto
        {
            MemberId = 42,
            Status = nameof(StaffAvailabilityStatus.Online)
        });

        Assert.NotNull(addedEntity);
        Assert.Equal(42, addedEntity!.MemberId);
        Assert.Equal(nameof(StaffAvailabilityStatus.Online), result.Status);
        repo.Verify(r => r.AddAsync(It.IsAny<SupportStaffStatus>()), Times.Once);
    }

    [Fact]
    public async Task CreateStatusAsync_throws_when_member_status_already_exists()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new SupportStaffStatus
            {
                MemberId = 9,
                Status = StaffAvailabilityStatus.Online
            });

        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await Assert.ThrowsAsync<Exception>(() => sut.CreateStatusAsync(new CreateSupportStaffStatusDto
        {
            MemberId = 9,
            Status = nameof(StaffAvailabilityStatus.Online)
        }));
        repo.Verify(r => r.AddAsync(It.IsAny<SupportStaffStatus>()), Times.Never);
    }

    [Fact]
    public async Task GetByMemberIdAsync_returns_dto_when_found()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var entity = new SupportStaffStatus { Id = 1, MemberId = 10, Status = StaffAvailabilityStatus.Online };
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(entity);
        mapper.Setup(m => m.Map<SupportStaffStatusDto>(entity))
            .Returns(new SupportStaffStatusDto { Id = 1, MemberId = 10, Status = "Online" });
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetByMemberIdAsync(10);

        Assert.NotNull(result);
        Assert.Equal("Online", result.Status);
    }

    [Fact]
    public async Task GetByMemberIdAsync_returns_null_when_repository_returns_null()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .Returns(Task.FromResult<SupportStaffStatus>(null!));
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetByMemberIdAsync(99);

        Assert.Null(result);
        mapper.Verify(m => m.Map<SupportStaffStatusDto>(It.IsAny<SupportStaffStatus>()), Times.Never);
    }

    [Fact]
    public async Task GetByStatusAsync_filters_and_maps()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var list = new List<SupportStaffStatus>
        {
            new() { Id = 1, MemberId = 1, Status = StaffAvailabilityStatus.Online }
        };
        SetupGetAllByStatus(repo, list);
        mapper.Setup(m => m.Map<List<SupportStaffStatusDto>>(list))
            .Returns(new List<SupportStaffStatusDto> { new() { Status = "Online" } });
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetByStatusAsync(nameof(StaffAvailabilityStatus.Online));

        Assert.Single(result);
    }

    [Fact]
    public async Task UpdateStatusAsync_updates_when_entity_exists()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var entity = new SupportStaffStatus { Id = 1, MemberId = 5, Status = StaffAvailabilityStatus.Offline };
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .ReturnsAsync(entity);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SupportStaffStatus>())).ReturnsAsync((SupportStaffStatus s) => s);
        var dto = new UpdateSupportStaffStatusDto { Status = nameof(StaffAvailabilityStatus.Online) };
        mapper.Setup(m => m.Map(It.IsAny<UpdateSupportStaffStatusDto>(), It.IsAny<SupportStaffStatus>()))
            .Callback<UpdateSupportStaffStatusDto, SupportStaffStatus>((_, e) =>
            {
                e.Status = StaffAvailabilityStatus.Online;
            });
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await sut.UpdateStatusAsync(5, dto);

        Assert.Equal(StaffAvailabilityStatus.Online, entity.Status);
        repo.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_throws_when_not_found()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetAsync(
                It.IsAny<Expression<Func<SupportStaffStatus, bool>>>(),
                It.IsAny<Func<IQueryable<SupportStaffStatus>, IQueryable<SupportStaffStatus>>?>(),
                It.IsAny<bool>()))
            .Returns(Task.FromResult<SupportStaffStatus>(null!));
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await Assert.ThrowsAsync<Exception>(() =>
            sut.UpdateStatusAsync(1, new UpdateSupportStaffStatusDto { Status = "Online" }));
    }
}
