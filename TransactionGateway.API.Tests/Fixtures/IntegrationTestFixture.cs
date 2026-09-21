using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using TransactionGateway.API.Data;

namespace TransactionGateway.API.Tests.Fixtures;

public class IntegrationTestFixture : IAsyncLifetime
{
    public PostgreSqlContainer Postgres { get; private set; } = null!;
    public RedisContainer Redis { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        Redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();

        await Postgres.StartAsync();
        await Redis.StartAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(Postgres.GetConnectionString())
            .Options;

        using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task ResetRateLimitsAsync()
    {
        using var redis = await ConnectionMultiplexer.ConnectAsync(Redis.GetConnectionString());
        var db = redis.GetDatabase();
        var server = redis.GetServer(Redis.GetConnectionString());

        foreach (var key in server.Keys(database: 0, pattern: "ratelimit:*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }

    public async Task DisposeAsync()
    {
        await Postgres.DisposeAsync();
        await Redis.DisposeAsync();
    }
}