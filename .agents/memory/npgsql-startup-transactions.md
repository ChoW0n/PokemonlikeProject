---
name: Npgsql startup transactions
description: PostgreSQL startup cleanup behavior when EF Core uses a retrying execution strategy
---

When an EF Core PostgreSQL context has `EnableRetryOnFailure`, any startup operation that opens a manual transaction must run the complete transaction inside `Database.CreateExecutionStrategy().ExecuteAsync(...)`.

**Why:** Npgsql's retrying execution strategy rejects user-initiated transactions outside its retriable unit, causing the application to fail during startup before it opens its port.

**How to apply:** Use the execution strategy around marker acquisition, reads, writes, and commit as one unit; let malformed data throw so the marker and all earlier updates roll back together.