using FluentAssertions;
using global::LostFoundService.Application.DTOs.LostAndFound;
using global::LostFoundService.Application.Interfaces.Repositories;
using global::LostFoundService.Application.Services;
using global::LostFoundService.Domain.Entities;
using global::LostFoundService.Domain.Enums;
using global::LostFoundService.Domain.Exceptions;
using Moq;
using Shared.Shared.Infrastructure;

namespace LostFoundService.Tests.Layers.Application;

public sealed class LostFoundItemVisibilityApplicationLayerTests
{
    private static LostAndFoundService CreateSut(
        out Mock<ILostAndFoundRepository> repository,
        out Mock<IRabbitMqPublisher> publisher)
    {
        repository = new Mock<ILostAndFoundRepository>();
        publisher = new Mock<IRabbitMqPublisher>();
        return new LostAndFoundService(repository.Object, publisher.Object);
    }

    [Fact]
    public async Task GetItemByIdAsync_public_can_read_received_item()
    {
        var sut = CreateSut(out var repository, out _);
        repository
            .Setup(r => r.GetItemByIdAsync(7, true, true, null))
            .ReturnsAsync(new LostFoundItem
            {
                Id = 7,
                Title = "Laptop",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Received
            });

        var item = await sut.GetItemByIdAsync(7, currentUserId: null, hideTerminalItemsForPublic: true);

        item.Id.Should().Be(7);
        item.AdminStatus.Should().Be(LostFoundAdminStatus.Received);
    }

    [Fact]
    public async Task GetItemByIdAsync_public_hides_delivered_item()
    {
        var sut = CreateSut(out var repository, out _);
        repository
            .Setup(r => r.GetItemByIdAsync(8, true, true, null))
            .ReturnsAsync(new LostFoundItem
            {
                Id = 8,
                Title = "Phone",
                Status = LostFoundPublicStatus.Active,
                AdminStatus = LostFoundAdminStatus.Delivered
            });

        var act = () => sut.GetItemByIdAsync(8, currentUserId: null, hideTerminalItemsForPublic: true);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateClaimAsync_allows_received_item()
    {
        var sut = CreateSut(out var repository, out _);
        LostFoundItemClaim? createdClaim = null;

        repository
            .Setup(r => r.GetItemByIdAsync(9, true, false, null))
            .ReturnsAsync(new LostFoundItem
            {
                Id = 9,
                Title = "Headphones",
                AdminStatus = LostFoundAdminStatus.Received
            });
        repository
            .Setup(r => r.ClaimExistsAsync(9, "user-1", LostFoundClaimType.Owner))
            .ReturnsAsync(false);
        repository
            .Setup(r => r.AddClaimAsync(It.IsAny<LostFoundItemClaim>()))
            .Callback<LostFoundItemClaim>(claim => createdClaim = claim)
            .Returns(Task.CompletedTask);
        repository
            .Setup(r => r.SaveChangesAsync())
            .Returns(Task.CompletedTask);

        var claim = await sut.CreateClaimAsync(9, new CreateLostFoundClaimCommand
        {
            UserId = "user-1",
            ClaimType = "owner",
            Message = "This is mine."
        });

        claim.ClaimType.Should().Be(LostFoundClaimType.Owner);
        createdClaim.Should().NotBeNull();
        createdClaim!.LostFoundItemId.Should().Be(9);
        repository.Verify(r => r.AddClaimAsync(It.IsAny<LostFoundItemClaim>()), Times.Once);
    }

    [Fact]
    public async Task CreateClaimAsync_rejects_delivered_item()
    {
        var sut = CreateSut(out var repository, out _);
        repository
            .Setup(r => r.GetItemByIdAsync(10, true, false, null))
            .ReturnsAsync(new LostFoundItem
            {
                Id = 10,
                Title = "Wallet",
                AdminStatus = LostFoundAdminStatus.Delivered
            });

        var act = () => sut.CreateClaimAsync(10, new CreateLostFoundClaimCommand
        {
            UserId = "user-2",
            ClaimType = "owner"
        });

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
