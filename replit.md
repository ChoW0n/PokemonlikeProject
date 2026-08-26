# Pokemon Battle Blazor

브라우저에서 버튼을 눌러 포켓몬 배틀을 진행할 수 있도록 준비한 순수 C# Blazor Server 웹 게임입니다.

## Run & Operate

- `pnpm --filter @workspace/api-server run dev` — run the API server (port 5000)
- `dotnet run --project PokemonBattle/PokemonBattle.csproj --no-launch-profile --urls http://0.0.0.0:3000` — run the Blazor Server preview
- `pnpm run typecheck` — full typecheck across all packages
- `pnpm run build` — typecheck + build all packages
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

- `PokemonBattle/Models/` — extensible Pokémon domain model placeholders
- `PokemonBattle/Pages/Battle.razor` — interactive battle screen and temporary move-slot controls
- `PokemonBattle/wwwroot/app.css` — responsive battle UI styling

## Architecture decisions

- Domain concepts are split into one C# file per role so Pokémon, type rules, fixed data, and the database can grow independently.
- The .NET 8 Blazor Web App template runs in Interactive Server mode, which provides the server-side interaction model intended for a Blazor Server game.
- The first screen intentionally keeps Pokémon and move data empty; the controls only verify the server-interactive shell until domain data is added.

## Product

현재는 포켓몬과 기술을 연결하기 전의 배틀 화면 골격입니다. 네 개의 기술 슬롯 버튼, 턴 표시, 선택 메시지, 초기화 버튼으로 프리뷰 상호작용을 확인할 수 있습니다.

## User preferences

- 사용자는 유니티 없이 순수 C#과 Blazor만 사용하는 확장형 구조를 원합니다.

## Gotchas

- 포켓몬과 기술을 추가할 때는 `PokemonBattle/Models/`의 역할별 파일을 먼저 채운 뒤 `Battle.razor`의 표시와 선택 로직을 연결합니다.

## Pointers

- See the `pnpm-workspace` skill for workspace structure, TypeScript setup, and package details
