using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TransactionGateway.API.Configuration;
using TransactionGateway.API.Controllers;
using TransactionGateway.API.Models;
using TransactionGateway.API.Services;

namespace TransactionGateway.API.Filters;

public class IdempotencyFilter : IAsyncActionFilter
{
    private const string HeaderName = "Idempotency-Key";
    private readonly IdempotencyService _idempotency;
    private readonly IdempotencySettings _settings;

    public IdempotencyFilter(
        IdempotencyService idempotency,
        IOptions<IdempotencySettings> settings)
    {
        _idempotency = idempotency;
        _settings = settings.Value;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        // Step 1: Require the header
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var keyValue)
            || string.IsNullOrWhiteSpace(keyValue))
        {
            context.Result = new BadRequestObjectResult(new
            {
                error = "missing_idempotency_key",
                message = $"Header '{HeaderName}' is required for this endpoint."
            });
            return;
        }

        var key = keyValue.ToString();
        var ttl = TimeSpan.FromHours(_settings.TtlHours);

        // Step 2: Try to claim the key atomically
        var status = await _idempotency.TryClaimAsync(key, ttl);

        if (status == IdempotencyStatus.Completed)
        {
            // Replay the cached response
            var cached = await _idempotency.GetResponseAsync(key);
            if (cached != null)
            {
                context.Result = new ContentResult
                {
                    StatusCode = cached.StatusCode,
                    Content = cached.Body,
                    ContentType = cached.ContentType ?? "application/json"
                };
                return;
            }
        }

        if (status == IdempotencyStatus.InFlight)
        {
            // Another request with this key is being processed
            context.Result = new ConflictObjectResult(new
            {
                error = "request_in_progress",
                message = "A request with this idempotency key is already being processed."
            });
            return;
        }

        // Step 3: We claimed it — execute the action
        var executedContext = await next();

        // Step 4: Store the response for future replays
        if (executedContext.Result is ObjectResult objectResult)
        {
            var response = new IdempotentResponse
            {
                StatusCode = objectResult.StatusCode ?? 200,
                Body = System.Text.Json.JsonSerializer.Serialize(objectResult.Value),
                ContentType = "application/json"
            };
            await _idempotency.StoreResponseAsync(key, response, ttl);
        }
    }
}