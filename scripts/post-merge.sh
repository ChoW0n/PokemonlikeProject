#!/bin/bash
set -e

# Keep normal merges strict, but recover when a merged workspace catalog changed
# without a matching lockfile update.
if ! pnpm install --frozen-lockfile; then
  pnpm install --no-frozen-lockfile
fi

# This project currently has no Drizzle tables. Running `db push` with an empty
# schema would propose deleting the Blazor/EF tables, so leave schema syncing to
# the owning app until a real Drizzle schema is added.
if find lib/db/src/schema -mindepth 1 -maxdepth 1 -type f ! -name 'index.ts' | grep -q .; then
  pnpm --filter db push
else
  echo "No Drizzle schema tables found; skipping db push."
fi
