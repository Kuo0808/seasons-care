using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Exceptions;

namespace SeasonsCare.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;

        public AuthService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            var lowercaseEmail = request.Email.ToLowerInvariant();

            if (await _userRepository.EmailExistsAsync(lowercaseEmail))
            {
                throw new DomainException(
                    "Email 已被註冊",
                    "EMAIL_ALREADY_EXISTS",
                    409
                );
            }

            try
            {
                // Using BCrypt.Net-Next
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                var user = new User
                {
                    Email = lowercaseEmail,
                    Username = request.Username,
                    PasswordHash = passwordHash,
                    CreatedBy = "System"
                };

                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception)
            {
                // Wrap in domain exception if it's a generic DB scale issue or error
                throw new DomainException(
                    "使用者註冊處理時發生未知錯誤，請稍後再試。",
                    "REGISTRATION_FAILED",
                    500
                );
            }
        }
    }
}
