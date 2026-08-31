---
name: Switch and escape rules
description: Behavioral contract for wild escape, voluntary switching traps, and forced switching immunity.
---

Wild escape permission is distinct from voluntary team switching: Run Away guarantees a wild escape, while trapping abilities still govern ordinary departure unless the ability explicitly bypasses them. Suction Cups only blocks an opponent-directed forced switch; it must not block self-switching moves such as U-turn.

**Why:** The same move metadata contains both target-ejection moves and moves that switch their user, so applying one generic forced-switch guard can incorrectly cancel a valid self-switch.

**How to apply:** Keep escape, voluntary switch, and forced switch predicates separate. When adding a switch move, classify whether the user or the target leaves before applying trapping or Suction Cups.