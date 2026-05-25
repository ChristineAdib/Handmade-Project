# OTP Email Verification Implementation Guide

## Overview
This document describes the OTP (One-Time Password) email verification feature added to the Handora registration system.

## Features Implemented

### 1. **Registration with OTP Verification**
- Users register with their credentials
- An OTP code is generated and sent to their email
- User must verify the OTP before they can log in
- OTP expires after 5 minutes

### 2. **OTP Verification Endpoint**
- Users can verify their email by submitting the OTP code
- Maximum 5 attempts allowed per OTP
- Clear error messages showing remaining attempts
- Automatic account activation upon successful verification

### 3. **Resend OTP**
- Users can request a new OTP if the previous one expired
- Old OTP is invalidated when a new one is sent
- Prevents email flooding with rate limiting at application level

### 4. **Email Service**
- SMTP-based email sending (configured for Gmail by default)
- HTML-formatted OTP emails with professional styling
- Comprehensive error logging

## Database Changes

### New Table: OtpVerifications
```sql
CREATE TABLE OtpVerifications (
    Id NVARCHAR(450) PRIMARY KEY,
    UserId NVARCHAR(MAX) NOT NULL,
    OtpCode NVARCHAR(6) NOT NULL,
    Email NVARCHAR(256) NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    AttemptCount INT DEFAULT 0,
    MaxAttempts INT DEFAULT 5,
    IsVerified BIT DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL,
    VerifiedAt DATETIME2 NULL
)
```

### User Table Updates
- Added `IsEmailVerified` (BIT, DEFAULT 0)
- Added `EmailVerifiedAt` (DATETIME2, NULL)

## API Endpoints

### 1. Register
**POST** `/api/auth/register`

**Request:**
```json
{
  "name": "John Doe",
  "email": "john@example.com",
  "password": "SecurePass123!",
  "confirmPassword": "SecurePass123!",
  "phoneNumber": "+1234567890",
  "role": "Buyer"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Registration initiated. Please verify your email with the OTP sent to your inbox.",
  "data": {
    "userId": "user-id",
    "name": "John Doe",
    "email": "john@example.com",
    "token": "",
    "tokenExpiry": "2026-05-23T18:08:00Z",
    "roles": ["Buyer"]
  }
}
```

### 2. Verify OTP
**POST** `/api/auth/verify-otp`

**Request:**
```json
{
  "email": "john@example.com",
  "otpCode": "123456"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Email verified successfully.",
  "data": {
    "message": "Email verified successfully. You can now log in.",
    "remainingAttempts": 5,
    "isVerified": true
  }
}
```

**Response (400 Bad Request - Invalid OTP):**
```json
{
  "success": false,
  "message": "Invalid OTP. 4 attempts remaining.",
  "data": null
}
```

### 3. Resend OTP
**POST** `/api/auth/resend-otp`

**Request:**
```json
{
  "email": "john@example.com"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "OTP resent successfully. Check your email.",
  "data": null
}
```

### 4. Login
**POST** `/api/auth/login`

**Request:**
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "userId": "user-id",
    "name": "John Doe",
    "email": "john@example.com",
    "token": "eyJhbGciOiJIUzI1NiIs...",
    "tokenExpiry": "2026-05-30T18:08:00Z",
    "roles": ["Buyer"]
  }
}
```

**Response (400 Bad Request - Email not verified):**
```json
{
  "success": false,
  "message": "Please verify your email before logging in.",
  "data": null
}
```

## Configuration

### SMTP Settings (appsettings.json)
```json
{
  "SmtpSettings": {
    "Server": "smtp.gmail.com",
    "Port": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "your-app-password",
    "EnableSsl": true
  }
}
```

### Gmail Configuration
1. Enable 2-Factor Authentication on your Gmail account
2. Generate an App Password: https://myaccount.google.com/apppasswords
3. Use the generated password in `SenderPassword`

### Other Email Providers
- **Outlook/Hotmail**: `smtp.outlook.com:587`
- **Yahoo**: `smtp.mail.yahoo.com:465` (EnableSsl: true)
- **Custom SMTP**: Configure with your provider's settings

## Files Created/Modified

### New Files
- `HandoraDomain/Models/AppUser/OtpVerification.cs` - OTP model
- `HandoraApplication/DTOs/AuthDTOs/OtpDtos.cs` - OTP DTOs
- `HandoraApplication/IServices/IEmailService.cs` - Email service interface
- `HandoraApplication/Services/EmailService.cs` - Email service implementation
- `HandoraDomain/Interfaces/IOtpRepository.cs` - OTP repository interface
- `HandoraInfrastructure/Repositries&UOW/OtpRepository.cs` - OTP repository implementation
- `HandoraInfrastructure/Migrations/20260523180800_AddOtpVerificationAndEmailVerification.cs` - Database migration

### Modified Files
- `HandoraDomain/Models/AppUser/User.cs` - Added email verification fields
- `HandoraApplication/Services/AuthService.cs` - Updated registration and login logic
- `HandoraApplication/IServices/IAuthService.cs` - Added OTP methods
- `HandoraApi/Controllers/AuthController.cs` - Added OTP endpoints
- `HandoraInfrastructure/Data/AppDbContext.cs` - Added OtpVerifications DbSet
- `HandoraApplication/ModuleApplicationDependences.cs` - Registered EmailService
- `HandoraInfrastructure/ModuleInfrastructureDependences.cs` - Registered OtpRepository
- `HandoraApi/appsettings.json` - Added SMTP configuration
- `HandoraApi/appsettings.Development.json` - Added SMTP configuration

## Implementation Details

### OTP Generation
- 6-digit random code
- Generated using `Random.Next(100000, 999999)`

### OTP Expiration
- Default: 5 minutes
- Configurable via `OTP_EXPIRY_MINUTES` constant in AuthService

### Attempt Limiting
- Maximum 5 attempts per OTP
- Configurable via `MaxAttempts` property in OtpVerification model
- Attempts are tracked and incremented on each failed verification

### Email Verification Flow
1. User registers → Account created but not verified
2. OTP sent to email → User receives 6-digit code
3. User submits OTP → Verified and can now log in
4. Login requires verified email

## Error Handling

### Common Error Scenarios

| Scenario | Error Message |
|----------|---------------|
| Email already registered | "An account with this email already exists." |
| OTP expired | "OTP has expired. Please request a new one." |
| Invalid OTP | "Invalid OTP. X attempts remaining." |
| Max attempts exceeded | "Maximum OTP attempts exceeded. Please request a new one." |
| Email not verified | "Please verify your email before logging in." |
| SMTP not configured | "Failed to send OTP email. Please try again." |

## Security Considerations

1. **OTP Storage**: OTPs are stored in the database (consider hashing in production)
2. **SMTP Credentials**: Store in secure configuration (use Azure Key Vault, AWS Secrets Manager, etc.)
3. **Rate Limiting**: Implement rate limiting on OTP requests to prevent abuse
4. **HTTPS**: Always use HTTPS in production
5. **Email Validation**: Verify email format before sending OTP

## Testing the Feature

### Using Swagger/OpenAPI
1. Navigate to `https://localhost:5001/swagger`
2. Call `/api/auth/register` with test data
3. Check console/logs for OTP code (if using mock service)
4. Call `/api/auth/verify-otp` with the OTP code
5. Call `/api/auth/login` to verify email verification requirement

### Manual Testing
```bash
# Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "password": "TestPass123!",
    "confirmPassword": "TestPass123!",
    "role": "Buyer"
  }'

# Verify OTP
curl -X POST https://localhost:5001/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'

# Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPass123!"
  }'
```

## Future Enhancements

1. **OTP Delivery Methods**: SMS, WhatsApp, Telegram
2. **Biometric Verification**: Fingerprint, Face ID
3. **Two-Factor Authentication**: TOTP (Time-based OTP)
4. **Email Verification Reminders**: Resend verification email after X days
5. **Rate Limiting**: Implement per-IP/per-email rate limiting
6. **OTP Customization**: Configurable length, characters, expiry time
7. **Audit Logging**: Track all OTP verification attempts

## Troubleshooting

### OTP Email Not Received
1. Check SMTP settings in appsettings.json
2. Verify email address is correct
3. Check spam/junk folder
4. Check application logs for SMTP errors

### "Failed to send OTP email" Error
1. Verify SMTP credentials are correct
2. Check if Gmail App Password is used (not regular password)
3. Ensure 2FA is enabled on Gmail account
4. Check firewall/network settings for port 587

### OTP Verification Fails
1. Ensure OTP hasn't expired (5 minutes)
2. Check for typos in OTP code
3. Verify email matches registration email
4. Check remaining attempts

## Support
For issues or questions, please refer to the application logs or contact the development team.
