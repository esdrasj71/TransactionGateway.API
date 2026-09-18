using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TransactionGateway.API.Configuration;
using TransactionGateway.API.Services;

namespace TransactionGateway.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisService _redis;
    private readonly RateLimitSettings _settings;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly string _luaScript;

    public RateLimitingMiddleware(
        RequestDelegate next,
        RedisService redis,
        IOptions<RateLimitSettings> settings,
        ILogger<RateLimitingMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;

        var scriptPath = Path.Combine(env.ContentRootPath, "Scripts", "rate_limit.lua");
        _luaScript = File.ReadAllText(scriptPath);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Identify the client
        var clientId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"ratelimit:{clientId}";

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        RedisResult result;
        try
        {
            result = await _redis.EvaluateScriptAsync(
                _luaScript,
                new RedisKey[] { key },
                new RedisValue[]
                {
                    _settings.Capacity,
                    _settings.RefillRatePerSecond,
                    now,
                    _settings.KeyTtlSeconds
                });
        }
        catch (RedisConnectionException ex)
        {
            // Fail-open: log and let the request through
            _logger.LogWarning(ex, "Redis unavailable — bypassing rate limit for {ClientId}", clientId);
            await _next(context);
            return;
        }

        // Lua returns { allowed, remaining, retry_after_ms }
        var values = (RedisResult[])result!;
        var allowed = (long)values[0] == 1;
        var remaining = (long)values[1];
        var retryAfterMs = (long)values[2];

        // Always return rate limit info to the client
        context.Response.Headers["X-RateLimit-Limit"] = _settings.Capacity.ToString();
        context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();

        if (!allowed)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.Headers["Retry-After"] = Math.Ceiling(retryAfterMs / 1000.0).ToString();
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = "rate_limit_exceeded",
                message = "Too many requests. Please slow down.",
                retryAfterMs
            }));
            return;
        }

        await _next(context);
    }
}