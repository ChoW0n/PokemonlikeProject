---
name: DataGen execution and drift
description: DataGen uses process-relative output paths and its live PokeAPI output may drift from committed catalogs.
---

DataGen must be run with `DataGen` as the working directory because its generated-file paths are relative to the process directory. Its output can also drift substantially from committed catalogs when PokeAPI data or generation assumptions change; compare non-target fields before accepting regeneration.

**Why:** Running from the repository root wrote generated files outside the workspace, while a correct run exposed move and auxiliary-data drift unrelated to the hidden-ability fix.

**How to apply:** Use `cd DataGen && dotnet run --no-launch-profile`, preserve pre-generation snapshots, and reject the result rather than committing if move lists, stats, evolution, sprite, or other non-target fields change.