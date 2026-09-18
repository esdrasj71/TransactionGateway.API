using Microsoft.AspNetCore.Mvc;

namespace TransactionGateway.API.Filters;

public class IdempotentAttribute : ServiceFilterAttribute
{
    public IdempotentAttribute() : base(typeof(IdempotencyFilter))
    {
    }
}