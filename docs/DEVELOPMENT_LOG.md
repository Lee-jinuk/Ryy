# DEVELOPMENT_LOG — RYY

실제 Unity 프로젝트를 검사해 기록한 구현 현황. 코드를 확인하지 않은 기능은 구현 완료로 기록하지 않는다.

## 파일 구조 (2026-10-14 검사)

- `Assets/Scripts/Core/YutGameManager.cs` — 유일한 게임 로직 스크립트
- `Assets/Scripts/Data/`, `Assets/Scripts/UI/` — `.gitkeep`만 존재 (빈 폴더)
- `Assets/Prefabs/`, `Assets/ScriptableObjects/` — `.gitkeep`만 존재
- `Assets/Scenes/SampleScene.unity` — Canvas, CalcText, TargetText, result, Text (Legacy), Button (Legacy), EventSystem 오브젝트 존재 확인

## 구현된 기능 (코드 확인 완료)

- `YutResult` enum 6종 (BackDo 포함)
- `Dictionary<YutResult, float>` 확률 테이블 (Gemini 제안값: 백도 6.25 / 도 18.75 / 개 37.5 / 걸 25 / 윷 6.25 / 모 6.25 %)
- 단일 타겟 선택: `selectedTarget`, `GameState { TargetSelection, Playing, RoundEnd }`
- 피버 콤보: 타겟 적중 시 `feverCombo++`, 배수 지수 합산, `feverCombo >= 2`부터 `currentMultiplier *= feverCombo` (X-Mult)
- 점수: 도10/개20/걸30/윷40/모50, 배수 지수 +1~+5, `baseScorePool * currentMultiplier`
- 라운드 종료 판정: `roundTargetScore = 1000` 기준 클리어/게임오버
- UI Text 필드 5개 (rollResultText, roundText, calcText, totalScoreText, targetYutText)
- `ConfirmTargetSelection()`, `ResetRound()` public (Button OnClick 연결용)
- 스페이스바 테스트 (`Update` 내 `Input.GetKeyDown`)

## 기획 문서와의 차이점 (보고 — 미수정)

`GAME_RULES.md` 기준으로 코드와 충돌하는 항목. 규칙상 임의로 수정하지 않고 보고한다.

- **투척 횟수**: 코드 `maxThrows = 7` ↔ 확정 규칙 5회. 코드 수정 보류 (사용자 승인 필요).
- **타겟/피버 시스템**: 기획상 "검토 중"이나 코드에 이미 구현됨 (이전 작업 지시로 구현). 채택/폐기/조정 결정 필요.
- **점수·배율·목표 점수**: 전부 미확정 규칙이나 코드에 임의값으로 구현됨 (도10~모50, 배수+1~+5, 목표 1000).
- **`Update()` 입력**: 프로젝트는 `activeInputHandler: 1`(New Input System 전용)인데 `Input.GetKeyDown` 사용 → Play 시 `InvalidOperationException` 발생함. Player Settings를 `Both`로 변경하거나 Input System API로 교체 필요.

## 미구현 기능

- 방울 덱빌딩 (덱 12개, 6개 제시 → 3개 선택, 상점 구매/제거) — 전부 미구현
- 부적 시스템 — 미구현
- 판(여러 라운드) 진행 구조 — 미구현
- 중단/점수 확정 시스템 — 미구현
- 씬 연동: `SampleScene.unity`에 UI 오브젝트는 있으나 `YutGameManager` 컴포넌트가 씬에 배치되어 있지 않음 (스크립트 GUID 미발견). Inspector 연결 및 버튼 이벤트 바인딩 필요.

## 로그

| 날짜 | 작업 | 비고 |
|---|---|---|
| 2026-10-14 | 로컬 Git 세팅, Unity .gitignore, 폴더 구조화, 단일 타겟 피버 시스템 구현, GitHub 연결 및 main/develop push | 피버 시스템은 이후 기획상 '검토 중'으로 재분류됨 |
| 2026-10-14 | docs/ 문서 체계 구축, AI_CONTEXT.md 이관 | 코드 변경 없음 |
