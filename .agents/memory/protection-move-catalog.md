---
name: Protection move catalog
description: Synchronization rule for protection move behavior, catalog data, and presentation registration.
---

Protection moves must be registered consistently in the rule metadata, the runtime move catalog, and the presentation catalog. A metadata-only entry can make the engine appear implemented while real battle selection rejects the move as unavailable.

Generated move data is not a sufficient runtime contract by itself: a name and description can remain present while stat-change entries or their activation chances are missing. Catalog regression tests should validate both entry completeness and observable battle behavior.

**Why:** Regression coverage exposed that several protection keys had effect branches but no actual `MoveDatabase` entries, so their special effects could never run through normal gameplay.

**How to apply:** When adding or splitting a protection move, verify its selectable move data, protection-effect mapping, and shield presentation key together, then cover the behavior with a battle-level regression test. When regenerating catalogs, also compare stat-change metadata and activation chances against the supported rules.