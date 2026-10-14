# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.

## 현재 상태

- 마지막 작업 일시: 2026-10-14
- 작업 담당 AI: Devin Desktop (Cognition)
- Git 커밋: 이 커밋의 해시로 갱신 예정 (직전: `1a694a3` feat: structure folders and implement single-target fever system)
- 현재 브랜치: `develop` (origin: https://github.com/Lee-jinuk/Ryy.git)

## 완료된 작업

- 로컬 Git 초기화, Unity `.gitignore`, `main` 초기 커밋, `develop` 분리
- 폴더 구조화: `Assets/Scripts/{Core,Data,UI}`, `Assets/Prefabs`, `Assets/ScriptableObjects`
- `YutGameManager.cs` → `Scripts/Core/` 이동 (.meta GUID 유지)
- 단일 타겟 피버 시스템 구현 (당시 지시 기준 — 현재는 '검토 중' 규칙으로 재분류)
- GitHub origin 연결, `main`/`develop` push 완료
- docs/ 협업 문서 체계 구축, `AI_CONTEXT.md` → docs 이관 완료

## 현재 문제점

- `YutGameManager.cs`의 `maxThrows = 7`이 확정 규칙(5회)과 다름 — 사용자 승인 대기
- 피버 시스템은 코드에 구현됐으나 기획상 '검토 중' — 채택 여부 미결
- `Input.GetKeyDown`이 `activeInputHandler: 1` 환경에서 런타임 예외 발생 → Player Settings `Both` 변경 또는 Input System API 교체 필요
- `YutGameManager` 컴포넌트가 `SampleScene.unity`에 배치되어 있지 않음 — UI 필드 연결 불가 상태

## 미확정 결정 사항 (사용자 승인 필요)

- 코드의 임의 점수/배율/목표 점수 값 채택 여부 (`GAME_RULES.md` 5절 전부 미확정)
- 7→5회 수정 시 피버 X-Mult 밸런스 영향 검토
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. 사용자 승인 후 `maxThrows` 7→5 수정
2. `SampleScene`에 `YutGameManager` 배치 + UI Text 5개 연결 + 버튼 OnClick 바인딩 (또는 에디터 스크립트로 자동 세팅)
3. 입력 처리 방식 결정 (Active Input Handling `Both` vs Input System API)
4. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기획 확정 후 착수

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs`
- `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
