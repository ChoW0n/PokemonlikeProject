---
name: Artifact workflow stale port
description: Recovering an artifact-owned .NET workflow after a restart collides with an older process.
---

If an artifact-owned .NET workflow fails with `Address already in use` after a restart, treat it as a stale process problem before changing application code. Verify the listener and process command, stop only the old project process, then restart the managed workflow once.

**Why:** The old process can continue serving the previous build while the managed workflow reports failure, which makes preview checks misleading and can look like a code regression.

**How to apply:** Check `ss`/`netstat`, confirm the process belongs to the project, terminate that process, restart the exact artifact workflow, and confirm a fresh `Now listening` log.