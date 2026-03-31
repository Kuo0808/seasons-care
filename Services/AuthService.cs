using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Models.Entities;
using SeasonsCare.Api.Repositories;

namespace SeasonsCare.Api.Services
{
    // [架構導覽] 商業邏輯層 (Business Logic Layer) - Service
    // 職責：系統的核心大腦。負責執行特定領域規則 (Domain Rules)、查核身分驗證、並跨越多個 Repository 進行系統級操作整合。
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ICareGroupRepository _careGroupRepository;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepository, ICareGroupRepository careGroupRepository, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _careGroupRepository = careGroupRepository;
            _configuration = configuration;
        }

        public async Task<LoginResponse> RegisterAsync(RegisterRequest request)
        {
            // 步驟 1：前置準備與資料正規化 (Email 小寫轉換避免重複註冊漏洞)
            var lowercaseEmail = request.Email.ToLowerInvariant();

            // 步驟 2：核心領域規則驗證 (檢核 Email 是否已存在)
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
                // 步驟 3：機敏資料處理 (使用 BCrypt 進行密碼單向雜湊加密)
                var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // 步驟 4：封裝為標準資料庫實體 Entity
                var user = new User
                {
                    Email = lowercaseEmail,
                    Username = string.Empty,
                    AvatarKey = string.Empty,
                    PasswordHash = passwordHash,
                    CreatedBy = "System"
                };

                // 步驟 5：透過單一工作單元 (Unit of Work) 宣告新增並將實體寫入資料庫
                await _userRepository.AddAsync(user);
                await _userRepository.SaveChangesAsync();

                // 步驟 6：呼叫內部輔助方法核發 JWT Token，並封裝成 Response DTO 往外回傳
                return await BuildLoginResponseAsync(user);
            }
            catch (DomainException)
            {
                throw;
            }
            catch (Exception)
            {
                throw new DomainException(
                    "使用者註冊處理時發生未知錯誤，請稍後再試。",
                    "REGISTRATION_FAILED",
                    500
                );
            }
        }

        public async Task<LoginResponse> CompleteProfileAsync(Guid currentUserId, CompleteProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId);

            if (user == null)
            {
                throw new DomainException(
                    "使用者不存在",
                    "NOT_FOUND",
                    404
                );
            }

            user.Username = request.Username;
            user.AvatarKey = request.AvatarKey;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.SaveChangesAsync();

            return await BuildLoginResponseAsync(user);
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

            return await BuildLoginResponseAsync(user);
        }

        public async Task UpdateLastViewedCareGroupAsync(Guid currentUserId, Guid careGroupId)
        {
            var user = await _userRepository.GetByIdAsync(currentUserId);

            if (user == null)
            {
                throw new DomainException(
                    "找不到使用者。",
                    "NOT_FOUND",
                    404
                );
            }

            var isMember = await _careGroupRepository.IsMemberAsync(careGroupId, currentUserId);
            if (!isMember)
            {
                throw new DomainException(
                    "您不是該照護群組的成員。",
                    "FORBIDDEN_ACCESS",
                    403
                );
            }

            user.LastViewedCareGroupId = careGroupId;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveChangesAsync();
        }

        private async Task<LoginResponse> BuildLoginResponseAsync(User user)
        {
            var token = GenerateJwtToken(user);
            var accessibleCareGroupIds = await _careGroupRepository.GetAccessibleCareGroupIdsAsync(user.Id);
            Guid? defaultCareGroupId = null;

            if (accessibleCareGroupIds.Count == 1)
            {
                defaultCareGroupId = accessibleCareGroupIds[0];
            }
            else if (accessibleCareGroupIds.Count > 1)
            {
                defaultCareGroupId = user.LastViewedCareGroupId.HasValue
                    && accessibleCareGroupIds.Contains(user.LastViewedCareGroupId.Value)
                    ? user.LastViewedCareGroupId.Value
                    : accessibleCareGroupIds[0];
            }

            return new LoginResponse
            {
                Token = token,
                CareGroupCount = accessibleCareGroupIds.Count,
                DefaultCareGroupId = defaultCareGroupId,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Username = user.Username,
                    AvatarKey = user.AvatarKey,
                    IsProfileCompleted = !string.IsNullOrWhiteSpace(user.Username) && !string.IsNullOrWhiteSpace(user.AvatarKey)
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
