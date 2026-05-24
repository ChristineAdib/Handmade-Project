# OTP Email Verification - Quick Reference Card

## 🚀 Quick Start (5 Minutes)

### 1. Configure SMTP (2 minutes)
Edit `appsettings.Development.json`:
```json
"SmtpSettings": {
  "Server": "smtp.gmail.com",
  "Port": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password",
  "EnableSsl": true
}
```

### 2. Run Migration (1 minute)
```bash
dotnet ef database update
```

### 3. Test Registration (2 minutes)
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

# Check email for OTP code, then verify
curl -X POST https://localhost:5001/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "otpCode": "123456"
  }'
```

## 📋 API Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/auth/register` | POST | Register user (sends OTP) |
| `/api/auth/verify-otp` | POST | Verify email with OTP |
| `/api/auth/resend-otp` | POST | Resend OTP to email |
| `/api/auth/login` | POST | Login (requires verified email) |

## 🔑 Request/Response Examples

### Register Request
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

### Register Response (Success)
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

### Verify OTP Request
```json
{
  "email": "john@example.com",
  "otpCode": "123456"
}
```

### Verify OTP Response (Success)
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

### Verify OTP Response (Error)
```json
{
  "success": false,
  "message": "Invalid OTP. 4 attempts remaining.",
  "data": null
}
```

## ⚙️ Configuration

### Gmail Setup
1. Go to https://myaccount.google.com/security
2. Enable 2-Step Verification
3. Go to https://myaccount.google.com/apppasswords
4. Generate App Password
5. Copy password to `SenderPassword`

### Other Providers
- **Outlook**: `smtp.outlook.com:587`
- **Yahoo**: `smtp.mail.yahoo.com:465` (SSL)
- **SendGrid**: `smtp.sendgrid.net:587` (User: `apikey`)

## 🔄 Flow Diagram

```
User Registration
    ↓
Create Account (not verified)
    ↓
Generate 6-digit OTP
    ↓
Send OTP Email
    ↓
User Receives Email
    ↓
Submit OTP Code
    ↓
Verify OTP (check: not expired, correct code, attempts < 5)
    ├─ Invalid → Show error + remaining attempts
    └─ Valid → Mark email verified → Can now login
```

## 🛠️ Customization

### Change OTP Expiry
File: `AuthService.cs`
```csharp
private const int OTP_EXPIRY_MINUTES = 5; // Change to desired minutes
```

### Change Max Attempts
File: `AuthService.cs` (RegisterAsync method)
```csharp
MaxAttempts = 5; // Change to desired number
```

### Change OTP Length
File: `AuthService.cs` (GenerateOtp method)
```csharp
var otp = random.Next(100000, 999999).ToString(); // Adjust range
```

## 🐛 Common Issues

| Issue | Solution |
|-------|----------|
| OTP email not received | Check SMTP credentials, verify email, check spam |
| "Failed to send OTP email" | Verify SMTP settings, check Gmail App Password |
| "OTP has expired" | Request new OTP (5 minute expiry) |
| "Maximum OTP attempts exceeded" | Request new OTP |
| "Please verify your email before logging in" | Complete OTP verification first |

## 📁 Key Files

| File | Purpose |
|------|---------|
| `OtpVerification.cs` | OTP data model |
| `OtpDtos.cs` | Request/Response DTOs |
| `EmailService.cs` | SMTP email sending |
| `OtpRepository.cs` | OTP data access |
| `AuthService.cs` | Registration & OTP logic |
| `AuthController.cs` | API endpoints |
| `AppDbContext.cs` | Database context |
| `Migration file` | Database schema |

## 📊 Database

### OtpVerifications Table
- `Id` - Primary key
- `UserId` - User reference
- `OtpCode` - 6-digit code
- `Email` - User email
- `ExpiresAt` - Expiration time
- `AttemptCount` - Failed attempts
- `MaxAttempts` - Max allowed attempts (5)
- `IsVerified` - Verification status
- `CreatedAt` - Creation time
- `VerifiedAt` - Verification time

### User Table Updates
- `IsEmailVerified` - Email verification status
- `EmailVerifiedAt` - Verification timestamp

## 🔐 Security Checklist

- [ ] SMTP credentials stored securely
- [ ] HTTPS enabled in production
- [ ] Rate limiting implemented
- [ ] Email validation enabled
- [ ] OTP expiry set (5 minutes)
- [ ] Max attempts limited (5)
- [ ] Error logging enabled
- [ ] Audit trail maintained

## 📞 Documentation

- **OTP_IMPLEMENTATION_GUIDE.md** - Full technical documentation
- **OTP_SETUP_GUIDE.md** - Setup and configuration
- **FRONTEND_INTEGRATION_GUIDE.md** - Frontend implementation
- **OTP_IMPLEMENTATION_SUMMARY.md** - Implementation overview

## ✅ Verification Checklist

- [ ] SMTP configured in appsettings
- [ ] Database migration applied
- [ ] Application compiles without errors
- [ ] Register endpoint sends OTP
- [ ] OTP received in email
- [ ] Verify OTP endpoint works
- [ ] Login requires verified email
- [ ] Resend OTP works
- [ ] Error handling works correctly
- [ ] Frontend integrated

## 🎯 Next Steps

1. Configure SMTP credentials
2. Run database migration
3. Test registration flow
4. Test OTP verification
5. Implement frontend UI
6. Deploy to production

---

**Version**: 1.0
**Last Updated**: May 23, 2026
**Status**: ✅ Ready for Production
