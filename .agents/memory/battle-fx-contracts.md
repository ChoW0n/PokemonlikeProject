---
name: Battle FX contracts
description: Visual presentation keys must resolve to the shared CSS/config vocabulary.
---

Presentation catalogs and client-side aliases must resolve to the same finite effect-key vocabulary used by effects-config and CSS; legacy aliases should normalize at the client boundary.

**Why:** A valid move-specific key can otherwise create an unstyled DOM class and silently remove the visual feedback while the battle rules still succeed.

**How to apply:** When adding a move profile or fallback, verify its resolved key against both effects-config.json and app.css, and keep cancellation/sequence guards around delayed visual work.