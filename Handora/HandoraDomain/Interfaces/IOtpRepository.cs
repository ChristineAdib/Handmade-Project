using HandoraDomain.Models.AppUser;

namespace HandoraDomain.Interfaces
{
    public interface IOtpRepository
    {
        Task<OtpVerification> CreateAsync(OtpVerification otp, CancellationToken ct = default);
        Task<OtpVerification?> GetByEmailAsync(string email, CancellationToken ct = default);
        Task<OtpVerification?> GetByIdAsync(string id, CancellationToken ct = default);
        Task UpdateAsync(OtpVerification otp, CancellationToken ct = default);
        Task DeleteAsync(string id, CancellationToken ct = default);
        Task DeleteExpiredAsync(CancellationToken ct = default);
    }
}
