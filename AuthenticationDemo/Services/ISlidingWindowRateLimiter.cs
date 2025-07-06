namespace AuthenticationDemo.Services
{
    public interface ISlidingWindowRateLimiter
    {
        Task<bool> IsAllowedAsync(string identifier, int maxRequests, int timeWindowSeconds);
        Task<int> GetRemainingRequestsAsync(string identifier, int maxRequests, int timeWindowSeconds);
        Task<int> GetResetTimeAsync(string identifier, int timeWindowSeconds);
    }
} 