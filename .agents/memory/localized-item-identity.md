---
name: Localized item identity
description: The battle runtime uses Korean item names as held-item identifiers.
---

아이템 이름은 화면 표시만이 아니라 배틀 런타임의 소지 도구 식별자로도 사용된다. 공식 한글명을 바꿀 때는 도구 데이터뿐 아니라 효과 판정, 저장·로드 검증, 회귀 테스트의 문자열도 함께 갱신해야 한다.

**Why:** 이름 변경 후 카탈로그만 수정하면 도구는 선택·저장되지만 실제 효과가 발동하지 않는 조용한 기능 오류가 생긴다.

**How to apply:** 아이템 명칭을 변경할 때 기존 이름을 전체 검색하고, 런타임 비교 지점과 관련 테스트를 같은 변경에 포함한다.