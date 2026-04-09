using global::AuthService.Application.Interfaces;
using global::AuthService.Application.Options;
using global::AuthService.Domain.Entities;
using FluentAssertions;
using Moq;

namespace MyAda.Tests.Services.AuthService;

public class RefreshTokenServiceTests
{
    [Fact]
    public async Task CreateRefreshTokenAsync_WhenUserIdIsValid_ShouldCreateAndPersistToken()
    {
        // Arrange
        const string userId = " user-123 ";
        var repositoryMock = new Mock<IRefreshTokenRepository>();
        var jwtOptions = new JwtOptions
        {
            RefreshTokenLifetimeDays = 14
        };

        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            jwtOptions);

        // Act
        var result = await service.CreateRefreshTokenAsync(userId);

        // Assert
        result.UserId.Should().Be("user-123");
        result.Token.Should().NotBeNullOrWhiteSpace();
        result.ExpiresAt.Should().BeAfter(result.CreatedAt);
        result.CreatedByIp.Should().BeNull();

        repositoryMock.Verify(
            repository => repository.AddAsync(It.Is<RefreshToken>(token =>
                token.UserId == "user-123" &&
                !string.IsNullOrWhiteSpace(token.Token))),
            Times.Once);
    }

    [Fact]
    public async Task CreateRefreshTokenAsync_WhenUserIdIsEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var repositoryMock = new Mock<IRefreshTokenRepository>();
        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            new JwtOptions());

        // Act
        Func<Task> action = async () => await service.CreateRefreshTokenAsync(" ");

        // Assert
        await action
            .Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("User id is required.*");

        repositoryMock.Verify(repository => repository.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateRefreshTokenAsync_WhenTokenIsActive_ShouldReturnTrue()
    {
        // Arrange
        const string tokenValue = "refresh-token";
        var repositoryMock = new Mock<IRefreshTokenRepository>();
        repositoryMock
            .Setup(repository => repository.GetByTokenAsync(tokenValue))
            .ReturnsAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = tokenValue,
                UserId = "user-1",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(5)
            });

        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            new JwtOptions());

        // Act
        var result = await service.ValidateRefreshTokenAsync(tokenValue);

        // Assert
        result.Should().BeTrue();
        repositoryMock.Verify(repository => repository.GetByTokenAsync(tokenValue), Times.Once);
    }

    [Fact]
    public async Task RevokeRefreshTokenAsync_WhenTokenExists_ShouldMarkTokenRevokedAndUpdateRepository()
    {
        // Arrange
        const string tokenValue = "refresh-token";
        var storedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenValue,
            UserId = "user-2",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        };

        var repositoryMock = new Mock<IRefreshTokenRepository>();
        repositoryMock
            .Setup(repository => repository.GetByTokenAsync(tokenValue))
            .ReturnsAsync(storedToken);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(storedToken))
            .Returns(Task.CompletedTask);

        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            new JwtOptions());

        // Act
        await service.RevokeRefreshTokenAsync(tokenValue);

        // Assert
        storedToken.RevokedAt.Should().NotBeNull();
        repositoryMock.Verify(repository => repository.GetByTokenAsync(tokenValue), Times.Once);
        repositoryMock.Verify(repository => repository.UpdateAsync(storedToken), Times.Once);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WhenTokenIsInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        const string tokenValue = "inactive-token";
        var repositoryMock = new Mock<IRefreshTokenRepository>();
        repositoryMock
            .Setup(repository => repository.GetByTokenAsync(tokenValue))
            .ReturnsAsync(new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = tokenValue,
                UserId = "user-3",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            });

        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            new JwtOptions());

        // Act
        Func<Task> action = async () => await service.RotateRefreshTokenAsync(tokenValue);

        // Assert
        await action
            .Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("Invalid or inactive refresh token");

        repositoryMock.Verify(repository => repository.UpdateAsync(It.IsAny<RefreshToken>()), Times.Never);
        repositoryMock.Verify(repository => repository.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateRefreshTokenAsync_WhenTokenIsActive_ShouldRevokeOldTokenAndAddNewOne()
    {
        // Arrange
        const string tokenValue = "active-token";
        var oldToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = tokenValue,
            UserId = "user-4",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(10)
        };

        var repositoryMock = new Mock<IRefreshTokenRepository>();
        repositoryMock
            .Setup(repository => repository.GetByTokenAsync(tokenValue))
            .ReturnsAsync(oldToken);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(oldToken))
            .Returns(Task.CompletedTask);
        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<RefreshToken>()))
            .Returns(Task.CompletedTask);

        var service = new global::AuthService.Application.Services.RefreshTokenService(
            repositoryMock.Object,
            new JwtOptions { RefreshTokenLifetimeDays = 7 });

        // Act
        var result = await service.RotateRefreshTokenAsync(tokenValue);

        // Assert
        result.UserId.Should().Be(oldToken.UserId);
        result.Token.Should().NotBe(oldToken.Token);
        oldToken.RevokedAt.Should().NotBeNull();
        oldToken.ReplacedByToken.Should().Be(result.Token);

        repositoryMock.Verify(repository => repository.GetByTokenAsync(tokenValue), Times.Once);
        repositoryMock.Verify(repository => repository.UpdateAsync(oldToken), Times.Once);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.Is<RefreshToken>(token =>
                token.UserId == oldToken.UserId &&
                token.Token == result.Token)),
            Times.Once);
    }
}
