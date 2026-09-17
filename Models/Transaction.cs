namespace TransactionGateway.API.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public string AccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
