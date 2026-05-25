using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed record VerifyOtpDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "OTP code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits.")]
        public string OtpCode { get; init; } = string.Empty;
    }

    public sealed record OtpResponseDto
    {
        public string Message { get; init; } = string.Empty;
        public int RemainingAttempts { get; init; }
        public bool IsVerified { get; init; }
    }

    public sealed record ResendOtpDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;
    }
}
