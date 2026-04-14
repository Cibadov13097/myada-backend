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
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync((SupportStaffStatus?)null);

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
            MemberId = memberId,
            Status = nameof(StaffAvailabilityStatus.Online)
        });

        Assert.NotNull(addedEntity);
        Assert.Equal(memberId, addedEntity!.MemberId);
        Assert.Equal(nameof(StaffAvailabilityStatus.Online), result.Status);
        repo.Verify(r => r.AddAsync(It.IsAny<SupportStaffStatus>()), Times.Once);
    }

    [Fact]
    public async Task CreateStatusAsync_throws_when_member_status_already_exists()
    {
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetByMemberIdAsync(memberId))
            .ReturnsAsync(new SupportStaffStatus
            {
                MemberId = memberId,
                Status = StaffAvailabilityStatus.Online
            });

        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await Assert.ThrowsAsync<Exception>(() => sut.CreateStatusAsync(new CreateSupportStaffStatusDto
        {
            MemberId = memberId,
            Status = nameof(StaffAvailabilityStatus.Online)
        }));

        repo.Verify(r => r.AddAsync(It.IsAny<SupportStaffStatus>()), Times.Never);
    }

    [Fact]
    public async Task GetByMemberIdAsync_returns_dto_when_found()
    {
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var entity = new SupportStaffStatus { Id = 1, MemberId = memberId, Status = StaffAvailabilityStatus.Online };
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync(entity);
        mapper.Setup(m => m.Map<SupportStaffStatusDto>(entity))
            .Returns(new SupportStaffStatusDto { Id = 1, MemberId = memberId, Status = "Online" });
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetByMemberIdAsync(memberId);

        Assert.NotNull(result);
        Assert.Equal("Online", result!.Status);
    }

    [Fact]
    public async Task GetByMemberIdAsync_returns_null_when_repository_returns_null()
    {
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync((SupportStaffStatus?)null);
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetByMemberIdAsync(memberId);

        Assert.Null(result);
        mapper.Verify(m => m.Map<SupportStaffStatusDto>(It.IsAny<SupportStaffStatus>()), Times.Never);
    }

    [Fact]
    public async Task GetOrCreateByMemberIdAsync_creates_offline_when_repository_returns_null()
    {
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync((SupportStaffStatus?)null);

        repo.Setup(r => r.AddAsync(It.IsAny<SupportStaffStatus>()))
            .ReturnsAsync((SupportStaffStatus entity) => entity);

        mapper.Setup(m => m.Map<SupportStaffStatusDto>(It.IsAny<SupportStaffStatus>()))
            .Returns((SupportStaffStatus entity) => new SupportStaffStatusDto
            {
                MemberId = entity.MemberId,
                Status = entity.Status.ToString()
            });

        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        var result = await sut.GetOrCreateByMemberIdAsync(memberId);

        Assert.Equal(memberId, result.MemberId);
        Assert.Equal(nameof(StaffAvailabilityStatus.Offline), result.Status);
        repo.Verify(r => r.AddAsync(It.Is<SupportStaffStatus>(
            x => x.MemberId == memberId && x.Status == StaffAvailabilityStatus.Offline)), Times.Once);
    }

    [Fact]
    public async Task GetByStatusAsync_filters_and_maps()
    {
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var list = new List<SupportStaffStatus>
        {
            new() { Id = 1, MemberId = Guid.NewGuid(), Status = StaffAvailabilityStatus.Online }
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
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        var entity = new SupportStaffStatus { Id = 1, MemberId = memberId, Status = StaffAvailabilityStatus.Offline };
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync(entity);
        repo.Setup(r => r.UpdateAsync(It.IsAny<SupportStaffStatus>())).ReturnsAsync((SupportStaffStatus s) => s);
        var dto = new UpdateSupportStaffStatusDto { Status = nameof(StaffAvailabilityStatus.Online) };
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await sut.UpdateStatusAsync(memberId, dto);

        Assert.Equal(StaffAvailabilityStatus.Online, entity.Status);
        repo.Verify(r => r.UpdateAsync(entity), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_creates_when_entity_does_not_exist()
    {
        var memberId = Guid.NewGuid();
        var repo = new Mock<ISupportStaffStatusRepository>();
        var mapper = new Mock<IMapper>();
        repo.Setup(r => r.GetByMemberIdAsync(memberId)).ReturnsAsync((SupportStaffStatus?)null);
        var dto = new UpdateSupportStaffStatusDto { Status = nameof(StaffAvailabilityStatus.OnBreak) };
        var sut = new SupportStaffStatusService(repo.Object, mapper.Object);

        await sut.UpdateStatusAsync(memberId, dto);

        repo.Verify(r => r.AddAsync(It.Is<SupportStaffStatus>(
            x => x.MemberId == memberId && x.Status == StaffAvailabilityStatus.OnBreak)), Times.Once);
    }
}
