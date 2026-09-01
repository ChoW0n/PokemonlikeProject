---
name: Explicit DI registration for legacy constructors
description: Multiple public service constructors can remain ambiguous to production DI even when one is annotated for activation.
---

When a service keeps a legacy constructor for direct regression-test construction, register its production implementation with an explicit factory in Program.cs instead of relying on constructor selection.

**Why:** The deployed .NET service provider selected both the factory-based and legacy AppDbContext constructors as candidates, causing a runtime 500 before any component could render.

**How to apply:** Keep compatibility constructors only for callers that need them; make the application registration explicitly resolve the shared DatabaseContextExecutor and other scoped dependencies.