---
name: Blazor EF Core context concurrency
description: Scoped DbContext calls in interactive Blazor circuits must not outlive the UI operation that started them.
---

In an interactive Blazor Server circuit, never start a database save as fire-and-forget work from a scoped service when another page or component may immediately query the same scope. Await the save before navigation or rendering the next state.

**Why:** A scoped `DbContext` is not thread-safe; a background save can overlap the destination component's query and terminate the circuit with “A second operation was started on this context instance”.

**How to apply:** Keep state persistence methods asynchronous, await them from event handlers and lifecycle methods, and only navigate after the database operation completes.