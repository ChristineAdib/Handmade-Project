using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed record ForgotPasswordDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;
    }
}
