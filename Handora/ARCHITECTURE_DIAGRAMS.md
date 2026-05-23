# OTP Email Verification - Architecture & Flow Diagrams

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENT APPLICATION                        │
│                    (React/Angular/Vue)                           │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                    HTTP/HTTPS Requests
                           │
┌──────────────────────────▼──────────────────────────────────────┐
│                      API LAYER                                   │
│  ┌────────────────────────────────────────────────────────────┐ │
│  │              AuthController                               │ │
│  │  ┌──────────────────────────────────────────────────────┐ │ │
│  │  │ POST /api/auth/register                              │ │ │
│  │  │ POST /api/auth/verify-otp                            │ │ │
│  │  │ POST /api/auth/resend-otp                            │ │ │
│  │  │ POST /api/auth/login                                 │ │ │
│  │  └──────────────────────────────────────────────────────┘ │ │
│  └────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬──────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  AUTH SERVICE    │ │ EMAIL SERVICE    │ │ OTP REPOSITORY   │
│                  │ │                  │ │                  │
│ • RegisterAsync  │ │ • SendOtpEmail   │ │ • CreateAsync    │
│ • LoginAsync     │ │ • SendEmailAsync │ │ • GetByEmailAsync│
│ • VerifyOtpAsync │ │                  │ │ • UpdateAsync    │
│ • ResendOtpAsync │ │ (SMTP)           │ │ • DeleteAsync    │
│ • GenerateOtp    │ │                  │ │                  │
└──────────────────┘ └────────┬─────────┘ └────────┬─────────┘
        │                     │                    │
        │                     ▼                    │
        │            ┌──────────────────┐         │
        │            │  SMTP SERVER     │         │
        │            │  (Gmail/Outlook) │         │
        │            └──────────────────┘         │
        │                                         │
        └─────────────────────┬───────────────────┘
                              │
        ┌─────────────────────▼───────────────────┐
        │                                         │
        ▼                                         ▼
┌──────────────────────┐              ┌──────────────────────┐
│  AUTH REPOSITORY     │              │  DATABASE            │
│                      │              │  (SQL Server)        │
│ • GetByEmailAsync    │              │                      │
│ • GetByIdAsync       │              │ ┌──────────────────┐ │
│ • CreateAsync        │              │ │ AspNetUsers      │ │
│ • UpdateAsync        │              │ │ (+ IsEmailVerified
│ • CheckPasswordAsync │              │ │  + EmailVerifiedAt)
│ • GetRolesAsync      │              │ └──────────────────┘ │
│ • AddToRoleAsync     │              │                      │
└──────────────────────┘              │ ┌──────────────────┐ │
                                      │ │ OtpVerifications │ │
                                      │ │ • Id             │ │
                                      │ │ • UserId         │ │
                                      │ │ • OtpCode        │ │
                                      │ │ • Email          │ │
                                      │ │ • ExpiresAt      │ │
                                      │ │ • AttemptCount   │ │
                                      │ │ • MaxAttempts    │ │
                                      │ │ • IsVerified     │ │
                                      │ │ • CreatedAt      │ │
                                      │ │ • VerifiedAt     │ │
                                      │ └──────────────────┘ │
                                      └──────────────────────┘
```

## Registration Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    USER REGISTRATION FLOW                        │
└─────────────────────────────────────────────────────────────────┘

1. USER SUBMITS REGISTRATION FORM
   ┌──────────────────────────────────────────┐
   │ Name, Email, Password, Role              │
   └──────────────────────────────────────────┘
                    │
                    ▼
2. VALIDATE INPUT
   ┌──────────────────────────────────────────┐
   │ • Check required fields                  │
   │ • Validate email format                  │
   │ • Check password length (min 8)          │
   │ • Check password match                   │
   └──────────────────────────────────────────┘
                    │
                    ▼
3. CHECK EMAIL UNIQUENESS
   ┌──────────────────────────────────────────┐
   │ Query: GetByEmailAsync(email)            │
   └──────────────────────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │                     │
      EXISTS              NOT EXISTS
         │                     │
         ▼                     ▼
    ERROR:              4. CREATE USER ACCOUNT
  "Email already        ┌──────────────────────────────────────────┐
   exists"              │ • Create User object                     │
                        │ • Set IsEmailVerified = false            │
                        │ • Hash password                          │
                        │ • Save to database                       │
                        │ • Add role (Buyer/Seller)               │
                        └──────────────────────────────────────────┘
                                     │
                                     ▼
                        5. GENERATE OTP
                        ┌──────────────────────────────────────────┐
                        │ • Generate 6-digit random code           │
                        │ • Create OtpVerification record          │
                        │ • Set ExpiresAt = Now + 5 minutes        │
                        │ • Set MaxAttempts = 5                    │
                        │ • Save to database                       │
                        └──────────────────────────────────────────┘
                                     │
                                     ▼
                        6. SEND OTP EMAIL
                        ┌──────────────────────────────────────────┐
                        │ • Format HTML email                      │
                        │ • Send via SMTP                          │
                        │ • Log result                             │
                        └──────────────────────────────────────────┘
                                     │
                        ┌────────────┴────────────┐
                        │                         │
                     SUCCESS                   FAILURE
                        │                         │
                        ▼                         ▼
                   7. RETURN RESPONSE        ERROR:
                   ┌──────────────────────────────────────────┐
                   │ • UserId                                 │
                   │ • Name                                   │
                   │ • Email                                  │
                   │ • Empty Token (not verified yet)         │
                   │ • Message: "Check your email for OTP"    │
                   └──────────────────────────────────────────┘
                                     │
                                     ▼
                        8. USER RECEIVES EMAIL
                        ┌──────────────────────────────────────────┐
                        │ Subject: Your OTP Verification Code      │
                        │ Body: [HTML formatted with OTP code]     │
                        │ Expires in 5 minutes                     │
                        └──────────────────────────────────────────┘
```

## OTP Verification Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    OTP VERIFICATION FLOW                         │
└─────────────────────────────────────────────────────────────────┘

1. USER SUBMITS OTP CODE
   ┌──────────────────────────────────────────┐
   │ Email: john@example.com                  │
   │ OTP Code: 123456                         │
   └──────────────────────────────────────────┘
                    │
                    ▼
2. VALIDATE INPUT
   ┌──────────────────────────────────────────┐
   │ • Check OTP is 6 digits                  │
   │ • Check email format                     │
   └──────────────────────────────────────────┘
                    │
                    ▼
3. FETCH OTP RECORD
   ┌──────────────────────────────────────────┐
   │ Query: GetByEmailAsync(email)            │
   │ Filter: NOT verified AND NOT expired     │
   └──────────────────────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │                     │
      FOUND              NOT FOUND
         │                     │
         ▼                     ▼
    4. CHECK EXPIRY      ERROR:
    ┌──────────────────┐  "Invalid or
    │ ExpiresAt >      │   expired OTP"
    │ DateTime.UtcNow? │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    YES    NO
      │     │
      ▼     ▼
    5.   ERROR:
    CHECK "OTP has
    ATTEMPTS expired"
    ┌──────────────────┐
    │ AttemptCount <   │
    │ MaxAttempts (5)? │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    YES    NO
      │     │
      ▼     ▼
    6.   ERROR:
    CHECK "Max attempts
    CODE  exceeded"
    ┌──────────────────┐
    │ OtpCode ==       │
    │ submitted code?  │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    YES    NO
      │     │
      ▼     ▼
    7.   8. INCREMENT
    MARK  ATTEMPTS
    EMAIL ┌──────────────────┐
    VERIFIED │ AttemptCount++   │
    ┌──────────────────┐ │ Save to DB   │
    │ User.IsEmailVerified │ └──────────────────┘
    │ = true           │         │
    │ EmailVerifiedAt  │         ▼
    │ = Now            │    ERROR:
    │ Save to DB       │    "Invalid OTP.
    └──────────────────┘    X attempts
         │                  remaining"
         ▼
    9. MARK OTP VERIFIED
    ┌──────────────────┐
    │ OtpVerification  │
    │ .IsVerified=true │
    │ .VerifiedAt=Now  │
    │ Save to DB       │
    └──────────────────┘
         │
         ▼
    10. RETURN SUCCESS
    ┌──────────────────┐
    │ Message:         │
    │ "Email verified  │
    │  successfully"   │
    │ IsVerified: true │
    └──────────────────┘
         │
         ▼
    11. USER CAN LOGIN
    ┌──────────────────┐
    │ Redirect to      │
    │ Login Page       │
    └──────────────────┘
```

## Login Flow with Email Verification

```
┌─────────────────────────────────────────────────────────────────┐
│                    LOGIN FLOW                                    │
└─────────────────────────────────────────────────────────────────┘

1. USER SUBMITS LOGIN FORM
   ┌──────────────────────────────────────────┐
   │ Email: john@example.com                  │
   │ Password: SecurePass123!                 │
   └──────────────────────────────────────────┘
                    │
                    ▼
2. VALIDATE INPUT
   ┌──────────────────────────────────────────┐
   │ • Check required fields                  │
   │ • Validate email format                  │
   └──────────────────────────────────────────┘
                    │
                    ▼
3. FETCH USER
   ┌──────────────────────────────────────────┐
   │ Query: GetByEmailAsync(email)            │
   └──────────────────────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │                     │
      FOUND              NOT FOUND
         │                     │
         ▼                     ▼
    4. CHECK          ERROR:
    PASSWORD          "Invalid email
    ┌──────────────────┐ or password"
    │ CheckPasswordAsync│
    │ (user, password) │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    VALID  INVALID
      │     │
      ▼     ▼
    5.   ERROR:
    CHECK "Invalid email
    BANNED or password"
    ┌──────────────────┐
    │ user.IsBanned?   │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    NO    YES
      │     │
      ▼     ▼
    6.   ERROR:
    CHECK "Account
    DELETED suspended"
    ┌──────────────────┐
    │ user.IsDeleted?  │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    NO    YES
      │     │
      ▼     ▼
    7.   ERROR:
    CHECK "Account
    EMAIL  not found"
    VERIFIED
    ┌──────────────────┐
    │ user.IsEmailVerified?
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    YES    NO
      │     │
      ▼     ▼
    8.   ERROR:
    GENERATE "Please verify
    JWT TOKEN your email"
    ┌──────────────────┐
    │ • Create token   │
    │ • Set expiry     │
    │ • Save token     │
    └──────────────────┘
         │
         ▼
    9. RETURN SUCCESS
    ┌──────────────────┐
    │ • UserId         │
    │ • Name           │
    │ • Email          │
    │ • JWT Token      │
    │ • TokenExpiry    │
    │ • Roles          │
    └──────────────────┘
         │
         ▼
    10. REDIRECT TO DASHBOARD
    ┌──────────────────┐
    │ Store token in   │
    │ localStorage     │
    │ Redirect to home │
    └──────────────────┘
```

## Resend OTP Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                    RESEND OTP FLOW                              │
└─────────────────────────────────────────────────────────────────┘

1. USER CLICKS RESEND OTP
   ┌──────────────────────────────────────────┐
   │ Email: john@example.com                  │
   └──────────────────────────────────────────┘
                    │
                    ▼
2. FETCH USER
   ┌──────────────────────────────────────────┐
   │ Query: GetByEmailAsync(email)            │
   └──────────────────────────────────────────┘
                    │
         ┌──────────┴──────────┐
         │                     │
      FOUND              NOT FOUND
         │                     │
         ▼                     ▼
    3. CHECK          ERROR:
    VERIFIED          "User not found"
    ┌──────────────────┐
    │ user.IsEmailVerified?
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    NO    YES
      │     │
      ▼     ▼
    4.   ERROR:
    DELETE "Email already
    OLD OTP verified"
    ┌──────────────────┐
    │ Find existing OTP│
    │ Delete if exists │
    └──────────────────┘
         │
         ▼
    5. GENERATE NEW OTP
    ┌──────────────────┐
    │ • Generate code  │
    │ • Create record  │
    │ • Set expiry     │
    │ • Save to DB     │
    └──────────────────┘
         │
         ▼
    6. SEND EMAIL
    ┌──────────────────┐
    │ • Format email   │
    │ • Send via SMTP  │
    │ • Log result     │
    └──────────────────┘
         │
      ┌──┴──┐
      │     │
    SUCCESS FAILURE
      │     │
      ▼     ▼
    7.   ERROR:
    RETURN "Failed to
    SUCCESS send OTP"
    ┌──────────────────┐
    │ Message:         │
    │ "OTP resent to   │
    │  your email"     │
    └──────────────────┘
```

## Database Schema Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    AspNetUsers (Users)                          │
├─────────────────────────────────────────────────────────────────┤
│ PK │ Id (string)                                                 │
├────┼─────────────────────────────────────────────────────────────┤
│    │ Name (string)                                               │
│    │ Email (string)                                              │
│    │ UserName (string)                                           │
│    │ PasswordHash (string)                                       │
│    │ PhoneNumber (string, nullable)                              │
│    │ ProfileImage (string, nullable)                             │
│    │ Bio (string, nullable)                                      │
│    │ IsActive (bool)                                             │
│    │ IsDeleted (bool)                                            │
│    │ IsBanned (bool)                                             │
│    │ ✨ IsEmailVerified (bool) - NEW                             │
│    │ ✨ EmailVerifiedAt (datetime, nullable) - NEW               │
│    │ CreatedAt (datetime)                                        │
│    │ UpdatedAt (datetime, nullable)                              │
│    │ Token (string)                                              │
└────┴─────────────────────────────────────────────────────────────┘
                              │
                              │ 1:N
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    OtpVerifications - NEW                       │
├─────────────────────────────────────────────────────────────────┤
│ PK │ Id (string)                                                 │
├────┼─────────────────────────────────────────────────────────────┤
│ FK │ UserId (string) → AspNetUsers.Id                            │
├────┼─────────────────────────────────────────────────────────────┤
│    │ OtpCode (string, max 6)                                     │
│    │ Email (string, max 256) - INDEXED                           │
│    │ ExpiresAt (datetime)                                        │
│    │ AttemptCount (int, default 0)                               │
│    │ MaxAttempts (int, default 5)                                │
│    │ IsVerified (bool, default false)                            │
│    │ CreatedAt (datetime)                                        │
│    │ VerifiedAt (datetime, nullable)                             │
└─────────────────────────────────────────────────────────────────┘
```

## Component Interaction Diagram

```
┌──────────────────────────────────────────────────────────────────┐
│                    COMPONENT INTERACTIONS                         │
└──────────────────────────────────────────────────────────────────┘

┌─────────────────────┐
│  AuthController     │
└──────────┬──────────┘
           │
    ┌──────┴──────┐
    │             │
    ▼             ▼
┌─────────────┐ ┌──────────────┐
│ AuthService │ │ EmailService │
└──────┬──────┘ └──────┬───────┘
       │                │
       │                ▼
       │         ┌──────────────┐
       │         │ SMTP Server  │
       │         └──────────────┘
       │
       ├─────────────────────────┐
       │                         │
       ▼                         ▼
┌──────────────────┐    ┌──────────────────┐
│ AuthRepository   │    │ OtpRepository    │
└────────┬─────────┘    └────────┬─────────┘
         │                       │
         └───────────┬───────────┘
                     │
                     ▼
            ┌──────────────────┐
            │   AppDbContext   │
            │  (SQL Server)    │
            └──────────────────┘
```

---

**Diagram Version**: 1.0
**Last Updated**: May 23, 2026
**Status**: ✅ Complete
