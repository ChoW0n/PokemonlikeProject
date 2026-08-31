#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

echo "=== Claude live workspace context ==="
echo "workspace: $ROOT"
echo "branch: $(git branch --show-current 2>/dev/null || echo unknown)"
echo "head: $(git log -1 --oneline 2>/dev/null || echo unavailable)"
echo

echo "=== working tree ==="
git status --short --untracked-files=all
echo

echo "=== changed files (source/config only) ==="
git status --short --untracked-files=all \
  | awk '$2 !~ /(^|\/)(bin|obj)\// && $2 !~ /\.(dll|pdb|cache)$/ { print }'
echo

echo "=== diff summary (source/config only) ==="
git diff --stat -- \
  ':(exclude)**/bin/**' \
  ':(exclude)**/obj/**' \
  ':(exclude)**/*.dll' \
  ':(exclude)**/*.pdb' \
  ':(exclude)**/*.cache'
echo

echo "=== recent commits ==="
git log --oneline -8 2>/dev/null || true
echo

echo "=== project checks ==="
if command -v dotnet >/dev/null 2>&1; then
  dotnet --version
else
  echo "dotnet: unavailable in this shell"
fi
echo "build: dotnet build PokemonBattle/PokemonBattle.csproj --no-restore"
echo "test:  dotnet test PokemonBattle.Tests/PokemonBattle.Tests.csproj --no-restore"
echo

echo "=== collaboration files ==="
echo "rules:   CLAUDE.md"
echo "handoff: docs/claude-handoff.md"
echo "project: replit.md"