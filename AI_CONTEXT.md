# AI_CONTEXT — YutRoguelike

AI/외부 협업자가 프로젝트 맥락을 빠르게 파악하기 위한 공용 문서.

## 프로젝트 개요

발라트로(Balatro) 스타일의 윷놀이 확률 조작 + 배수 폭발 덱빌딩 로그라이크.
Unity + C# (New Input System 패키지 사용).

## 핵심 규칙

- 라운드당 기본 기회 7회 (`maxThrows = 7`, `currentThrows`로 사용량 추적).
- 윷 패 기본 확률: 백도 6.25%, 도 18.75%, 개 37.5%, 걸 25%, 윷 6.25%, 모 6.25%.
- 확률 테이블은 `Dictionary<YutResult, float>` 기반 인스턴스 변수 → 추후 유물 효과로 동적 조작 가능.
- 기본 점수: 백도 0, 도 10, 개 20, 걸 30, 윷 40, 모 50.
- 배수 지수(타겟 적중 시 합산): 도 +1, 개 +2, 걸 +3, 윷 +4, 모 +5.

## 단일 타겟 & 피버(Fever) 시스템

- 라운드 시작 시 유저가 1개의 '타겟 윷'을 선택 (`GameState.TargetSelection` → `Playing`).
- 타겟 윷 적중 시:
  - 던지기 횟수 차감 없음 (보너스 턴)
  - `feverCombo++` 후 해당 패의 배수 지수를 `currentMultiplier`에 합산
  - 연속 적중(`feverCombo >= 2`)부터 전체 배수에 콤보 수치만큼 곱연산(X-Mult) 폭발
- 비타겟 윷 등장 시: `currentThrows` +1, 기본 점수만 합산, `feverCombo = 0` 초기화.
- 백도 등장 시: `currentThrows` +1, `baseScorePool` 0 리셋, `feverCombo = 0` 초기화.
- 라운드 종료(7회 소진) 시 `baseScorePool * currentMultiplier`가 `roundTargetScore`(기본 1000) 이상이면 클리어, 미만이면 게임오버.

## 밸런스 방향성

- 저배수 패(도/개): 높은 확률로 최대 8피버까지 노리는 안정형 빌드.
- 고배수 패(윷/모): 아이템(확률 보정, 1회 실패 방어 실드 등)으로 최대 5피버 일발 역전을 노리는 로망형 빌드.

## Git & 폴더 규칙

- `develop` 브랜치에서 일일 작업, 안정화 시 `main`에 병합.
- 스크립트는 `Assets/Scripts/Core`, `Assets/Scripts/Data`, `Assets/Scripts/UI`로 분리 관리.
- `Assets/Prefabs`, `Assets/ScriptableObjects`도 Git으로 추적 (빈 폴더는 `.gitkeep` 유지).

## 진행 상태

- 로컬 Git 세팅 완료 (원격 GitHub 미연결).
- 폴더 구조화 및 단일 타겟 피버 룰 구현 예정.
