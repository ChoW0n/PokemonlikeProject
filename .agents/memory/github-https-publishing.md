---
name: GitHub HTTPS publishing
description: Replit workspace GitHub authentication and remote verification.
---

When the user explicitly requests a GitHub push, run `gh auth setup-git` before `git push origin main`, then verify the remote branch with `git ls-remote` and compare it to `git rev-parse HEAD`.

**Why:** A direct HTTPS push can reject the configured credential even when GitHub CLI is already authenticated. SSH also depends on a configured key and non-interactive host verification.

**How to apply:** Keep `origin` as the HTTPS GitHub remote, use the GitHub CLI credential bridge, and do not report completion until local and remote commit IDs match.