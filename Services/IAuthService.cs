using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Auth;

namespace SeasonsCare.Api.Services
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}
