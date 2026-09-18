using Microsoft.EntityFrameworkCore;
using TransactionGateway.API.Configuration;
using TransactionGateway.API.Controllers;
using TransactionGateway.API.Data;
using TransactionGateway.API.Filters;
using TransactionGateway.API.Middleware;
using TransactionGateway.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<RateLimitSettings>(
    builder.Configuration.GetSection("RateLimit"));

builder.Services.Configure<IdempotencySettings>(
    builder.Configuration.GetSection("Idempotency"));

builder.Services.AddScoped<IdempotencyService>();
builder.Services.AddScoped<IdempotencyFilter>();
builder.Services.AddSingleton<RedisService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();