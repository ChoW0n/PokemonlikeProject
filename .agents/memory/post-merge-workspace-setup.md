---
name: Post-merge workspace setup
description: Constraints for dependency installation and database setup after task merges
---

The post-merge setup should retry dependency installation without a frozen lockfile when workspace catalog configuration has drifted, then leave the lockfile synchronized for later strict installs. Database schema pushes must remain non-destructive and non-interactive.

**Why:** This workspace's generic Drizzle package can have an empty schema while the Blazor application owns populated PostgreSQL tables through Entity Framework. A forced Drizzle push can propose deleting application data, and a normal push cannot answer its confirmation prompt in the post-merge environment.

**How to apply:** Guard the Drizzle push until real schema tables exist. Do not add a force flag just to silence a data-loss prompt; review schema ownership before enabling automatic synchronization.