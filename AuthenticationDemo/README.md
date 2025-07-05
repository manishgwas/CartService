# Authentication Demo with Google OAuth

This project demonstrates a comprehensive authentication system that supports both JWT tokens and Google OAuth authentication.

## Features

- **JWT Authentication**: Traditional username/password authentication with JWT tokens
- **Google OAuth**: Google Sign-In integration with automatic user creation
- **Role-based Authorization**: Admin and User roles with different access levels
- **Hybrid Authentication**: APIs accept both JWT tokens and Google ID tokens
- **Automatic User Linking**: Links existing accounts to Google accounts

## Setup

### 1. Google OAuth Configuration

1. Go to the [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google+ API
4. Go to "Credentials" and create an OAuth 2.0 Client ID
5. Add your application's domain to the authorized origins
6. Copy the Client ID and Client Secret

### 2. Update Configuration

Update `appsettings.json` with your Google OAuth credentials:

```json
{
  "GoogleOAuth": {
    "ClientId": "YOUR_GOOGLE_CLIENT_ID",
    "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
  }
}
```

### 3. Database Setup

Run the following commands to set up the database:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## API Endpoints

### Authentication Endpoints

#### JWT Authentication

- `POST /api/auth/signup` - Register a new user
- `POST /api/auth/login` - Login with email/password
- `POST /api/auth/refresh-token` - Refresh JWT token
- `POST /api/auth/logout` - Logout and revoke refresh token

#### Google Authentication

- `POST /api/auth/google-login` - Login with Google ID token
- `POST /api/auth/google-to-jwt` - Convert Google token to JWT (recommended)

#### Role-based Endpoints

- `GET /api/auth/admin-only` - Admin only access
- `GET /api/auth/user-only` - User only access
- `GET /api/auth/profile` - Get user profile (any authenticated user)

### Other Protected Endpoints

- `GET /api/cart` - Get user's cart
- `POST /api/cart/add` - Add item to cart
- `POST /api/cart/remove` - Remove item from cart
- `POST /api/cart/update` - Update cart item
- `POST /api/order/checkout` - Place order

## Authentication Flow

### Option 1: Direct Google Token Usage

1. Get Google ID token from client-side Google Sign-In
2. Send token in Authorization header: `Bearer <google_id_token>`
3. API automatically validates and creates/links user account

### Option 2: Convert to JWT (Recommended)

1. Get Google ID token from client-side Google Sign-In
2. Call `POST /api/auth/google-to-jwt` with the Google ID token
3. Receive JWT token and refresh token
4. Use JWT token for subsequent requests: `Bearer <jwt_token>`

## Request Examples

### Google Login

```bash
curl -X POST "https://localhost:7001/api/auth/google-to-jwt" \
  -H "Content-Type: application/json" \
  -d '{
    "idToken": "google_id_token_here"
  }'
```

### Using JWT Token

```bash
curl -X GET "https://localhost:7001/api/auth/profile" \
  -H "Authorization: Bearer jwt_token_here"
```

### Using Google Token Directly

```bash
curl -X GET "https://localhost:7001/api/auth/profile" \
  -H "Authorization: Bearer google_id_token_here"
```

## Client-Side Integration

### JavaScript (Google Sign-In)

```javascript
// Load Google Sign-In
gapi.load("auth2", function () {
  gapi.auth2.init({
    client_id: "YOUR_GOOGLE_CLIENT_ID",
  });
});

// Sign in
gapi.auth2
  .getAuthInstance()
  .signIn()
  .then(function (googleUser) {
    const idToken = googleUser.getAuthResponse().id_token;

    // Option 1: Use Google token directly
    fetch("/api/auth/profile", {
      headers: {
        Authorization: `Bearer ${idToken}`,
      },
    });

    // Option 2: Convert to JWT (recommended)
    fetch("/api/auth/google-to-jwt", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({ idToken: idToken }),
    })
      .then((response) => response.json())
      .then((data) => {
        // Store JWT token
        localStorage.setItem("jwt_token", data.jwtToken);
      });
  });
```

## Security Features

- **Token Validation**: All tokens are validated on every request
- **Automatic User Creation**: New Google users are automatically created
- **Account Linking**: Existing accounts can be linked to Google accounts
- **Role-based Access**: Different endpoints require different user roles
- **Token Expiration**: JWT tokens expire after 15 minutes
- **Refresh Tokens**: Long-lived refresh tokens for seamless authentication

## Error Handling

The API returns appropriate HTTP status codes and error messages:

- `400 Bad Request`: Invalid request data or authentication failure
- `401 Unauthorized`: Missing or invalid authentication token
- `403 Forbidden`: Insufficient permissions for the requested resource

## Development

### Running the Application

```bash
dotnet run
```

### Testing with Swagger

1. Navigate to `https://localhost:7001/swagger`
2. Use the interactive API documentation
3. Test authentication flows with sample tokens

### Database Migrations

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
```

## Configuration Options

### JWT Settings

- `AccessTokenExpirationMinutes`: JWT token lifetime (default: 15)
- `RefreshTokenExpirationDays`: Refresh token lifetime (default: 7)

### Google OAuth Settings

- `ClientId`: Your Google OAuth client ID
- `ClientSecret`: Your Google OAuth client secret

## Troubleshooting

### Common Issues

1. **Invalid Google Token**: Ensure your Google Client ID matches the one in your configuration
2. **CORS Issues**: Add your frontend domain to Google OAuth authorized origins
3. **Database Errors**: Run database migrations to ensure schema is up to date
4. **Token Expiration**: Use refresh tokens to get new JWT tokens

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
