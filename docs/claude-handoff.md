# Pokemon Battle — Claude 앱/웹 Sonnet 인수인계

> 이 문서는 현재 작업의 읽기 쉬운 요약입니다. 최신 파일 상태는 항상
> `bash scripts/export-claude-context.sh`로 번들을 다시 생성하고 실제 변경 내용을 확인하세요.

## Claude 앱/웹 접근 방식

Claude 앱/웹은 Replit 작업공간의 로컬 파일을 자동으로 읽을 수 없습니다.
Replit Shell에서 다음 명령을 실행한 후 생성된 `claude-context-bundle.md`를 Claude 대화에 업로드하세요.

```bash
bash scripts/export-claude-context.sh
```

전체 소스가 필요한 대규모 작업은 다음 명령을 사용합니다.

```bash
bash scripts/export-claude-context.sh full
```

새 변경을 반영하려면 번들을 다시 생성해 재업로드해야 합니다. 실시간 자동 동기화가 필요하면
GitHub 저장소 연결을 별도로 구성해야 하며, 이 문서만으로 외부 Claude에 접근 권한이 생기지는 않습니다.

## 제품과 목표

Unity 없이 순수 C#/.NET 8 Blazor Server로 만든 싱글플레이 포켓몬 배틀 게임입니다.
로그인, 사용자별 진행 저장, 포켓몬 해금, 최대 6마리 팀 구성, 기술·특성·도구 선택,
상대 미리보기, 턴제 전투, 교체, 승패·진화, 프리셋, 전설 출현 진행,
관리자 콘솔과 감사 로그가 연결되어 있습니다.

## 현재까지 구현된 큰 기능

- Pokémon·기술·특성·아이템·타입·도감·배틀 인스턴스의 역할 분리
- PP, 몸부림, 구애 아이템 기술 잠금, 상태 이상, 랭크 변화, 반동·흡수·다중 타격
- 특성·아이템 효과 핸들러 구조와 기술 효과 핸들러 구조
- 프리셋의 독립 깊은 복사와 레벨 유지 규칙
- 사용자별 런/점수/해금/전설 진행률·출현 이력 저장
- 관리자 사용자 조회, 검색, 상세, 권한 변경, 비밀번호 재설정,
  계정·런 초기화, 전체 해금, 점수·전설 진행률 설정, 테스트 팀 주입
- 관리자 작업 감사 로그
- 워글 스프라이트 매칭 수정 및 포켓로그 스타일 기술 연출

## 최근 작업: 날씨와 필드 전투 규칙

현재 작업은 `#10 - 날씨와 필드 특성이 실제 전투 규칙에 반영되게 하기`입니다.

### 날씨

- `BattleWeather`가 맑음·쾌청·비·모래바람·싸라기눈을 표현합니다.
- 특성 기반 날씨는 교체 등장 시 적용되고 영구 유지됩니다.
- 기술 기반 날씨는 5턴 유지 후 맑음으로 돌아갑니다.
- 웨더볼의 타입·위력이 날씨에 따라 바뀝니다.
- 솔라빔은 비·모래바람·싸라기눈에서 약해집니다.
- 번개·폭풍·눈보라는 날씨별 명중률 규칙을 사용합니다.
- 광합성·아침햇살·달빛의 회복량이 날씨에 따라 달라집니다.

### 필드

- 날씨와 분리된 `BattleField`가 그래스·일렉트릭·사이코·미스트필드를 표현합니다.
- 필드 기술은 5턴 유지 후 필드 없음으로 돌아갑니다.
- 그래스필드: 풀 기술 강화, 지진·땅고르기·매그니튜드 약화, 턴 종료 회복
- 일렉트릭필드: 전기 기술 강화, 주요 상태 이상 방지
- 사이코필드: 에스퍼 기술 강화, 상대를 향한 우선도 기술 차단
- 미스트필드: 드래곤 기술 약화, 주요 상태 이상과 혼란 방지
- `풀모피`의 그래스필드 방어 보정이 실제 필드 상태를 참조합니다.
- 배틀 헤더에 현재 날씨와 필드를 표시합니다.

## 현재 변경 파일

현재 작업 트리에서 기능 변경과 관련된 파일은 다음 영역입니다.

- 전투 상태: `PokemonBattle/Models/BattleWeather.cs`,
  `PokemonBattle/Models/BattleField.cs`
- 기술 규칙·기술 데이터: `PokemonBattle/Models/Move.cs`,
  `PokemonBattle/Models/MoveDatabase.cs`
- 포켓몬 계산: `PokemonBattle/Models/Pokemon.cs`
- 전투 실행·이벤트: `PokemonBattle/Services/BattleEngine.cs`,
  `PokemonBattle/Services/BattleEffects.cs`
- 효과 적용: `PokemonBattle/Services/MoveEffectHandlers.cs`,
  `PokemonBattle/Services/AbilityItemEffectHandlers.cs`
- 화면: `PokemonBattle/Pages/Battle.razor`
- 회귀 테스트: `PokemonBattle.Tests/WeatherAndFieldRegressionTests.cs`,
  `PokemonBattle.Tests/XunitSettings.cs`

생성 산출물인 `bin/`·`obj/`는 기능 변경으로 간주하지 말고 소스 diff만 검토하세요.

## 검증된 상태

- `dotnet build PokemonBattle/PokemonBattle.csproj --no-restore` 성공
- `dotnet test PokemonBattle.Tests/PokemonBattle.Tests.csproj --no-restore` 성공
- 현재 회귀 테스트 총 49개 통과
- Replit 워크플로 `artifacts/pokemon-battle: web` 정상 실행
- `/healthz` 응답 200
- Blazor 브라우저 연결 및 로그인 화면 프리뷰 확인

## 다음 에이전트가 따라야 할 제안

1. 먼저 현재 diff와 테스트를 다시 읽고, 이미 구현된 날씨·필드 규칙을 중복 구현하지 마세요.
2. 새 전투 규칙은 `MoveRuleMetadata`와 효과 핸들러를 우선 활용하고, `Battle.razor`에는 표시·입력만 추가하세요.
3. 새 기술이나 특성을 연결할 때는 기술 데이터 설명만 추가하지 말고 실행 경로와 결정적 회귀 테스트를 함께 추가하세요.
4. 날씨·필드 상태를 만지는 모든 테스트는 `Reset()`을 호출하고 테스트 간 공유 상태가 섞이지 않게 유지하세요.
5. 서버 코드 변경 후에는 기존 워크플로를 재시작하고 `/healthz`, 빌드, 전체 테스트를 다시 확인하세요.
6. 작업 완료 시 이 문서의 `현재 변경 파일`·`검증된 상태`를 최신 사실에 맞게 갱신하세요.
7. Claude 앱/웹에서 작업했다면 변경 후 새 번들을 생성해 다음 대화에 전달하세요.

## 충돌 방지 규칙

- 다른 에이전트의 미커밋 변경을 `git reset`, `git clean`, 강제 checkout으로 삭제하지 마세요.
- `PokemonBattle/`가 실제 게임 대상이며 React 샘플을 수정해 문제를 해결하지 마세요.
- 데이터베이스·시크릿·배포 설정은 요청 범위를 벗어나면 변경하지 마세요.
- 한국어 UI 문구와 현재 전투 이벤트 흐름을 유지하세요.