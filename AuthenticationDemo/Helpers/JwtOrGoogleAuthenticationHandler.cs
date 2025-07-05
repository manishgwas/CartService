using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthenticationDemo.Repositories;

namespace AuthenticationDemo.Helpers
{
    public class JwtOrGoogleAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public JwtOrGoogleAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                return AuthenticateResult.Fail("Authorization header not found.");
            }

            var authorizationHeader = Request.Headers["Authorization"].ToString();
            if (!authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail("Bearer token not found.");
            }

            var token = authorizationHeader.Substring("Bearer ".Length).Trim();

            // Try JWT authentication first
            var jwtResult = await TryJwtAuthentication(token);
            if (jwtResult.Succeeded)
            {
                return jwtResult;
            }

            // If JWT fails, try Google authentication
            var googleResult = await TryGoogleAuthentication(token);
            if (googleResult.Succeeded)
            {
                return googleResult;
            }

            // Both failed
            return AuthenticateResult.Fail("Invalid token.");
        }

        private async Task<AuthenticateResult> TryJwtAuthentication(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "");
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                
                var identity = new ClaimsIdentity(principal.Claims, Scheme.Name);
                var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
                
                return AuthenticateResult.Success(ticket);
            }
            catch
            {
                return AuthenticateResult.Fail("JWT validation failed.");
            }
        }

        private async Task<AuthenticateResult> TryGoogleAuthentication(string token)
        {
            try
            {
                // Validate the Google ID token
                var payload = await GoogleOAuthHelper.ValidateIdTokenAsync(
                    token, 
                    _configuration["GoogleOAuth:ClientId"] ?? "");

                if (payload == null || !payload.EmailVerified)
                {
                    return AuthenticateResult.Fail("Invalid Google token.");
                }

                // Get user repository from DI
                var userRepository = Context.RequestServices.GetRequiredService<IUserRepository>();

                // Get or create user
                var user = await userRepository.GetByEmailAsync(payload.Email);
                if (user == null)
                {
                    // Create new user from Google account
                    user = new Models.User
                    {
                        Email = payload.Email,
                        GoogleId = payload.Subject,
                        Role = "User" // Default role for Google users
                    };
                    await userRepository.AddAsync(user);
                    await userRepository.SaveChangesAsync();
                }
                else if (string.IsNullOrEmpty(user.GoogleId))
                {
                    // Link existing user to Google account
                    user.GoogleId = payload.Subject;
                    userRepository.Update(user);
                    await userRepository.SaveChangesAsync();
                }

                // Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("GoogleId", payload.Subject),
                    new Claim("AuthProvider", "Google")
                };

                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail($"Google authentication failed: {ex.Message}");
            }
        }
    }
} 