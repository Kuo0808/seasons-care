using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SeasonsCare.Api.DTOs.Auth;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Exceptions;
using SeasonsCare.Api.Services;

namespace SeasonsCare.Api.Controllers
{
    // [架構導覽] 展示層 (Presentation Layer) - Controller
    // 職責：負責路由定義 (Routing)、接收 HTTP 請求、驗證輸入資料 (透過 Filter) 並回傳統一格式結果。本身不處理實質的商業規則。
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [EndpointSummary("註冊新帳號")]
        [EndpointDescription("建立新使用者帳號。前端需在 request body 提供 email 與 password；成功後會直接回傳 JWT token、使用者資訊，以及登入後可用的初始資料。")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // 步驟 1：將請求參數轉交給 Service 層 (大腦) 處理實質的註冊邏輯
            var result = await _authService.RegisterAsync(request);
            
            // 步驟 2：包裝為專案統一標準的 ApiResponse 並回傳 201 Created
            var response = new ApiResponse<LoginResponse>(result, "註冊成功", HttpContext.TraceIdentifier);

            return StatusCode(201, response);
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [EndpointSummary("使用者登入")]
        [EndpointDescription("使用既有帳號登入。前端需在 request body 提供 email 與 password；成功後會回傳 JWT token、使用者資訊，以及目前可存取的照護群組相關資料。")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            // 步驟 1：基本輸入防呆 (多數由 Validator 處理，這裡為雙重保險)
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new DomainException(
                    "登入請求缺少必要欄位。",
                    "INVALID_LOGIN_REQUEST",
                    400
                );
            }

            // 步驟 2：轉交 Service 層執行密碼比對與 JWT 核發
            var result = await _authService.LoginAsync(request);
            
            // 步驟 3：包裝為專案統一標準的 ApiResponse 並回傳 200 OK
            var response = new ApiResponse<LoginResponse>(result, "登入成功", HttpContext.TraceIdentifier);

            return Ok(response);
        }
    }
}
