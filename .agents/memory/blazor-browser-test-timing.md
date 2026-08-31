---
name: Blazor browser test timing
description: Interactive Server browser tests need event-aware input and rendering waits.
---

Interactive Server browser tests should type into bound auth inputs and assert their values before submitting; after async battle actions, wait for the completed log marker before issuing another command.

**Why:** Directly filling a bound username field and clicking during an in-flight Blazor render caused the browser check to submit stale input or race a forced switch.

**How to apply:** Prefer per-keystroke input for auth forms, wait briefly after full-page navigation for the circuit to attach before filling bound fields, and use a stable rendered completion marker, such as a final log entry attribute, before chaining UI actions.