---
name: Skill rating persistence
description: Rules for carrying player skill and a fixed per-run difficulty adjustment through restarts.
---

Skill rating is account-scoped and separate from the current run. The run stores its difficulty adjustment and round-performance records so a reconnect never recalculates an active run from a changed rating.

**Why:** Difficulty must react to completed-run performance without changing mid-run, while old accounts and old run rows need a neutral, safe default.

**How to apply:** Update the rating only when a run is finalized (loss or explicit successful new-run transition); calculate the next adjustment once at run start and clamp it to the defined range.