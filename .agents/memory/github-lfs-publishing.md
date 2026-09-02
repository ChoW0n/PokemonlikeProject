---
name: GitHub LFS publishing
description: Publishing this project when generated artifact bundles are tracked by Git LFS
---

GitHub may reject a push with `GH008` when a generated bundle introduces an LFS pointer whose object is unavailable on the remote, even if the local pre-push hook is skipped.

**Why:** The project contains generated self-contained bundles under artifact output paths; GitHub's server-side LFS validation checks every pointer reachable from the pushed history, while the Replit environment may fail the signed LFS upload with `Not Implemented`.

**How to apply:** Keep source and configuration changes in the pushed history, but exclude or restore generated bundle changes to a pointer/object already present on the remote. Verify the final tree and use the authenticated Git push only after no unknown LFS pointer remains in outgoing history.