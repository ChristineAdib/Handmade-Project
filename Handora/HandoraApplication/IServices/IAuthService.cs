using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.Helpers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken ct = default);
        Task<OtpResponseDto> VerifyOtpAsync(VerifyOtpDto dto, CancellationToken ct = default);
        Task<bool> ResendOtpAsync(ResendOtpDto dto, CancellationToken ct = default);
        Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
        Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);

        Task<IEnumerable<GetUserDto>> GetAllUsersAsync(CancellationToken ct = default);
        Task<GetUserDto> GetUserByIdAsync(string id, CancellationToken ct = default);
        Task<GetUserDto> UpdateUserAsync(string id, UpdateUserDto dto, CancellationToken ct = default);
        Task DeleteUserAsync(string id, CancellationToken ct = default);
        Task<AuthResponseDto> AssignSellerRoleAndGenerateTokenAsync(string userId, CancellationToken ct = default);
        
        Task<IEnumerable<GetUserDto>> GetUsersFilteredAsync(string? role = null, bool? isActive = null, CancellationToken ct = default);
        Task<Result> UpgradeToSellerAsync(string userId);
        Task<Result> ToggleUserBanStatusAsync(string userId);
    }
}
