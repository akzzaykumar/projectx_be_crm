# Google Sign-In - Quick Start Guide

## 🚀 Quick Setup (5 Minutes)

### Step 1: Get Google Credentials (2 minutes)

1. Go to https://console.cloud.google.com/
2. Create/Select project → APIs & Services → Credentials
3. Create OAuth 2.0 Client ID (Web application)
4. Add origins: `http://localhost:5154`
5. Add redirect: `http://localhost:5154/signin-google`
6. Copy **Client ID** and **Client Secret**

### Step 2: Configure Application (1 minute)

Update `appsettings.Development.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "PASTE_YOUR_CLIENT_ID_HERE.apps.googleusercontent.com",
      "ClientSecret": "PASTE_YOUR_CLIENT_SECRET_HERE"
    }
  }
}
```

### Step 3: Apply Database Migration (1 minute)

```bash
dotnet ef database update --project src/ActivoosCRM.Infrastructure --startup-project src/ActivoosCRM.API
```

### Step 4: Test (1 minute)

```bash
# Start the application
dotnet run --project src/ActivoosCRM.API

# Navigate to http://localhost:5154 (Swagger)
# Test /api/auth/google-login endpoint
```

## 📱 Frontend Integration

### HTML/JavaScript (Copy & Paste)

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://accounts.google.com/gsi/client" async defer></script>
</head>
<body>
    <div id="g_id_onload"
         data-client_id="YOUR_CLIENT_ID.apps.googleusercontent.com"
         data-callback="handleLogin">
    </div>
    <div class="g_id_signin"></div>

    <script>
        async function handleLogin(response) {
            const result = await fetch('http://localhost:5154/api/auth/google-login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    idToken: response.credential,
                    rememberMe: false
                })
            });
            
            const data = await result.json();
            if (data.success) {
                localStorage.setItem('accessToken', data.data.accessToken);
                alert('Login successful!');
            }
        }
    </script>
</body>
</html>
```

### React (Copy & Paste)

```bash
npm install @react-oauth/google
```

```jsx
import { GoogleOAuthProvider, GoogleLogin } from '@react-oauth/google';

function App() {
  const handleSuccess = async (credentialResponse) => {
    const response = await fetch('http://localhost:5154/api/auth/google-login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        idToken: credentialResponse.credential,
        rememberMe: false
      })
    });

    const data = await response.json();
    if (data.success) {
      localStorage.setItem('accessToken', data.data.accessToken);
    }
  };

  return (
    <GoogleOAuthProvider clientId="YOUR_CLIENT_ID">
      <GoogleLogin onSuccess={handleSuccess} />
    </GoogleOAuthProvider>
  );
}
```

## 🧪 Quick Test with cURL

```bash
# Get a test token from https://developers.google.com/oauthplayground/
# Then run:

curl -X POST http://localhost:5154/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "YOUR_GOOGLE_ID_TOKEN",
    "rememberMe": false
  }'
```

## ✅ Verification Checklist

- [ ] Google Cloud Console configured
- [ ] Client ID and Secret in appsettings
- [ ] Database migration applied
- [ ] Application starts without errors
- [ ] Swagger UI accessible at http://localhost:5154
- [ ] `/api/auth/google-login` endpoint visible in Swagger

## 🐛 Common Issues

| Issue | Solution |
|-------|----------|
| "Google authentication is not configured" | Check appsettings.json has correct Client ID/Secret |
| "Invalid Google authentication token" | Token expired (max 1 hour), get a new one |
| Database error | Run migration: `dotnet ef database update` |
| CORS error | Check CORS policy in Program.cs |

## 📖 Full Documentation

- **Complete Guide**: `docs/GOOGLE_SIGNIN_IMPLEMENTATION.md`
- **Summary**: `docs/GOOGLE_SIGNIN_SUMMARY.md`
- **API Docs**: http://localhost:5154 (when running)

## 🎯 Production Deployment

```bash
# Use environment variables instead of appsettings:
export Authentication__Google__ClientId="your-production-client-id"
export Authentication__Google__ClientSecret="your-production-secret"

# Add production URLs to Google Console:
# - https://yourdomain.com
# - https://yourdomain.com/signin-google
```

---

**Need Help?** Check the full documentation or contact the dev team!
