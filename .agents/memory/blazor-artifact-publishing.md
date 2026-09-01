---
name: Blazor artifact publishing
description: Production-runtime constraint and packaging rule for this Blazor artifact.
---

Publish the Blazor server as a portable, self-contained Linux executable rather than invoking `dotnet` in artifact production.

Do not attach the workspace-wide `pnpm run verify` command as the root Publishing pre-build hook; keep database-backed regression tests as a separate development validation step.

**Why:** The development workflow can receive the .NET SDK module, but the artifact production runtime does not expose a `dotnet` command and fails with a spawn-not-found error. Publishing also prepares the production database before running the root hook, so database integration tests can receive a production connection that rejects insecure SSL settings even when the application build is valid.

**How to apply:** Regenerate the portable bundle whenever C# or Razor source changes, keep development on the .NET SDK workflow, run regression tests before publishing rather than inside Publishing, and use a dedicated HTTP 200 health endpoint for startup checks.