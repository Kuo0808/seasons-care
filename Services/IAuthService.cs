using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Services
{
    public interface IAuthService
    {
        Task<LoginResponse> RegisterAsync(RegisterRequest request);
        Task<LoginResponse> CompleteProfileAsync(Guid currentUserId, CompleteProfileRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
        Task UpdateLastViewedCareGroupAsync(Guid currentUserId, Guid careGroupId);
    }
}
