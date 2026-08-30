---
name: Blazor ARIA booleans
description: How dynamic accessibility state attributes must be rendered in Blazor markup
---

Dynamic `aria-expanded` and `aria-pressed` values should be rendered as explicit `"true"` or `"false"` strings rather than binding a C# boolean directly.

**Why:** Blazor's boolean-attribute rendering can omit a false value or emit an empty attribute for true, which does not reliably expose the ARIA state to assistive technology or browser assertions.

**How to apply:** For every stateful control, use a conditional string expression and assert both states in browser-level accessibility tests.