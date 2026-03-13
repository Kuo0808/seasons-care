using System;
using System.Threading.Tasks;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;
using SeasonsCare.Api.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace SeasonsCare.Api.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _configuration = configuration;
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

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            var lowercaseEmail = request.Email.ToLowerInvariant();
            var user = await _userRepository.GetByEmailAsync(lowercaseEmail);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                throw new DomainException(
                    "帳號或密碼錯誤",
                    "LOGIN_FAILED",
                    401
                );
            }

            var token = GenerateJwtToken(user);

            return new LoginResponse
            {
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username
                }
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var keyStr = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
