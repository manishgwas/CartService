namespace AuthenticationDemo.DTOs
{
    public class AuthResponseDto
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; } // seconds
        public UserDto User { get; set; }
        public string? GoogleAccessToken { get; set; } // Optional: Google's access token from OAuth flow
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
    }
} 