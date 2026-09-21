using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace TransactionGateway.API.Tests.Fixtures;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly IntegrationTestFixture _fixture;

    public CustomWebApplicationFactory(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _fixture.Postgres.GetConnectionString(),
                ["ConnectionStrings:Redis"] = _fixture.Redis.GetConnectionString(),
                ["RateLimit:Capacity"] = "5",
                ["RateLimit:RefillRatePerSecond"] = "0.001", 
                ["Idempotency:TtlHours"] = "1"
            });
        });
    }
}