using HandoraApplication.DTOs.AuthDTOs;
using HandoraApplication.Helpers.AuthHelper;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;

namespace HandoraApplication.Services
{
    public sealed class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepo;
        private readonly IOtpRepository _otpRepo;
        private readonly IEmailService _emailService;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<AuthService> _logger;
        private readonly IFileService _fileService;
        private readonly string _googleClientId;
        private const int OTP_EXPIRY_MINUTES = 5;
        private const int OTP_LENGTH = 6;

        public AuthService(
            IAuthRepository authRepo,
            IOtpRepository otpRepo,
            IEmailService emailService,
            JwtHelper jwtHelper,
            ILogger<AuthService> logger,
            IFileService fileService,
            IConfiguration configuration)
        {
            _authRepo = authRepo;
            _otpRepo = otpRepo;
            _emailService = emailService;
            _jwtHelper = jwtHelper;
            _logger = logger;
            _fileService = fileService;
            _googleClientId = configuration["Google:ClientId"] ?? string.Empty;
        }

        public async Task<AuthResponseDto> RegisterAsync( RegisterDto dto, CancellationToken ct = default)
        {
            var existing = await _authRepo.GetByEmailAsync(dto.Email, ct);
            if (existing is not null)
                throw new AuthException("An account with this email already exists.");

            string? profilePictureUrl = null;
            if (dto.ProfileImage is not null)
                profilePictureUrl = await _fileService.UploadFileAsync(dto.ProfileImage, "profiles");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                UserName = dto.Email.Split("@")[0],
                PhoneNumber = dto.PhoneNumber,
                CreatedAt = DateTime.UtcNow,
                Token = string.Empty,
                IsEmailVerified = false,
                Bio = dto.Bio,
                ProfileImage = profilePictureUrl
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

        public async Task<IEnumerable<GetUserDto>> GetAllUsersAsync(CancellationToken ct = default)
        {
            var users = await _authRepo.GetAllAsync(ct);
            var result = new List<GetUserDto>();

            foreach (var user in users)
            {
                var roles = await _authRepo.GetRolesAsync(user);
                result.Add(MapToDto(user, roles));
            }

            return result;
        }

        public async Task<GetUserDto> GetUserByIdAsync(string id, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByIdAsync(id, ct)
                ?? throw new Exception("User not found.");

            var roles = await _authRepo.GetRolesAsync(user);
            return MapToDto(user, roles);
        }

        public async Task<GetUserDto> UpdateUserAsync(string id, UpdateUserDto dto, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByIdAsync(id, ct)
                ?? throw new Exception("User not found.");

            if (dto.Name is not null) user.Name = dto.Name;
            if (dto.PhoneNumber is not null) user.PhoneNumber = dto.PhoneNumber;
            if (dto.Bio is not null) user.Bio = dto.Bio;

            if (dto.ProfileImage is not null)
            {
                if (user.ProfileImage is not null)
                    await _fileService.DeleteFileAsync(user.ProfileImage);

                user.ProfileImage = await _fileService.UploadFileAsync(dto.ProfileImage, "profiles");
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _authRepo.UpdateAsync(user, ct);

            var roles = await _authRepo.GetRolesAsync(user);
            return MapToDto(user, roles);
        }

        public async Task DeleteUserAsync(string id, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByIdAsync(id, ct)
                ?? throw new Exception("User not found.");

            await _authRepo.DeleteAsync(user, ct);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByEmailAsync(dto.Email, ct);
            if (user is null || user.IsDeleted)
            {
                _logger.LogInformation("Forgot password requested for non-existent or deleted email: {Email}", dto.Email);
                return;
            }

            var token = await _authRepo.GeneratePasswordResetTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(user.Email!);
            var resetUrl = $"http://localhost:4200/reset-password?email={encodedEmail}&token={encodedToken}";

            var subject = "Reset Your Password - Handora";
            var body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #2c221e;'>
                        <h2>Reset Your Password</h2>
                        <p>Hello {user.Name},</p>
                        <p>We received a request to reset the password for your Handora account.</p>
                        <p>Please click the button below to choose a new password:</p>
                        <p style='margin: 30px 0;'>
                            <a href='{resetUrl}' style='background-color: #5a463b; color: #ffffff; padding: 12px 24px; text-decoration: none; border-radius: 20px; font-weight: bold;'>Reset Password</a>
                        </p>
                        <p>If you did not request a password reset, please ignore this email.</p>
                        <hr style='border: none; border-top: 1px solid #dcd3c7;'>
                        <p style='color: #85766c; font-size: 12px;'>This is an automated message, please do not reply.</p>
                    </body>
                </html>";

            await _emailService.SendEmailAsync(user.Email!, subject, body, ct);
        }

        public async Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByEmailAsync(dto.Email, ct);
            if (user is null || user.IsDeleted)
                throw new AuthException("Invalid request.");

            var result = await _authRepo.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new AuthException(string.Join(", ", errors));
            }
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginDto dto, CancellationToken ct = default)
        {
            GoogleJsonWebSignature.Payload payload;
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _googleClientId }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(dto.Credential, settings);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google token validation failed.");
                throw new AuthException("Invalid Google token.");
            }

            if (payload == null || string.IsNullOrEmpty(payload.Email))
            {
                throw new AuthException("Invalid Google token payload.");
            }

            var user = await _authRepo.GetByEmailAsync(payload.Email, ct);

            if (user is null)
            {
                var baseUsername = payload.Email.Split("@")[0];
                var username = baseUsername;
                int suffix = 1;
                
                while (await _authRepo.GetByUsernameAsync(username) is not null)
                {
                    username = $"{baseUsername}{suffix}";
                    suffix++;
                }

                user = new User
                {
                    Name = payload.Name ?? baseUsername,
                    Email = payload.Email,
                    UserName = username,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsEmailVerified = true,
                    EmailVerifiedAt = DateTime.UtcNow,
                    EmailConfirmed = true,
                    ProfileImage = payload.Picture,
                    Token = string.Empty
                };

                var createResult = await _authRepo.CreateAsync(user, ct);
                if (!createResult.Succeeded)
                {
                    var errors = createResult.Errors.Select(e => e.Description).ToList();
                    _logger.LogWarning("Google user registration failed for {Email}: {Errors}", payload.Email, errors);
                    throw new AuthException(string.Join(", ", errors));
                }

                await _authRepo.AddToRoleAsync(user, AppRoles.Buyer);
                _logger.LogInformation("Google user auto-registered as Buyer: {Email}", payload.Email);
            }
            else
            {
                if (user.IsBanned)
                    throw new AuthException("Your account has been suspended. Please contact support.");

                if (user.IsDeleted)
                    throw new AuthException("Account not found.");

                if (!user.IsEmailVerified)
                {
                    user.IsEmailVerified = true;
                    user.EmailVerifiedAt = DateTime.UtcNow;
                    user.EmailConfirmed = true;
                    await _authRepo.UpdateAsync(user, ct);
                }
            }

            return await BuildAuthResponseAsync(user);
        }

        private static GetUserDto MapToDto(User user, IList<string> roles) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email!,
            PhoneNumber = user.PhoneNumber,
            ProfileImage = user.ProfileImage,
            Bio = user.Bio,
            IsActive = user.IsActive,
            IsBanned = user.IsBanned,
            CreatedAt = user.CreatedAt,
            Roles = roles
        };

        public async Task<AuthResponseDto> AssignSellerRoleAndGenerateTokenAsync(string userId, CancellationToken ct = default)
        {
            var user = await _authRepo.GetByIdAsync(userId, ct);
            if (user is null)
                throw new AuthException("User not found.");

            var roles = await _authRepo.GetRolesAsync(user);
            if (!roles.Contains(AppRoles.Seller))
            {
                await _authRepo.AddToRoleAsync(user, AppRoles.Seller);
            }

            return await BuildAuthResponseAsync(user);
        }
    }
}
