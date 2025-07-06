using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AuthenticationDemo.Services;
using AuthenticationDemo.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AuthenticationDemo.Attributes;

namespace AuthenticationDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("signup")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public async Task<IActionResult> Signup([FromBody] SignupRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.SignupAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.LoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("google-login")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.GoogleLoginAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        [SlidingWindowRateLimit(10, 60, "ip")] // 10 requests per minute per IP
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.RefreshTokenAsync(dto);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("logout")]
        [SlidingWindowRateLimit(10, 60, "ip")] // 10 requests per minute per IP
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                await _authService.LogoutAsync(dto);
                return Ok(new { message = "Logged out" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("google-to-jwt")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public async Task<IActionResult> ConvertGoogleToJwt([FromBody] GoogleLoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var result = await _authService.GoogleLoginAsync(dto);
                return Ok(new { 
                    message = "Google authentication successful. Use the JWT token for subsequent requests.",
                    jwtToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    user = result.User
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [Authorize(Policy = "RequireAdminRole")]
        [HttpGet("admin-only")]
        public IActionResult AdminOnly()
        {
            var authProvider = User.FindFirst("AuthProvider")?.Value ?? "JWT";
            return Ok(new { 
                message = "Hello, Admin! You have access to this endpoint.",
                authProvider = authProvider,
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value
            });
        }

        [Authorize(Policy = "RequireUserRole")]
        [HttpGet("user-only")]
        public IActionResult UserOnly()
        {
            var authProvider = User.FindFirst("AuthProvider")?.Value ?? "JWT";
            return Ok(new { 
                message = "Hello, User! You have access to this endpoint.",
                authProvider = authProvider,
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value
            });
        }

        [Authorize(Policy = "RequireAuthenticatedUser")]
        [HttpGet("profile")]
        public IActionResult GetProfile()
        {
            var authProvider = User.FindFirst("AuthProvider")?.Value ?? "JWT";
            var googleId = User.FindFirst("GoogleId")?.Value;
            
            return Ok(new
            {
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value,
                role = User.FindFirst(ClaimTypes.Role)?.Value,
                authProvider = authProvider,
                googleId = googleId,
                message = $"Profile accessed via {authProvider} authentication"
            });
        }

        [HttpGet("google/redirect")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public IActionResult GoogleRedirect()
        {
            var clientId = _authService.GetGoogleClientId();
            var redirectUri = Url.Action("GoogleCallback", "Auth", null, Request.Scheme, Request.Host.Value);
            var scope = "openid email profile";
            var responseType = "code";
            
            var googleAuthUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                              $"client_id={clientId}&" +
                              $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                              $"scope={Uri.EscapeDataString(scope)}&" +
                              $"response_type={responseType}&" +
                              $"access_type=offline&" +
                              $"prompt=consent";
            
            return Redirect(googleAuthUrl);
        }

        [HttpGet("google/callback")]
        [SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
        public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string error)
        {
            if (!string.IsNullOrEmpty(error))
            {
                return BadRequest(new { error = $"Google OAuth error: {error}" });
            }

            if (string.IsNullOrEmpty(code))
            {
                return BadRequest(new { error = "Authorization code is missing" });
            }

            try
            {
                var redirectUri = Url.Action("GoogleCallback", "Auth", null, Request.Scheme, Request.Host.Value);
                var result = await _authService.ExchangeGoogleCodeForTokensAsync(code, redirectUri);
                
                // Return the tokens and user info
                return Ok(new { 
                    message = "Google authentication successful",
                    jwtToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    user = result.User,
                    googleAccessToken = result.GoogleAccessToken // Optional: if you need it
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
} 