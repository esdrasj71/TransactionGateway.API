namespace TransactionGateway.API.Models
{
    public class IdempotentResponse
    {
        public int StatusCode { get; set; }
        public string? Body { get; set; }
        public string? ContentType { get; set; }
    }
}
