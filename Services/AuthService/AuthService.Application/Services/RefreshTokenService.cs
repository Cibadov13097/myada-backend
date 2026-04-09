using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain.Entities;
using System.Security.Cryptography;

namespace AuthService.Application.Services
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly JwtOptions _jwtOptions;

        public RefreshTokenService(
            IRefreshTokenRepository refreshTokenRepository,
            JwtOptions jwtOptions)
        {
            _refreshTokenRepository = refreshTokenRepository;
            _jwtOptions = jwtOptions;
        }

        public async Task<RefreshToken> CreateRefreshTokenAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("User id is required.", nameof(userId));

            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureToken(),
                UserId = userId.Trim(),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
                CreatedByIp = null
            };

            await _refreshTokenRepository.AddAsync(token);
            return token;
        }

        public async Task<RefreshToken?> GetByTokenAsync(string token)
        {
            return await _refreshTokenRepository.GetByTokenAsync(token);
        }

        public async Task<bool> ValidateRefreshTokenAsync(string token)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            return refreshToken != null && !refreshToken.IsExpired && !refreshToken.IsRevoked;
        }

        public async Task RevokeRefreshTokenAsync(string token)
        {
            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(token);
            if (refreshToken == null)
                return;

            refreshToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.UpdateAsync(refreshToken);
        }

        public async Task<RefreshToken> RotateRefreshTokenAsync(string token)
        {
            var oldToken = await _refreshTokenRepository.GetByTokenAsync(token);

            if (oldToken == null || !oldToken.IsActive)
                throw new InvalidOperationException("Invalid or inactive refresh token");

            var newToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = GenerateSecureToken(),
                UserId = oldToken.UserId,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenLifetimeDays),
                CreatedByIp = null
            };

            oldToken.RevokedAt = DateTime.UtcNow;
            oldToken.ReplacedByToken = newToken.Token;

            await _refreshTokenRepository.UpdateAsync(oldToken);
            await _refreshTokenRepository.AddAsync(newToken);

            return newToken;
        }

        private static string GenerateSecureToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
