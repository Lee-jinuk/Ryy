# ARCHITECTURE — RYY 매니저 구조와 데이터 소유권

2026-10-18 오전 리팩터링 기준. 기능 분리 원칙과 각 스크립트의 책임·데이터 소유권을 정리한다.

## 설계 원칙

- **Inspector 연결 보존 최우선**: UI 참조는 **화면별 UI 컴포넌트(`Assets/Scripts/UI/*UI.cs`, MonoBehaviour)**가 소유한다(2026-10-28 51차 분리). `YutGameManager`의 기존 직렬화 필드는 `legacy*`로 개명돼 `[FormerlySerializedAs]`로 씬 연결을 보존하며, 실제 읽기는 `YutGameManager`의 **프로퍼티 접근자 한 곳**(컴포넌트 연결 우선 → 레거시 폴백)을 거친다 — 코드상 `gm.xxx` 호출부는 변경 없음. 2026-10-28 52차: `test2.unity`·`SampleScene.unity`·`Test.unity` 이전 완료로 실사용 씬의 기준은 컴포넌트 연결 단일이며, `legacy*`는 `_Recovery` 백업 등 미이전 씬 폴백으로만 남는다(제거 조건: 모든 실사용 씬 새 구조 저장 확인 후 필드·접근자·`UITransferTool` 일괄 삭제). 게임 로직 매니저는 모두 **일반 C# 클래스(POJO)**이며 `Awake()`에서 생성된다 — 씬·OnClick 연결에 영향이 없다.
- **UI 표시 소유권**: 개별 Text·Image·Button 갱신은 각 화면 컴포넌트의 `Refresh(gm)` 등이 작성하고 `GameUIManager.UpdateUI`는 호출만 조정한다 — 같은 Text를 두 컴포넌트가 쓰지 않는다. 컴포넌트는 표시만 하고 재추첨·피해·카드 소모·턴 차감·초기화를 하지 않는다. `YutGameManager.EnsureUIComponents()`가 Awake에서 컴포넌트를 자동 부착하므로 이전 도구 미실행 씬도 동작한다. 씬에 영구 반영 + 레거시 참조 복사는 에디터 메뉴 `YUTROLL/UI 참조 이전 (열린 씬)`(`Assets/Scripts/Editor/UITransferTool.cs` — 빈 필드만 복사·멱등·Undo 지원).
- **설정 데이터는 에셋이 기준 (2026-10-19)**: 부적 정의·보스 목록·밸런스 수치·연출 딜레이는 ScriptableObject 에셋(`Assets/ScriptableObjects/`)에만 저장한다. `YutGameManager`는 에셋 참조 필드를 들고 읽기 프로퍼티로 노출 — Scene에 설정 목록을 중복 저장하지 않는다. 런타임 덱·손패·보스 상성·말판 배치 등 실행 상태는 매니저가 별도 소유.
- **데이터 소유권 단일화**: 각 상태를 쓰는 곳은 책임 매니저 한 곳뿐이다. 필드 저장소는 Inspector 노출을 위해 `YutGameManager`에 두더라도 쓰기 권한은 해당 매니저가 가진다. 다른 스크립트에서 같은 값을 복제·누적하지 않는다.
- **계산과 연출 분리**: 점수·이동은 `ThrowSettlement`에 즉시 확정되고, 화면 연출(`GameUIManager.PresentSettlement`)은 그 기록을 재생만 한다. 스킵해도 같은 결과가 표시된다.
- `YutGameManager`를 삭제·개명하지 않는다. 버튼 OnClick이 호출하는 public 메서드 시그니처는 전부 유지하고 실제 로직만 매니저에 위임한다.

## 파일 구성

| 파일 | 위치 | 역할 |
|---|---|---|
| `YutGameManager.cs` | Scripts/Core | 게임 총괄 — 상태 전환, 실행 순서, 투척→정산 흐름, 관문 진입, OnClick 진입점, 설정 에셋·런타임 상태 소유 + **UI 참조 단일 읽기 경로**(컴포넌트 우선·레거시 폴백 접근자) + `EnsureUIComponents`(런타임 컴포넌트 자동 부착) |
| `YutRollManager.cs` | Scripts/Core | 윷 투척 세션 소유·확률 API·윷 스탯 + `ApplyPercentBonuses`(%p 증분·비례 차감·정규화 공통 규칙 — 최초 투척 결과·속성 발생 확률 공용 소유) + `CollectInitialProbBonus`/`ComputeAdjustedResultWeights`(방울 보정+윷 결과 점괘(`GetYutProbBonus`) 단일 버킷 합산 → `ApplyPercentBonuses` 1회 적용 — 순서 무관) + `WouldChangeInitialWeights`(점괘 실효 순수 조회) + 세션 `rerollFrontUpChance`(양면령 기본값 + `GetRerollFrontBonus`−`GetRerollBackBonus` 순보정, 20~80% 제한 — 세션 생성 시 계산) (`YutProbabilityEntry`, `YutStatEntry` 타입 정의 포함) |
| `YutThrowSession.cs` | Scripts/Core | 윷 4개 투척 세션 — 차례 준비에 생성(`rolled=false` = 준비 단계, 부적 사용 가능) → `InitialRoll`은 면만 던짐(준비 효과 보존) → 재투척(기본: 서로 다른 윷 `maxRerolls`개·각 1회 + 재기부 `specialRerolls` 특별 기회·`speciallyRerolled` 윷당 1회), `rerollsBanned` 결단령 금지(`rerollsForfeited`는 레거시 보존), `rerollAttempted` 재투척 실행 기록(승부부 판정 — 일반·특별·윤회령 무료 포함), `isNak` 최초 투척 1% 낙(면 조합과 무관한 고정 결과 — `InitialRollNak`, 재투척 전부 차단), `moveDelta`·`resultMoveDelta`·`stopMove` 이동 보정(환위부·환영부·정지부), `catchWidenRange` 포위부, `waterComboBonusCount` 수룡부, `forestDotBoostMult` 초목부, 결과별 부적 `armedResultBonus`/`succeededResultBonus`, 방울 세션 설정(`freeRerollChance` 윤회령·`rerollFrontUpChance` 양면령·`initialResultWeights` 최초 투척 결과 보정 — 세션 생성 시 YutRollManager가 설정 + 확률 부적 예약 소비, `InitialRoll`의 `RollWeightedFaces`가 결과 선추첨 후 면 조합 생성 / 재투척 실행 시 추첨·적용), 확정 상태 |
| `BoardManager.cs` | Scripts/Core | 30칸 속성 말판 생성(**매 실제 보스전 시작 시 재배치** — `SetupBoard`, 속성 확률 점괘(`GetAttrProbBonus`)을 `ApplyPercentBonuses` 규칙으로 반영)·`WouldChangeAttrWeights`(점괘 실효 순수 조회)·`ReshuffleAttributes`(역행부 — START·말·보스 위치·연계·도트·완주 유지, 속성만 재추첨). 말 이동·말판 표시는 `UpdateBoardUI`/`MovePieceVisual`/`MoveBossPieceVisual`/`MoveBossPreviewVisual` 이름을 유지한 채 `BoardUI`에 위임 |
| `YutBoard.cs` | Scripts/Core | 말판 데이터 (`YutBoard`, `BoardTile`, `TileAttribute` — UI 참조 없는 순수 데이터) |
| `ScoreManager.cs` | Scripts/Core | 점수 정산·저주 처리 (`ScoreEffectStep`, `ThrowSettlement`, `EffectSourceType/Target/Op` 타입 정의 포함). 기초 피해(기본점수×**유효 윷 배율**)와 전체배율을 분리 계산 — **유효 윷 배율 = 성장 반영 윷 배율 × 결과별 부적(성공 시 ×2) × 추적부(근접 ×1.5)** — 이번 공격에만 적용, 영구 스탯 미변경·전체배율 이중 적용 금지, `EffectTarget.YutMult` 단계로 기록해 윷 배율 표시 상승(2026-10-28 확정). **전체배율은 가산 항목 우선 합산 후 곱연산 항목(피해 부적만)을 곱하는 구조**. 잡기 판정 = 최종 착지~보스 원형 최단 거리 ≤ `catchWidenRange`(포위부 확장 포함 공통 판정). 런타임 `baseTotalMult` 소유. 낙은 `SettleNakThrow` 전용 경로 — 이동·착지·잡기 없이 공격 0 + 투척 1회 소모 |
| `BossManager.cs` | Scripts/Core | 보스 데이터 접근·순차 소개·건너뛰기 판별·보스 상성 능력(`useAffinity` 데이터 주도 — 무작위 생성 없음)·보스 말 위치·피해 단계별 이동 (`BossData`, `BossAffinity` 타입 정의 포함) |
| `BellManager.cs` | Scripts/Core | 방울 장착 상태 — 보유 인스턴스(`ownedBells` 최대 4·종류당 1개)·획득/제거(`TryAddBell`/`RemoveBell`)·보유 조회(`HasBell`/`GetBell`)·`EffectiveChance`(공령명 반영 유효 확률 — 원본 `chance` 미변경)·런 리셋. **효과 적용은 각 담당 시스템이 수행**: 청류령·연수령=TileEffectManager 물 연계 / 잔향령·목박령=YutGameManager 도트 지급 / 속박령=BossManager 이동 보정 / 윤회령·삼고령·양면령·결단령(금지)=YutRollManager 세션 설정+YutThrowSession 실행 / 결단령(방울가산 `totalMultAdd`)·폭염령(말판가산 `tileMultAdd`)·접전령(방울 곱연산)·축지령(이동)·윤환령(완주 가산)=ScoreManager 정산 / 화염령(말판 곱연산, 출처 방울 기록)=TileEffectManager 불 경로 / 지맥령·금맥령(땅 카드 선택 억제)=ScoreManager·(말판 배율·수입)=TileEffectManager / 걸윷령·쌍개령·극단령=YutRollManager 최초 투척 보정+YutThrowSession 면 생성 / 공령명=BellManager `EffectiveChance` 조회 경로 / 재기부 차단=TalismanManager |
| `FortuneUpgradeManager.cs` | Scripts/Core | 복주머니 점괘 강화 24종 — 획득 횟수(`acquiredCounts`)·누적 강화 `(kind,target)` 키의 `cumulativeBonus`(윷 배율·기본점수는 `YutStatEntry` 필드 가산, 나머지는 조회 시 합산 — `GetYutProbBonus`/`GetAttrProbBonus`/`GetRerollFrontBonus`/`GetRerollBackBonus`/`ShopDiscountPending`/`ShopDiscountPercent`)·상점 할인 상태(`ShopDiscountPending` 누적 대기 ↔ `shopDiscountActive`/`ShopDiscountPercent` 적용 중, `OnBossCleared`는 보스 처치(`EnterClear`)에서만 호출)·선택 대기(`isChoosingFortune`·`pendingCandidates`)·런 초기화. `IsSupported`(종류+대상 유효) + `HasEffect`(선택 전후 실제 변화 판정 — 할인 상한·면 상쇄·확률 분포 변화 반영, 순수 조회) = 후보·판매 공통 유효성 검사, `SupportedCandidateCount` = 판매 가능 후보 수 |
| `EconomyManager.cs` | Scripts/Core | 냥(화폐) — `money`·`AddMoney`/`TrySpend` 단일 경로·시작 자금 런당 1회·보스 클리어 보상(`GrantBossClearReward` — `clearRewardPaid`로 보스전당 1회, `BeginBossFight`에서 리셋) |
| `ShopManager.cs` | Scripts/Core | 상점 — 진입 조건(Cleared+보스 소개 화면)·상품 후보(보스 구간 유지)·구매/판매 거래·유료 리프레시·연타 차단. 상품은 `ShopOffer` 런타임 자료형(ShopOfferKind enum) — 방울·복주머니 혼합 추첨, 복주머니 구매는 `FortuneUpgradeManager.BeginChoice`와 연결. 복주머니 판매 가능·선택창 구성은 `CanSellFortunePouch`/`FortuneChoiceReady` 공통 판정 — 새로고침과 구매가 같은 기준. `PriceFor` = 할인 구매가 단일 조회(Ceil(정가×(1−적용율)), 상한 `ShopConfig.shopDiscountMaxPercent`) — 슬롯 표시·구매 가능 판정·차감 공용, `FortunePouchAvailable`/`CanSellFortunePouch`로 유효 후보 부족 복주머니를 구매 불가 표시·미차감 거부 |
| `TileEffectManager.cs` | Scripts/Core | 속성 착지 효과 — 불×2(+복주머니 강화, `CurrentAttrMult` 공통 경로; 폭염령은 전체배율 말판가산이라 말판 자체 배율에 관여하지 않음) + 화염령 유효 확률 말판 곱연산 ×`damageMult`(출처는 `AddBellDamageStep`으로 방울 기록, 미발동은 `AddBellInfoStep`)·물 연속(보스전, 청류령 끊김 방어 `waterComboGuardArmed` + 연수령 확률 +1과 수룡부 `waterComboBonusCount` 추가 연계 — 각 독립 1회 반영, 이번 공격부터 적용)·풀 도트(보스전, 차례 시작 틱 + 초목부 `forestDotBoostMult` — 이번 등록·증가분만 ×value, 최종 등록량 후 정수화)·땅 공격(×1 / 지맥령 `tileMult`×2·금맥령 `bonusMoney` 수입 — 둘 다 카드 선택 제거, 선택 억제는 ScoreManager의 `landedOnBellTile`)·START 공격 ×1+부적 선택(땅 전용 방울 미적용, 도트 미등록). `ResolveLandingEffect`는 확정 정산에서만 호출 |
| `BellSlotUI.cs` | Scripts/UI | 방울 고정 슬롯 표시 컴포넌트 — name/desc/status Text + actionButton 명시 참조, `Bind`/`Clear`로 표시·클릭 전달만 (계산 없음) + `UsesText`(남은 발동 표시 공용) |
| `GameUIManager.cs` | Scripts/UI | **UI 갱신 조정자(얇은 진입점)** — `UpdateUI`가 화면별 컴포넌트 `Refresh`를 호출하고, 정산 연출(`PresentSettlement`)·패널 공용 헬퍼(`SetPanel`/`ActivatePanel`)만 직접 담당 (규칙 계산 없음) |
| `StartScreenUI.cs` | Scripts/UI | 시작 화면 — `startPanel` 참조 |
| `BossSelectScreenUI.cs` | Scripts/UI | 보스 소개 화면 — 패널·이름·목표·능력·상성·건너뛰기 + `FillInfo`/`Refresh`(건너뛰기 표시) |
| `GameScreenUI.cs` | Scripts/UI | 게임 화면 최상위 — `gamePanel`·`moneyText`(보유 냥) 갱신 |
| `ClearScreenUI.cs` | Scripts/UI | 클리어 화면 — 패널·보스 이름·점수·메시지·'다음 관문' 버튼 |
| `ShopScreenUI.cs` | Scripts/UI | 상점 화면 — 구매 4칸·판매 4칸·잔액·리프레시 표시 (`Refresh` — 가격·판정은 ShopManager 소유) |
| `GameOverScreenUI.cs` | Scripts/UI | 게임오버 화면 — 패널·보스 이름·최종·목표 점수 |
| `YutThrowUI.cs` | Scripts/UI | 윷 투척 — 면 PNG·스프라이트·재투척·던지기·확정 버튼·상태창·중앙 결과 (`Refresh` — 세션 읽기만) |
| `ScoreUI.cs` | Scripts/UI | 점수·능력치 — 베이직·윷 배율·전체배율·이번/누적 피해·투척 횟수·능력치 정보판·배율 이유 (`Refresh`/`SetMultiplierReason`) |
| `BossCombatUI.cs` | Scripts/UI | 전투 중 보스 정보 — 이름·현재/최대 HP·능력·상성 (`Refresh`) |
| `BoardUI.cs` | Scripts/UI | 말판 — 칸 속성·땅 아이콘·플레이어/보스 말 위치·이동 예고·완주·착지 정보 (`Refresh`·`UpdateBoardDisplay`·`MovePiece`·`MoveBossPiece`·`MovePreview`) |
| `TalismanHandUI.cs` | Scripts/UI | 부적 손패 — 이름·설명 10칸·사용 버튼 활성·안내 (`Refresh`) |
| `EquippedBellUI.cs` | Scripts/UI | 게임 화면 장착 방울 4칸 표시 (`Refresh`) |
| `TalismanChoiceUI.cs` | Scripts/UI | 부적 선택창 — 후보 이름·설명·레어도 7칸·진행 안내·확정 버튼·패널 동기화 (`Refresh`) |
| `MoveAdjustUI.cs` | Scripts/UI | 환위부 ±1 선택창 패널 표시 (`Refresh`) |
| `FortuneChoiceUI.cs` | Scripts/UI | 복주머니 점괘 3택1 선택창 (`Refresh` — 후보는 FortuneUpgradeManager 보관) |
| `UsedTalismanUI.cs` | Scripts/UI | 사용 부적 목록 — 항목 Prefab 동적 생성·useId별 단계 제거·정산 정리 (`Sync`·`RemoveUsedItem`·`ClearFeedback`) |
| `UITransferTool.cs` | Scripts/Editor | 에디터 이전 도구 — 메뉴 `YUTROLL/UI 참조 이전 (열린 씬) — 미리보기/실행`. 컴포넌트 자동 부착 + `legacy*` → 컴포넌트 참조 복사(비어 있을 때만·덮어쓰기 금지·멱등·Undo 지원) |
| `TalismanData.cs` | Scripts/Data | 부적 카드 정의 자료형 — `TalismanRarity`/`TalismanEffectKind`/`TalismanUseTiming`/`TalismanAppearance`/`TalismanData` (cardId·이름·설명·레어도·효과·수치·속성·`includeInDeck`·`useTiming`·`appearance`) |
| `UsedTalismanRecord.cs` | Scripts/Data | 사용 부적 표시 기록 — useId·cardId·이름·설명·효과·수치·resultNote (표시 전용, 효과 실행과 분리) |
| `TalismanDatabase.cs` | Scripts/Data | 부적 정의 에셋 — 카드 목록의 단일 기준 (`ScriptableObjects/TalismanDatabase.asset`) |
| `BossDatabase.cs` | Scripts/Data | 보스 목록 에셋 — 순서·이름·HP·능력 단일 기준 (`ScriptableObjects/BossDatabase.asset`) |
| `GameBalanceConfig.cs` | Scripts/Data | 밸런스 에셋 — 투척 횟수(6)·윷 배율·보스 이동 (기본 고정 4~7칸, `useBossMoveDamageStages` 활성 시 피해 비율 단계 배열 — 구조 보존·비활성) (`ScriptableObjects/GameBalanceConfig.asset`) |
| `PresentationConfig.cs` | Scripts/Data | 연출 에셋 — 말 이동·정산 단계 딜레이 + 탐욕부 전용 딜레이(`greedStepPreDelay`/`greedStepPostDelay`) (`ScriptableObjects/PresentationConfig.asset`) |
| `BellData.cs` | Scripts/Data | 방울 정의·보유 자료형 — `BellEffectKind`(20종: 기존 `BossMoveReduce=3`/`FreeReroll=5`/`WaterComboGuard=8`/`DotDoubleChance=9` + 신규 11~26 — 구 번호 폐기·재사용 금지)/`BellData`(bellId·효과·대상·chance·`chanceBoostable`·`bonusCount`·`damageMult`·`tileMultBonus`(미사용—tileMultAdd로 대체)·`tileMultAdd`(말판가산)·`tileMult`·`bonusMoney`·`frontUpChance`·`proximityRange`·`probBonusPercent`·`chanceBoostMult`·`perLapBonus`·`totalMultAdd`(방울가산)·가격·`effectEnabled`)/`OwnedBell`(정의 필드 복사 + instanceId) |
| `BellDatabase.cs` | Scripts/Data | 방울 정의 에셋 — 20종 단일 기준 (`ScriptableObjects/BellDatabase.asset`) — 구현 완료 종만 `effectEnabled`로 상점 활성화 |
| `ShopConfig.cs` | Scripts/Data | 상점·경제 에셋 — 시작 자금·장착 한도·상품 수 (`ScriptableObjects/ShopConfig.asset`) |
| `TalismanManager.cs` | Scripts/Core | 부적 런타임 — 뽑기 덱(`drawPile`, 활성 25종 각 1장·사용 소멸)·손패·선택 창 상태(시작 7택5 / 땅·START 3택1 공용 + 환위부 ±1 선택 `isChoosingMoveAdjust`)·사용 검증(`CanUseCard` = 카드 `useTiming`별 구간: `BeforeFirstThrow` 준비 단계=노란 / `AfterRollBeforeConfirm` 투척 후~확정 전=하얀, 낙 차단)·효과 발동·세션 피해 효과(`sessionEffects`)·**차례 지속 효과 `turnEffects`/`chaseEffects`(족쇄부·추적부 — `OnTurnStart`=`PrepareTurn`에서 1회 차감, UI 조회로 차감 없음, 사용 차례를 1턴으로 계산)·다음 차례 확률 예약 `pendingProbBoosts`(도걸부·모윷부 — YutRollManager `PrepareSession`이 `ConsumeProbBoosts`로 1회 소비, 보스 전환 소멸)**·`IsMoveDisplayEffect`(이동 연출 표시 대상 — 진격·후퇴·환영·정지부) |

## 데이터 소유권

| 데이터 | 저장 위치 | 쓰기 담당 |
|---|---|---|
| 투척 세션 `session` (면·재투척·확정·준비 효과) | YutRollManager | YutGameManager(`PrepareTurn` 차례 준비에 생성 — 보스전 시작·정산 종료 후 / `RollYut` 최초 투척·`RerollStick`·`ConfirmThrow` 가드) / YutThrowSession(면 판정·기본/특별 재투척 소비·결과별 부적 평가) — 준비 단계에 쓴 부적 효과는 세션 필드 보관, `InitialRoll`이 지우지 않음 |
| 세션 부적·방울 상태 `specialRerolls`/`moveDelta`/`resultMoveDelta`/`stopMove`/`catchWidenRange`/`waterComboBonusCount`/`forestDotBoostMult`/`armed·succeededResultBonus`/`rerollAttempted`/`rerollsBanned`/`maxRerolls`/`freeRerollChance`/`rerollFrontUpChance` | YutThrowSession | TalismanManager(카드 사용 시 설정 — 노란은 준비 단계, 하얀은 투척 후~확정 전 / 결단령·낙 보유 시 재기부 발동 불가) / YutRollManager(세션 생성 시 보유 방울 반영 + 확률 부적 예약 소비: 윤회령 유효 확률·삼고령 기회·결단령 금지·양면령 앞면·결과 가중치) / YutThrowSession(재투척 평가·소비·무료 판정·면 추첨·`rerollAttempted` 기록) / ScoreManager(정산에서 moveDelta·stopMove·resultMoveDelta·catchWidenRange·resultBonus 적용) / TileEffectManager(물 연계·도트 세션 수치 적용) — 최초 투척을 넘겨 유지, 다음 차례 세션 생성으로 해제 |
| 보스 상성 — `BossData.useAffinity`+`affinityWeak`/`Strong`/`Mult` (런타임 상태 없음 — 데이터 주도) | BossData (BossManager 조회) | BossManager (`HasAffinityAbility` 활성 판정 / `GetAffinity` null=비상성 / `AffinityMultiplier`) — 천운부도 같은 경로 |
| 물 연속 `waterComboCount`·끊김 방어 `waterComboGuardArmed`·풀 도트 `forestDot`/`forestDotActive` (보스전 단위) | TileEffectManager | TileEffectManager (`ResolveLandingEffect` 확정 정산에서만 갱신 — 연계 새 시작(0→) 시 청류령 방어 준비, 연수령은 정상 증가 후 확률 +1, 물 외 착지는 `BreakOrGuardWaterCombo`가 소모/끊김 처리 / YutGameManager `ApplyTurnStartDot` 차례 시작 틱 + 잔향령 추가 판정 + 지급마다 목박령 봉쇄 판정 / `ResetForBoss` 보스 전환·런 리셋) |
| 보스 말 위치 `bossPieceIndex`·이동 계획 `movePlanReady`/`plannedBaseSteps` | BossManager | BossManager (`PrepareBossMove` — 차례 준비(PrepareTurn)에 거리 1회 추첨·보관 / `PlannedEffectiveSteps`·`PlannedDestination` — 속박령(`bonusCount` 감소·최소 1)·봉쇄(봉쇄부·목박령 공용 `pendingMoveSeal`)를 계산 시점에 반영, 봉쇄 미소모 / `MoveBossPiece` — 최초 투척 성공 시 계획 1회 소비·재추첨 없음 / `ResetBossPiece` 보스전 시작 START 리셋+계획 폐기). 시각 위치는 `BoardUI.MoveBossPiece`, 예고 표식은 `BoardUI.MovePreview`(`bossMovePreview` — 준비 단계만 표시, 표시 전용) — 호출 경로는 BoardManager의 동명 래퍼 유지 |
| 부적 덱 `drawPile`·손패 `hand`·선택 상태 `isChoosing`/`candidates` | TalismanManager | **보스전 단위** — `BeginBossFight`→`ResetForBoss()`가 이전 손패·후보·사용 기록·세션 효과·차례 지속 효과·확률 예약을 정리하고 `EnsureDeck`이 활성 카드 1장씩 새로 셔플(사용·소멸 카드도 복귀, `includeInDeck` 활성 카드 — **2026-10-28 25종 개편으로 활성 25종**, 천기부 보류·고집·삼재·윤회·도박·천운부 제외). `OpenStartPick`·`OpenLandPick` / `SelectCandidate`·`ConfirmChoice` / `UseTalisman` — 같은 보스전 안에서 사용 카드 소멸, 원본 `TalismanData`(에셋) 불변. 후보 부족 시 `Mathf.Min` 축소·0장 생략. 손패 한도 `HandLimit=10` — 모든 획득은 `AcquireCard` 경유, 초과분 보스전에서 소멸 |
| '이번 투척' 부적 효과 `sessionEffects` | TalismanManager | TalismanManager(준비 단계 발동 시 추가 / `ClearSessionEffects` — `PrepareTurn` 차례 준비·보스전·런 경계에서 1회) → ScoreManager가 정산에서 조건 검사·적용. 추혼부는 `caught` 조건의 곱셈 카드로 루프에서 1회 적용, `fx.mult` = 최종 피해 배율 |
| 사용 부적 표시 기록 `usedThisThrow` (`UsedTalismanRecord`) | TalismanManager | 표시 전용 — 사용 성공 시 useId·이름·설명·효과·결과 안내 복사. `ScoreEffectStep.useId`로 정산 단계와 연결 → `UsedTalismanUI`가 동적 항목 생성·단계별 제거. 기록 삭제가 실제 효과·덱·손패를 바꾸지 않음. 새 세션·보스전·런에서 정리 |
| 잡기 판정 `caught`/`catchMult`/`actualAffinityMult` | ThrowSettlement | ScoreManager (확정 정산에서 최종 착지~보스 `BoardDistance` ≤ `session.catchWidenRange`(포위부 ±N, 기본 0) — 통과 칸 미검사, 일반·포위 잡기 공통 판정(중복 실행 없음) → 잡기 성공 시 `catchMultiplier` 에셋값 단일 적용, 추혼부·탐욕부 취소는 동일 `caught`에 반응 — 연출은 기록 재생) |
| 런타임 기본 전체배율 `baseTotalMult` | ScoreManager | ScoreManager (`GameBalanceConfig.baseTotalMult`의 런 복사본 — 보스 전환 유지·`ResetForNewRun`에서만 설정값 초기화. 정산 시 `settlement.baseTotalMult`로 복사해 **가산 합계의 첫 항목**으로 사용 — 윤환령·가산 부적·가산 방울은 `lapBonus`/`talismanAddMult`/`bellAddMult` 별도 항으로 `AdditiveTotal`에 합산) |
| 런 누적 완주 `board.lapCount` | YutBoard | YutBoard.MovePiece — 보스전 이동 진행도(`totalMoved`)가 START 경계(30칸 배수)를 넘은 횟수만 가산. **런 단위 수명 — 보스 전환·건너뛰기에서 유지, `ResetForNewRun`에서만 0** (2026-10-27 확정). `SetupBoard`는 말 위치·진행도만 리셋. 윤환령은 장착 중 `lapCount`×`perLapBonus`를 가산 항에 읽기 전용으로 사용 |
| 방울 곱연산 배율 `bellDamageMult` / 가산 `bellAddMult`·`tileAddMult` | ThrowSettlement | ScoreManager (`ApplyBellDamageMults`/`BellDamageMultFor` — 공격 칸에서만 평가, 조건 만족 방울의 `damageMult` 순차 곱. 현재 곱연산 대상은 접전령(원형 최단 거리 `proximityRange` 이내 ×2)뿐 — 범위 초과도 `AddBellInfoStep` 안내 단계. 결단령은 `BellAddFor`로 방울가산 +`totalMultAdd` / 폭염령은 `BellTileAddFor`로 대상 속성 착지 시 말판가산 +`tileMultAdd` / 화염령은 TileEffectManager 불 경로의 말판 곱연산 — 둘 경로 중복 적용 없음) — 실제 피해(`preEffectDamage`)·최종 전체배율(`totalMult`)에 자동 반영 |
| 런타임 확률 가중치 `runtimeWeights` | YutRollManager | YutRollManager (`BuildWeights`/`ModifyProbability`) — 신규 면 조합 투척은 미사용, API 호환용 |
| 런타임 윷 스탯 `yutStats` | YutRollManager | YutRollManager (`BuildYutStats` — 기본값을 **새 객체로 복사**, `UpgradeYutStat`) / FortuneUpgradeManager(복주머니 윷 배율 강화가 `mult` float 누적) |
| 말 위치·완주·칸 배치 `board` | YutGameManager.board (Inspector 노출) | YutBoard.MovePiece(데이터 이동+완주 경계 판정) / BoardManager(`SetupBoard` — 매 실제 보스전 시작 시 배치 재생성·말 위치·이동 진행도 초기화·시각 위치) — 배치·말 위치·누적 완주의 초기화는 분리 (완주는 새 런에서만 0) |
| 점수 상태 `totalScore`/`currentThrows` | YutGameManager (Inspector 노출) | ScoreManager만 |
| 표시용 임시값 `basicScore`/`multiplier`/`throwScore`/`displayTotal` | YutGameManager | YutGameManager(투척 시작 초기화) / GameUIManager(연출 재생) |
| 보스 진행 `selectedBossIndex`(전투 중) / `pendingBossIndex`(소개 중) | gm 필드 / BossManager | BossManager |
| 보유 방울 `ownedBells` | BellManager | BellManager (`TryAddBell` 한도·중복 검사 / `RemoveBell` 인스턴스 제거 / `ResetForNewRun` 런 리셋) — 런 단위 상태, 보스 전환 유지. 현재 4종 모두 지속형이라 인스턴스 제거 시 효과 즉시 해제 (구 성장 런 상태는 방울 4종 재편으로 삭제) |
| 돈 `money` | EconomyManager | EconomyManager (`AddMoney`/`TrySpend`만) — 시작 자금은 `ResetForNewRun` 런당 1회, 상점·클리어 보상(`GrantBossClearReward`)이 경유 |
| 상점 상태 `isOpen`/`offers`/`soldOfferIndices`/`refreshCount` | ShopManager | ShopManager (`OpenShop` — 보스 구간 목록 유지, 비어 있을 때만 추첨 / `BuyOffer`·`SellBellInstance`·`RefreshShop` 원자적 처리 / `ResetForBoss` — `BossManager.ShowBossSelect`에서 호출해 보스 전환마다 초기화) — 열림 중 보스 진입·건너뛰기 차단 |
| 게임 상태 `currentState` | YutGameManager | YutGameManager (상태 전환 메서드) |
| 카드 정의·보스 목록·밸런스 수치·연출 딜레이·방울 정의·상점/경제 | ScriptableObject 에셋 6종 (`talismanDatabase`/`bossDatabase`/`balanceConfig`/`presentationConfig`/`bellDatabase`/`shopConfig` 참조) | 사용자(에셋 편집만) — 코드는 읽기 전용. 필수 4종 누락 시 `ConfigReady` 가드로 게임 진입 차단. bellDatabase/shopConfig 미연결은 경고+기본값. `baseProbabilities`/`backDoPenalty`는 레거시로 씬 직렬화 유지(미사용) |
| 연출 플래그 `isPresenting`/`hasRolled`/`lastResult` | YutGameManager (internal) | YutGameManager / GameUIManager |

## 상태·흐름 요약

`Start → (StartGame → 보스 소개) → EnterBossBattle → Playing → Cleared/GameOver`
`Cleared → (다음 관문 → 보스 소개) ⇄ OpenShop/CloseShop(상점 — 구매·판매) → EnterBossBattle → ...`

- `PrepareTurn()` = 차례 준비(새 세션 생성 rolled=false + 이전 '이번 투척' 부적 효과 해제 + `talismanManager.OnTurnStart` 차례 지속 효과 차감 + `PrepareBossMove` 보스 이동 계획 추첨 — 보스전 시작·정산 종료 후 다음 차례에서 호출) → 준비 단계 부적 사용 가능(`CanUseNow` = 미투척 세션 + 카드 `useTiming`) + 보스 이동 예고 표시(`bossMovePreview` — 족쇄부·봉쇄부 사용 시 즉시 갱신, 계획 재추첨 없음) → `RollYut()` = 최초 투척(낙 1% 판정 → `InitialRollNak`/`InitialRoll` 분기 — 준비 효과 보존) + 보스 말 1회 이동(준비된 계획 소비 — 봉쇄부로 1회 금지 가능) → `RerollStick(0~3)` 세션 한도까지 → `ConfirmThrow()` = 확정 → `SettleNormalThrow`(이동 보정 합산(정지부=0·환영부=결과별) → 착지·잡기(포위부 범위) 평가 → **윷 기본배율 보정(결과별 부적·추적부 — YutMult 단계)** → 가산 항목 수집(기본+윤환령 완주+가산 부적+가산 방울) → 곱연산 부적(피해 부적) → 잡기 → 곱연산 방울 → `TileEffectManager.ResolveLandingEffect`(불×2/물 연계(청류령 방어·연수령·수룡부)/풀 도트(초목부)/땅 — 공통 누적값 `runningForSteps` 인자로 단계 기록) → 보스 상성) → `ThrowSettlement` 확정 → `PresentSettlement`(UI — 이동 시 `IsMoveDisplayEffect` 카드 안내, 단계별 이유 표시→Mult 갱신→간격→useId 카드 제거) → `FinishPresentation`에서 표시 정리+말 스냅+클리어/게임오버 판정 → 계속 시 `PrepareTurn`(다음 차례) → 땅·START 착지 시 `OpenLandPick`(부적 3택1 — 땅은 지맥령·금맥령 보유 시 `landedOnBellTile` 억제로 생략) → 선택창 없으면 즉시·선택 확정 후 `ApplyTurnStartDot`(풀 도트 차례 시작 틱 — 처치 시 즉시 클리어)
- 보스전 진입(`StartBossBattle`) = `BeginBossFight` + `EnterPlaying` + `OpenStartPick`(부적 7택5 — 선택 완료 전 최초 투척 차단)
- 세션은 일반 결과 5종만 생성 — 저주 경로(`HandleCurseResult`)는 API로 유지하되 현재 도달 불가 (백도·낙 발생 조건 미확정)
- 클리어 판정 `totalScore >= goalScore`(마지막 투척 처치 포함 우선) / 기본 투척(6회) 소진 미달 `EnterGameOver` — 라운드·판 구조 폐지 (2026-10-18)
- 저주(백도·낙)는 말 이동·점수 계산 없이 `HandleCurseResult` 고정 처리

## 버튼 OnClick 공용 메서드 (전부 `YutGameManager`에 유지)

`StartGame`, `EnterBossBattle`, `SkipBoss`, `GoToNextBoss`, `RestartRun`, `RollYut`, `RerollStick(1~4)`, `ConfirmThrow`, `SkipPresentation`, `OpenShop`, `CloseShop`, `RefreshShop`, `SelectTalismanOption(1~5)`, `ConfirmTalismanChoice`, `UseTalisman(1~10)`, `ModifyProbability`, `ResetProbabilities`, `UpgradeYutStat`

(상품 구매·판매 버튼은 `BellSlotUI` 슬롯의 `actionButton`에 `Bind`로 onClick 연결 — Inspector OnClick 불요. 리프레시 버튼은 `RefreshShop`을 OnClick 연결)

## 새 기능 추가 시 배치 기준

- 윷 결과·확률 변경 → `YutRollManager`
- 말판 칸 효과·이동 방식 → `YutBoard`(데이터) + `BoardManager`(표현)
- 점수 공식·효과 발동 → `ScoreManager`의 `SettleNormalThrow` steps 지점 (3)에 `ScoreEffectStep` 추가
- 보스 능력·관문 규칙 → `BossManager` + `BossData` 필드 확장
- 방울 효과 → `BellData`(`BellDatabase` 에셋) 정의 추가 + `BellManager`는 장착·보유 조회·유효 확률(`EffectiveChance`)만 담당 — 실제 효과는 책임 시스템에 구현하고 `HasBell`/`GetBell`로 조회 (속박=BossManager 이동 / 청류·연수=TileEffectManager 물 연계 / 잔향·목박=YutGameManager 도트 지급 / 윤회·삼고·양면·결단 금지=YutThrowSession 재투척 / 결단·화염·접전 배율·축지 이동·윤환 완주=ScoreManager 정산 / 지맥·금맥·폭염=ScoreManager+TileEffectManager 착지 효과) — 구매·판매는 `ShopManager` |
- 부적 카드·효과 → `TalismanData`(`effect` enum + 수치) 정의 추가 + `TalismanManager.ApplyEffect` 분기 + 피해 효과는 `ScoreManager` 정산의 부적 단계에 조건 추가. 윷 기본배율 보정(결과별·근접)은 `EffectTarget.YutMult` 경로, 전체배율 효과는 `TotalMult` 경로로 분리 — 신규 enum은 끝에만 추가(기존 번호·cardId 불변)
- 화면 표시·연출만 → 해당 화면의 UI 컴포넌트(`Scripts/UI/*UI.cs`, `Refresh` 등) + 조정은 `GameUIManager`. 새 UI 필드는 담당 컴포넌트에 추가하고 `YutGameManager`에는 접근자만 연결한다 (레거시 폴백 필드는 이전 완료까지 유지)
- `YutGameManager`에 다시 로직을 몰아넣지 않는다 — 상태 전환·흐름 조정만
