# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-14 오전 작업 요약 (GPT 전달용)]

```
[2026-10-14 오전 변경사항 요약 - Gemini & Devin]

- 입력 오류 해결: ProjectSettings.asset의 activeInputHandler를 1(New Input System 전용) → 2(Both)로 변경. Input.GetKeyDown 런타임 예외 해결됨.
- YutGameManager.cs (Assets/Scripts/Core/):
  * maxThrows 7 → 5 (확정 규칙 반영)
  * 피버 보상 상한 적용: feverCap = 8, 2콤보 이상 곱연산 시 appliedFever = Mathf.Min(feverCombo, feverCap) 사용 → 8콤보 이상 보상 동일
  * UI 방식 결정 변경: 코드 동적 생성 폐기 → 사용자가 에디터에서 직접 UI 배치 후 Inspector 드래그앤드롭 연결 방식으로 전환
  * 인스펙터 연결용 public 변수:
    - Text: mainResultText(중앙 결과), basicScoreText(베이직), multScoreText(배수), totalScoreText(총점/목표), throwCountText(투척 n/5), targetYutText(타겟+피버콤보)
    - GameObject 패널: startPanel(게임 시작 화면), targetSelectPanel(타겟 선택 화면)
    - GameObject[] 슬롯: bellSlots(방울 3칸), talismanSlots(부적 4칸) — 표시 자리만 준비, 로직 미구현
  * 버튼 OnClick용 public 함수:
    - StartGame() — startPanel OFF, targetSelectPanel ON, 상태 TargetSelection
    - SelectTargetAndStart(int yutIndex) — 인덱스 0=도 1=개 2=걸 3=윷 4=모 → 타겟 설정 후 본 게임 시작
    - SelectTargetDo()/Gae()/Geol()/Yut()/Mo() — 파라미터 없는 개별 버전
    - RollYut() — 윷 던지기, ResetRound() — 라운드 리셋 후 타겟 선택 화면으로 복귀
  * 게임 플로우: GameState.Start(시작 화면) → TargetSelection(타겟 선택) → Playing → RoundEnd → ResetRound로 타겟 선택 복귀. GameState enum에 Start 추가됨
  * Awake()가 startPanel ON / targetSelectPanel OFF 초기 상태를 보장 (씬에서 패널 상태를 잘못 저장해도 안전)
- SampleScene.unity: YutGameManager 보유 오브젝트명 GameObject → GameManager로 정리, 씬 직렬화 maxThrows 7→5, 구형 Button (Legacy) 비활성화, 직렬화 필드명 갱신(rollResultText→mainResultText 등 — 기존 Text 오브젝트 연결 유지)
- 규칙 변경 (GAME_RULES.md 7절): 피버 배수 상한 8 확정, 타겟 적중 보너스 턴 + 콤보 배수 '검토 중'→'확정', 화면 구성·플로우 합의 기록
- 협업 규칙 추가 (AGENTS.md): 문서 갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더 의무화
- 기타: Assets/Scripts/ 루트의 0바이트 stray YutGameManager.cs 삭제 (실제 파일은 Scripts/Core/에 있음)
- Git: develop 브랜치 커밋 후 origin push 완료

오후 작업 시 참고:
- 현재 코드는 UI 텍스트/패널이 인스펙터에 연결돼야 화면에 표시됨 (연결 전도 null-safe로 오류 없이 동작). 사용자가 에디터에서 UI 배치·연결 진행 중.
- 남은 미확정 규칙(점수·배율 값, 방울/부적 상세, 중단 시스템)은 GAME_RULES.md 참조.
- 코드로 UI를 동적 생성하는 방식은 폐기됨 — 다시 추가하지 말 것.
```

## 현재 상태

- 마지막 작업 일시: 2026-10-14 오전
- 작업 담당 AI: Devin Desktop (Cognition)
- Git 커밋: 본 문서를 포함한 커밋 — `git log -1` 참조 (직전: `0ddd3b7`)
- 현재 브랜치: `develop` (origin: https://github.com/Lee-jinuk/Ryy.git)

### [2026-10-14 오전 - Gemini & Devin 작업분]

- `activeInputHandler` 1→2 (Both) — 입력 런타임 예외 해결
- `maxThrows` 5 확정 반영 (스크립트 + 씬 직렬화 값 모두)
- 피버 배수 상한 8 (`feverCap` 필드, `appliedFever = Mathf.Min(feverCombo, feverCap)`)
- UI 방식 전환: 코드 동적 생성 폐기 → 인스펙터 수동 연결 방식 (사용자가 에디터에서 직접 배치)
  - public 변수: Text 6종(mainResult/basic/mult/total/throwCount/targetYut), 패널 2종(start/targetSelect), 슬롯 배열 2종(bellSlots[3], talismanSlots[4])
  - public 함수: `StartGame`, `SelectTargetAndStart(int)` + 개별 5종, `RollYut`, `ResetRound`
  - 플로우: Start → TargetSelection → Playing → RoundEnd, `GameState.Start` 추가
- `GameManager` 오브젝트 정리, 씬 직렬화 필드명 갱신, stray 스크립트 삭제, 세션 헤더 문서 규칙 추가

### [2026-10-14 - 초기 작업분]

- 로컬 Git 초기화, Unity `.gitignore`, `main` 초기 커밋, `develop` 분리
- 폴더 구조화: `Assets/Scripts/{Core,Data,UI}`, `Assets/Prefabs`, `Assets/ScriptableObjects`
- `YutGameManager.cs` → `Scripts/Core/` 이동 (.meta GUID 유지)
- 단일 타겟 피버 시스템 구현 (당시 지시 기준 — 이후 '검토 중' 분류를 거쳐 상한 8 포함 채택 확정)
- GitHub origin 연결, `main`/`develop` push 완료
- docs/ 협업 문서 체계 구축, `AI_CONTEXT.md` → docs 이관 완료

## 현재 문제점

- ~~`maxThrows = 7` ↔ 확정 규칙 5회 불일치~~ → 해결
- ~~피버 시스템 채택 여부 미결~~ → 해결 (상한 8 포함 채택, 밸런스 수치는 계속 검토)
- ~~`Input.GetKeyDown` 런타임 예외~~ → 해결 (Both 입력 핸들러)
- ~~`YutGameManager` 씬 미배치 / UI 미연결~~ → 해결 (씬 배치 확인 + 텍스트 필드 연결 유지)
- **UI 수동 연결 대기 (사용자 작업)**: 사용자가 에디터에서 startPanel/targetSelectPanel/버튼/슬롯 오브젝트를 만들어 Inspector에 연결할 예정. 연결 전까지는 텍스트가 비어 보이지만 코드는 null-safe로 정상 동작함.

## 미확정 결정 사항 (사용자 승인 필요)

- 코드의 임의 점수/배율/목표 점수 값 채택 여부 (`GAME_RULES.md` 5절 전부 미확정: 도10~모50, 배수+1~+5, 목표 1000)
- 비타겟/백도 페널티 수치 감각 (`GAME_RULES.md` 7절 '검토 중')
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. 사용자가 에디터에서 UI 배치·인스펙터 연결 완료 후 플레이 테스트 → 피버 상한 8·배율 밸런스 감각 보고
2. `GAME_RULES.md` 5절 점수 계산 규칙 확정 → 코드 임의값 반영 여부 결정
3. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기획 확정 후 착수. `bellSlots`/`talismanSlots` 배열은 이미 준비됨
4. `feverCap`, `roundTargetScore` 등은 Inspector에서 직렬화 필드로 즉시 조정 가능

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs` (게임 로직 + UI 자동 생성)
- `Assets/Scenes/SampleScene.unity` (`GameManager` 오브젝트에 매니저 컴포넌트 배치됨)
- `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2`)
- `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
