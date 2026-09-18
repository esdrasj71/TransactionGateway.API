namespace TransactionGateway.API.Models
{
    public record RateLimitResult(bool Allowed, long Remaining, long RetryAfterMs);
}
