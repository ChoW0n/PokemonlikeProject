---
name: Blazor artifact publishing
description: Production-runtime constraint and packaging rule for this Blazor artifact.
---

Publish the Blazor server as a portable, self-contained Linux executable rather than invoking `dotnet` in artifact production.

**Why:** The development workflow can receive the .NET SDK module, but the artifact production runtime does not expose a `dotnet` command and fails with a spawn-not-found error.

**How to apply:** Regenerate the portable bundle whenever C# or Razor source changes, keep development on the .NET SDK workflow, and use a dedicated HTTP 200 health endpoint for startup checks.