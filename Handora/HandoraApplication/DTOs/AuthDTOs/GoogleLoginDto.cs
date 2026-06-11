using System.ComponentModel.DataAnnotations;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed record GoogleLoginDto
    {
        [Required]
        public string Credential { get; init; } = string.Empty;
    }
}
