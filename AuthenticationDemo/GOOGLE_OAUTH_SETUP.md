# Google OAuth Setup Guide

This guide explains how to set up and use the complete Google OAuth 2.0 flow in your authentication system.

## 🔧 Setup Instructions

### 1. Google Cloud Console Configuration

1. **Go to Google Cloud Console**

   - Visit [https://console.cloud.google.com/](https://console.cloud.google.com/)
   - Create a new project or select an existing one

2. **Enable Google+ API**

   - Go to "APIs & Services" > "Library"
   - Search for "Google+ API" and enable it
   - Also enable "Google Identity" if available

3. **Create OAuth 2.0 Credentials**

   - Go to "APIs & Services" > "Credentials"
   - Click "Create Credentials" > "OAuth 2.0 Client IDs"
   - Choose "Web application" as the application type

4. **Configure OAuth Consent Screen**

   - Go to "APIs & Services" > "OAuth consent screen"
   - Choose "External" user type
   - Fill in the required information:
     - App name: "Authentication Demo"
     - User support email: Your email
     - Developer contact information: Your email
   - Add scopes: `openid`, `email`, `profile`
   - Add test users if needed

5. **Configure Authorized Redirect URIs**

   - In your OAuth 2.0 client settings, add these redirect URIs:
     - `https://localhost:7001/api/auth/google/callback` (for development)
     - `https://yourdomain.com/api/auth/google/callback` (for production)
   - Add your domain to "Authorized JavaScript origins":
     - `https://localhost:7001` (for development)
     - `https://yourdomain.com` (for production)

6. **Copy Credentials**
   - Copy the Client ID and Client Secret
   - Update your `appsettings.json`:

```json
{
  "GoogleOAuth": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  }
}
```

## 🔄 OAuth Flow Overview

The complete OAuth 2.0 flow works as follows:

### Step 1: Redirect User to Google

```
GET /api/auth/google/redirect
```

- User clicks "Start Google OAuth Flow"
- Backend constructs Google OAuth URL with:
  - Client ID
  - Redirect URI
  - Scopes: `openid email profile`
  - Response type: `code`
  - Access type: `offline` (for refresh tokens)
  - Prompt: `consent` (always show consent screen)

### Step 2: Google Authorization

- User is redirected to Google's consent screen
- User grants permissions
- Google redirects back to your callback URL with an authorization code

### Step 3: Handle Callback

```
GET /api/auth/google/callback?code=AUTHORIZATION_CODE
```

- Backend receives the authorization code
- Exchanges code for access tokens
- Gets user information from Google
- Creates or links user account
- Generates JWT token and refresh token

### Step 4: Return Tokens

- User receives JWT token and refresh token
- Can now access protected endpoints

## 🚀 Usage Examples

### 1. Start OAuth Flow (Browser)

```javascript
// Redirect user to start OAuth flow
window.location.href = "/api/auth/google/redirect";
```

### 2. Handle Callback (Automatic)

The callback is handled automatically by the backend. The user will be redirected back to your application with the tokens.

### 3. Use JWT Token

```javascript
// Use the JWT token for API calls
const response = await fetch("/api/auth/profile", {
  headers: {
    Authorization: `Bearer ${jwtToken}`,
  },
});
```

## 📱 Demo Application

The project includes a demo HTML page at `https://localhost:7001/index.html` that demonstrates:

1. **Google OAuth Flow**: Click "Start Google OAuth Flow" to begin
2. **Direct Token Validation**: Paste a Google ID token to convert to JWT
3. **Protected Endpoint Testing**: Test various protected endpoints

## 🔐 API Endpoints

### OAuth Flow Endpoints

- `GET /api/auth/google/redirect` - Start OAuth flow
- `GET /api/auth/google/callback` - Handle OAuth callback

### Token Management

- `POST /api/auth/google-to-jwt` - Convert Google ID token to JWT
- `POST /api/auth/refresh-token` - Refresh JWT token
- `POST /api/auth/logout` - Logout and revoke tokens

### Protected Endpoints

- `GET /api/auth/profile` - Get user profile (any authenticated user)
- `GET /api/auth/admin-only` - Admin only access
- `GET /api/auth/user-only` - User only access

## 🔒 Security Features

### OAuth Flow Security

- **State Parameter**: Can be added to prevent CSRF attacks
- **PKCE**: Can be implemented for additional security
- **Scope Validation**: Only requested scopes are granted
- **Token Validation**: All tokens are validated on every request

### User Account Management

- **Automatic User Creation**: New Google users are automatically created
- **Account Linking**: Existing accounts can be linked to Google accounts
- **Email Verification**: Only verified Google emails are accepted
- **Role Assignment**: Google users get "User" role by default

## 🛠️ Development Setup

### 1. Run the Application

```bash
dotnet run
```

### 2. Access the Demo

- Open `https://localhost:7001/index.html`
- Click "Start Google OAuth Flow"
- Complete the Google authorization
- Test protected endpoints

### 3. Test with Swagger

- Open `https://localhost:7001/swagger`
- Use the interactive API documentation
- Test OAuth endpoints manually

## 🔧 Configuration Options

### Google OAuth Settings

```json
{
  "GoogleOAuth": {
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret"
  }
}
```

### JWT Settings

```json
{
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "AuthenticationDemo",
    "Audience": "AuthenticationDemoAudience",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

## 🚨 Troubleshooting

### Common Issues

1. **"Invalid redirect_uri"**

   - Ensure the redirect URI in Google Console matches exactly
   - Check for trailing slashes or protocol mismatches

2. **"Client ID not configured"**

   - Verify your `appsettings.json` has the correct Google OAuth settings
   - Check that the configuration keys match exactly

3. **"Email not verified"**

   - Google accounts must have verified email addresses
   - Test with a verified Google account

4. **"Authorization code expired"**

   - Authorization codes expire quickly (usually 10 minutes)
   - Ensure the callback is handled promptly

5. **CORS Issues**
   - Add your domain to Google OAuth authorized origins
   - Configure CORS in your ASP.NET Core application if needed

### Debug Mode

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "AuthenticationDemo": "Debug"
    }
  }
}
```

## 📋 Best Practices

1. **Always use HTTPS** in production
2. **Store secrets securely** (use Azure Key Vault, AWS Secrets Manager, etc.)
3. **Implement state parameter** to prevent CSRF attacks
4. **Add PKCE** for additional security
5. **Handle token refresh** automatically
6. **Log authentication events** for security monitoring
7. **Implement rate limiting** on OAuth endpoints
8. **Use secure session management**

## 🔄 Flow Diagram

```
User → /api/auth/google/redirect → Google OAuth → /api/auth/google/callback → JWT Token
  ↓
Protected API Endpoints ← Authorization Header ← Client Application
```

This completes the full Google OAuth 2.0 implementation with proper security, error handling, and user management!
