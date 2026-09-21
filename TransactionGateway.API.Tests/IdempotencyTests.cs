using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TransactionGateway.API.Models;
using TransactionGateway.API.Tests.Fixtures;

namespace TransactionGateway.API.Tests;

public class IdempotencyTests : IClassFixture<IntegrationTestFixture>
{
    private readonly CustomWebApplicationFactory _factory;

    public IdempotencyTests(IntegrationTestFixture fixture)
    {
        _factory = new CustomWebApplicationFactory(fixture);
        fixture.ResetRateLimitsAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public async Task MissingHeader_Returns400()
    {
        var client = _factory.CreateClient();
        var body = new { accountId = "TEST-1", amount = 10m, currency = "USD" };

        var response = await client.PostAsJsonAsync("/api/transactions", body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DuplicateRequest_ReturnsSameResponse()
    {
        var client = _factory.CreateClient();
        var key = Guid.NewGuid().ToString();
        var body = new { accountId = "TEST-2", amount = 20m, currency = "USD" };

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(body)
        };
        req1.Headers.Add("Idempotency-Key", key);

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
        {
            Content = JsonContent.Create(body)
        };
        req2.Headers.Add("Idempotency-Key", key);

        var response1 = await client.SendAsync(req1);
        var response2 = await client.SendAsync(req2);

        response1.StatusCode.Should().Be(HttpStatusCode.Created);
        response2.StatusCode.Should().Be(HttpStatusCode.Created);

        var body1 = await response1.Content.ReadFromJsonAsync<Transaction>();
        var body2 = await response2.Content.ReadFromJsonAsync<Transaction>();

        body1!.Id.Should().Be(body2!.Id);
    }

    [Fact]
    public async Task ConcurrentRequestsWithSameKey_OnlyOneCreated()
    {
        var client = _factory.CreateClient();
        var key = Guid.NewGuid().ToString();
        var body = new { accountId = "TEST-RACE", amount = 30m, currency = "USD" };

        HttpRequestMessage BuildRequest()
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/transactions")
            {
                Content = JsonContent.Create(body)
            };
            req.Headers.Add("Idempotency-Key", key);
            return req;
        }

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => client.SendAsync(BuildRequest()))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Exactly one should be 201; the others should be 409 or replay 201 with the same id
        var successful = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var conflicts = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        successful.Count.Should().BeGreaterThan(0);
        (successful.Count + conflicts.Count).Should().Be(5);

        // All successful responses should refer to the same transaction id
        var ids = new List<int>();
        foreach (var r in successful)
        {
            var t = await r.Content.ReadFromJsonAsync<Transaction>();
            ids.Add(t!.Id);
        }
        ids.Distinct().Should().HaveCount(1);
    }
}