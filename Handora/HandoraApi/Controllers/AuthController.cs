using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.Authorization;
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
            [FromForm] RegisterDto dto,
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
            return Ok(ApiResponse<object>.Ok(null!, "OTP resent successfully. Check your email."));
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

        // GET api/auth/users
        [HttpGet("users")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<GetUserDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll(CancellationToken ct)
        {
            var users = await _authService.GetAllUsersAsync(ct);
            return Ok(ApiResponse<IEnumerable<GetUserDto>>.Ok(users, "Users retrieved successfully."));
        }

        // GET api/auth/users/{id}
        [HttpGet("users/{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<GetUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(string id, CancellationToken ct)
        {
            var user = await _authService.GetUserByIdAsync(id, ct);
            return Ok(ApiResponse<GetUserDto>.Ok(user, "User retrieved successfully."));
        }

        // PUT api/auth/users/{id}
        [HttpPut("users/{id}")]
        [Authorize]
        [ProducesResponseType(typeof(ApiResponse<GetUserDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(string id, [FromForm] UpdateUserDto dto, CancellationToken ct)
        {
            var user = await _authService.UpdateUserAsync(id, dto, ct);
            return Ok(ApiResponse<GetUserDto>.Ok(user, "User updated successfully."));
        }

        // DELETE api/auth/users/{id}
        [HttpDelete("users/{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(string id, CancellationToken ct)
        {
            await _authService.DeleteUserAsync(id, ct);
            return Ok(ApiResponse<object>.Ok(null!, "User deleted successfully."));
        }

        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordDto dto,
            CancellationToken ct)
        {
            await _authService.ForgotPasswordAsync(dto, ct);
            return Ok(ApiResponse<object>.Ok(null!, "If an account with that email exists, we sent a password reset link to it."));
        }

        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordDto dto,
            CancellationToken ct)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto, ct);
                return Ok(ApiResponse<object>.Ok(null!, "Password has been reset successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        [HttpPost("google")]
        [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GoogleLogin(
            [FromBody] GoogleLoginDto dto,
            CancellationToken ct)
        {
            try
            {
                var result = await _authService.GoogleLoginAsync(dto, ct);
                return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Google login successful."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }
    }
}
