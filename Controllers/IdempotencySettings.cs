namespace TransactionGateway.API.Controllers
{
    public class IdempotencySettings
    {
        public int TtlHours { get; set; } = 24;
    }
}
