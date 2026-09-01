---
name: Trace ability lifecycle
description: Trace copies a live opponent ability on entry but must restore the holder's original ability after switching out.
---

Trace's copied ability is temporary battle state, not a permanent replacement for the Pokémon's own ability.

**Why:** A copied ability can otherwise leak into later switch-ins and make subsequent battle outcomes depend on an earlier opponent.

**How to apply:** Keep the original ability separate from the mutable active ability, restore it at switch-out, then resolve entry abilities from the active value.