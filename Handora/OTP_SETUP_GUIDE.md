# OTP Email Verification - Setup Instructions

## Quick Start Guide

### Step 1: Update appsettings.json

Update your `appsettings.Development.json` and `appsettings.json` with SMTP credentials:

```json
"SmtpSettings": {
  "Server": "smtp.gmail.com",
  "Port": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password",
  "EnableSsl": true
}
```

### Step 2: Gmail Setup (if using Gmail)

1. Go to https://myaccount.google.com/security
2. Enable 2-Step Verification
3. Go to https://myaccount.google.com/apppasswords
4. Select "Mail" and "Windows Computer"
5. Copy the generated 16-character password
6. Paste it in `SenderPassword` in appsettings.json

### Step 3: Run Database Migration

```bash
# In the HandoraApi directory
dotnet ef database update
```

Or if using Package Manager Console in Visual Studio:
```powershell
Update-Database
```

### Step 4: Test the Feature

#### Using Swagger UI:
1. Start the application
2. Navigate to `https://localhost:5001/swagger`
3. Expand the Auth controller
4. Test `/api/auth/register` endpoint
5. Check your email for the OTP code
6. Test `/api/auth/verify-otp` endpoint with the OTP
7. Test `/api/auth/login` endpoint

#### Using cURL:

```bash
# 1. Register
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "your-test-email@gmail.com",
    "password": "TestPassword123!",
    "confirmPassword": "TestPassword123!",
    "phoneNumber": "+1234567890",
    "role": "Buyer"
  }'

# 2. Verify OTP (use the code from your email)
curl -X POST https://localhost:5001/api/auth/verify-otp \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-test-email@gmail.com",
    "otpCode": "123456"
  }'

# 3. Login
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "your-test-email@gmail.com",
    "password": "TestPassword123!"
  }'
```

## Architecture Overview

```
Registration Flow:
┌─────────────┐
│   Register  │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────┐
│ Create User (not verified)  │
│ Generate OTP (6 digits)     │
│ Save OTP to Database        │
│ Send OTP Email              │
└──────┬──────────────────────┘
       │
       ▼
┌──────────────────────────┐
│ User Receives OTP Email  │
└──────┬───────────────────┘
       │
       ▼
┌──────────────────────────┐
│ Verify OTP Endpoint      │
│ Check: Not Expired       │
│ Check: Correct Code      │
│ Check: Attempts < 5      │
└──────┬───────────────────┘
       │
       ├─ Invalid ──► Error + Remaining Attempts
       │
       └─ Valid ──► Mark Email as Verified
                    User Can Now Login
```

## Key Components

### 1. **OtpVerification Model**
- Stores OTP codes with expiration
- Tracks verification attempts
- Links to user email

### 2. **EmailService**
- Sends OTP via SMTP
- HTML formatted emails
- Error handling and logging

### 3. **AuthService Updates**
- Modified registration to require OTP verification
- New OTP verification method
- Resend OTP functionality
- Login now checks email verification status

### 4. **Database**
- New `OtpVerifications` table
- User table updated with verification fields
- Indexed by email for fast lookups

## Configuration Options

### SMTP Providers

**Gmail:**
```json
"SmtpSettings": {
  "Server": "smtp.gmail.com",
  "Port": 587,
  "SenderEmail": "your-email@gmail.com",
  "SenderPassword": "your-app-password",
  "EnableSsl": true
}
```

**Outlook/Hotmail:**
```json
"SmtpSettings": {
  "Server": "smtp.outlook.com",
  "Port": 587,
  "SenderEmail": "your-email@outlook.com",
  "SenderPassword": "your-password",
  "EnableSsl": true
}
```

**SendGrid (via SMTP):**
```json
"SmtpSettings": {
  "Server": "smtp.sendgrid.net",
  "Port": 587,
  "SenderEmail": "apikey",
  "SenderPassword": "SG.your-sendgrid-api-key",
  "EnableSsl": true
}
```

## Customization

### Change OTP Expiry Time
In `AuthService.cs`, modify:
```csharp
private const int OTP_EXPIRY_MINUTES = 5; // Change this value
```

### Change OTP Length
In `AuthService.cs`, modify the `GenerateOtp()` method:
```csharp
private string GenerateOtp()
{
    var random = new Random();
    var otp = random.Next(100000, 999999).ToString(); // Adjust range
    return otp;
}
```

### Change Max Attempts
In `OtpVerification` model or when creating OTP:
```csharp
MaxAttempts = 10; // Change from 5 to 10
```

## Troubleshooting

### Issue: "Failed to send OTP email"
**Solutions:**
- Verify SMTP credentials in appsettings.json
- Check if Gmail App Password is used (not regular password)
- Ensure 2FA is enabled on Gmail
- Check firewall/network for port 587 access
- Review application logs for detailed error

### Issue: OTP Email Not Received
**Solutions:**
- Check spam/junk folder
- Verify email address is correct
- Wait a few seconds (email delivery takes time)
- Check application logs for SMTP errors
- Try resending OTP

### Issue: "OTP has expired"
**Solutions:**
- OTP is valid for 5 minutes only
- Click "Resend OTP" to get a new code
- New OTP will be sent to email

### Issue: "Maximum OTP attempts exceeded"
**Solutions:**
- Click "Resend OTP" to get a new code
- New code resets the attempt counter

## Security Best Practices

1. **Never log OTP codes** in production
2. **Use HTTPS** for all endpoints
3. **Store SMTP credentials** in secure configuration (Azure Key Vault, AWS Secrets Manager)
4. **Implement rate limiting** on OTP requests
5. **Hash OTP codes** before storing (future enhancement)
6. **Set reasonable expiry times** (5-10 minutes)
7. **Limit OTP attempts** (5 attempts recommended)
8. **Monitor failed attempts** for suspicious activity

## API Response Examples

### Successful Registration
```json
{
  "success": true,
  "message": "Registration initiated. Please verify your email with the OTP sent to your inbox.",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Test User",
    "email": "test@example.com",
    "token": "",
    "tokenExpiry": "2026-05-23T18:08:00Z",
    "roles": ["Buyer"]
  }
}
```

### Successful OTP Verification
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

### Failed OTP Verification
```json
{
  "success": false,
  "message": "Invalid OTP. 4 attempts remaining.",
  "data": null
}
```

### Successful Login (After Email Verification)
```json
{
  "success": true,
  "message": "Login successful.",
  "data": {
    "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Test User",
    "email": "test@example.com",
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "tokenExpiry": "2026-05-30T18:08:00Z",
    "roles": ["Buyer"]
  }
}
```

## Next Steps

1. ✅ Update appsettings with SMTP credentials
2. ✅ Run database migration
3. ✅ Test registration flow
4. ✅ Test OTP verification
5. ✅ Test login with verified email
6. ✅ Deploy to production with secure credentials

## Support

For detailed implementation information, see `OTP_IMPLEMENTATION_GUIDE.md`
