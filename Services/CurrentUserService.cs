using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace SeasonsCare.Api.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid UserId
        {
            get
            {
                var user = _httpContextAccessor.HttpContext?.User;
                var userIdString = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
                {
                    throw new UnauthorizedAccessException("尚未登入或 Token 無效。");
                }
                return userId;
            }
        }
    }
}
