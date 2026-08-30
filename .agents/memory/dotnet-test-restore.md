---
name: .NET test restore
description: Environment-specific recovery when the .NET test build cannot find the cached xUnit analyzer assembly.
---

When a `dotnet test --no-restore` run reports that the cached xUnit analyzer DLL is missing, refresh the test project's NuGet assets with a forced no-cache restore before treating the test project as broken.

**Why:** The source and project references can be valid while the local package cache or generated assets are incomplete, producing misleading compiler errors.

**How to apply:** Run a forced no-cache restore for the test project, then rerun the normal no-restore build/test validation.