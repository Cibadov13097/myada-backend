using System.Security.Claims;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedException("User context is missing.");

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated.");

            return userId;
        }
    }
}
