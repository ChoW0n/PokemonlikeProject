# Claude 앱/웹 협업 지침

이 저장소는 Replit Agent와 Claude 앱/웹의 Sonnet이 번갈아 작업할 수 있는 Pokemon Battle 프로젝트입니다.
작업을 시작할 때 반드시 현재 파일을 기준으로 판단하고, 인수인계 문서는 보조 정보로만 사용하세요.

## Claude 앱/웹에서 최신 파일 읽기

Claude 앱/웹은 이 Replit 작업공간을 자동으로 읽지 않습니다. Replit Shell에서 아래 명령을 실행한 후
생성된 파일을 Claude 대화에 업로드하세요.

```bash
bash scripts/export-claude-context.sh
```

기능 작업에 필요한 전체 소스까지 전달해야 하면 다음을 사용하세요.

```bash
bash scripts/export-claude-context.sh full
```

기본 출력 파일은 `claude-context-bundle.md`입니다. 작업이 바뀔 때마다 다시 생성해야 최신 상태가
반영됩니다. 이 파일에는 시크릿 값이나 데이터베이스 연결 문자열을 넣지 않습니다.

자동 동기화가 필요하면 Replit 프로젝트를 GitHub 저장소와 연결하고, Claude 웹의 해당 저장소 접근
기능을 사용해야 합니다. 이 저장소의 로컬 파일을 Claude 앱/웹으로 자동 전송하는 기능은 없습니다.

Claude에 함께 보낼 권장 요청문:

```text
첨부한 claude-context-bundle.md는 Replit 작업공간에서 방금 생성한 최신 스냅샷이다.
문서 내용보다 첨부된 실제 파일 내용과 변경 diff를 우선하라.
이미 있는 미커밋 변경을 삭제하거나 덮어쓰지 말고, 현재 작업과 충돌하는지 먼저 확인하라.
다음 작업을 제안할 때 수정할 파일, 영향 범위, 테스트 계획을 함께 제시하고,
구현 시에는 기존 PokemonBattle 구조와 한국어 UI를 유지하라.
```

Replit Agent가 직접 작업할 때는 다음 명령으로 최신 상태를 확인하세요.

```bash
bash scripts/claude-context.sh
cat docs/claude-handoff.md
```

## 프로젝트 기준

- 실제 제품 코드는 `PokemonBattle/`의 순수 C#/.NET 8 Blazor Server 앱입니다.
- React 템플릿과 `artifacts/api-server`는 이 게임 기능의 구현 대상이 아닙니다.
- 도메인 데이터는 `PokemonBattle/Models/`, 화면은 `PokemonBattle/Pages/`, 전투 규칙은 `PokemonBattle/Services/`에 있습니다.
- 전투 규칙은 화면 코드에 직접 넣지 말고 `BattleEngine` 또는 효과 핸들러 계층에 연결하세요.
- 데이터베이스 변경은 기존 PostgreSQL/EF Core 구조와 호환되게 작성하고, Drizzle 스키마로 기존 EF 테이블을 덮어쓰지 마세요.
- 한국어 UI와 기존 명명·구조를 유지하세요.
- 비밀번호, 세션 값, 연결 문자열, API 키 등 비밀값을 출력하거나 문서에 기록하지 마세요.

## 검증 기준

코드 변경 후 다음을 실행하세요.

```bash
dotnet build PokemonBattle/PokemonBattle.csproj --no-restore
dotnet test PokemonBattle.Tests/PokemonBattle.Tests.csproj --no-restore
git diff --check
```

전투 상태를 변경하는 테스트는 `BattleWeather`와 `BattleField`를 반드시 정리해야 합니다.
두 상태는 모델 계산과 효과 핸들러가 함께 참조하는 공유 전투 상태이므로 테스트 병렬 실행도 피합니다.

Blazor 서버 코드를 변경했다면 Replit의 기존 워크플로
`artifacts/pokemon-battle: web`을 재시작하고 로그와 `/healthz`를 확인하세요.
워크플로를 새로 만들거나 기존 워크플로 이름을 바꾸지 마세요.

## 협업 방식

1. 작업 전 `scripts/claude-context.sh`로 최신 상태를 읽습니다.
2. 변경 전 `git diff`와 관련 테스트를 확인합니다.
3. 한 작업 단위가 끝나면 `docs/claude-handoff.md`의 현재 작업/검증 섹션을 갱신합니다.
4. 문서에는 구현 로그를 길게 쌓지 말고, 다음 에이전트가 알아야 할 결정·주의점만 남깁니다.
5. 기존 미커밋 변경을 포함해 테스트하고, 다른 에이전트의 변경을 임의로 reset/clean하지 않습니다.

현재 상태의 상세 인수인계는 `docs/claude-handoff.md`를 참고하세요.