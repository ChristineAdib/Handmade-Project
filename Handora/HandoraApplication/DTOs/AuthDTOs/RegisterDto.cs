using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.AuthDTOs
{
    public sealed record RegisterDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(100)]
        public string Name { get; init; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; init; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; init; } = string.Empty;

        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; init; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number.")]
        public string? PhoneNumber { get; init; }
        [Required]
        [RegularExpression($"^({AppRoles.Buyer}|{AppRoles.Seller})$",
        ErrorMessage = "Role must be 'Buyer' or 'Seller'.")]
        public string Role { get; init; } = AppRoles.Buyer;

        public IFormFile? ProfileImage { get; set; }
        public string? Bio { get; set; }

    }
}