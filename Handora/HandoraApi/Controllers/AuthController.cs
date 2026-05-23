using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HandoraApi.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register(
            [FromBody] RegisterDto dto,
            CancellationToken ct)
        {
            var result = await _authService.RegisterAsync(dto, ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration initiated. Please verify your email with the OTP sent to your inbox."));
        }

        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(ApiResponse<OtpResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyOtp(
            [FromBody] VerifyOtpDto dto,
            CancellationToken ct)
        {
            var result = await _authService.VerifyOtpAsync(dto, ct);
            return Ok(ApiResponse<OtpResponseDto>.Ok(result, "Email verified successfully."));
        }

        [HttpPost("resend-otp")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResendOtp(
            [FromBody] ResendOtpDto dto,
            CancellationToken ct)
        {
            await _authService.ResendOtpAsync(dto, ct);
            return Ok(ApiResponse<object>.Ok(null, "OTP resent successfully. Check your email."));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login(
            [FromBody] LoginDto dto,
            CancellationToken ct)
        {
            var result = await _authService.LoginAsync(dto, ct);
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Login successful."));
        }
    }
}
