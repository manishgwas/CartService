using System;
using System.Security.Cryptography;

namespace AuthenticationDemo.Helpers
{
    public static class RefreshTokenHelper
    {
        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }
    }
} 