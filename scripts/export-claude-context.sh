#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MODE="${1:-changed}"
OUTPUT="${2:-claude-context-bundle.md}"
cd "$ROOT"

if [[ "$MODE" != "changed" && "$MODE" != "full" ]]; then
  echo "usage: bash scripts/export-claude-context.sh [changed|full] [output-file]" >&2
  exit 2
fi

is_safe_file() {
  local file="$1"
  [[ "$file" != *"/bin/"* ]] &&
    [[ "$file" != *"/obj/"* ]] &&
    [[ "$file" != *.dll ]] &&
    [[ "$file" != *.pdb ]] &&
    [[ "$file" != *.cache ]] &&
    [[ "$file" != *.sqlite ]] &&
    [[ "$file" != *.db ]] &&
    [[ "$file" != *.json ]] &&
    [[ "$file" != *appsettings* ]]
}

if [[ "$MODE" == "full" ]]; then
  mapfile -t SOURCE_FILES < <(
    find PokemonBattle PokemonBattle.Tests \
      -type f \( -name '*.cs' -o -name '*.razor' -o -name '*.csproj' \) \
      -not -path '*/bin/*' -not -path '*/obj/*' | sort
  )
else
  mapfile -t SOURCE_FILES < <(
    {
      git diff --name-only
      git ls-files --others --exclude-standard
    } | sort -u | while read -r file; do
      [[ -n "$file" ]] && is_safe_file "$file" && printf '%s\n' "$file"
    done
  )
fi

{
  echo "# Pokemon Battle — Claude 작업공간 스냅샷"
  echo
  echo "> 이 파일은 Replit에서 생성된 업로드용 스냅샷입니다. 다음 변경 후 다시 생성하세요."
  echo
  echo "## 스냅샷 메타데이터"
  echo
  echo "- 브랜치: $(git branch --show-current 2>/dev/null || echo unknown)"
  echo "- HEAD: $(git log -1 --oneline 2>/dev/null || echo unavailable)"
  echo "- 모드: $MODE"
  echo
  echo "## 현재 Git 상태"
  echo
  echo '```text'
  git status --short --untracked-files=all
  echo '```'
  echo
  echo "## 소스 변경 요약"
  echo
  echo '```text'
  git diff --stat -- \
    ':(exclude)**/bin/**' \
    ':(exclude)**/obj/**' \
    ':(exclude)**/*.dll' \
    ':(exclude)**/*.pdb' \
    ':(exclude)**/*.cache'
  echo '```'
  echo
  echo "## 프로젝트 인수인계"
  echo
  cat docs/claude-handoff.md
  echo
  echo "## 프로젝트 규칙"
  echo
  cat CLAUDE.md
  echo
  echo "## 현재 소스 diff"
  echo
  echo '```diff'
  git diff -- \
    ':(exclude)**/bin/**' \
    ':(exclude)**/obj/**' \
    ':(exclude)**/*.dll' \
    ':(exclude)**/*.pdb' \
    ':(exclude)**/*.cache'
  echo '```'
  echo
  echo "## 전달된 파일 내용"
  echo
  for file in "${SOURCE_FILES[@]}"; do
    [[ -f "$file" ]] || continue
    echo "### \`$file\`"
    echo
    echo '```'
    cat "$file"
    echo
    echo '```'
    echo
  done
} > "$OUTPUT"

printf 'Created %s (%s bytes, %s files)\n' "$OUTPUT" "$(wc -c < "$OUTPUT")" "${#SOURCE_FILES[@]}"