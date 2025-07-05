namespace AuthenticationDemo.DTOs
{
    public class SignupRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; } = "User";
    }
} 