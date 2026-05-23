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
            return Ok(ApiResponse<AuthResponseDto>.Ok(result, "Registration successful."));
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
