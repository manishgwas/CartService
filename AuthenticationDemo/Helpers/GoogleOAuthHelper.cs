using Google.Apis.Auth;
using System.Text;
using System.Text.Json;

namespace AuthenticationDemo.Helpers
{
    public static class GoogleOAuthHelper
    {
        public static async Task<GoogleJsonWebSignature.Payload> ValidateIdTokenAsync(string idToken, string clientId)
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };
            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }

        public static async Task<GoogleTokenResponse> ExchangeCodeForTokensAsync(string code, string clientId, string clientSecret, string redirectUri)
        {
            using var httpClient = new HttpClient();
            
            var tokenRequest = new
            {
                code,
                client_id = clientId,
                client_secret = clientSecret,
                redirect_uri = redirectUri,
                grant_type = "authorization_code"
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to exchange code for tokens: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent);
            
            if (tokenResponse == null)
            {
                throw new Exception("Failed to deserialize token response");
            }

            return tokenResponse;
        }

        public static async Task<GoogleUserInfo> GetUserInfoAsync(string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to get user info: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(responseContent);
            
            if (userInfo == null)
            {
                throw new Exception("Failed to deserialize user info");
            }

            return userInfo;
        }
    }

    public class GoogleTokenResponse
    {
        public string access_token { get; set; } = string.Empty;
        public string token_type { get; set; } = string.Empty;
        public int expires_in { get; set; }
        public string refresh_token { get; set; } = string.Empty;
        public string scope { get; set; } = string.Empty;
        public string id_token { get; set; } = string.Empty;
    }

    public class GoogleUserInfo
    {
        public string id { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public bool verified_email { get; set; }
        public string name { get; set; } = string.Empty;
        public string given_name { get; set; } = string.Empty;
        public string family_name { get; set; } = string.Empty;
        public string picture { get; set; } = string.Empty;
        public string locale { get; set; } = string.Empty;
    }
} 