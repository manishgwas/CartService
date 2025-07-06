# Sliding Window Rate Limiter Implementation

This document describes the implementation of a sliding window rate limiter using Redis ZSET (Sorted Set) for the AuthenticationDemo project.

## Overview

The sliding window rate limiter provides smooth rate limiting by tracking requests within a rolling time window, unlike fixed window counters that can allow bursts at window boundaries.

## Architecture

### Core Components

1. **ISlidingWindowRateLimiter** - Interface defining rate limiting operations
2. **SlidingWindowRateLimiter** - Implementation using Redis ZSET
3. **SlidingWindowRateLimitAttribute** - Custom attribute for applying rate limiting to controllers
4. **Redis Cache** - Backend storage using StackExchange.Redis

### How It Works

1. **ZSET Structure**: Uses Redis ZSET with key pattern `rate_limit:{identifier}`

   - **Key**: `rate_limit:{username/ip/userid}`
   - **Score**: Current timestamp in seconds (Unix epoch)
   - **Value**: Unique request ID (GUID)

2. **Request Processing**:

   ```
   When a request arrives:
   1. Calculate cutoff time = current_time - time_window_seconds
   2. Remove all entries with score < cutoff_time (ZREMRANGEBYSCORE)
   3. Count remaining entries (ZCARD)
   4. If count < max_requests:
      - Add current request (ZADD)
      - Set expiration (EXPIRE)
      - Allow request
   5. Else:
      - Reject request (429 Too Many Requests)
   ```

3. **Cleanup**: Keys automatically expire after `time_window_seconds + 60` seconds

## Implementation Details

### Redis ZSET Operations

```csharp
// Remove old entries
await _database.SortedSetRemoveRangeByScoreAsync(key, 0, cutoffTime);

// Get current count
var currentCount = await _database.SortedSetLengthAsync(key);

// Add current request
await _database.SortedSetAddAsync(key, Guid.NewGuid().ToString(), currentTime);

// Set expiration
await _database.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60));
```

### Atomic Operations

Uses Redis transactions to ensure atomicity:

```csharp
var transaction = _database.CreateTransaction();
transaction.SortedSetRemoveRangeByScoreAsync(key, 0, cutoffTime);
var countTask = transaction.SortedSetLengthAsync(key);
var addTask = transaction.SortedSetAddAsync(key, Guid.NewGuid().ToString(), currentTime);
transaction.KeyExpireAsync(key, TimeSpan.FromSeconds(timeWindowSeconds + 60));
var results = await transaction.ExecuteAsync();
```

## Usage

### 1. Apply to Controller Actions

```csharp
[HttpPost("login")]
[SlidingWindowRateLimit(5, 60, "ip")] // 5 requests per minute per IP
public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
{
    // Your action logic
}
```

### 2. Identifier Types

- **"ip"**: Rate limit by IP address
- **"user"**: Rate limit by username
- **"userid"**: Rate limit by user ID
- **"email"**: Rate limit by email
- **"custom"**: Custom identifier (IP + UserID combination)

### 3. Configuration Examples

```csharp
// Authentication endpoints - 5 requests per minute per IP
[SlidingWindowRateLimit(5, 60, "ip")]

// Payment endpoints - 10 requests per minute per user
[SlidingWindowRateLimit(10, 60, "userid")]

// Webhook endpoints - 50 requests per minute per IP
[SlidingWindowRateLimit(50, 60, "ip")]

// Admin operations - 5 requests per minute per admin user
[SlidingWindowRateLimit(5, 60, "userid")]
```

## Response Headers

When rate limiting is applied, the following headers are added to responses:

- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Remaining requests in current window
- `X-RateLimit-Reset`: Timestamp when the window resets

## Rate Limit Exceeded Response

When a request is rate limited, returns HTTP 429 with:

```json
{
  "error": "Rate limit exceeded",
  "message": "Too many requests. Limit: 5 requests per 60 seconds.",
  "remainingRequests": 0,
  "resetTime": 1703123456,
  "retryAfter": 45
}
```

## Configuration

### Redis Connection

Add to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Redis": "localhost:6379"
  }
}
```

### Service Registration

In `Program.cs`:

```csharp
// Add Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "AuthenticationDemo_";
});

// Add Redis Connection Multiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    return ConnectionMultiplexer.Connect(configuration);
});

// Add Sliding Window Rate Limiter
builder.Services.AddScoped<ISlidingWindowRateLimiter, SlidingWindowRateLimiter>();
```

## Testing

### Test Page

Access the test page at: `http://localhost:5000/sliding-window-test.html`

Features:

- Test individual endpoints
- Bulk request testing
- Real-time statistics
- Response header analysis
- Rate limit visualization

### Manual Testing

```bash
# Test authentication rate limiting
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'

# Test payment rate limiting (requires JWT token)
curl -X GET http://localhost:5000/api/payment/user \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Advantages

1. **Smooth Limiting**: No burst allowance at window boundaries
2. **Accurate Tracking**: Precise request counting within time windows
3. **Automatic Cleanup**: Old entries automatically removed
4. **Scalable**: Redis handles high concurrency
5. **Flexible**: Multiple identifier types supported
6. **Atomic Operations**: Race condition prevention

## Performance Considerations

1. **Redis Operations**: Each request requires 2-3 Redis operations
2. **Memory Usage**: ZSET entries are automatically cleaned up
3. **Network Latency**: Redis connection adds minimal overhead
4. **Concurrency**: Redis handles concurrent requests efficiently

## Monitoring

### Redis Commands for Debugging

```bash
# View all rate limit keys
redis-cli KEYS "rate_limit:*"

# View entries for a specific user
redis-cli ZRANGE "rate_limit:user123" 0 -1 WITHSCORES

# Check key expiration
redis-cli TTL "rate_limit:user123"

# Monitor Redis operations
redis-cli MONITOR
```

### Metrics to Track

- Rate limit hits per endpoint
- Average response times
- Redis operation latency
- Memory usage of rate limit keys

## Fallback Strategy

If Redis is unavailable, the rate limiter gracefully degrades:

- Requests continue without rate limiting
- Logs warning messages
- No application crashes

## Security Considerations

1. **Identifier Validation**: Sanitize user identifiers
2. **Redis Security**: Use authentication and SSL for Redis
3. **Key Prefixing**: Prevents key collisions
4. **Expiration**: Automatic cleanup prevents memory leaks

## Comparison with Fixed Window

| Aspect         | Sliding Window  | Fixed Window                |
| -------------- | --------------- | --------------------------- |
| Burst Handling | Smooth          | Allows bursts at boundaries |
| Memory Usage   | Higher          | Lower                       |
| Accuracy       | High            | Medium                      |
| Implementation | Complex         | Simple                      |
| Performance    | Slightly slower | Faster                      |

## Best Practices

1. **Choose Appropriate Limits**: Balance security with usability
2. **Monitor Usage**: Track rate limit hits and adjust limits
3. **Use Different Limits**: Stricter for auth, relaxed for read operations
4. **Test Thoroughly**: Verify behavior under load
5. **Document Limits**: Make rate limits clear to API consumers

## Troubleshooting

### Common Issues

1. **Redis Connection Failed**

   - Check Redis server status
   - Verify connection string
   - Check network connectivity

2. **Rate Limiting Not Working**

   - Verify attribute is applied correctly
   - Check Redis operations in logs
   - Ensure service is registered

3. **High Memory Usage**
   - Check for expired keys not being cleaned up
   - Monitor ZSET sizes
   - Verify expiration settings

### Debug Commands

```bash
# Check Redis status
redis-cli PING

# Monitor rate limit operations
redis-cli MONITOR | grep rate_limit

# Check application logs
dotnet run --environment Development
```
