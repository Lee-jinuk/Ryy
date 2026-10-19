# ARCHITECTURE — RYY 매니저 구조와 데이터 소유권

2026-10-18 오전 리팩터링 기준. 기능 분리 원칙과 각 스크립트의 책임·데이터 소유권을 정리한다.

## 설계 원칙

- **Inspector 연결 보존 최우선**: UI 참조·실행 상태 등 직렬화된 public 필드는 `YutGameManager`에 유지한다. 새 매니저는 모두 **일반 C# 클래스(POJO)**이며 `Awake()`에서 생성된다 — 씬·컴포넌트·OnClick 연결에 영향이 없다.
- **설정 데이터는 에셋이 기준 (2026-10-19)**: 부적 정의·보스 목록·밸런스 수치·연출 딜레이는 ScriptableObject 에셋(`Assets/ScriptableObjects/`)에만 저장한다. `YutGameManager`는 에셋 참조 필드를 들고 읽기 프로퍼티로 노출 — Scene에 설정 목록을 중복 저장하지 않는다. 런타임 덱·손패·보스 상성·말판 배치 등 실행 상태는 매니저가 별도 소유.
- **데이터 소유권 단일화**: 각 상태를 쓰는 곳은 책임 매니저 한 곳뿐이다. 필드 저장소는 Inspector 노출을 위해 `YutGameManager`에 두더라도 쓰기 권한은 해당 매니저가 가진다. 다른 스크립트에서 같은 값을 복제·누적하지 않는다.
- **계산과 연출 분리**: 점수·이동은 `ThrowSettlement`에 즉시 확정되고, 화면 연출(`GameUIManager.PresentSettlement`)은 그 기록을 재생만 한다. 스킵해도 같은 결과가 표시된다.
- `YutGameManager`를 삭제·개명하지 않는다. 버튼 OnClick이 호출하는 public 메서드 시그니처는 전부 유지하고 실제 로직만 매니저에 위임한다.

## 파일 구성

| 파일 | 위치 | 역할 |
|---|---|---|
| `YutGameManager.cs` | Scripts/Core | 게임 총괄 — 상태 전환, 실행 순서, 투척→정산 흐름, 관문 진입, OnClick 진입점, 직렬화 필드 저장소 |
| `YutRollManager.cs` | Scripts/Core | 윷 투척 세션 소유·확률 API·윷 스탯 (`YutProbabilityEntry`, `YutStatEntry` 타입 정의 포함) |
| `YutThrowSession.cs` | Scripts/Core | 윷 4개 투척 세션 — 면 조합(앞면 0~4 → 모~윷), 재투척(서로 다른 윷 최대 2개·각 1회 + `bonusRerolls` 세션 한도), `rerollsForfeited` 포기, 확정 상태 |
| `BoardManager.cs` | Scripts/Core | 30칸 속성 말판 생성(런당 1회)·말 이동 연출·말판 UI 갱신 |
| `YutBoard.cs` | Scripts/Core | 말판 데이터 (`YutBoard`, `BoardTile`, `TileAttribute`, `BellData` — UI 참조 없는 순수 데이터) |
| `ScoreManager.cs` | Scripts/Core | 점수 정산·저주 처리 (`ScoreEffectStep`, `ThrowSettlement`, `EffectSourceType/Target/Op` 타입 정의 포함) |
| `BossManager.cs` | Scripts/Core | 보스 데이터 접근·순차 소개·건너뛰기 판별·런타임 보스 상성·보스 말 위치·피해 단계별 이동 (`BossData`, `BossAffinity` 타입 정의 포함) |
| `BellManager.cs` | Scripts/Core | 방울 이벤트 — 땅 칸 부적 대체로 발동 경로 없음 (코드·필드는 호환용 유지) |
| `TalismanData.cs` | Scripts/Data | 부적 카드 정의 자료형 — `TalismanRarity`/`TalismanEffectKind`/`TalismanData` (cardId·이름·설명·레어도·효과·수치·속성) |
| `TalismanDatabase.cs` | Scripts/Data | 부적 정의 에셋 — 카드 목록의 단일 기준 (`ScriptableObjects/TalismanDatabase.asset`) |
| `BossDatabase.cs` | Scripts/Data | 보스 목록 에셋 — 순서·이름·HP·능력 단일 기준 (`ScriptableObjects/BossDatabase.asset`) |
| `GameBalanceConfig.cs` | Scripts/Data | 밸런스 에셋 — 투척 횟수·윷 배율·보스 이동 단계 범위 (`ScriptableObjects/GameBalanceConfig.asset`) |
| `PresentationConfig.cs` | Scripts/Data | 연출 에셋 — 말 이동·정산 단계 딜레이 (`ScriptableObjects/PresentationConfig.asset`) |
| `TalismanManager.cs` | Scripts/Core | 부적 런타임 — 뽑기 덱(`drawPile`, 10종 1장씩·사용 소멸)·손패·선택 창 상태(시작 5택3 / 땅 3택1 공용)·사용 검증(`CanUseNow`)·효과 발동·세션 피해 효과(`sessionEffects`) |
| `GameUIManager.cs` | Scripts/UI | 텍스트 갱신·패널 토글·정산 연출 코루틴 (규칙 계산 없음) |

## 데이터 소유권

| 데이터 | 저장 위치 | 쓰기 담당 |
|---|---|---|
| 투척 세션 `session` (면·재투척·확정) | YutRollManager | YutThrowSession(면 판정) / YutGameManager(세션 시작·재투척·확정 진입점 가드) |
| 보스 상성 `runAffinities` | BossManager | BossManager (`EnsureAffinity` 1회 생성 / `ResetAffinities` 런 리셋) |
| 보스 말 위치 `bossPieceIndex` | BossManager | BossManager (`MoveBossPiece` — 최초 투척 성공 시만 호출, 피해 단계별 `bossMoveStageMin/Max` 추첨, `pendingMoveSeal` 봉쇄부 이동 금지 소모 / `ResetBossPiece` 보스전 시작 START 리셋). 시각 위치는 BoardManager.MoveBossPieceVisual |
| 부적 덱 `drawPile`·손패 `hand`·선택 상태 `isChoosing`/`candidates` | TalismanManager | TalismanManager (`EnsureDeck` 런 1회 셔플 / `OpenStartPick`·`OpenLandPick` / `SelectCandidate`·`ConfirmChoice` / `UseTalisman`) — 사용 카드는 런에서 소멸, 원본 `TalismanData` 불변. 손패 한도 `HandLimit=10` — 모든 획득은 `AcquireCard` 경유, 초과분 런 소멸 |
| '이번 투척' 부적 효과 `sessionEffects` | TalismanManager | TalismanManager(발동 시 추가 / `ClearSessionEffects` — 새 세션 시작·보스전·런 경계) → ScoreManager가 정산에서 조건 검사·적용. 추혼부는 잡기 배율 대체로만 사용(피해 곱셈 루프 제외), `fx.mult` = 최종 피해 배율 |
| 잡기 판정 `caught`/`catchMult` | ThrowSettlement | ScoreManager (확정 정산에서 도착 칸 = bossPieceIndex 비교, ×2 1회 계산 — 연출은 기록 재생) |
| 런타임 확률 가중치 `runtimeWeights` | YutRollManager | YutRollManager (`BuildWeights`/`ModifyProbability`) — 신규 면 조합 투척은 미사용, API 호환용 |
| 런타임 윷 스탯 `yutStats` | YutRollManager | YutRollManager (`BuildYutStats` — 기본값을 **새 객체로 복사**, `UpgradeYutStat`) |
| 말 위치·완주·칸 배치 `board` | YutGameManager.board (Inspector 노출) | YutBoard.MovePiece(데이터 이동) / BoardManager(배치 생성·말 위치 초기화·시각 위치) — 배치(`layoutGenerated`)와 말 위치 초기화 분리 |
| 점수 상태 `totalScore`/`currentThrows` | YutGameManager (Inspector 노출) | ScoreManager만 |
| 표시용 임시값 `basicScore`/`multiplier`/`throwScore`/`displayTotal` | YutGameManager | YutGameManager(투척 시작 초기화) / GameUIManager(연출 재생) |
| 보스 진행 `selectedBossIndex`(전투 중) / `pendingBossIndex`(소개 중) | gm 필드 / BossManager | BossManager |
| 방울 상태 `isChoosingBell`/`currentBellOptions`/`ownedBells` | BellManager / gm(ownedBells) | BellManager |
| 게임 상태 `currentState` | YutGameManager | YutGameManager (상태 전환 메서드) |
| 카드 정의·보스 목록·밸런스 수치·연출 딜레이 | ScriptableObject 에셋 4종 (`talismanDatabase`/`bossDatabase`/`balanceConfig`/`presentationConfig` 참조) | 사용자(에셋 편집만) — 코드는 읽기 전용. 누락 시 `ConfigReady` 가드로 게임 진입 차단. `baseProbabilities`/`backDoPenalty`는 레거시로 씬 직렬화 유지(미사용) |
| 연출 플래그 `isPresenting`/`hasRolled`/`lastResult` | YutGameManager (internal) | YutGameManager / GameUIManager |

## 상태·흐름 요약

`Start → (StartGame → 보스 소개) → EnterBossBattle → Playing → Cleared/GameOver`

- `RollYut()` = 세션 시작(윷 4개 최초 투척 + 이전 세션 부적 효과 해제) + 보스 말 1회 이동(누적 피해 단계 범위 — 봉쇄부로 1회 금지 가능) → `RerollStick(0~3)` 세션 한도까지 → 부적 사용 가능 구간(최초 투척 후~확정 전) → `ConfirmThrow()` = 확정 → `SettleNormalThrow`(칸 점수 × 상성 × 윷 배율 × 잡기 × 부적) → `ThrowSettlement` 확정 → `PresentSettlement`(UI) → `FinishPresentation`에서 클리어/게임오버 판정 → 땅 착지 시 `OpenLandPick`(부적 3택1)
- 보스전 진입(`StartBossBattle`) = `BeginBossFight` + `EnterPlaying` + `OpenStartPick`(부적 5택3 — 선택 완료 전 최초 투척 차단)
- 세션은 일반 결과 5종만 생성 — 저주 경로(`HandleCurseResult`)는 API로 유지하되 현재 도달 불가 (백도·낙 발생 조건 미확정)
- 클리어 판정 `totalScore >= goalScore`(마지막 투척 처치 포함 우선) / 5회 소진 미달 `EnterGameOver` — 라운드·판 구조 폐지 (2026-10-18)
- 저주(백도·낙)는 말 이동·점수 계산 없이 `HandleCurseResult` 고정 처리

## 버튼 OnClick 공용 메서드 (전부 `YutGameManager`에 유지)

`StartGame`, `EnterBossBattle`, `SkipBoss`, `GoToNextBoss`, `RestartRun`, `RollYut`, `RerollStick(1~4)`, `ConfirmThrow`, `SkipPresentation`, `SelectBellOption(1~3)`, `ConfirmBellChoice`, `SelectBellAndConfirm(1~3)`, `SelectTalismanOption(1~5)`, `ConfirmTalismanChoice`, `UseTalisman(1~10)`, `ModifyProbability`, `ResetProbabilities`, `UpgradeYutStat`

## 새 기능 추가 시 배치 기준

- 윷 결과·확률 변경 → `YutRollManager`
- 말판 칸 효과·이동 방식 → `YutBoard`(데이터) + `BoardManager`(표현)
- 점수 공식·효과 발동 → `ScoreManager`의 `SettleNormalThrow` steps 지점 (3)에 `ScoreEffectStep` 추가
- 보스 능력·관문 규칙 → `BossManager` + `BossData` 필드 확장
- 방울 효과 → `BellData` 필드 확장 + `BellManager` + `ScoreManager` steps 연동
- 부적 카드·효과 → `TalismanData`(`effect` enum + 수치) 정의 추가 + `TalismanManager.ApplyEffect` 분기 + 피해 효과는 `ScoreManager` 정산의 부적 단계에 조건 추가
- 화면 표시·연출만 → `GameUIManager`
- `YutGameManager`에 다시 로직을 몰아넣지 않는다 — 상태 전환·흐름 조정만
