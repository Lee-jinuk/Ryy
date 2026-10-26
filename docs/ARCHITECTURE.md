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
| `YutThrowSession.cs` | Scripts/Core | 윷 4개 투척 세션 — 차례 준비에 생성(`rolled=false` = 준비 단계, 부적 사용 가능) → `InitialRoll`은 면만 던짐(준비 효과 보존) → 재투척(기본: 서로 다른 윷 최대 1개·각 1회 + 재기부 `specialRerolls` 특별 기회·`speciallyRerolled` 윷당 1회), `rerollsForfeited` 포기, `moveDelta` 이동 보정, 결과별 부적 `armedResultBonus`/`succeededResultBonus`, 확정 상태 |
| `BoardManager.cs` | Scripts/Core | 30칸 속성 말판 생성(런당 1회)·말 이동 연출·말판 UI 갱신 |
| `YutBoard.cs` | Scripts/Core | 말판 데이터 (`YutBoard`, `BoardTile`, `TileAttribute` — UI 참조 없는 순수 데이터) |
| `ScoreManager.cs` | Scripts/Core | 점수 정산·저주 처리 (`ScoreEffectStep`, `ThrowSettlement`, `EffectSourceType/Target/Op` 타입 정의 포함). 기초 피해(기본점수×윷 배율)와 전체배율을 분리 계산 — `EffectTarget.YutMult`/`TotalMult`로 단계 기록 단위 구분, 런타임 `baseTotalMult` 소유 |
| `BossManager.cs` | Scripts/Core | 보스 데이터 접근·순차 소개·건너뛰기 판별·보스 상성 능력(`useAffinity` 데이터 주도 — 무작위 생성 없음)·보스 말 위치·피해 단계별 이동 (`BossData`, `BossAffinity` 타입 정의 포함) |
| `BellManager.cs` | Scripts/Core | 방울 장착·효과 — 보유 인스턴스(`ownedBells`)·성장 처리(`ApplyGrowth`)·속성 런 성장(`attrAttackBonus` 공격력·`attrMultBonus` 착지 배율)·지속 효과 조회·청개구리 유효 상성 |
| `FortuneUpgradeManager.cs` | Scripts/Core | 복주머니 점괘 강화 — 획득 횟수(`acquiredCounts`)·누적 강화 `(kind,target)` 키의 `cumulativeBonus`(윷 배율은 `YutStatEntry.mult` 가산, 나머지는 조회 시 합산)·선택 대기(`isChoosingFortune`·`pendingCandidates`)·런 초기화. `IsSupported` = 후보·적용 공통 유효성 검사 (종류 + 대상 — YutMult는 도~모, AttrMult는 불만), `SupportedCandidateCount` = 판매 가능 후보 수 |
| `EconomyManager.cs` | Scripts/Core | 냥(화폐) — `money`·`AddMoney`/`TrySpend` 단일 경로·시작 자금 런당 1회·보스 클리어 보상(`GrantBossClearReward` — `clearRewardPaid`로 보스전당 1회, `BeginBossFight`에서 리셋) |
| `ShopManager.cs` | Scripts/Core | 상점 — 진입 조건(Cleared+보스 소개 화면)·상품 후보(보스 구간 유지)·구매/판매 거래·유료 리프레시·연타 차단. 상품은 `ShopOffer` 런타임 자료형(ShopOfferKind enum) — 방울·복주머니 혼합 추첨, 복주머니 구매는 `FortuneUpgradeManager.BeginChoice`와 연결. 복주머니 판매 가능·선택창 구성은 `CanSellFortunePouch`/`FortuneChoiceReady` 공통 판정 — 새로고침과 구매가 같은 기준 |
| `TileEffectManager.cs` | Scripts/Core | 속성 착지 효과 — 불×1.5·물 연속(보스전)·풀 도트(보스전, 차례 시작 틱). `ResolveLandingEffect`는 확정 정산에서만 호출 |
| `BellSlotUI.cs` | Scripts/UI | 방울 고정 슬롯 표시 컴포넌트 — name/desc/status Text + actionButton 명시 참조, `Bind`/`Clear`로 표시·클릭 전달만 (계산 없음) |
| `TalismanData.cs` | Scripts/Data | 부적 카드 정의 자료형 — `TalismanRarity`/`TalismanEffectKind`/`TalismanUseTiming`/`TalismanAppearance`/`TalismanData` (cardId·이름·설명·레어도·효과·수치·속성·`includeInDeck`·`useTiming`·`appearance`) |
| `UsedTalismanRecord.cs` | Scripts/Data | 사용 부적 표시 기록 — useId·cardId·이름·설명·효과·수치·resultNote (표시 전용, 효과 실행과 분리) |
| `TalismanDatabase.cs` | Scripts/Data | 부적 정의 에셋 — 카드 목록의 단일 기준 (`ScriptableObjects/TalismanDatabase.asset`) |
| `BossDatabase.cs` | Scripts/Data | 보스 목록 에셋 — 순서·이름·HP·능력 단일 기준 (`ScriptableObjects/BossDatabase.asset`) |
| `GameBalanceConfig.cs` | Scripts/Data | 밸런스 에셋 — 투척 횟수·윷 배율·보스 이동 단계 범위 (`ScriptableObjects/GameBalanceConfig.asset`) |
| `PresentationConfig.cs` | Scripts/Data | 연출 에셋 — 말 이동·정산 단계 딜레이 + 탐욕부 전용 딜레이(`greedStepPreDelay`/`greedStepPostDelay`) (`ScriptableObjects/PresentationConfig.asset`) |
| `BellData.cs` | Scripts/Data | 방울 정의·보유 자료형 — `BellEffectKind`/`BellData`(bellId·효과·대상·성장량·한도·가격·`effectEnabled`)/`OwnedBell`(instanceId·남은 횟수) |
| `BellDatabase.cs` | Scripts/Data | 방울 정의 에셋 — 12종 단일 기준 (`ScriptableObjects/BellDatabase.asset`) |
| `ShopConfig.cs` | Scripts/Data | 상점·경제 에셋 — 시작 자금·장착 한도·상품 수 (`ScriptableObjects/ShopConfig.asset`) |
| `TalismanManager.cs` | Scripts/Core | 부적 런타임 — 뽑기 덱(`drawPile`, 활성 카드 1장씩·사용 소멸)·손패·선택 창 상태(시작 5택3 / 땅 3택1 공용 + 환위부 ±1 선택 `isChoosingMoveAdjust`)·사용 검증(`CanUseCard` = 카드 `useTiming`별 구간: `BeforeFirstThrow` 준비 단계 / `AfterRollBeforeConfirm` 투척 후~확정 전)·효과 발동·세션 피해 효과(`sessionEffects`) |
| `GameUIManager.cs` | Scripts/UI | 텍스트 갱신·패널 토글·정산 연출 코루틴·사용 부적 패널·배율 이유 표시 (규칙 계산 없음) |

## 데이터 소유권

| 데이터 | 저장 위치 | 쓰기 담당 |
|---|---|---|
| 투척 세션 `session` (면·재투척·확정·준비 효과) | YutRollManager | YutGameManager(`PrepareTurn` 차례 준비에 생성 — 보스전 시작·정산 종료 후 / `RollYut` 최초 투척·`RerollStick`·`ConfirmThrow` 가드) / YutThrowSession(면 판정·기본/특별 재투척 소비·결과별 부적 평가) — 준비 단계에 쓴 부적 효과는 세션 필드 보관, `InitialRoll`이 지우지 않음 |
| 세션 부적 상태 `specialRerolls`/`moveDelta`/`armed·succeededResultBonus`/`rerollsForfeited` | YutThrowSession | TalismanManager(준비 단계 카드 사용 시 설정) / YutThrowSession(재투척 평가·소비) / ScoreManager(정산에서 moveDelta·resultBonus 적용) — 최초 투척을 넘겨 유지, 다음 차례 세션 생성으로 해제 |
| 보스 상성 — `BossData.useAffinity`+`affinityWeak`/`Strong`/`Mult` (런타임 상태 없음 — 데이터 주도) | BossData (BossManager 조회) | BossManager (`HasAffinityAbility` 활성 판정 / `GetAffinity` null=비상성 / `AffinityMultiplier`) — 천운부·재복방울·청개구리방울도 같은 경로 |
| 물 연속 `waterComboCount`·풀 도트 `forestDot`/`forestDotActive` (보스전 단위) | TileEffectManager | TileEffectManager (`ResolveLandingEffect` 확정 정산에서만 갱신 / `ApplyTurnStartDot` 차례 시작 틱 / `ResetForBoss` 보스 전환·런 리셋) |
| 보스 말 위치 `bossPieceIndex`·이동 계획 `movePlanReady`/`plannedBaseSteps` | BossManager | BossManager (`PrepareBossMove` — 차례 준비(PrepareTurn)에 거리 1회 추첨·보관 / `PlannedEffectiveSteps`·`PlannedDestination` — 족쇄방울·봉쇄부를 계산 시점에 반영, 봉쇄 미소모 / `MoveBossPiece` — 최초 투척 성공 시 계획 1회 소비·재추첨 없음 / `ResetBossPiece` 보스전 시작 START 리셋+계획 폐기). 시각 위치는 BoardManager.MoveBossPieceVisual, 예고 표식은 MoveBossPreviewVisual(`bossMovePreview` — 준비 단계만 표시, 표시 전용) |
| 부적 덱 `drawPile`·손패 `hand`·선택 상태 `isChoosing`/`candidates` | TalismanManager | **보스전 단위** — `BeginBossFight`→`ResetForBoss()`가 이전 손패·후보·사용 기록·세션 효과를 정리하고 `EnsureDeck`이 활성 카드 1장씩 새로 셔플(사용·소멸 카드도 복귀, `includeInDeck` 활성 카드 — 2026-10-22 오후 20종 전환 진행 중으로 현재 7종). `OpenStartPick`·`OpenLandPick` / `SelectCandidate`·`ConfirmChoice` / `UseTalisman` — 같은 보스전 안에서 사용 카드 소멸, 원본 `TalismanData`(에셋) 불변. 후보 부족 시 `Mathf.Min` 축소·0장 생략. 손패 한도 `HandLimit=10` — 모든 획득은 `AcquireCard` 경유, 초과분 보스전에서 소멸 |
| '이번 투척' 부적 효과 `sessionEffects` | TalismanManager | TalismanManager(준비 단계 발동 시 추가 / `ClearSessionEffects` — `PrepareTurn` 차례 준비·보스전·런 경계에서 1회) → ScoreManager가 정산에서 조건 검사·적용. 추혼부는 `caught` 조건의 곱셈 카드로 루프에서 1회 적용, `fx.mult` = 최종 피해 배율 |
| 사용 부적 표시 기록 `usedThisThrow` (`UsedTalismanRecord`) | TalismanManager | 표시 전용 — 사용 성공 시 useId·이름·설명·효과·결과 안내 복사. `ScoreEffectStep.useId`로 정산 단계와 연결 → GameUIManager가 동적 항목 생성·단계별 제거. 기록 삭제가 실제 효과·덱·손패를 바꾸지 않음. 새 세션·보스전·런에서 정리 |
| 잡기 판정 `caught`/`catchMult`/`actualAffinityMult` | ThrowSettlement | ScoreManager (확정 정산에서 도착 칸 = bossPieceIndex 비교 → 잡기 성공 시 `catchMultiplier`·`catchBonusMult` 에셋값 적용, 추혼부는 `caught` 조건의 부적 곱셈 — 연출은 기록 재생) |
| 런타임 기본 전체배율 `baseTotalMult` | ScoreManager | ScoreManager (`GameBalanceConfig.baseTotalMult`의 런 복사본 — 보스 전환 유지·`ResetForNewRun`에서만 설정값 초기화. 정산 시 `settlement.baseTotalMult`로 복사해 전체배율 출발점으로 사용) |
| 별도 방울 피해 배율 `bellDamageMult` | ThrowSettlement | ScoreManager (현재 ×1 — 효과 없음). 추후 방울 피해 배율 효과 추가 시 이 필드에 설정하면 실제 피해(`preEffectDamage`)·최종 전체배율(`totalMult`)에 자동 반영, 연출 단계는 `ScoreEffectStep`(TotalMult)으로 별도 기록 필요 |
| 런타임 확률 가중치 `runtimeWeights` | YutRollManager | YutRollManager (`BuildWeights`/`ModifyProbability`) — 신규 면 조합 투척은 미사용, API 호환용 |
| 런타임 윷 스탯 `yutStats` | YutRollManager | YutRollManager (`BuildYutStats` — 기본값을 **새 객체로 복사**, `UpgradeYutStat`) / BellManager(결과 성장 방울이 `mult` float +0.5 누적) |
| 말 위치·완주·칸 배치 `board` | YutGameManager.board (Inspector 노출) | YutBoard.MovePiece(데이터 이동) / BoardManager(배치 생성·말 위치 초기화·시각 위치) — 배치(`layoutGenerated`)와 말 위치 초기화 분리 |
| 점수 상태 `totalScore`/`currentThrows` | YutGameManager (Inspector 노출) | ScoreManager만 |
| 표시용 임시값 `basicScore`/`multiplier`/`throwScore`/`displayTotal` | YutGameManager | YutGameManager(투척 시작 초기화) / GameUIManager(연출 재생) |
| 보스 진행 `selectedBossIndex`(전투 중) / `pendingBossIndex`(소개 중) | gm 필드 / BossManager | BossManager |
| 보유 방울 `ownedBells`·속성 성장 `attrAttackBonus`·`attrMultBonus` | BellManager | BellManager (`TryAddBell` 한도·중복 검사 / `RemoveBell` 인스턴스 제거 / `ApplyGrowth` 성장 발동·한도 소진 소멸 / `ResetForNewRun` 런 리셋) — 인스턴스 제거가 누적 성장을 회수하지 않음. `attrMultBonus`(화령 불 배율 성장)는 `GameBalanceConfig` 원본과 분리된 런 상태, `TileEffectManager`·정보판이 `GetAttrMultBonus`로 합산 |
| 돈 `money` | EconomyManager | EconomyManager (`AddMoney`/`TrySpend`만) — 시작 자금은 `ResetForNewRun` 런당 1회, 상점·재복방울·클리어 보상(`GrantBossClearReward`)이 경유 |
| 상점 상태 `isOpen`/`offers`/`soldOfferIndices`/`refreshCount` | ShopManager | ShopManager (`OpenShop` — 보스 구간 목록 유지, 비어 있을 때만 추첨 / `BuyOffer`·`SellBellInstance`·`RefreshShop` 원자적 처리 / `ResetForBoss` — `BossManager.ShowBossSelect`에서 호출해 보스 전환마다 초기화) — 열림 중 보스 진입·건너뛰기 차단 |
| 게임 상태 `currentState` | YutGameManager | YutGameManager (상태 전환 메서드) |
| 카드 정의·보스 목록·밸런스 수치·연출 딜레이·방울 정의·상점/경제 | ScriptableObject 에셋 6종 (`talismanDatabase`/`bossDatabase`/`balanceConfig`/`presentationConfig`/`bellDatabase`/`shopConfig` 참조) | 사용자(에셋 편집만) — 코드는 읽기 전용. 필수 4종 누락 시 `ConfigReady` 가드로 게임 진입 차단. bellDatabase/shopConfig 미연결은 경고+기본값. `baseProbabilities`/`backDoPenalty`는 레거시로 씬 직렬화 유지(미사용) |
| 연출 플래그 `isPresenting`/`hasRolled`/`lastResult` | YutGameManager (internal) | YutGameManager / GameUIManager |

## 상태·흐름 요약

`Start → (StartGame → 보스 소개) → EnterBossBattle → Playing → Cleared/GameOver`
`Cleared → (다음 관문 → 보스 소개) ⇄ OpenShop/CloseShop(상점 — 구매·판매) → EnterBossBattle → ...`

- `PrepareTurn()` = 차례 준비(새 세션 생성 rolled=false + 이전 '이번 투척' 부적 효과 해제 + `PrepareBossMove` 보스 이동 계획 추첨 — 보스전 시작·정산 종료 후 다음 차례에서 호출) → 준비 단계 부적 사용 가능(`CanUseNow` = 미투척 세션 + 카드 `useTiming`) + 보스 이동 예고 표시(`bossMovePreview`) → `RollYut()` = 최초 투척(세션의 면만 던짐 — 준비 효과 보존) + 보스 말 1회 이동(준비된 계획 소비 — 봉쇄부로 1회 금지 가능) → `RerollStick(0~3)` 세션 한도까지 → `ConfirmThrow()` = 확정 → `SettleNormalThrow`(이동 보정 → 착지 확정 → 방울 성장 발동 → 잡기 판정 → 상성(능력 보스만·청개구리 우선) → 결과별 부적 → 윷점수×배율×부적 곱셈 D → 재복방울 수입 → `TileEffectManager.ResolveLandingEffect` 불×1.5/물 연계/풀 도트) → `ThrowSettlement` 확정 → `PresentSettlement`(UI — 이동 시 진격부·후퇴부 안내, 단계별 이유 표시→Mult 갱신→간격→useId 카드 제거) → `FinishPresentation`에서 표시 정리+말 스냅+클리어/게임오버 판정 → 계속 시 `PrepareTurn`(다음 차례) → 땅 착지 시 `OpenLandPick`(부적 3택1) → 선택창 없으면 즉시·선택 확정 후 `ApplyTurnStartDot`(풀 도트 차례 시작 틱 — 처치 시 즉시 클리어)
- 보스전 진입(`StartBossBattle`) = `BeginBossFight` + `EnterPlaying` + `OpenStartPick`(부적 5택3 — 선택 완료 전 최초 투척 차단)
- 세션은 일반 결과 5종만 생성 — 저주 경로(`HandleCurseResult`)는 API로 유지하되 현재 도달 불가 (백도·낙 발생 조건 미확정)
- 클리어 판정 `totalScore >= goalScore`(마지막 투척 처치 포함 우선) / 5회 소진 미달 `EnterGameOver` — 라운드·판 구조 폐지 (2026-10-18)
- 저주(백도·낙)는 말 이동·점수 계산 없이 `HandleCurseResult` 고정 처리

## 버튼 OnClick 공용 메서드 (전부 `YutGameManager`에 유지)

`StartGame`, `EnterBossBattle`, `SkipBoss`, `GoToNextBoss`, `RestartRun`, `RollYut`, `RerollStick(1~4)`, `ConfirmThrow`, `SkipPresentation`, `OpenShop`, `CloseShop`, `RefreshShop`, `SelectTalismanOption(1~5)`, `ConfirmTalismanChoice`, `UseTalisman(1~10)`, `ModifyProbability`, `ResetProbabilities`, `UpgradeYutStat`

(상품 구매·판매 버튼은 `BellSlotUI` 슬롯의 `actionButton`에 `Bind`로 onClick 연결 — Inspector OnClick 불요. 리프레시 버튼은 `RefreshShop`을 OnClick 연결)

## 새 기능 추가 시 배치 기준

- 윷 결과·확률 변경 → `YutRollManager`
- 말판 칸 효과·이동 방식 → `YutBoard`(데이터) + `BoardManager`(표현)
- 점수 공식·효과 발동 → `ScoreManager`의 `SettleNormalThrow` steps 지점 (3)에 `ScoreEffectStep` 추가
- 보스 능력·관문 규칙 → `BossManager` + `BossData` 필드 확장
- 방울 효과 → `BellData`(`BellDatabase` 에셋) 정의 추가 + `BellManager` 장착·효과 처리 + 지속 효과는 해당 시스템(BossManager 이동·ScoreManager 상성/정산)이 `HasBell`로 조회 — 구매·판매는 `ShopManager` |
- 부적 카드·효과 → `TalismanData`(`effect` enum + 수치) 정의 추가 + `TalismanManager.ApplyEffect` 분기 + 피해 효과는 `ScoreManager` 정산의 부적 단계에 조건 추가
- 화면 표시·연출만 → `GameUIManager`
- `YutGameManager`에 다시 로직을 몰아넣지 않는다 — 상태 전환·흐름 조정만
