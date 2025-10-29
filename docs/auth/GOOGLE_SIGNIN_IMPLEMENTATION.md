# Google Sign-In Authentication Implementation

## Overview

This document describes the implementation of Google Sign-In authentication in the ActivoosCRM application. The implementation allows users to authenticate using their Google accounts, providing a seamless and secure login experience.

## Features

- ✅ Google OAuth 2.0 Integration
- ✅ Automatic user account creation on first Google login
- ✅ Secure ID token validation
- ✅ JWT token generation for authenticated sessions
- ✅ Support for existing users with Google accounts
- ✅ Email pre-verification for Google users
- ✅ Comprehensive error handling and logging

## Architecture

### Components

1. **API Controller**: `AuthController` - Exposes the `/api/auth/google-login` endpoint
2. **Command Handler**: `GoogleLoginCommandHandler` - Processes Google authentication
3. **Domain Entity**: `User` - Extended with Google authentication fields
4. **Database Migration**: `AddGoogleAuthToUser` - Adds required database columns

### Database Schema Changes

The `User` entity has been extended with the following fields:

```csharp
public string? GoogleId { get; private set; }
public string? ExternalAuthProvider { get; private set; }
public bool IsExternalAuth { get; private set; } = false;
```

## Setup Guide

### 1. Google Cloud Console Configuration

#### Create OAuth 2.0 Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Navigate to **APIs & Services** > **Credentials**
4. Click **Create Credentials** > **OAuth client ID**
5. Select **Web application** as the application type
6. Configure the OAuth consent screen if prompted
7. Add **Authorized JavaScript origins**:
   ```
   http://localhost:5154
   https://yourdomain.com
   ```
8. Add **Authorized redirect URIs**:
   ```
   http://localhost:5154/signin-google
   https://yourdomain.com/signin-google
   ```
9. Save and copy the **Client ID** and **Client Secret**

### 2. Application Configuration

#### Update appsettings.json

Add the Google authentication configuration:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

#### Update appsettings.Development.json

Add the same configuration for development:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id.apps.googleusercontent.com",
      "ClientSecret": "your-google-client-secret"
    }
  }
}
```

**Note**: Never commit actual credentials to source control. Use environment variables or Azure Key Vault in production.

### 3. Apply Database Migrations

Run the migration to add Google authentication fields:

```bash
dotnet ef database update --project src/ActivoosCRM.Infrastructure/ActivoosCRM.Infrastructure.csproj --startup-project src/ActivoosCRM.API/ActivoosCRM.API.csproj
```

## API Usage

### Endpoint

```
POST /api/auth/google-login
```

### Request

```json
{
  "idToken": "google-id-token-from-client",
  "rememberMe": false
}
```

### Response (Success - 200 OK)

```json
{
  "success": true,
  "message": "Login successful",
  "data": {
    "accessToken": "jwt-access-token",
    "refreshToken": "refresh-token",
    "expiresIn": 3600,
    "userId": "user-guid",
    "email": "user@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "role": "Customer",
    "isNewUser": false
  },
  "errorCode": null
}
```

### Response (Error - 400 Bad Request)

```json
{
  "success": false,
  "message": "Invalid Google authentication token",
  "data": null,
  "errorCode": "GOOGLE_LOGIN_FAILED"
}
```

## Client-Side Integration

### HTML + JavaScript Example

```html
<!DOCTYPE html>
<html>
<head>
    <title>Google Sign-In</title>
    <script src="https://accounts.google.com/gsi/client" async defer></script>
</head>
<body>
    <div id="g_id_onload"
         data-client_id="YOUR_CLIENT_ID.apps.googleusercontent.com"
         data-callback="handleCredentialResponse">
    </div>
    <div class="g_id_signin" data-type="standard"></div>

    <script>
        function handleCredentialResponse(response) {
            // Send the ID token to your backend
            fetch('http://localhost:5154/api/auth/google-login', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    idToken: response.credential,
                    rememberMe: false
                }),
            })
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    // Store tokens
                    localStorage.setItem('accessToken', data.data.accessToken);
                    localStorage.setItem('refreshToken', data.data.refreshToken);
                    console.log('Login successful:', data);
                    // Redirect or update UI
                } else {
                    console.error('Login failed:', data.message);
                }
            })
            .catch(error => console.error('Error:', error));
        }
    </script>
</body>
</html>
```

### React Example

```jsx
import { GoogleLogin } from '@react-oauth/google';
import { GoogleOAuthProvider } from '@react-oauth/google';

function App() {
  const handleGoogleSuccess = async (credentialResponse) => {
    try {
      const response = await fetch('http://localhost:5154/api/auth/google-login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          idToken: credentialResponse.credential,
          rememberMe: false
        }),
      });

      const data = await response.json();
      
      if (data.success) {
        localStorage.setItem('accessToken', data.data.accessToken);
        localStorage.setItem('refreshToken', data.data.refreshToken);
        console.log('Login successful');
      }
    } catch (error) {
      console.error('Login error:', error);
    }
  };

  return (
    <GoogleOAuthProvider clientId="YOUR_CLIENT_ID.apps.googleusercontent.com">
      <GoogleLogin
        onSuccess={handleGoogleSuccess}
        onError={() => console.log('Login Failed')}
      />
    </GoogleOAuthProvider>
  );
}
```

## Security Considerations

### Token Validation

- The backend validates Google ID tokens using the official `Google.Apis.Auth` library
- Tokens are verified against your Client ID to prevent token replay attacks
- Expired or invalid tokens are rejected

### User Account Security

- Google-authenticated users have their email automatically verified
- Account locking and rate limiting apply to Google accounts
- Users created via Google cannot use password-based login unless they set a password separately

### Best Practices

1. **Always use HTTPS in production**
2. **Validate tokens on the server side** - Never trust client-side validation alone
3. **Use environment variables** for Client ID and Client Secret
4. **Implement proper CORS policies** to restrict API access
5. **Log authentication attempts** for security auditing
6. **Set appropriate token expiration times**

## Flow Diagram

```
┌─────────┐         ┌──────────────┐         ┌────────────┐
│         │         │              │         │            │
│ Client  │────────>│   Google     │────────>│  ActivoosCRM │
│         │  Login  │   OAuth      │  Token  │     API      │
│         │         │              │         │            │
└─────────┘         └──────────────┘         └────────────┘
     │                                              │
     │         ┌──────────────────────┐            │
     │         │ 1. Validate ID Token │            │
     │         │ 2. Extract User Info │            │
     │         │ 3. Create/Find User  │            │
     │         │ 4. Generate JWT      │            │
     │         │ 5. Return Tokens     │            │
     │         └──────────────────────┘            │
     │                                              │
     │<─────────────────────────────────────────────┘
     │              Access + Refresh Tokens
```

## Error Handling

### Common Error Scenarios

| Error | Status Code | Error Code | Description |
|-------|------------|------------|-------------|
| Invalid Google token | 400 | GOOGLE_LOGIN_FAILED | The provided ID token is invalid or expired |
| Google auth not configured | 400 | GOOGLE_LOGIN_FAILED | Server configuration is missing |
| Account inactive | 401 | ACCOUNT_LOCKED_OR_INACTIVE | User account has been deactivated |
| Account locked | 401 | ACCOUNT_LOCKED_OR_INACTIVE | Account is temporarily locked |
| Server error | 400 | GOOGLE_LOGIN_FAILED | Internal server error during authentication |

## Testing

### Manual Testing with Swagger

1. Start the application
2. Navigate to `http://localhost:5154` (Swagger UI)
3. Find the `/api/auth/google-login` endpoint
4. Obtain a Google ID token from a test client
5. Send the request with the ID token
6. Verify the response contains valid JWT tokens

### Postman Testing

```bash
POST http://localhost:5154/api/auth/google-login
Content-Type: application/json

{
  "idToken": "your-google-id-token",
  "rememberMe": false
}
```

## Troubleshooting

### "Google authentication is not configured"

- Verify `Authentication:Google:ClientId` is set in appsettings.json
- Verify `Authentication:Google:ClientSecret` is set in appsettings.json

### "Invalid Google authentication token"

- Ensure the ID token is current and not expired (tokens expire after 1 hour)
- Verify the token was issued for your Client ID
- Check that the Google Sign-In client library is correctly configured

### "Authorized redirect URI mismatch"

- Verify the redirect URI in Google Cloud Console matches your application URL
- Include both `http://localhost:5154/signin-google` and production URLs

## Future Enhancements

- [ ] Link existing email/password accounts with Google
- [ ] Support for multiple external providers (Facebook, Microsoft, Apple)
- [ ] Account unlinking functionality
- [ ] Admin dashboard for managing external auth providers
- [ ] Enhanced profile information sync from Google

## Dependencies

- `Microsoft.AspNetCore.Authentication.Google` (v9.0.10)
- `Google.Apis.Auth` (v1.72.0)
- `Microsoft.Extensions.Configuration.Abstractions` (v9.0.10)

## References

- [Google Identity Documentation](https://developers.google.com/identity/gsi/web/guides/overview)
- [Google Sign-In JavaScript Library](https://developers.google.com/identity/gsi/web/reference/js-reference)
- [ASP.NET Core Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/)
- [JWT Bearer Authentication](https://jwt.io/)

## Support

For issues or questions regarding Google Sign-In implementation:
- Check the application logs at `logs/log-YYYYMMDD.txt`
- Review the API documentation at `/swagger`
- Contact the development team

---

**Last Updated**: October 26, 2025
**Author**: Development Team
**Version**: 1.0.0
