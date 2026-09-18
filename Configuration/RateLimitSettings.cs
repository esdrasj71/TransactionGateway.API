namespace TransactionGateway.API.Configuration
{
    public class RateLimitSettings
    {
        public int Capacity { get; set; } = 10;
        public double RefillRatePerSecond { get; set; } = 1.0;
        public int KeyTtlSeconds { get; set; } = 3600;
    }
}
