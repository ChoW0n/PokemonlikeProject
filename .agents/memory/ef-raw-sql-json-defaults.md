---
name: EF raw SQL JSON defaults
description: PostgreSQL schema bootstrap SQL that contains literal JSON object defaults.
---

When a schema bootstrap command uses EF Core `ExecuteSqlRaw` and a SQL literal contains `{}` (for example a JSON column default), write it as `{{}}` in the format string.

**Why:** `ExecuteSqlRaw` treats braces as composite-format placeholders, so an unescaped JSON object default raises a `FormatException` during application startup or integration-test setup.

**How to apply:** Check every new raw SQL block for JSON object literals before restarting the app or running persistence tests.