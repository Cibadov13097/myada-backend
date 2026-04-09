using AuthService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<RefreshToken> CreateRefreshTokenAsync(string userId);

        Task<RefreshToken?> GetByTokenAsync(string token);

        Task<bool> ValidateRefreshTokenAsync(string token);

        Task RevokeRefreshTokenAsync(string token);

        Task<RefreshToken> RotateRefreshTokenAsync(string token);
    }
}
