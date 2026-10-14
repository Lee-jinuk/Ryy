# DEVELOPMENT_LOG — RYY

실제 Unity 프로젝트를 검사해 기록한 구현 현황. 코드를 확인하지 않은 기능은 구현 완료로 기록하지 않는다.

## 파일 구조 (2026-10-14 검사)

- `Assets/Scripts/Core/YutGameManager.cs` — 유일한 게임 로직 스크립트
- `Assets/Scripts/Data/`, `Assets/Scripts/UI/` — `.gitkeep`만 존재 (빈 폴더)
- `Assets/Prefabs/`, `Assets/ScriptableObjects/` — `.gitkeep`만 존재
- `Assets/Scenes/SampleScene.unity` — Canvas, CalcText, TargetText, result, Text (Legacy), Button (Legacy), EventSystem 오브젝트 존재 확인

### [2026-10-14 오전 - Gemini & Devin 작업분]

- `ProjectSettings/ProjectSettings.asset`: `activeInputHandler` 1→2 (Both) 변경 — `Input.GetKeyDown` 런타임 예외 해결
- `Assets/Scripts/Core/YutGameManager.cs`:
  - `maxThrows` 7→5 (확정 규칙 반영)
  - `feverCap = 8` 필드 추가, 2콤보 이상 곱연산에 `Mathf.Min(feverCombo, feverCap)` 적용 (피버 보상 상한)
  - `Awake()`에서 EventSystem/Canvas 존재 확인 후 UI 자동 구성: 도·개·걸·윷·모 타겟 버튼 5개, 윷 던지기, 라운드 리셋 버튼 + 텍스트 5종 자동 생성·배치·`onClick.AddListener` 바인딩. 인스펙터 수동 연결 없이 Play 즉시 마우스 플레이 가능
  - 상태별 UI 제어: TargetSelection에서만 타겟 버튼 표시, Playing에서만 던지기 버튼 활성화
- `SampleScene.unity`: `YutGameManager`가 붙은 오브젝트명 `GameObject`→`GameManager` 정리, 직렬화된 `maxThrows` 7→5, 위치 (1199.9, 347.7)→(0,0) 리셋, 구형 `Button (Legacy)` 오브젝트 비활성화 (자동 생성 버튼으로 대체)
- `Assets/Scripts/YutGameManager.cs` 0바이트 stray 파일 + .meta 삭제 (실제 스크립트는 `Scripts/Core/`에 존재)
- 협업 규칙 추가: 문서 갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더 사용 (`AGENTS.md`)
- `YutGameManager.cs` 2차 리팩토링 — 코드 동적 UI 생성 방식 폐기, 인스펙터 수동 연결 방식으로 전환:
  - public UI 변수: `mainResultText`, `basicScoreText`, `multScoreText`, `totalScoreText`, `throwCountText`, `targetYutText` (Text 6종)
  - public 패널 변수: `startPanel`, `targetSelectPanel` (GameObject)
  - public 슬롯 배열: `bellSlots[3]` (방울), `talismanSlots[4]` (부적) — 표시 자리만 준비, 로직 미구현
  - public 버튼 함수: `StartGame()`, `SelectTargetAndStart(int yutIndex)` (0=도 1=개 2=걸 3=윷 4=모), 개별 함수 `SelectTargetDo/Gae/Geol/Yut/Mo()`, `RollYut()`, `ResetRound()`
  - 게임 플로우: `GameState.Start`(시작 화면) → `StartGame()` → `TargetSelection`(타겟 선택 화면) → `SelectTargetAndStart()` → `Playing` → 5회 소진 시 `RoundEnd` → `ResetRound()`로 타겟 선택 복귀
  - `GameState` enum에 `Start` 상태 추가, `Awake()`에서 startPanel ON / targetSelectPanel OFF 초기 상태 보장
- `SampleScene.unity`: 씬 직렬화 필드명 갱신 (`rollResultText`→`mainResultText`, `roundText`→`throwCountText`, `calcText`→`basicScoreText`, `multScoreText` 추가) — 기존 Text 오브젝트 연결 유지됨
- 패널 전환 버그 진단 강화 (`StartGame()` 리포트 대응):
  - `targetSelectPanel` 미연결 / `startPanel` 미연결 시 `Debug.LogWarning`으로 명시 (기존엔 무음 실패)
  - 패널을 켰는데 `activeInHierarchy`가 false면 "부모가 비활성" 경고 출력 (startPanel 자식으로 중첩된 경우 탐지)
  - `Awake()`의 `WarnIfPanelNested()`: targetSelectPanel이 startPanel 자식(또는 같은 오브젝트)이면 시작 시 경고
  - `StartGame()`에서 타겟 패널 ON → 시작 패널 OFF 순서로 변경

### [2026-10-14 오후 - GPT 기획 / Devin 구현]

**GPT 요청 내용**: 타겟 선택 화면을 전체 화면 전환 방식에서 "메인 게임 화면 위에 뜨는 모달 팝업" 방식으로 변경. 시작 화면 → [게임 시작] → 메인 화면 노출 + 어두운 반투명 딤 + 중앙 타겟 선택 팝업 → 선택 시 오버레이만 사라지고 본 게임 진행. 블러는 복잡해지면 반투명 딤으로 대체하고 추후 작업으로 기록.

**Devin 구현 내용** (`YutGameManager.cs` 코드만 — 씬/오브젝트는 사용자가 에디터에서 구성):

- public 필드 추가: `gamePanel` (메인 게임 화면), `targetSelectOverlay` (딤 배경 + 입력 차단 오버레이). `targetSelectPanel`은 overlay 자식으로 두는 구조를 전제로 선택적 연결 필드로 유지
- `StartGame()`: `gamePanel` ON → `targetSelectOverlay` ON → `targetSelectPanel` ON → `startPanel` OFF. 상태 `TargetSelection`
- `ConfirmTargetSelection()`/`SelectTargetAndStart()`: overlay + panel OFF, `gamePanel`은 유지, 상태 `Playing`
- `ResetRound()`: overlay + panel 다시 ON → 타겟 재선택 흐름
- `Awake()`: `startPanel` ON / `gamePanel`·overlay·panel OFF 초기 상태
- `ActivatePanel()` 헬퍼: 켜야 할 패널 미연결·부모 비활성 시 `LogWarning` 진단 유지
- `WarnIfPanelNested()` 확장: overlay/panel이 startPanel 자식이면 경고
- 블러 미구현 — 기본 UI Image 반투명 딤 방식 사용 (추후 작업)

**추가 구현 — 윷 결과 7종 확장 + 저주 계열** (`YutGameManager.cs`):

- `YutResult` enum 6종 → 7종 (`Nak` 추가, enum 맨 끝에 추가해 기존 값 유지)
- 저주 판별: `public static bool IsCurse(YutResult)` — 백도·낙을 저주 계열로 구분. `RollYut`에서 `IsCurse` 분기 → `HandleCurseResult()`로 일반/저주 처리 명확히 분리
- 백도 보스 분기 (`HandleCurseResult`): `bossIndex ≤ 5` → 베이직 점수 차감(`backDoPenalty`, 0 하한 보장) / `≥ 6` → 점수 풀 초기화(기존 동작). 둘 다 투척 1회 소모
- 낙: 획득 점수 0, 투척 1회 소모, 누적 점수 유지
- 확률 시스템 재설계: 인스펙터 편집용 `List<YutProbabilityEntry> baseProbabilities`(기본 데이터) → `Awake`에서 `runtimeWeights` 딕셔너리로 복사해 추첨. 런타임 조작은 `runtimeWeights`만 건드려 기본 데이터 오염 방지
  - `ModifyProbability(result, deltaPercent)` — 보스/방울/주술 조작용 public API (음수 → 0 보정)
  - `ResetProbabilities()` — 기본값으로 재구축
  - `ValidateWeights()` — 합계 ≠ 100% 시 `LogWarning`, 추첨은 가중치 정규화로 항상 동작
- 중앙 결과 텍스트: 백도(보스 등급별 차감/초기화 메시지)·낙("낙… 점수 획득 없음") 표시 추가
- **구현 판단 사항 (기획 미명시, 검토 필요)**: 저주 결과 발생 시 `feverCombo = 0` 리셋 적용 — "비타겟은 콤보 초기화" 기존 규칙과 정합성 있게 해석
- **테스트값(확정 아님)**: 개 32.5%, 낙 5% (확률), `bossIndex = 1`, `backDoPenalty = 30` — 모두 Inspector에서 즉시 조정 가능
- 미구현(구조만 준비): 보스 시스템 본체(`bossIndex` 필드만 존재), 저주 아이템 4종 — `IsCurse`/`ModifyProbability`로 연결 가능

**추가 구현 — UI 텍스트 분리 + 던지기 버튼 표시 조건** (GPT 기획, 코드만 — 씬 미수정):

- GPT 요청: 이미지에 문구가 포함된 UI이므로 코드는 숫자/이름만 출력, 투척 횟수 이원화, 던지기 버튼 상태별 표시 제어
- `throwCountText` 필드 제거 → `remainText`(남은 횟수 숫자) + `throwDataText`(전체 횟수 숫자)로 완전 분리. `투척:`/`/` 등 문자열 미출력
- `basicScoreText`/`multScoreText` → 숫자만 출력 (`베이직:`/`배수:`/`x` 제거)
- `targetYutText` → 타겟 윷 이름만 출력 (`타겟:`/피버 표기 제거), 타겟 미확정 상태(Start/TargetSelection)는 빈 문자열
- `public GameObject rollButton` 필드 추가 — `UpdateUI()`에서 `currentState == Playing`일 때만 `SetActive(true)`. Start/TargetSelection/RoundEnd(및 향후 BossSelection)에서는 자동 숨김. GamePanel이 모달 뒤에 표시돼도 버튼 오브젝트만 개별 제어
- 게임 로직·점수 계산·피버·확률·패널 구조 변경 없음. UI 동적 생성 없음. Legacy Text 유지
- **Inspector 재연결 필요**: `Throw Count Text` 필드 제거됨 → 기존 텍스트는 `Remain Text`에, 전체 횟수 표시용 텍스트는 `Throw Data Text`에 연결. `Roll Button` 필드에 던지기 버튼 오브젝트 연결
- 참고: 피버 콤보 수치의 텍스트 표시는 제거됨 (`feverCombo` 필드·Debug.Log는 유지 — 표시용 텍스트 필요 시 별도 필드 추가)

**사용자 에디터 작업 필요** (권장 Hierarchy): `Canvas > GamePanel / TargetSelectOverlay(전체화면 Image, 알파 딤, Raycast Target ON으로 뒤쪽 클릭 차단) > TargetSelectPanel(중앙 팝업 + Title + 도개걸윷모 버튼)`, `StartPanel`은 최상단 순서 유지. GameManager Inspector에 `gamePanel`·`targetSelectOverlay`·`targetSelectPanel` 연결. UI 렌더 순서는 자식 순서(나중=위)이므로 Overlay가 GamePanel보다 아래(나중)에 위치해야 함.

**추가 확인 (사용자 리포트)**: 타겟 버튼 무반응 원인은 코드가 아닌 Inspector OnClick 미연결이었음 — 사용자 연결 후 정상 작동 확인. `SelectTargetDo/Gae/Geol/Yut/Mo()` 등 타겟 선택 함수는 수정 없이 유지.

**기획 원칙 기록 (구현 없음)**: UI 확장성 가이드 — 기준 1920×1080, Canvas Scaler Scale With Screen Size/Match 0.5, 앵커 기반 리사이즈, 배경·HUD 스케일 분리, 21:9/20:9 가로 지향, 세로 미구현 (상세: `AI_HANDOFF.md`)

**테스트/남은 문제**: Devin은 에디터 미실행 — 사용자 연결 후 Play 테스트 필요. 블러 효과는 추후 작업.

## 구현된 기능 (코드 확인 완료)

- `YutResult` enum 7종 (BackDo + Nak 포함, 2026-10-14 오후 확장)
- 인스펙터 편집 확률 리스트 `baseProbabilities` → 런타임 `runtimeWeights` 분리 구조 (테스트값: 백도 6.25 / 도 18.75 / 개 32.5 / 걸 25 / 윷 6.25 / 모 6.25 / 낙 5 %)
- 단일 타겟 선택: `selectedTarget`, `GameState { TargetSelection, Playing, RoundEnd }`
- 피버 콤보: 타겟 적중 시 `feverCombo++`, 배수 지수 합산, `feverCombo >= 2`부터 `currentMultiplier *= feverCombo` (X-Mult)
- 점수: 도10/개20/걸30/윷40/모50, 배수 지수 +1~+5, `baseScorePool * currentMultiplier`
- 라운드 종료 판정: `roundTargetScore = 1000` 기준 클리어/게임오버
- UI Text 필드 5개 (rollResultText, roundText, calcText, totalScoreText, targetYutText)
- `ConfirmTargetSelection()`, `ResetRound()` public (Button OnClick 연결용)
- 스페이스바 테스트 (`Update` 내 `Input.GetKeyDown`)
- (오전 2차 반영) `GameState.Start` 상태, `StartGame()`/`SelectTargetAndStart(int)`/`SelectTarget도~Mo()` 버튼 함수, 인스펙터 연결용 UI 변수 6종 + 패널 2종 + 슬롯 배열 2종

## 기획 문서와의 차이점 (보고 — 미수정)

`GAME_RULES.md` 기준으로 코드와 충돌하는 항목. 규칙상 임의로 수정하지 않고 보고한다.

- **투척 횟수**: ~~코드 `maxThrows = 7` ↔ 확정 규칙 5회~~ → 해결 (2026-10-14 오전, 코드·씬 모두 5로 수정)
- **타겟/피버 시스템**: 사용자 승인으로 채택 확정 + 피버 배수 상한 8 적용 (2026-10-14 오전). 밸런스 수치는 계속 검토 대상.
- **점수·배율·목표 점수**: 전부 미확정 규칙이나 코드에 임의값으로 구현됨 (도10~모50, 배수+1~+5, 목표 1000).
- **`Update()` 입력**: ~~`activeInputHandler: 1` 환경에서 `Input.GetKeyDown` 런타임 예외~~ → 해결 (2026-10-14 오전, `activeInputHandler: 2` Both로 변경)

## 미구현 기능

- 방울 덱빌딩 (덱 12개, 6개 제시 → 3개 선택, 상점 구매/제거) — 전부 미구현
- 부적 시스템 — 미구현
- 판(여러 라운드) 진행 구조 — 미구현
- 중단/점수 확정 시스템 — 미구현
- ~~씬 연동: `YutGameManager` 미배치~~ → 해결 (2026-10-14 오전). `GameManager` 오브젝트에 컴포넌트 존재 확인.
- **사용자 수동 배치 대기**: UI 레이아웃은 사용자가 에디터에서 직접 구성. 패널/버튼/슬롯 오브젝트 생성 후 Inspector 연결 필요 (연결 전까지 해당 텍스트는 빈 상태 — 코드는 null-safe로 정상 동작).

## 로그

| 날짜 | 작업 | 비고 |
|---|---|---|
| 2026-10-14 | 로컬 Git 세팅, Unity .gitignore, 폴더 구조화, 단일 타겟 피버 시스템 구현, GitHub 연결 및 main/develop push | 피버 시스템은 이후 기획상 '검토 중'으로 재분류됨 |
| 2026-10-14 | docs/ 문서 체계 구축, AI_CONTEXT.md 이관 | 코드 변경 없음 |
| 2026-10-14 오전 | 입력 핸들러 Both 전환, maxThrows 5, 피버 상한 8, 씬 정리 | 이후 동적 UI 방식 폐기 |
| 2026-10-14 오전 | 동적 UI 생성 폐기 → 인스펙터 연결용 변수·버튼 함수로 리팩토링, 화면 플로우(시작→타겟선택→본게임) 구현 | UI 배치·연결은 사용자가 에디터에서 수동 작업 예정 |
| 2026-10-14 오후 | 타겟 선택을 모달 팝업 방식으로 전환 (`gamePanel`/`targetSelectOverlay` 필드, 딤 오버레이 토글 로직) | GPT 기획 / Devin 코드 구현, 블러는 추후 |
| 2026-10-14 오후 | 윷 결과 7종 확장(낙 추가), 저주 계열 구조(`IsCurse`/`HandleCurseResult`), 확률 시스템 재설계(인스펙터 편집 + 런타임 가중치 분리), 백도 보스 분기 | GPT 기획 / Devin 구현. 확률·차감량·bossIndex는 테스트값 |
