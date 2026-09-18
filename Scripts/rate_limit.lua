-- Token bucket rate limiter
-- KEYS[1] = rate limit key (e.g., "ratelimit:user123")
-- ARGV[1] = bucket capacity (max tokens)
-- ARGV[2] = refill rate (tokens per second)
-- ARGV[3] = current timestamp in milliseconds
-- ARGV[4] = TTL for the key in seconds (cleanup)

local key = KEYS[1]
local capacity = tonumber(ARGV[1])
local refill_rate = tonumber(ARGV[2])
local now = tonumber(ARGV[3])
local ttl = tonumber(ARGV[4])

-- Step 1: Read the current state
local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
local tokens = tonumber(bucket[1])
local last_refill = tonumber(bucket[2])

-- Step 2: Initialize if this is the first request
if tokens == nil then
    tokens = capacity
    last_refill = now
end

-- Step 3: Calculate refill based on elapsed time
local elapsed = now - last_refill
local tokens_to_add = math.floor(elapsed * refill_rate / 1000)

if tokens_to_add > 0 then
    tokens = math.min(capacity, tokens + tokens_to_add)
    last_refill = now
end

-- Step 4: Decide — allow or reject
local allowed = 0
if tokens >= 1 then
    tokens = tokens - 1
    allowed = 1
end

-- Persist the new state
redis.call('HMSET', key, 'tokens', tokens, 'last_refill', last_refill)
redis.call('EXPIRE', key, ttl)

-- Return: [allowed, remaining_tokens, retry_after_ms]
local retry_after = 0
if allowed == 0 then
    retry_after = math.ceil(1000 / refill_rate)
end

return { allowed, tokens, retry_after }