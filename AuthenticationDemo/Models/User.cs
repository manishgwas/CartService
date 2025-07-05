using System.Collections.Generic;

namespace AuthenticationDemo.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Role { get; set; } // e.g., "User", "Admin"
        public string GoogleId { get; set; } // For Google OAuth
        public ICollection<RefreshToken> RefreshTokens { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
} 