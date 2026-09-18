using System.Text.Json;
using StackExchange.Redis;
using TransactionGateway.API.Models;

namespace TransactionGateway.API.Services
{
    public enum IdempotencyStatus
    {
        Claimed,
        InFlight,
        Completed
    }
    public class IdempotencyService
    {
        private readonly RedisService _redis;

        public IdempotencyService(RedisService redis)
        {
            _redis = redis;
        }

        private static string StatusKey(string key) => $"idempotency:{key}:status";
        private static string ResponseKey(string key) => $"idempotency:{key}:response";
        public async Task<IdempotencyStatus> TryClaimAsync(string key, TimeSpan ttl)
        {
            var db = _redis.GetDatabase();

            // Atomic claim: only succeeds if key doesn't exist
            var claimed = await db.StringSetAsync(
                StatusKey(key),
                "in-flight",
                ttl,
                When.NotExists);

            if (claimed) return IdempotencyStatus.Claimed;

            // Key already exists — check its status
            var status = await db.StringGetAsync(StatusKey(key));
            return status == "completed"
                ? IdempotencyStatus.Completed
                : IdempotencyStatus.InFlight;
        }
        public async Task StoreResponseAsync(string key, IdempotentResponse response, TimeSpan ttl)
        {
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(response);

            // Overwrite the status and store the response
            await db.StringSetAsync(StatusKey(key), "completed", ttl);
            await db.StringSetAsync(ResponseKey(key), json, ttl);
        }
        public async Task<IdempotentResponse?> GetResponseAsync(string key)
        {
            var db = _redis.GetDatabase();
            var json = await db.StringGetAsync(ResponseKey(key));

            return json.IsNullOrEmpty
                ? null
                : JsonSerializer.Deserialize<IdempotentResponse>(json!);
        }
    }
}
