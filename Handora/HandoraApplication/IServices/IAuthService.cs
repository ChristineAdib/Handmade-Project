using HandoraApplication.DTOs.AuthDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default);
        Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
        Task<OtpResponseDto> VerifyOtpAsync(VerifyOtpDto dto, CancellationToken ct = default);
        Task<bool> ResendOtpAsync(ResendOtpDto dto, CancellationToken ct = default);
    }
}
