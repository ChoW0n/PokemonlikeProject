---
name: PokeAPI move metadata generation
description: Rules for classifying machine-only moves and emitting generated C# arrays safely.
---

PokeAPI의 TM 전용 판정은 `version_group_details`가 비어 있지 않고 모든 습득 방법이 `machine`일 때만 참으로 취급한다. 조회 실패, 누락된 방법 이름, 빈 응답은 잠금 대상에 포함하지 않는다.

**Why:** 세대별 습득 방법이 섞인 기술을 TM 전용으로 잠그면 정상적인 레벨업·알·튜터 기술까지 잘못 제한할 수 있고, 조회 실패를 잠금으로 처리하면 데이터 생성 오류가 사용자 기능 오류로 바뀐다.

**How to apply:** 생성 기술 목록과 UI 호환 목록을 갱신할 때 이 판정을 공통으로 사용하고, 생성된 C#의 빈 문자열 배열은 `new[] { }`가 아닌 `Array.Empty<string>()`으로 출력한다.