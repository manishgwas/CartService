using AuthenticationDemo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthenticationDemo.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SlidingWindowRateLimitAttribute : Attribute, IAsyncActionFilter
    {
        private readonly int _maxRequests;
        private readonly int _timeWindowSeconds;
        private readonly string _identifierType; // "ip", "user", "custom"

        public SlidingWindowRateLimitAttribute(int maxRequests, int timeWindowSeconds, string identifierType = "ip")
        {
            _maxRequests = maxRequests;
            _timeWindowSeconds = timeWindowSeconds;
            _identifierType = identifierType.ToLower();
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var rateLimiter = context.HttpContext.RequestServices.GetService<ISlidingWindowRateLimiter>();
            
            if (rateLimiter == null)
            {
                // If rate limiter is not available, continue without rate limiting
                await next();
                return;
            }

            var identifier = GetIdentifier(context);
            
            if (string.IsNullOrEmpty(identifier))
            {
                // If no identifier found, continue without rate limiting
                await next();
                return;
            }

            var isAllowed = await rateLimiter.IsAllowedAsync(identifier, _maxRequests, _timeWindowSeconds);
            
            if (!isAllowed)
            {
                var remainingRequests = await rateLimiter.GetRemainingRequestsAsync(identifier, _maxRequests, _timeWindowSeconds);
                var resetTime = await rateLimiter.GetResetTimeAsync(identifier, _timeWindowSeconds);
                
                context.Result = new ObjectResult(new
                {
                    error = "Rate limit exceeded",
                    message = $"Too many requests. Limit: {_maxRequests} requests per {_timeWindowSeconds} seconds.",
                    remainingRequests,
                    resetTime,
                    retryAfter = Math.Max(1, resetTime - (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                })
                {
                    StatusCode = 429 // Too Many Requests
                };
                return;
            }

            // Add rate limit headers to response
            context.HttpContext.Response.OnStarting(() =>
            {
                var remaining = rateLimiter.GetRemainingRequestsAsync(identifier, _maxRequests, _timeWindowSeconds).Result;
                var reset = rateLimiter.GetResetTimeAsync(identifier, _timeWindowSeconds).Result;
                
                context.HttpContext.Response.Headers["X-RateLimit-Limit"] = _maxRequests.ToString();
                context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.HttpContext.Response.Headers["X-RateLimit-Reset"] = reset.ToString();
                
                return Task.CompletedTask;
            });

            await next();
        }

        private string GetIdentifier(ActionExecutingContext context)
        {
            return _identifierType switch
            {
                "ip" => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                "user" => context.HttpContext.User?.Identity?.Name ?? 
                         context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                "userid" => context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
                "email" => context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "anonymous",
                "custom" => GetCustomIdentifier(context),
                _ => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            };
        }

        private string GetCustomIdentifier(ActionExecutingContext context)
        {
            // You can implement custom logic here
            // For example, combine IP and user ID
            var ip = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userId = context.HttpContext.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            
            return $"{ip}_{userId}";
        }
    }
} 