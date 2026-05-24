# Frontend Integration Guide - OTP Email Verification

## Overview
This guide explains how to integrate the OTP email verification feature on the frontend (React/Angular/Vue).

## Registration Flow (Frontend)

### Step 1: Registration Form
Display a standard registration form with the following fields:
- Name
- Email
- Password
- Confirm Password
- Phone Number (optional)
- Role (Buyer/Seller)

```jsx
// React Example
const [formData, setFormData] = useState({
  name: '',
  email: '',
  password: '',
  confirmPassword: '',
  phoneNumber: '',
  role: 'Buyer'
});

const handleRegister = async (e) => {
  e.preventDefault();
  
  try {
    const response = await fetch('/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(formData)
    });
    
    const result = await response.json();
    
    if (result.success) {
      // Show OTP verification page
      setShowOtpVerification(true);
      setUserEmail(formData.email);
    } else {
      // Show error message
      setError(result.message);
    }
  } catch (error) {
    setError('Registration failed. Please try again.');
  }
};
```

### Step 2: OTP Verification Page
After successful registration, show an OTP verification page:

```jsx
const [otpCode, setOtpCode] = useState('');
const [attempts, setAttempts] = useState(5);
const [timeLeft, setTimeLeft] = useState(300); // 5 minutes

const handleVerifyOtp = async (e) => {
  e.preventDefault();
  
  try {
    const response = await fetch('/api/auth/verify-otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        email: userEmail,
        otpCode: otpCode
      })
    });
    
    const result = await response.json();
    
    if (result.success) {
      // Redirect to login
      navigate('/login');
      showSuccessMessage('Email verified! You can now log in.');
    } else {
      // Show error with remaining attempts
      setError(result.message);
      // Update attempts from response if available
    }
  } catch (error) {
    setError('Verification failed. Please try again.');
  }
};

const handleResendOtp = async () => {
  try {
    const response = await fetch('/api/auth/resend-otp', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: userEmail })
    });
    
    const result = await response.json();
    
    if (result.success) {
      setTimeLeft(300); // Reset timer
      showSuccessMessage('OTP resent to your email');
    } else {
      setError(result.message);
    }
  } catch (error) {
    setError('Failed to resend OTP');
  }
};
```

## UI Components

### Registration Form Component
```jsx
function RegistrationForm({ onSuccess }) {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    password: '',
    confirmPassword: '',
    phoneNumber: '',
    role: 'Buyer'
  });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);

  const validateForm = () => {
    const newErrors = {};
    
    if (!formData.name.trim()) newErrors.name = 'Name is required';
    if (!formData.email.trim()) newErrors.email = 'Email is required';
    if (!formData.password) newErrors.password = 'Password is required';
    if (formData.password.length < 8) newErrors.password = 'Password must be at least 8 characters';
    if (formData.password !== formData.confirmPassword) newErrors.confirmPassword = 'Passwords do not match';
    
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    
    if (!validateForm()) return;
    
    setLoading(true);
    try {
      const response = await fetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(formData)
      });
      
      const result = await response.json();
      
      if (result.success) {
        onSuccess(formData.email);
      } else {
        setErrors({ submit: result.message });
      }
    } catch (error) {
      setErrors({ submit: 'Registration failed. Please try again.' });
    } finally {
      setLoading(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="registration-form">
      <div className="form-group">
        <label>Name</label>
        <input
          type="text"
          value={formData.name}
          onChange={(e) => setFormData({...formData, name: e.target.value})}
          className={errors.name ? 'error' : ''}
        />
        {errors.name && <span className="error-message">{errors.name}</span>}
      </div>

      <div className="form-group">
        <label>Email</label>
        <input
          type="email"
          value={formData.email}
          onChange={(e) => setFormData({...formData, email: e.target.value})}
          className={errors.email ? 'error' : ''}
        />
        {errors.email && <span className="error-message">{errors.email}</span>}
      </div>

      <div className="form-group">
        <label>Password</label>
        <input
          type="password"
          value={formData.password}
          onChange={(e) => setFormData({...formData, password: e.target.value})}
          className={errors.password ? 'error' : ''}
        />
        {errors.password && <span className="error-message">{errors.password}</span>}
      </div>

      <div className="form-group">
        <label>Confirm Password</label>
        <input
          type="password"
          value={formData.confirmPassword}
          onChange={(e) => setFormData({...formData, confirmPassword: e.target.value})}
          className={errors.confirmPassword ? 'error' : ''}
        />
        {errors.confirmPassword && <span className="error-message">{errors.confirmPassword}</span>}
      </div>

      <div className="form-group">
        <label>Phone Number (Optional)</label>
        <input
          type="tel"
          value={formData.phoneNumber}
          onChange={(e) => setFormData({...formData, phoneNumber: e.target.value})}
        />
      </div>

      <div className="form-group">
        <label>Role</label>
        <select
          value={formData.role}
          onChange={(e) => setFormData({...formData, role: e.target.value})}
        >
          <option value="Buyer">Buyer</option>
          <option value="Seller">Seller</option>
        </select>
      </div>

      {errors.submit && <div className="error-message">{errors.submit}</div>}

      <button type="submit" disabled={loading}>
        {loading ? 'Registering...' : 'Register'}
      </button>
    </form>
  );
}
```

### OTP Verification Component
```jsx
function OtpVerification({ email, onSuccess }) {
  const [otp, setOtp] = useState('');
  const [attempts, setAttempts] = useState(5);
  const [timeLeft, setTimeLeft] = useState(300);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [canResend, setCanResend] = useState(false);

  // Timer effect
  useEffect(() => {
    if (timeLeft <= 0) {
      setError('OTP has expired. Please request a new one.');
      setCanResend(true);
      return;
    }

    const timer = setTimeout(() => setTimeLeft(timeLeft - 1), 1000);
    return () => clearTimeout(timer);
  }, [timeLeft]);

  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  };

  const handleVerify = async (e) => {
    e.preventDefault();
    
    if (otp.length !== 6) {
      setError('OTP must be 6 digits');
      return;
    }

    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/auth/verify-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          email: email,
          otpCode: otp
        })
      });

      const result = await response.json();

      if (result.success) {
        onSuccess();
      } else {
        setError(result.message);
        // Update attempts if available in response
        if (result.data?.remainingAttempts !== undefined) {
          setAttempts(result.data.remainingAttempts);
        }
      }
    } catch (error) {
      setError('Verification failed. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleResend = async () => {
    setLoading(true);
    setError('');

    try {
      const response = await fetch('/api/auth/resend-otp', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email: email })
      });

      const result = await response.json();

      if (result.success) {
        setOtp('');
        setTimeLeft(300);
        setAttempts(5);
        setCanResend(false);
      } else {
        setError(result.message);
      }
    } catch (error) {
      setError('Failed to resend OTP');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="otp-verification">
      <h2>Verify Your Email</h2>
      <p>We've sent a 6-digit code to {email}</p>

      <form onSubmit={handleVerify}>
        <div className="form-group">
          <label>Enter OTP Code</label>
          <input
            type="text"
            maxLength="6"
            value={otp}
            onChange={(e) => setOtp(e.target.value.replace(/\D/g, ''))}
            placeholder="000000"
            className="otp-input"
            disabled={loading}
          />
        </div>

        <div className="timer">
          Time remaining: <strong>{formatTime(timeLeft)}</strong>
        </div>

        {error && <div className="error-message">{error}</div>}

        <div className="attempts-info">
          Attempts remaining: <strong>{attempts}</strong>
        </div>

        <button type="submit" disabled={loading || otp.length !== 6}>
          {loading ? 'Verifying...' : 'Verify OTP'}
        </button>
      </form>

      <div className="resend-section">
        <p>Didn't receive the code?</p>
        <button
          onClick={handleResend}
          disabled={!canResend && timeLeft > 0}
          className="resend-button"
        >
          Resend OTP
        </button>
      </div>
    </div>
  );
}
```

### Complete Registration Flow Component
```jsx
function RegistrationFlow() {
  const [step, setStep] = useState('register'); // 'register' or 'verify'
  const [email, setEmail] = useState('');
  const navigate = useNavigate();

  const handleRegistrationSuccess = (userEmail) => {
    setEmail(userEmail);
    setStep('verify');
  };

  const handleVerificationSuccess = () => {
    navigate('/login');
  };

  return (
    <div className="registration-flow">
      {step === 'register' ? (
        <RegistrationForm onSuccess={handleRegistrationSuccess} />
      ) : (
        <OtpVerification 
          email={email} 
          onSuccess={handleVerificationSuccess}
        />
      )}
    </div>
  );
}
```

## CSS Styling Example

```css
.registration-form {
  max-width: 400px;
  margin: 0 auto;
  padding: 20px;
}

.form-group {
  margin-bottom: 20px;
}

.form-group label {
  display: block;
  margin-bottom: 8px;
  font-weight: 500;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 14px;
}

.form-group input.error {
  border-color: #dc3545;
  background-color: #fff5f5;
}

.error-message {
  color: #dc3545;
  font-size: 12px;
  margin-top: 4px;
  display: block;
}

.otp-verification {
  max-width: 400px;
  margin: 0 auto;
  padding: 20px;
  text-align: center;
}

.otp-input {
  font-size: 24px;
  letter-spacing: 10px;
  text-align: center;
  font-weight: bold;
}

.timer {
  margin: 20px 0;
  font-size: 16px;
}

.attempts-info {
  margin: 15px 0;
  font-size: 14px;
  color: #666;
}

.resend-section {
  margin-top: 30px;
  padding-top: 20px;
  border-top: 1px solid #ddd;
}

.resend-button {
  background-color: #007bff;
  color: white;
  padding: 10px 20px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  margin-top: 10px;
}

.resend-button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}

button[type="submit"] {
  width: 100%;
  padding: 12px;
  background-color: #28a745;
  color: white;
  border: none;
  border-radius: 4px;
  font-size: 16px;
  cursor: pointer;
  margin-top: 20px;
}

button[type="submit"]:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}
```

## State Management (Redux/Zustand Example)

```javascript
// Redux Slice Example
const authSlice = createSlice({
  name: 'auth',
  initialState: {
    user: null,
    email: null,
    isEmailVerified: false,
    otpAttempts: 5,
    otpTimeLeft: 300,
    loading: false,
    error: null
  },
  reducers: {
    setEmail: (state, action) => {
      state.email = action.payload;
    },
    setOtpAttempts: (state, action) => {
      state.otpAttempts = action.payload;
    },
    setOtpTimeLeft: (state, action) => {
      state.otpTimeLeft = action.payload;
    },
    setLoading: (state, action) => {
      state.loading = action.payload;
    },
    setError: (state, action) => {
      state.error = action.payload;
    },
    verificationSuccess: (state) => {
      state.isEmailVerified = true;
      state.error = null;
    }
  }
});
```

## Error Handling

```javascript
const handleApiError = (error) => {
  if (error.response?.status === 400) {
    return error.response.data.message;
  } else if (error.response?.status === 500) {
    return 'Server error. Please try again later.';
  } else {
    return 'An error occurred. Please try again.';
  }
};
```

## Best Practices

1. **Input Validation**: Validate OTP format (6 digits only)
2. **Timer Management**: Show countdown timer for OTP expiry
3. **Attempt Tracking**: Display remaining attempts
4. **Error Messages**: Show clear, user-friendly error messages
5. **Loading States**: Disable buttons during API calls
6. **Accessibility**: Use proper labels and ARIA attributes
7. **Responsive Design**: Mobile-friendly OTP input
8. **Copy-Paste Support**: Allow pasting OTP from email

## Testing

```javascript
// Jest/React Testing Library Example
describe('OTP Verification', () => {
  it('should verify OTP successfully', async () => {
    const { getByText, getByPlaceholderText } = render(
      <OtpVerification email="test@example.com" onSuccess={jest.fn()} />
    );
    
    const input = getByPlaceholderText('000000');
    fireEvent.change(input, { target: { value: '123456' } });
    
    const button = getByText('Verify OTP');
    fireEvent.click(button);
    
    await waitFor(() => {
      expect(getByText('Email verified successfully')).toBeInTheDocument();
    });
  });

  it('should show error for invalid OTP', async () => {
    const { getByText, getByPlaceholderText } = render(
      <OtpVerification email="test@example.com" onSuccess={jest.fn()} />
    );
    
    const input = getByPlaceholderText('000000');
    fireEvent.change(input, { target: { value: '000000' } });
    
    const button = getByText('Verify OTP');
    fireEvent.click(button);
    
    await waitFor(() => {
      expect(getByText(/Invalid OTP/)).toBeInTheDocument();
    });
  });
});
```

## Deployment Considerations

1. **API Base URL**: Use environment variables for API endpoints
2. **HTTPS**: Always use HTTPS in production
3. **CORS**: Configure CORS properly on backend
4. **Rate Limiting**: Implement frontend rate limiting
5. **Security Headers**: Implement CSP and other security headers

## Support

For backend implementation details, see `OTP_IMPLEMENTATION_GUIDE.md` and `OTP_SETUP_GUIDE.md`
