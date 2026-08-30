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

## Pointers

- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details
