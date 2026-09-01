---
name: Deployment UBB suspension
description: Replit deployment builds can succeed and then be suspended by a usage-based billing hold.
---

A deployment with a successful build that later reports suspendedReason ubb is an account or billing-hold condition, not an application build failure.

**Why:** Replit publishing documentation associates unresolved usage-based billing or payment failures with suspended published apps, even when the container build and service startup succeeded.

**How to apply:** Check the account Billing settings for an outstanding usage invoice or payment-method issue, clear the hold, then publish again. Do not try to bypass the hold in application code; if billing is current, provide Replit Support the build ID and the ubb suspension reason.
