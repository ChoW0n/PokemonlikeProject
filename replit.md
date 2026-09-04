# Pokemon Battle Blazor

브라우저에서 버튼을 눌러 포켓몬 배틀을 진행할 수 있도록 준비한 순수 C# Blazor Server 웹 게임입니다.

## Run & Operate

- `pnpm --filter @workspace/api-server run dev` — run the API server (port 5000)
- `dotnet run --project PokemonBattle/PokemonBattle.csproj --no-launch-profile --urls http://0.0.0.0:3000` — run the Blazor Server preview
- `pnpm run typecheck` — full typecheck across all packages
- `pnpm run build` — typecheck + build all packages
- `pnpm run test` — run the PokemonBattle.Tests .NET battle regression suite
- `pnpm run verify` — build the app, then run the battle regression suite as a separate validation step
- `pnpm --filter @workspace/api-spec run codegen` — regenerate API hooks and Zod schemas from the OpenAPI spec
- `pnpm --filter @workspace/db run push` — push DB schema changes (dev only)
- Required env: `DATABASE_URL` — Postgres connection string

## Stack

- pnpm workspaces, Node.js 24, TypeScript 5.9
- API: Express 5
- DB: PostgreSQL + Drizzle ORM
- Validation: Zod (`zod/v4`), `drizzle-zod`
- API codegen: Orval (from OpenAPI spec)
- Build: esbuild (CJS bundle)
- Battle UI: .NET 8 Blazor Web App with Interactive Server render mode

## Where things live

- `PokemonBattle/Models/` — Pokémon, moves, abilities, items, type rules, teams, and generated game data
- `PokemonBattle/Pages/` — login, registration, opponent preview, team building, battle, and result screens
- `PokemonBattle/Services/` — authentication, unlocks, run persistence, presets, and shared game state
- `PokemonBattle/Pages/Battle.razor` — turn order, PP, switching, damage, status, ability, and item execution
- `PokemonBattle/wwwroot/app.css` — responsive game UI styling

## Architecture decisions

- Domain concepts are split into one C# file per role so Pokémon, type rules, fixed data, and the database can grow independently.
- The .NET 8 Blazor Web App template runs in Interactive Server mode, which provides the server-side interaction model intended for a Blazor Server game.
- `DataGen/` generates Pokémon, move, and ability catalogs from PokeAPI; combat behavior is implemented separately in the runtime models and battle page.
- Display descriptions do not automatically implement effects. An ability, move, or item only works in battle when its runtime rule is connected.

## Product

현재는 로그인, 사용자별 진행 저장, 포켓몬 해금, 최대 6마리 팀 구성, 기술·특성·도구 선택, 상대 미리보기, 턴제 배틀, 교체, 승패 및 진화 진행이 연결된 싱글플레이 프로토타입입니다.

## User preferences

- 사용자는 유니티 없이 순수 C#과 Blazor만 사용하는 확장형 구조를 원합니다.

## Gotchas

- 데이터베이스에 이름과 설명이 존재해도 전투 효과가 구현된 것은 아닙니다. 새 특성·기술·도구는 표시 데이터와 런타임 판정을 함께 연결해야 합니다.
- 구애 도구의 기술 잠금과 PP 사용 가능 여부는 `Pokemon`의 공통 검증을 통해 처리해야 하며, 전투 화면에서 다른 기술로 조용히 대체하면 안 됩니다.

## Git 커밋 및 원격 반영

작업 완료의 기준은 로컬 커밋 생성이 아니라 GitHub 원격 저장소의 `main` 브랜치까지 반영된 상태입니다. 이번 작업에서 확인한 순서는 다음과 같습니다.

```bash
git add <변경 파일>
git commit -m "<작업 내용>"
gh auth setup-git
git push origin main
```

`gh auth setup-git`은 이미 연결된 GitHub CLI 인증을 Git의 HTTPS 인증으로 연결합니다. 먼저 `git commit`만 하고 `git push`를 빠뜨리거나, `gh auth setup-git` 없이 `git push origin main`을 실행하면 저장된 HTTPS 자격 증명이 거부되어 원격 반영이 완료되지 않을 수 있습니다.

SSH remote를 임의로 지정하는 방식은 이 저장소의 HTTPS `origin`과 별개로 동작하지 않을 수 있고, SSH 키와 호스트 확인 설정이 없는 환경에서는 인증 단계에서 멈춥니다. 따라서 이 저장소에서는 위 순서대로 GitHub CLI 인증을 Git에 연결한 뒤 `git push origin main`의 결과를 확인합니다.

## Pointers

- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details
