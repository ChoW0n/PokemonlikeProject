---
name: PostgreSQL persistence tests
description: Isolation and cleanup rules for integration tests that exercise the shared PostgreSQL service.
---

PostgreSQL integration tests should use a uniquely named temporary schema rather than shared application tables, and cleanup must run in a `finally` block.

**Why:** The development database is shared with the running application; fixed test usernames or destructive table changes can alter real development state and make parallel runs flaky.

**How to apply:** Create only the tables needed by the scenario inside the temporary schema, set the connection search path to that schema, and drop the schema with `CASCADE` after every test.