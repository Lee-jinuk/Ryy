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
- 추가 수정 (사용자 요청): `totalScoreText`도 라벨 제거 → 총점 숫자만 출력, 신규 `goalScoreText`(목표 점수 숫자) 필드 추가로 점수판 완전 분리. 기존 `totalScoreText` 연결은 유지됨, `goalScoreText`만 신규 연결 필요

**사용자 에디터 작업 필요** (권장 Hierarchy): `Canvas > GamePanel / TargetSelectOverlay(전체화면 Image, 알파 딤, Raycast Target ON으로 뒤쪽 클릭 차단) > TargetSelectPanel(중앙 팝업 + Title + 도개걸윷모 버튼)`, `StartPanel`은 최상단 순서 유지. GameManager Inspector에 `gamePanel`·`targetSelectOverlay`·`targetSelectPanel` 연결. UI 렌더 순서는 자식 순서(나중=위)이므로 Overlay가 GamePanel보다 아래(나중)에 위치해야 함.

**추가 확인 (사용자 리포트)**: 타겟 버튼 무반응 원인은 코드가 아닌 Inspector OnClick 미연결이었음 — 사용자 연결 후 정상 작동 확인. `SelectTargetDo/Gae/Geol/Yut/Mo()` 등 타겟 선택 함수는 수정 없이 유지.

**기획 원칙 기록 (구현 없음)**: UI 확장성 가이드 — 기준 1920×1080, Canvas Scaler Scale With Screen Size/Match 0.5, 앵커 기반 리사이즈, 배경·HUD 스케일 분리, 21:9/20:9 가로 지향, 세로 미구현 (상세: `AI_HANDOFF.md`)

**추가 구현 — 투척별 점수 정산 + 효과 단계 기록 + 연출 재생 구조** (GPT 기획 / Devin 구현, `YutGameManager.cs`):

- GPT 요청: 투척마다 독립 정산(`ThrowScore = Basic × Mult`) → `TotalScore` 1회 합산, 부적·방울·주술·피버 효과 단계를 기록해 순차 연출 가능한 구조, MainResult 단순화, 새 Text 필드 2종 (`ThrowScoreText`/`FeverText`)
- 점수 모델 교체: `baseScorePool`/`currentMultiplier`(누적 풀×배율) 폐기 → 투척 표시용 임시값 `basicScore`/`multiplier` + `throwScore` + 누적 `totalScore`. 투척마다 임시값 초기화, 이전 투척 수치 미누적
- 윷 스탯 테이블: `List<YutStatEntry> baseYutStats` (도 10×2 / 개 20×3 / 걸 30×4 / 윷 40×5 / 모 50×6) → 런타임 `yutStats` 딕셔너리 복사. `UpgradeYutStat(result, basic, mult)` API — 주술 영구 강화용, 기본 데이터 오염 없음. 기존 타겟 적중 +1~+5 배수 보너스 방식 폐기
- 효과 기록 구조: `ScoreEffectStep` (원인/식별자/표시명/대상 Basic·Multiplier/연산 Add·Multiply/연산값/전·후 수치/증가량/순서) + `ThrowSettlement` (결과/초기·최종 베이직·멀티플/확정 투척점수/콤보/단계 목록). ×2와 +2 같은 연산 차이는 `op`+`opValue`+`delta`로 구분 기록. `currentSettlement` 공개로 인스펙터 디버그 가능
- 피버: 타겟 적중 시 `feverCombo++`, 2연속부터 `finalMult × Min(feverCombo, feverCap)`을 피버 단계(`EffectSourceType.Fever`)로 기록. **구현 판단 사항 (검토 필요)**: 구 기준은 '전체 누적 배율 ×콤보'였으나 투척 정산 구조로 이식하면서 '이번 투척 멀티플 ×콤보'로 적용 — 산출 차이 있으므로 확정 필요
- 정산-연출 분리: `SettleThrow()`에서 점수 즉시 확정(토탈 1회 합산) → `PresentSettlement()` 코루틴이 기록을 순서대로 재생(결과 표시 → 초기 베이직·멀티플 → 효과 단계 → 투척점수). `effectStepDelay = 0.35f` 테스트값
- 중복 방지: 연출 중 `isPresenting`으로 RollYut 무시 + rollButton 숨김. `SkipPresentation()` — 스킵해도 동일 결과 표시(점수는 이미 확정). `ResetRound()`는 진행 중 코루틴 정지 후 초기화
- `mainResultText`: 윷 결과 이름만 표시 (도·개·걸·윷·모·낙·백도). Start/TargetSelection에서는 오브젝트 비활성, Playing 진입 시 빈칸, 라운드/타겟 변경 시 초기화 — 안내 문구 전부 제거
- `feverText`: `FeverLabel()` — 0빈칸/1 Hit/2 피버/3+ 피버N 표시
- 백도 페널티 기준 변경: `baseScorePool` → `totalScore` 차감/초기화 (이전 판 확정 점수 불침범 규칙 유지 — 확정 점수 개념은 아직 미구현)
- 신규 Inspector 필드: `throwScoreText`, `feverText`, `baseYutStats`, `effectStepDelay`, `currentSettlement` — 사용자가 `ThrowScoreText`/`FeverText` Text 오브젝트 생성·연결 필요 (미연결 시 null-safe 동작)
- 삭제 필드: `baseScorePool`, `currentMultiplier`, `ResultMessage()`, `BaseScore()`, `MultiplierBonus()` — 씬에 남은 구형 직렬화 키는 무시됨
- 검증 결과 (코드 시뮬레이션): 도→10×2=20, 이후 개→20×3=60, 토탈 80 — 임시값 미누적 확인. 피버 1회 Hit/2회 피버/3회 피버3 표시 확인. Devin은 에디터 미실행 — 사용자 Play 테스트 필요

**추가 구현 — 보스전 5종 + 클리어/게임오버 + 정산 연출 완성** (GPT 기획 / Devin 구현, `YutGameManager.cs`):

- GPT 요청: 보스 5명 데이터·선택, 보스 HP=목표 점수 + HP 바, 낙 +2%p 공통 특수능력, 투척별 순차 정산 연출, 클리어·게임오버 패널과 복귀 흐름, 관문당 3판 구조
- 보스 데이터: `[Serializable] BossData`(이름/최대HP/능력설명/`nakBonusPercent`) + `List<BossData> bosses` 5명 (첫~다섯 번째 저승지기, HP 1000~5000 — 전부 임시값). 보스 번호 = `selectedBossIndex + 1`로 백도 1~5/6~10 분기에 사용 (기존 `bossIndex` 필드 폐기)
- 보스 선택: `SelectBoss(int)` + `SelectBoss1()`~`SelectBoss5()` — Start 상태에서만 동작(전투 중 변경 방지). 선택 시 목표 점수·이름·능력 미리보기 표시
- 보스 특수능력: `ApplyBossAbility()` — `runtimeWeights`에 낙 +2%p(퍼센트포인트), 증가분을 일반 5종에서 상대 비중 유지하며 비례 차감, 백도 불변, 음수 보정. `BeginBossFight()`에서 `BuildWeights()` 후 적용 → 재도전 시 중복 누적 없음. `BackToBossSelect()`는 `ResetProbabilities()`로 효과 제거
- 판 구조: `roundsPerBoss = 3`, `roundIndex`, `roundScore`(이번 판 누적 — 백도 차감 범위로 실체화, 이전 판 확정분 불침범). `totalScore`는 보스전 전체 누적(판 넘어 유지) = 보스 HP와 대응
- 상태 전환: `GameState.Cleared`/`GameOver` 추가 (RoundEnd 폐기). `FinishPresentation` 후처리 — `totalScore >= goalScore` → `EnterClear()` / 판 소진 시 `roundIndex >= roundsPerBoss` → `EnterGameOver()`, 아니면 `AdvanceToNextRound()`(판 카운터·콤보 리셋 후 타겟 재선택)
- 클리어/게임오버 패널: `clearPanel`/`gameOverPanel` GameObject + 결과 텍스트 5종(`clearBossNameText`/`clearScoreText`/`gameOverBossNameText`/`gameOverScoreText`/`gameOverGoalText`) — 사용자가 에디터에서 생성·연결. `RetryBoss()`(다시 도전) / `BackToBossSelect()`(보스 선택 복귀) public 제공. 두 패널 상호 배타 표시
- 보스 HP UI: `bossNameText`/`bossHpText`/`bossMaxHpText`/`bossAbilityText` + `bossHpBar`(Image fillAmount) 또는 `bossHpSlider`(Slider value) — 연결된 쪽만 갱신, 둘 다 null-safe. 잔여 HP = Max(0, goalScore − displayTotal)
- 정산 연출 완성: `displayTotal` 도입 — 토탈 텍스트·보스 HP 바는 '토탈 합산' 단계에서만 갱신 (500→740 연출 + HP 바 동기화). 단계: 결과명 → 초기 베이직·멀티플 → 효과 단계별 → 투척 점수 → 토탈+HP. `throwScore` 표시 필드도 '투척 점수 표시' 단계에서만 갱신되도록 수정
- `ResetRound()` = `RetryBoss()`(같은 보스 1판부터 재시작)으로 재정의 — 기존 '현재 라운드 리셋' 의미를 보스전 재시작으로 확장
- 검증 (코드 시뮬레이션): 낙 +2%p 적용 시 일반 5종 합계 88.75→86.75 비례 차감 + 백도 6.25 불변 + 낙 5→7 = 합계 100 유지. 재시작 시 BuildWeights 재구축으로 중복 없음. 연출 중 isPresenting으로 연타 차단. Devin 에디터 미실행 — Play 테스트는 사용자 확인 필요

**추가 구현 — 18칸 원형 말판 + 방울 이벤트** (GPT 기획 / Devin 구현):

- GPT 요청: 점수 획득을 말판 기반으로 변경 (윷 투척→말 이동→칸 점수=베이직→멀티플→효과→정산), 방울 이벤트 칸 + 3개 중 1개 선택, 기존 보스/정산 연출 유지, 말판 로직을 별도 파일로 분리
- 신규 파일 `Assets/Scripts/Core/YutBoard.cs`: `BoardTile`(인덱스/baseScore/bonusScore/hasBellEvent/bellUsedThisRound), `BellData`(이름/설명 — 효과 미구현 테스트용), `YutBoard`(tiles/pieceIndex/totalMoved/lapCount + `MovePiece(steps, path)` — 경유 칸 기록, 마지막 칸 도달·통과 시 완주 판정)
- 말판 배치: `SetupBoard()` — 10~180 중복 없이 Fisher-Yates 셔플, `bellEventTileCount`(기본 2)개 방울 칸 무작위 지정, 말 위치·완주 초기화. `BeginBossFight()`에서만 호출 → 3판 고정, 재도전 시 재배치
- 베이직 변경: `도착 칸 점수 + lapCount×10 + yutStats.basicBonus` — `YutStatEntry.basic`을 `basicBonus`(기본 0)로 개칭해 구형 기본 베이직 10~50 폐기 (씬 구형 직렬화 키는 무시됨)
- 이동 연출: `PresentSettlement`에 말 이동 단계 삽입 — 정산의 `movePath` 경유 칸을 `moveStepDelay`(기본 0.15초) 간격으로 한 칸씩 이동 (`playerPiece.position = tile.position`). 저주는 이동 없음. 판정은 도착 칸만
- 연출 순서: 결과명 → 말 이동 → 도착 칸 베이직 → 윷 멀티플 → 효과 단계 → 투척 점수 → 토탈+보스 HP
- 방울 이벤트: 착지·미사용 칸이면 정산 연출 완료 후 `OpenBellChoice()` — 풀에서 무작위 3개 제시 → `SelectBellOption(n)`+`ConfirmBellChoice()` 또는 원클릭 `SelectBellAndConfirm(n)` → `ownedBells`에 추가 + `bellSlots`에 표시. `isChoosingBell` 중 투척 차단 + rollButton 숨김. 판당 칸별 1회(`bellUsedThisRound`), `AdvanceToNextRound`에서 소멸·리셋. 판이 끝나는 투척에선 이벤트 생략(임시 방울이 소멸하므로)
- 백도·낙 이동 규칙 미확정 — 현재 이동 없음으로 구현, 임의 규칙 적용 안 함 (보고)
- 신규 Inspector 필드: `boardPanel`/`boardTiles[18]`/`playerPiece`/`lapCountText`/`moveStepDelay`/`bellEventTileCount`/`bellPool`/`bellChoicePanel`/`bellOptionNameTexts[3]`/`bellOptionDescTexts[3]`/`ownedBells`. 칸 점수 표시는 타일 자식 Text, 방울 아이콘은 타일의 `BellIcon` 이름 자식으로 자동 인식
- 검증 (코드 시뮬레이션): 완주 판정 `totalMoved >= 17 + 18*lapCount` — 마지막 칸 도달=완주, 도착 후 재출발은 미카운트 확인. Devin 에디터 미실행 — Play 테스트는 사용자 확인 필요

**추가 수정 — 말판 시작점 규칙 + 보스 UI 단순화** (GPT 기획 / Devin 구현):

- 시작점 변경: 별도 시작점(pieceIndex −1) 폐기 → `pieceIndex = 0`(BoardTile_01)에서 시작, 시작 칸도 일반 점수 칸. 첫 도 → BoardTile_02 도착. `BeginBossFight`에서 `MovePieceVisual(0)`로 말을 시작 칸에 배치
- 완주 판정 단순화: `totalMoved >= 18 × (lapCount + 1)` — BoardTile_18→BoardTile_01 경계 통과마다 +1 (기존 '마지막 칸 도달' 판정과 결과 동일, 시작점 온칸 구조에 맞게 정리)
- 보스 UI 단순화: 보스는 HP를 깎는 적이 아닌 목표 점수 관문 — `bossHpText`/`bossMaxHpText`/`bossHpBar`/`bossHpSlider` 필드·HP 갱신 로직 제거. `bossNameText`/`bossAbilityText`만 유지. 목표 점수(`goalScore`)·클리어/게임오버 판정·보스 데이터·낙 +2%p는 변경 없음
- 씬에 남은 구형 직렬화 키(bossHpText 등)는 Unity가 무시 — 해당 필드에 연결돼 있던 참조만 Inspector에서 제거하면 됨

**테스트/남은 문제**: Devin은 에디터 미실행 — 사용자 연결 후 Play 테스트 필요. 블러 효과는 추후 작업. 부적·방울 이미지 진동 등 실제 애니메이션은 아이템 UI 연결 단계에서 확장 (현재는 Text 갱신 + 단계 재생 구조만). 방울 실제 효과·사용 버튼 미구현 — 현재 획득·표시만.

### [2026-10-15 - GPT 기획 / Devin 구현]

**보스 클리어 후 다음 관문 진행** — 기존 구현 확인 결과 클리어 후 이동 기능 없음 → 추가:

- `GoToNextBoss()` public 메서드: Cleared 상태 + 다음 보스 존재 시에만 동작 → `selectedBossIndex++` 후 `StartGame()`(=`BeginBossFight` + `EnterTargetSelection`) 재사용
- 초기화는 기존 `BeginBossFight`/`SetupBoard`가 전부 커버: 총점·판 점수·투척 점수·베이직·멀티플·피버·투척 횟수·말 위치(BoardTile_01)·완주·말판 재배치·방울 칸·임시 방울 + `StopPresentation`으로 잔여 연출 정리 + `BuildWeights` 재구축으로 낙 +2%p 중복 없음. `yutStats` 등 런 전체 성장 데이터는 초기화하지 않음(향후 유지 구조)
- 최종 클리어: `EnterClear`에서 마지막 보스(`selectedBossIndex == bosses.Count-1`) 판정 → `nextBossButton` 숨김 + `clearMessageText`에 "최종 클리어" 표시
- 신규 Inspector 필드: `nextBossButton`(GameObject, OnClick→`GoToNextBoss()`), `clearMessageText`(Text, 선택)
- 기존 `RetryBoss`/`BackToBossSelect`·클리어/게임오버 판정 변경 없음

**테스트/남은 문제**: Devin은 에디터 미실행 — Play 테스트 필요. `nextBossButton` 미연결 시 최종 클리어 구분 메시지만 표시됨.

### [2026-10-16 오후 - GPT 기획 / Devin 구현]

**보스 소개 화면 + 게임오버 전체 초기화 + 판 번호 표시** (`YutGameManager.cs` 코드만 — UI 오브젝트는 사용자가 에디터에서 생성):

- 보스 소개 화면: `bossSelectPanel` + `bossSelectNameText`/`bossSelectGoalText`/`bossSelectAbilityText` 필드 추가. `GoToNextBoss()`는 이제 다음 보스 소개 패널만 표시하고, 신규 `EnterBossBattle()`('진입' 버튼용 public)이 `pendingBossIndex` → `selectedBossIndex` 확정 후 실제 전투 진입. `StartGame()`('게임 시작')도 같은 패널을 열도록 변경해 최초 선택·다음 관문이 같은 UI 공유. 소개 패널이 열린 상태에서 `SelectBoss()` 호출 시 표시 내용 갱신. `RetryBoss()`는 소개 생략하고 즉시 재시작(기존 동작 유지). 내부 공용 `StartBossBattle()` = `BeginBossFight` + `EnterTargetSelection`
- 게임오버 초기화: 신규 `ResetRunState()` — 연출 코루틴·방울 선택 상태·임시 방울·윷 결과 표시·피버·투척/판 카운터·점수 전부·말판(칸 강화·방울 칸·사용 플래그)·말 위치(BoardTile_01)·완주·런 성장(`BuildYutStats`)·확률 효과(`ResetProbabilities`) 초기화. `BackToBossSelect()`가 이를 호출하도록 강화 + '처음으로' 버튼용 `RestartRun()` public 추가 (같은 동작)
- 판 번호: `roundProgressText` 필드 추가 — `UpdateUI()`에서 `"{roundIndex} / {roundsPerBoss}"` 출력, Start 상태 빈칸, `BeginBossFight`/`AdvanceToNextRound`에서 갱신됨
- `WarnIfPanelNested()` 확장: bossSelectPanel이 clearPanel 자식이면 경고
- 스페이스바 테스트: Start에서 소개 패널 열려 있으면 EnterBossBattle, Cleared에서 패널 열려 있으면 EnterBossBattle
- ⚠ 신규 Inspector 연결 필요: `Boss Select Panel` + `Boss Select Name/Goal/Ability Text` + `Round Progress Text` — 전부 미연결도 null-safe

**테스트/남은 문제**: Devin 에디터 미실행 — Play 테스트 필요. 보스 소개 패널이 StartPanel 위를 덮는 모달로 만들면 뒤쪽 보스 버튼 클릭이 차단될 수 있음 (재선택 허용하려면 패널을 옆에 배치하거나 Raycast Target 조정). 보스전 건너뛰기는 규칙 미확정으로 미구현.

## 구현된 기능 (코드 확인 완료)

- `YutResult` enum 7종 (BackDo + Nak 포함, 2026-10-14 오후 확장)
- 인스펙터 편집 확률 리스트 `baseProbabilities` → 런타임 `runtimeWeights` 분리 구조 (테스트값: 백도 6.25 / 도 18.75 / 개 32.5 / 걸 25 / 윷 6.25 / 모 6.25 / 낙 5 %)
- 단일 타겟 선택: `selectedTarget`, `GameState { TargetSelection, Playing, RoundEnd }`
- 18칸 원형 말판(`YutBoard`/`BoardTile` — `YutBoard.cs` 분리): 10~180 무작위 배치(보스전 시작 시), 윷별 1~5칸 이동, 도착 칸 점수=베이직, 완주 ×10 보너스, 말 이동 연출(`moveStepDelay`)
- 방울 이벤트: 방울 칸 착지 → 3개 제시 → 1개 선택 → 판 한정 `ownedBells` + `bellSlots` 표시 (실제 효과 미구현)
- 투척별 독립 정산: `ThrowScore = BasicScore × Multiplier` → `TotalScore` 1회 합산 (투척 임시값 매 투척 초기화)
- 윷 스탯: 멀티플 도×2~모×6 + `basicBonus`(칸 점수에 가산되는 윷별 강화치, 기본 0) — `baseYutStats` → 런타임 `yutStats` + `UpgradeYutStat()` 영구 강화 API
- 효과 단계 기록·재생: `ScoreEffectStep`/`ThrowSettlement` — 부적·방울·주술·피버 순차 발동 연출 구조 (정산 확정 후 코루틴 재생, 스킵 가능)
- 피버 콤보: 타겟 적중 시 `feverCombo++`, 2연속부터 이번 투척 멀티플 ×Min(콤보, 8) — 피버 단계로 기록 (이식 수식, 확정 검토 필요)
- 보스전 구조: `BossData` 5명(임시)/보스 선택(`SelectBoss1~5`)/목표=보스 HP/낙 +2%p 특수능력(비례 차감·백도 불변·중복 없음)/관문당 3판(`roundsPerBoss`)×판당 5회/`roundScore` 판별 누적(백도 범위)/`Cleared`·`GameOver` 상태 + 클리어·게임오버 패널 + `RetryBoss`/`BackToBossSelect`
- 보스 UI 연결 필드: `bossNameText`/`bossAbilityText` (HP 텍스트·체력바 필드는 목표 점수 관문 전환으로 제거됨)
- 관문 진행: `GoToNextBoss()`(클리어 패널 '다음 관문' 버튼) → 보스 소개 패널 → `EnterBossBattle()`('진입' 버튼)으로 보스 1→5 수동 진행, 보스 5 클리어 시 `nextBossButton` 숨김 + `clearMessageText` "최종 클리어"
- 보스 소개 화면: `bossSelectPanel` + 이름/목표/능력 텍스트 3종 — 최초 '게임 시작'과 클리어 후 '다음 관문'이 공용 사용. '다시 도전'은 소개 생략
- 게임오버 런 초기화: `ResetRunState()` + `RestartRun()`('처음으로' 버튼) / `BackToBossSelect()` 동일 적용 — 런 전체 데이터 초기화 후 시작 화면 복귀
- 판 번호 표시: `roundProgressText` — `"1 / 3"` 형식
- 라운드 종료 판정: `totalScore >= goalScore`(보스 HP) 클리어 / 3판 소진 미달 게임오버
- UI Text 필드: `mainResultText`(결과명 전용)/`basicScoreText`/`multScoreText`/`totalScoreText`/`goalScoreText`/`remainText`/`throwDataText`/`targetYutText`/`throwScoreText`/`feverText` — 전부 숫자·이름만 출력, null-safe
- `ConfirmTargetSelection()`, `ResetRound()` public (Button OnClick 연결용)
- 스페이스바 테스트 (`Update` 내 `Input.GetKeyDown`)
- (오전 2차 반영) `GameState.Start` 상태, `StartGame()`/`SelectTargetAndStart(int)`/`SelectTarget도~Mo()` 버튼 함수, 인스펙터 연결용 UI 변수 6종 + 패널 2종 + 슬롯 배열 2종

## 기획 문서와의 차이점 (보고 — 미수정)

`GAME_RULES.md` 기준으로 코드와 충돌하는 항목. 규칙상 임의로 수정하지 않고 보고한다.

- **투척 횟수**: ~~코드 `maxThrows = 7` ↔ 확정 규칙 5회~~ → 해결 (2026-10-14 오전, 코드·씬 모두 5로 수정)
- **타겟/피버 시스템**: 사용자 승인으로 채택 확정 + 피버 배수 상한 8 적용 (2026-10-14 오전). 밸런스 수치는 계속 검토 대상.
- **점수·배율·목표 점수**: ~~전부 미확정 규칙이나 코드에 임의값으로 구현됨 (도10~모50, 배수+1~+5, 목표 1000)~~ → 투척 정산 구조·윷 스탯 확정 (2026-10-14 오후, GPT 기획). 목표 1000은 여전히 테스트값.
- **피버 적용식**: 구 기준 '전체 누적 배율 ×콤보' → 투척 정산 구조로 '이번 투척 멀티플 ×Min(콤보,8)' 이식 — 산출 차이 있어 확정 필요.
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
| 2026-10-14 오후 | 투척별 점수 정산 구조 도입 (`ThrowScore`→`TotalScore`), 윷 스탯 테이블 + 영구 강화 API, `ScoreEffectStep`/`ThrowSettlement` 효과 기록·코루틴 재생 구조, MainResult 결과명 전용 + 신규 `throwScoreText`/`feverText` | GPT 기획 / Devin 구현. 피버 ×콤보 이식식·effectStepDelay·백도 차감량은 테스트값/검토 필요 |
| 2026-10-14 오후 | 보스전 5종(임시 저승지기 1~5, HP 1000~5000) + 보스 선택(`SelectBoss`), 낙 +2%p 공통 특수능력, 관문당 3판 구조, 클리어·게임오버 패널 + `RetryBoss`/`BackToBossSelect`, 보스 HP UI(Image/Slider), 정산 연출 단계 완성(`displayTotal` 토탈·HP 동기화) | GPT 기획 / Devin 구현. 보스 이름·HP·능력 수치 전부 테스트값 |
| 2026-10-14 오후 | 18칸 원형 말판(`YutBoard.cs` 분리) — 점수 무작위 배치, 윷별 이동·완주 보너스, 방울 이벤트 칸 + 3택1 임시 방울, 말 이동 연출 | GPT 기획 / Devin 구현. 방울 효과·백도/낙 이동 규칙 미확정, `bellEventTileCount` 2개는 프로토타입값 |
| 2026-10-14 오후 | 말판 시작점을 BoardTile_01로 변경(별도 시작점 폐기), 완주 판정을 순환 통과로 단순화, 보스 HP UI 필드 제거(보스=목표 점수 관문) | GPT 기획 / Devin 구현. 클리어/게임오버·보스 데이터 유지 |
| 2026-10-15 | 보스 클리어 후 다음 관문 진행 — `GoToNextBoss()` + `nextBossButton`/`clearMessageText` 필드, 보스 5 클리어 = 최종 클리어 | GPT 기획 / Devin 구현. 초기화는 기존 BeginBossFight 재사용 |
| 2026-10-16 오후 | 보스 소개 화면(`bossSelectPanel`+텍스트 3종, `EnterBossBattle`), 게임오버 런 전체 초기화(`ResetRunState`/`RestartRun`), 판 번호 표시(`roundProgressText`) | GPT 기획 / Devin 구현. `StartGame`은 소개 화면 표시로 변경, `RetryBoss` 직행 유지 |
