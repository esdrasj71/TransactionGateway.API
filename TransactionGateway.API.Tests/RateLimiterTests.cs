using System.Net;
using FluentAssertions;
using TransactionGateway.API.Tests.Fixtures;

namespace TransactionGateway.API.Tests;

public class RateLimiterTests : IClassFixture<IntegrationTestFixture>
{
    private readonly CustomWebApplicationFactory _factory;

    public RateLimiterTests(IntegrationTestFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture);
        fixture.ResetRateLimitsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task RateLimit_EnforcedAndHeadersPresent()
    {
        var client = _factory.CreateClient();

        // Capacity is 5, refill effectively 0
        for (int i = 0; i < 5; i++)
        {
            var ok = await client.GetAsync("/api/transactions");
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            ok.Headers.Should().ContainKey("X-RateLimit-Limit");
            ok.Headers.Should().ContainKey("X-RateLimit-Remaining");
        }

        var rejected = await client.GetAsync("/api/transactions");
        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        rejected.Headers.Should().ContainKey("Retry-After");
    }
}