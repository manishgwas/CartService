using AuthenticationDemo.DTOs;
using AuthenticationDemo.Helpers;
using AuthenticationDemo.Models;
using AuthenticationDemo.Repositories;

namespace AuthenticationDemo.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRefreshTokenRepository _refreshTokenRepo;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _config;

        public AuthService(IUserRepository userRepo, IRefreshTokenRepository refreshTokenRepo, JwtHelper jwtHelper, IConfiguration config)
        {
            _userRepo = userRepo;
            _refreshTokenRepo = refreshTokenRepo;
            _jwtHelper = jwtHelper;
            _config = config;
        }

        public async Task<AuthResponseDto> SignupAsync(SignupRequestDto dto)
        {
            var existing = await _userRepo.GetByEmailAsync(dto.Email);
            if (existing != null)
                throw new Exception("Email already registered");

            var user = new User
            {
                Email = dto.Email,
                PasswordHash = PasswordHasher.HashPassword(dto.Password),
                Role = dto.Role
            };
            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
        {
            var user = await _userRepo.GetByEmailAsync(dto.Email);
            if (user == null || !PasswordHasher.VerifyPassword(dto.Password, user.PasswordHash))
                throw new Exception("Invalid credentials");
            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto)
        {
            var payload = await GoogleOAuthHelper.ValidateIdTokenAsync(dto.IdToken, _config["GoogleOAuth:ClientId"]);
            if (payload == null || !payload.EmailVerified)
                throw new Exception("Invalid Google token");
            var user = await _userRepo.GetByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new User
                {
                    Email = payload.Email,
                    GoogleId = payload.Subject,
                    Role = "User"
                };
                await _userRepo.AddAsync(user);
            }
            else
            {
                user.GoogleId = payload.Subject;
                _userRepo.Update(user);
            }
            await _userRepo.SaveChangesAsync();
            return await GenerateAuthResponse(user);
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto)
        {
            var refreshToken = await _refreshTokenRepo.GetByTokenAsync(dto.RefreshToken);
            if (refreshToken == null || refreshToken.Revoked != null || refreshToken.Expires < DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token");
            var user = refreshToken.User;
            // Revoke old token
            _refreshTokenRepo.Revoke(refreshToken);
            await _refreshTokenRepo.SaveChangesAsync();
            return await GenerateAuthResponse(user);
        }

        public async Task LogoutAsync(LogoutRequestDto dto)
        {
            var refreshToken = await _refreshTokenRepo.GetByTokenAsync(dto.RefreshToken);
            if (refreshToken != null && refreshToken.Revoked == null)
            {
                _refreshTokenRepo.Revoke(refreshToken);
                await _refreshTokenRepo.SaveChangesAsync();
            }
        }

        public string GetGoogleClientId()
        {
            return _config["GoogleOAuth:ClientId"] ?? throw new Exception("Google Client ID not configured");
        }

        public async Task<AuthResponseDto> ExchangeGoogleCodeForTokensAsync(string code, string redirectUri)
        {
            var clientId = _config["GoogleOAuth:ClientId"] ?? throw new Exception("Google Client ID not configured");
            var clientSecret = _config["GoogleOAuth:ClientSecret"] ?? throw new Exception("Google Client Secret not configured");

            // Exchange authorization code for tokens
            var tokenResponse = await GoogleOAuthHelper.ExchangeCodeForTokensAsync(code, clientId, clientSecret, redirectUri);

            // Get user info from Google
            var userInfo = await GoogleOAuthHelper.GetUserInfoAsync(tokenResponse.access_token);

            if (!userInfo.verified_email)
            {
                throw new Exception("Email not verified with Google");
            }

            // Get or create user
            var user = await _userRepo.GetByEmailAsync(userInfo.email);
            if (user == null)
            {
                user = new User
                {
                    Email = userInfo.email,
                    GoogleId = userInfo.id,
                    Role = "User" // Default role for Google users
                };
                await _userRepo.AddAsync(user);
            }
            else if (string.IsNullOrEmpty(user.GoogleId))
            {
                // Link existing user to Google account
                user.GoogleId = userInfo.id;
                _userRepo.Update(user);
            }

            await _userRepo.SaveChangesAsync();

            // Generate our JWT token and refresh token
            var authResponse = await GenerateAuthResponse(user);
            
            // Include Google's access token in the response
            authResponse.GoogleAccessToken = tokenResponse.access_token;

            return authResponse;
        }

        private async Task<AuthResponseDto> GenerateAuthResponse(User user)
        {
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshTokenStr = RefreshTokenHelper.GenerateRefreshToken();
            var refreshToken = new RefreshToken
            {
                Token = refreshTokenStr,
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpirationDays"])),
                Created = DateTime.UtcNow,
                UserId = user.Id
            };
            await _refreshTokenRepo.AddAsync(refreshToken);
            await _refreshTokenRepo.SaveChangesAsync();
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenStr,
                ExpiresIn = int.Parse(_config["Jwt:AccessTokenExpirationMinutes"]) * 60,
                User = new UserDto { Id = user.Id, Email = user.Email, Role = user.Role }
            };
        }
    }
} 