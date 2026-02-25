using System.Security.Claims;
using LostFoundService.Domain.Exceptions;

namespace LostFoundService.Application.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>
        /// JWT standard claim for subject (user id). Use this to avoid dependency on JWT package in Application layer.
        /// </summary>
        public const string SubClaimType = "sub";

        /// <summary>
        /// Gets user id from JWT claims. Prefers "sub", then NameIdentifier (ASP.NET mapping).
        /// </summary>
        public static string GetUserId(this ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedException("User context is missing.");

            var userId = user.FindFirst(SubClaimType)?.Value
                         ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("User is not authenticated.");

            return userId;
        }
    }
}
