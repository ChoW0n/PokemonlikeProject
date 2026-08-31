---
name: .NET test restore
description: Environment-specific recovery when .NET build/test cannot find cached analyzer assemblies.
---

When a `dotnet build` or `dotnet test --no-restore` run reports that cached analyzer DLLs are missing, refresh the relevant project's NuGet assets with a forced no-cache restore before treating the source or project references as broken.

**Why:** The source and project references can be valid while the local package cache or generated assets are incomplete, producing misleading compiler errors in either the app or test project.

**How to apply:** Run a forced no-cache restore for the test project, then rerun the normal no-restore build/test validation.