---
name: Technical machine rewards
description: Rules for selecting new technical machine rewards from a player's current team.
---

The reward pool is the union of each team member's machine-only move metadata, excluding every technical machine already present in the user's inventory. Select with weak preference weighting plus randomness; only use the general move list when the machine-only pool is empty, and return no reward if that fallback is also exhausted.

**Why:** MoveNames includes ordinary learnable moves and therefore does not represent what a technical machine newly unlocks. Excluding equipped moves or sorting by preference makes rewards repeat or become deterministic.

**How to apply:** Query positive-quantity inventory rows inside the same progression operation before selecting. Keep the existing reward probabilities unchanged and route rival, general-battle, and covenant rewards through the shared selector.