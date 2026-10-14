# AGENTS.md — RYY 공통 협업 규칙

ChatGPT, Gemini, Devin Desktop 등 이 프로젝트에 참여하는 모든 AI가 따르는 원칙.

## 필수 확인 문서

작업 시작 전 반드시 확인한다.

1. `AGENTS.md` (본 문서)
2. `docs/GAME_RULES.md` — 게임 규칙의 기준 문서
3. `docs/AI_HANDOFF.md` — 직전 작업 인수인계 내용

## 규칙

- 게임 규칙의 기준 문서는 `docs/GAME_RULES.md`다.
- 미확정 규칙은 사용자 승인 없이 확정하거나 구현하지 않는다.
- 기존 확정 규칙을 변경할 경우 `GAME_RULES.md`의 변경 이력에 기록한다.
- 개발 완료 후 `docs/DEVELOPMENT_LOG.md`와 `docs/AI_HANDOFF.md`를 업데이트한다.
- AI가 제안한 아이디어와 사용자가 확정한 규칙을 구분한다.
- 다른 AI가 작업을 이어받을 수 있도록 결과를 문서화한다.
- 기존 코드에 구현된 규칙이 기획 문서와 충돌하면 임의로 수정하지 않고 차이점을 보고한다.
- 코드를 확인하지 않은 기능을 구현 완료로 기록하지 않는다.
- 문서(`GAME_RULES.md` 변경 이력, `DEVELOPMENT_LOG.md`, `AI_HANDOFF.md`)를 업데이트할 때 날짜만 적지 말고 `### [YYYY-MM-DD 오전/오후 - 담당 AI]` 형식의 헤더로 작업 세션을 구분해 기록한다.
- `AI_HANDOFF.md` 최상단에는 다음 AI에게 그대로 복사해 전달할 수 있는 `[날짜 시간대 변경사항 요약]` 블록을 항상 유지한다.

## Git 규칙

- 일일 작업은 `develop` 브랜치에서 진행한다.
- 안정화 시 `main`에 병합한다.
- 기존 미커밋 작업을 덮어쓰지 않는다.

## 폴더 규칙

- 스크립트: `Assets/Scripts/Core`, `Assets/Scripts/Data`, `Assets/Scripts/UI`
- `Assets/Prefabs`, `Assets/ScriptableObjects`
- 빈 폴더는 `.gitkeep`으로 추적한다.
