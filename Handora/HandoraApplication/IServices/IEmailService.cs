namespace HandoraApplication.IServices
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string email, string otpCode, CancellationToken ct = default);
        Task<bool> SendEmailAsync(string email, string subject, string body, CancellationToken ct = default);
    }
}
