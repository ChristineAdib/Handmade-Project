using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IAuthRepository authRepo,
            JwtHelper jwtHelper,
            ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
            _jwtHelper = jwtHelper;
            _logger = logger;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto, CancellationToken ct = default)
        {
            var existing = await _authRepo.GetByEmailAsync(dto.Email, ct);
            if (existing is not null)
                throw new AuthException("An account with this email already exists.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                UserName = dto.Email.Split("@")[0],
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                Token = string.Empty
            };

            var result = await _authRepo.CreateAsync(user, dto.Password, ct);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, errors);
                throw new AuthException(string.Join(", ", errors));
            }
            await _authRepo.AddToRoleAsync(user, dto.Role);

            return await BuildAuthResponseAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByEmailAsync(dto.Email, ct);

            if (user is null || !await _authRepo.CheckPasswordAsync(user, dto.Password))
                throw new AuthException("Invalid email or password.");

            if (user.IsBanned)
                throw new AuthException("Your account has been suspended. Please contact support.");

            if (user.IsDeleted)
                throw new AuthException("Account not found.");

            return await BuildAuthResponseAsync(user);
        }


        private async Task<AuthResponseDto> BuildAuthResponseAsync(User user)
        {
            var roles = await _authRepo.GetRolesAsync(user);
            var (token, expiry) = _jwtHelper.GenerateToken(user, roles);

            user.Token = token;
            user.UpdatedAt = DateTime.UtcNow;
            await _authRepo.UpdateAsync(user);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email!,
                Token = token,
                TokenExpiry = expiry,
                Roles = roles
            };
        }
    }
}
