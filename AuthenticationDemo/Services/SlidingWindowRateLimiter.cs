using StackExchange.Redis;

namespace AuthenticationDemo.Services
{
    public class SlidingWindowRateLimiter : ISlidingWindowRateLimiter
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;

        public SlidingWindowRateLimiter(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _database = redis.GetDatabase();
        }

        public async Task<bool> IsAllowedAsync(string identifier, int maxRequests, int timeWindowSeconds)
        {
            var key = $"rate_limit:{identifier}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cutoffTime = currentTime - timeWindowSeconds;

            // Use Redis transaction to ensure atomicity
            var transaction = _database.CreateTransaction();
            
            // 1. Remove entries older than the time window
            transaction.SortedSetRemoveRangeByScoreAsync(key, 0, cutoffTime);
            
            // 2. Get current count of requests in the window
            var countTask = transaction.SortedSetLengthAsync(key);
            
            // 3. Add current request if within limit
            var addTask = transaction.SortedSetAddAsync(key, Guid.NewGuid().ToString(), currentTime);
            
            // 4. Set expiration for the key (cleanup after time window)
            transaction.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60)); // Extra 60 seconds for cleanup
            
            // Execute transaction
            var results = await transaction.ExecuteAsync();
            
            if (!results)
            {
                // Transaction failed, fallback to non-atomic check
                return await FallbackCheckAsync(identifier, maxRequests, timeWindowSeconds);
            }

            var currentCount = await countTask;
            
            // If count is less than max requests, allow the request
            if (currentCount < maxRequests)
            {
                await addTask; // Add the current request
                return true;
            }

            return false;
        }

        public async Task<int> GetRemainingRequestsAsync(string identifier, int maxRequests, int timeWindowSeconds)
        {
            var key = $"rate_limit:{identifier}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cutoffTime = currentTime - timeWindowSeconds;

            // Remove old entries
            await _database.SortedSetRemoveRangeByScoreAsync(key, 0, cutoffTime);
            
            // Get current count
            var currentCount = await _database.SortedSetLengthAsync(key);
            
            // Refresh TTL if there are entries (key exists and has data)
            if (currentCount > 0)
            {
                await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60));
            }
            
            return Math.Max(0, maxRequests - (int)currentCount);
        }

        public async Task<int> GetResetTimeAsync(string identifier, int timeWindowSeconds)
        {
            var key = $"rate_limit:{identifier}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            // Get the oldest entry in the sorted set
            var oldestEntry = await _database.SortedSetRangeByScoreAsync(key, order: Order.Ascending, take: 1);
            
            if (oldestEntry.Length == 0)
            {
                // No entries, reset time is now
                return (int)currentTime;
            }

            // Get the score (timestamp) of the oldest entry
            var oldestScore = await _database.SortedSetScoreAsync(key, oldestEntry[0]);
            if (!oldestScore.HasValue)
            {
                return (int)currentTime;
            }

            // Refresh TTL since we're accessing the key
            await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60));

            // Reset time is oldest entry + time window
            return (int)(oldestScore.Value + timeWindowSeconds);
        }

        private async Task<bool> FallbackCheckAsync(string identifier, int maxRequests, int timeWindowSeconds)
        {
            var key = $"rate_limit:{identifier}";
            var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var cutoffTime = currentTime - timeWindowSeconds;

            // Remove old entries
            await _database.SortedSetRemoveRangeByScoreAsync(key, 0, cutoffTime);
            
            // Get current count
            var currentCount = await _database.SortedSetLengthAsync(key);
            
            if (currentCount < maxRequests)
            {
                // Add current request
                await _database.SortedSetAddAsync(key, Guid.NewGuid().ToString(), currentTime);
                await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60));
                return true;
            }

            return false;
        }
    }
} 