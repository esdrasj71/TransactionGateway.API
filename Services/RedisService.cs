using StackExchange.Redis;

namespace TransactionGateway.API.Services
{
    public class RedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisService> _logger;

        public RedisService(IConfiguration config, ILogger<RedisService> logger)
        {
            var connectionString = config.GetConnectionString("Redis");
            _redis = ConnectionMultiplexer.Connect(connectionString);
            _logger = logger;
        }

        public IDatabase GetDatabase() => _redis.GetDatabase();

        public async Task<RedisResult> EvaluateScriptAsync(
            string script,
            RedisKey[] keys,
            RedisValue[] args)
        {
            var db = _redis.GetDatabase();
            return await db.ScriptEvaluateAsync(script, keys, args);
        }
    }
}
