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
        private readonly IOtpRepository _otpRepo;
        private readonly IEmailService _emailService;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthService> _logger;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const int OTP_LENGTH = 6;

        public AuthService(
            IAuthRepository authRepo,
            IOtpRepository otpRepo,
            IEmailService emailService,
            JwtHelper jwtHelper,
            ILogger<AuthService> logger)
        {
            _authRepo = authRepo;
            _otpRepo = otpRepo;
            _emailService = emailService;
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
                Token = string.Empty,
                IsEmailVerified = false
            };

            var result = await _authRepo.CreateAsync(user, dto.Password, ct);

            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, errors);
                throw new AuthException(string.Join(", ", errors));
            }

            await _authRepo.AddToRoleAsync(user, dto.Role);

            var otpCode = GenerateOtp();
            var otpVerification = new OtpVerification
            {
                UserId = user.Id,
                Email = dto.Email,
                OtpCode = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                MaxAttempts = 5
            };

            await _otpRepo.CreateAsync(otpVerification, ct);

            var emailSent = await _emailService.SendOtpEmailAsync(dto.Email, otpCode, ct);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to send OTP email to {Email}", dto.Email);
                throw new AuthException("Failed to send OTP email. Please try again.");
            }

            _logger.LogInformation("Registration initiated for {Email}. OTP sent.", dto.Email);

            return new AuthResponseDto
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email!,
                Token = string.Empty,
                TokenExpiry = DateTime.UtcNow,
                Roles = new List<string> { dto.Role }
            };
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

            if (!user.IsEmailVerified)
                throw new AuthException("Please verify your email before logging in.");

            return await BuildAuthResponseAsync(user);
        }

        public async Task<OtpResponseDto> VerifyOtpAsync(VerifyOtpDto dto, CancellationToken ct = default)
        {
            var otp = await _otpRepo.GetByEmailAsync(dto.Email, ct);

            if (otp is null)
                throw new AuthException("Invalid or expired OTP. Please request a new one.");

            if (otp.ExpiresAt <= DateTime.UtcNow)
                throw new AuthException("OTP has expired. Please request a new one.");

            if (otp.AttemptCount >= otp.MaxAttempts)
                throw new AuthException("Maximum OTP attempts exceeded. Please request a new one.");

            otp.AttemptCount++;

            if (otp.OtpCode != dto.OtpCode)
            {
                await _otpRepo.UpdateAsync(otp, ct);
                var remainingAttempts = otp.MaxAttempts - otp.AttemptCount;
                throw new AuthException($"Invalid OTP. {remainingAttempts} attempts remaining.");
            }

            var user = await _authRepo.GetByIdAsync(otp.UserId, ct);
            if (user is null)
                throw new AuthException("User not found.");

            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            await _authRepo.UpdateAsync(user);

            otp.IsVerified = true;
            otp.VerifiedAt = DateTime.UtcNow;
            await _otpRepo.UpdateAsync(otp, ct);

            _logger.LogInformation("Email verified for user {UserId}", user.Id);

            return new OtpResponseDto
            {
                Message = "Email verified successfully. You can now log in.",
                RemainingAttempts = otp.MaxAttempts - otp.AttemptCount,
                IsVerified = true
            };
        }

        public async Task<bool> ResendOtpAsync(ResendOtpDto dto, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByEmailAsync(dto.Email, ct);
            if (user is null)
                throw new AuthException("User not found.");

            if (user.IsEmailVerified)
                throw new AuthException("This email is already verified.");

            var existingOtp = await _otpRepo.GetByEmailAsync(dto.Email, ct);
            if (existingOtp is not null)
            {
                await _otpRepo.DeleteAsync(existingOtp.Id, ct);
            }

            var otpCode = GenerateOtp();
            var otpVerification = new OtpVerification
            {
                UserId = user.Id,
                Email = dto.Email,
                OtpCode = otpCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(OTP_EXPIRY_MINUTES),
                MaxAttempts = 5
            };

            await _otpRepo.CreateAsync(otpVerification, ct);

            var emailSent = await _emailService.SendOtpEmailAsync(dto.Email, otpCode, ct);
            if (!emailSent)
            {
                _logger.LogWarning("Failed to resend OTP email to {Email}", dto.Email);
                throw new AuthException("Failed to send OTP email. Please try again.");
            }

            _logger.LogInformation("OTP resent to {Email}", dto.Email);
            return true;
        }

        private string GenerateOtp()
        {
            var random = new Random();
            var otp = random.Next(100000, 999999).ToString();
            return otp;
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
