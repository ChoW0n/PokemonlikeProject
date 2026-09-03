---
name: Blazor browser test timing
description: Interactive Server browser tests need event-aware input and rendering waits.
---

Interactive Server browser tests should type into bound auth inputs and assert their values before submitting; after async battle actions, wait for the completed log marker before issuing another command.

**Why:** Directly filling a bound username field and clicking during an in-flight Blazor render caused the browser check to submit stale input or race a forced switch.

**How to apply:** Prefer per-keystroke input for auth forms, wait briefly after full-page navigation for the circuit to attach before filling bound fields, and wait for a stable rendered state before chaining UI actions. For animated controls that remain in the DOM while hidden, scope selectors to the visible container instead of checking only `disabled`; short, repeated waits for the rendered menu are safer than a single immediate click. Client-restored controls can overwrite early clicks, so confirm the restored value before using a stateful toggle. Playwright also treats `aria-disabled="true"` as disabled; use a forced pointer event when verifying a deliberately inspectable-but-not-selectable card. Long setup-round scenarios can occasionally miss their win condition under a busy shared test database; rerun that case in isolation before treating a single failure as a product regression.