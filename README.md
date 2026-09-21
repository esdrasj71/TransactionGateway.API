# Transaction Gateway API

A production-minded REST API for processing transactions with **idempotency**, **distributed rate limiting**, and **exact response replay** — built with .NET 7, PostgreSQL, and Redis.

[![.NET](https://img.shields.io/badge/.NET-7.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)](https://www.postgresql.org/)
[![Redis](https://img.shields.io/badge/Redis-7-DC382D)](https://redis.io/)
[![Docker](https://img.shields.io/badge/Docker-Compose-2496ED)](https://docs.docker.com/compose/)

---

## Why This Exists

Most CRUD APIs break the moment a client retries a request. A dropped network connection during a payment can create a duplicate charge. 
A misbehaving client can hammer an endpoint with thousands of requests.

This API demonstrates the patterns that prevent those failures:

- **Idempotency** — the same request sent twice produces the same result, never a duplicate transaction
- **Rate limiting** — a token bucket enforced atomically in Redis protects the API from abuse
- **Exact response replay** — retried requests receive the original response, preserving status codes and bodies
- **Distributed correctness** — all coordination state lives in Redis, so the behavior holds across multiple API instances

---

**Stack:**
- **API:** .NET 7, ASP.NET Core Web API, Entity Framework Core
- **Database:** PostgreSQL 16 (persistent volume)
- **Cache/Coordination:** Redis 7
- **Containerization:** Docker, docker-compose

---

## Quick Start

Requires Docker Desktop. No local Postgres, Redis, or .NET SDK needed.

```bash
git clone https://github.com/esdrasj71/TransactionGateway.API.git
cd TransactionGateway.API
docker compose up --build
