---
name: Delayed attack lifecycle
description: Future Sight and Doom Desire reservations must tick and resolve independently of the caster's active battle slot.
---

Delayed attacks are turn-end effects, not follow-up actions owned by the caster's current move. The reservation must keep its target reference, consume a turn while the caster is switched out, and still resolve if the caster is asleep, paralyzed, flinched, or takes no action. If the recorded target has fainted, retarget the living opponent; if none exists, clear the reservation and log the failure.

**Why:** A reservation timer tied only to active Pokémon stopped advancing after the caster switched out, leaving the delayed attack permanently pending.

**How to apply:** Register every Pokémon that can receive a reservation, advance inactive reservation timers once per turn, and resolve all ready reservations from the turn-end pipeline.