using System.Threading.Tasks;
using AuthenticationDemo.DTOs;

namespace AuthenticationDemo.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignupAsync(SignupRequestDto dto);
        Task<AuthResponseDto> LoginAsync(LoginRequestDto dto);
        Task<AuthResponseDto> GoogleLoginAsync(GoogleLoginRequestDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto dto);
        Task LogoutAsync(LogoutRequestDto dto);
        string GetGoogleClientId();
        Task<AuthResponseDto> ExchangeGoogleCodeForTokensAsync(string code, string redirectUri);
    }
} 