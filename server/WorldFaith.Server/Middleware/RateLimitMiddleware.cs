using System.Collections.Concurrent;
using System.Net;

namespace WorldFaith.Server.Middleware;

/// <summary>
/// Sliding window rate limiter per IP.
/// Config: max requests per window, window duration.
/// Bypass: SignalR endpoints and health check.
/// </summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitMiddleware> _logger;
    private readonly RateLimitOptions _options;

    // IP → list of request timestamps
    private static readonly ConcurrentDictionary<string, Queue<DateTime>> _windows = new();

    public RateLimitMiddleware(RequestDelegate next, ILogger<RateLimitMiddleware> logger, RateLimitOptions options)
    {
        _next = next;
        _logger = logger;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";

        // No rate-limit: SignalR, health, static files
        if (path.StartsWith("/hubs/") || path == "/health" || path.StartsWith("/swagger"))
        {
            await _next(context);
            return;
        }

        var ip = GetClientIp(context);
        var now = DateTime.UtcNow;

        // Auth endpoints have stricter limits
        var (maxReq, windowSec) = path.StartsWith("/api/auth/")
            ? (_options.AuthMaxRequests, _options.AuthWindowSeconds)
            : (_options.ApiMaxRequests, _options.ApiWindowSeconds);

        var key = $"{ip}:{path.Split('/').Take(3).Aggregate((a, b) => $"{a}/{b}")}";
        var window = _windows.GetOrAdd(key, _ => new Queue<DateTime>());

        lock (window)
        {
            var cutoff = now.AddSeconds(-windowSec);
            while (window.Count > 0 && window.Peek() < cutoff)
                window.Dequeue();

            if (window.Count >= maxReq)
            {
                _logger.LogWarning("Rate limit hit: {Ip} → {Path} ({Count}/{Max})",
                    ip, path, window.Count, maxReq);

                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers["Retry-After"] = windowSec.ToString();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    $"{{\"error\":\"Too many requests\",\"retryAfter\":{windowSec}}}");
                return;
            }

            window.Enqueue(now);
        }

        // Add rate limit headers
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-RateLimit-Limit"]     = maxReq.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = "1";
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // Support X-Forwarded-For from Nginx reverse proxy
        var forwarded = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwarded))
            return forwarded.Split(',')[0].Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

public class RateLimitOptions
{
    public int ApiMaxRequests { get; set; }  = 100;  // 100 req/60s
    public int ApiWindowSeconds { get; set; } = 60;
    public int AuthMaxRequests { get; set; }  = 10;   // 10 req/60s (login, register)
    public int AuthWindowSeconds { get; set; } = 60;
}

public static class RateLimitExtensions
{
    public static IApplicationBuilder UseWorldFaithRateLimit(
        this IApplicationBuilder app, Action<RateLimitOptions>? configure = null)
    {
        var options = new RateLimitOptions();
        configure?.Invoke(options);
        return app.UseMiddleware<RateLimitMiddleware>(options);
    }
}
