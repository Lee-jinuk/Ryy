# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-20 오후 작업 요약 (다음 AI 전달용)]

```
[2026-10-21 오전 (21차) 변경사항 요약 - 사용자 지시 / Devin 구현]

★ 최신 — 보스 다음 이동 위치 미리보기 (사용자 확정):
- BossManager에 이동 계획 분리: 차례 준비(PrepareTurn)에
  거리 1회 추첨·보관 → 최초 투척에서 계획 1회 소비해 이동
  (실행 시 재추첨 없음). 족쇄방울·봉쇄부는 유효 거리·목적지
  계산에 반영 — 예고 계산·UI 갱신은 봉쇄 미소모
- 신규 Inspector 필드: bossMovePreview (RectTransform) —
  준비 단계에만 예정 도착 칸에 표시, 투척 접수 시 숨김.
  Raycast 해제됨. 잡기 판정은 실제 보스 위치 사용
- 봉쇄부 사용 즉시 예고가 현재 칸으로 갱신, 봉쇄된 투척
  다음 차례에는 정상 예고 복귀
- 검증: build 0·0. Unity Play 대기 — 예고=실제 이동 일치·
  봉쇄 갱신·잔류 없음 확인 필요

[이전] 2026-10-21 오전 (20차) — 정산 후 부적 버튼 재활성화
수정:
- FinishPresentation에서 UpdateUI가 PrepareTurn 전에 실행돼
  손패 버튼이 확정 세션 기준 비활성으로 남던 문제 수정 —
  PrepareTurn+땅 선택 처리 후 최종 UpdateUI로 CanUseNow
  기준 재판정. 버튼 강제 활성 아님, PrepareTurn 중복 호출 없음
- 규칙 명확화: '확정 후 불가' = 그 차례의 확정~정산 중 불가,
  다음 차례 준비에서 다시 사용 가능
- 검증: build 0·0. Unity Play 대기 — 정산 후 버튼 활성·
  땅 선택 후 활성 확인 필요

[이전] 2026-10-21 오전 (19차) — 기본 재투척 1회·말판 확률·
부적 사용 시점 변경 (사용자 확정):
- 재투척: 기본 기회 2→1회 (MaxRerolls=1). 재기부 특별 재투척
  유지 → 재기부 사용 시 기본1+특별1. 동일 윷 제한 기존대로
- 말판: 불30/물30/풀30/땅10/쇠0 — 쇠는 생성만 제외(정의·
  표시·enum 유지), 예외 반환값도 Earth. 새 런에서 확인
- 부적 사용 구간: '최초 투척 후~확정 전' → **최초 던지기 전
  준비 단계**. 투척 후·재투척 사이·확정 후 사용 불가
- 세션 수명 변경: 차례 준비(PrepareTurn)에 세션 생성
  (rolled=false), 최초 투척은 면만 던짐 → 준비 때 쓴 재기부·
  봉쇄부·승부부·진격/후퇴·윷령/모령 효과가 투척 후에도 유지.
  이전 차례 효과는 다음 차례 준비에서 1회 정리
- 카드 데이터 신규 필드: useTiming(전 카드 BeforeFirstThrow
  명시)·appearance(노란/하얀 부적 외형 분류 — 어느 카드가 어느
  색인지 미확정, 전부 미정). 에셋 17종 반영 완료
- 검증: build 0·0. Unity Play 대기 — 재투척 횟수·쇠 0칸·
  준비 사용 효과 유지·투척 후 사용 차단 확인 필요

[이전] 2026-10-20 오후 (18차) — 방울 장착·상점·경제 시스템
(사용자 확정, 과거 방울 선택 이벤트 완전 대체):
- 신규: EconomyManager(냥, 시작 자금 50 = 런당 1회)·ShopManager
  (보스 소개 화면 상점 — Cleared 상태만, 무작위 후보 4종)·
  BellManager 재작성(장착 최대 4·종류당 1개, 런 단위 유지)·
  BellData/OwnedBell/BellDatabase/ShopConfig + 에셋 2종
- 방울 12종: 배율 성장 5종(+0.5×5회)·속성 공격력 성장 3종
  (+5×10회)·특수 4종 — 족쇄(보스 이동 −1, 최소 1칸)·
  청개구리(강점 ×3·나머지 ×0.5, 잡기 강제 상성보다 우선)·
  재복(원래 약점 공격 +1냥/정산)·천운(확률 미확정 → 비활성)
- 성장은 발동한 이번 공격부터 적용(확정) — 정산 단계에
  "도깨비방울 — 도 배율 +0.5" 등 기록. 한도 소진·판매·소멸해도
  누적 성장은 런 종료까지 유지(확정)
- 가격: 통일 구매 20냥·판매 10냥(BellData.buyPrice/sellPrice)
- YutStatEntry.mult·settlement 배율 float화 (+0.5 지원)
- 제거: bellPool·bellChoicePanel·bellOption*·bellSlots·
  talismanSlots 필드 + SelectBell*/ConfirmBellChoice 함수
- 신규 버튼 함수: OpenShop·CloseShop (상점 항목 버튼은 코드 연결)
- 신규 Prefab: BellItem·ShopItem / 신규 에셋: BellDatabase·ShopConfig
  — Scene에 bellDatabase·shopConfig 연결 완료
- Inspector 작업: moneyText·shopMoneyText·bellContent·
  bellItemPrefab·shopPanel·shopOfferContent·shopBellContent·
  shopItemPrefab 연결 + bossSelectPanel에 '상점' 버튼(OpenShop)
- 검증: dotnet build 0·0. Unity Play 대기 — 상점 거래·성장·
  특수 방울 효과·런 초기화 확인 필요

[이전] 2026-10-20 오후 (17차) — 배율 이유 표시 확장 + 탐욕부
연출 타이밍 (사용자 확정, 표시만 — 계산 미변경):
- 이유 표시 전 단계 커버: 윷 기본 배율 → 결과별 부적 → 상성
  (일반 상성 ×1 신규 표기 / 약점 상성 ×2 / 강점 상성 ×0.5 /
  잡기 상성 ×2) → 보스말 잡기 ×3 → 피해 부적 → 조건 불충족 →
  취소 — 감소·유지 단계 포함 전부 표시
- 탐욕부 전용 타이밍: "탐욕부 ×2" 표시 → greedStepPreDelay(0.5s)
  후 배율 갱신 → greedStepPostDelay(0.5s) 후 카드 제거.
  PresentationConfig 신규 필드 2개. 잡기 취소 시 취소 안내만
  재생(상승 연출 미적용)
- ScoreEffectStep.preApplyDelay/postApplyDelay 신규(기본 -1 =
  일반 타이밍) — 단계별 커스텀 딜레이는 기록 시 확정
- 검증: dotnet build 0·0. Unity Play 대기 — 탐욕부 타이밍·
  취소 연출·정상/스킵 일치

[이전] 2026-10-20 오후 (16차) — 사용 부적 패널 + 배율 이유 표시
(사용자 확정, 표시 기능만 — 계산 규칙 미변경):
- `UsedTalismanRecord` 신규 — 사용 성공 시 useId·이름·설명·효과·
  수치·결과 안내 복사 보관 (TalismanManager.usedThisThrow).
  표시 전용 — 항목 제거가 효과·덱·손패에 영향 없음
- `ScoreEffectStep.useId`/`reasonText` 신규 — 정산 단계를 사용
  기록과 연결 ("추혼부 ×4", "잡기 상성 ×2", "보스말 잡기 ×3" 등)
- 조건 불충족·비피해 카드도 안내 단계로 정리 (×1 — Mult 미변경).
  진격부·후퇴부는 이동 연출과 함께 표시·제거
- 연출 순서 = 이유 → Mult 갱신 → 간격 → 카드 제거 (useId>0 단계만)
- `YutGameManager` 신규 필드: multiplierReasonText·
  usedTalismanPanel·usedTalismanContent·usedTalismanItemPrefab —
  전부 미연결 시 무시(경고 후 표시 생략)
- 신규 Prefab: `Assets/Prefabs/UsedTalismanItem.prefab`
  (NameText 첫 자식 / DescText 둘째 자식)
- 스킵 시 이유 비움·남은 항목 정리 + 플레이어 말 최종 칸 스냅
- 사용자 Inspector 작업 필요 — GamePanel에 사용 부적 패널·
  Content·multiplierReasonText 생성 후 4개 필드 연결
- 검증: dotnet build 0·0. Unity Play 대기

[이전] 2026-10-20 오후 (15차) — 부적 제외 범위 정정 (사용자 확정,
14차 '활성 2종'은 범위 전달 오류로 취소):
- 현재 활성 부적 = **14종** (기존 10종 + 진격부·후퇴부·
  윷령부·모령부) / 제외 = 일도부·이개부·삼걸부 3종만
  (`includeInDeck: 0` — 정의·enum·효과 코드 유지)
- 제외 카드는 덱·시작 선택·땅 선택 등 모든 획득 경로에서 미등장.
  덱 충분 시 시작 5택3·땅 3택1 정상 동작, 소진 시 축소·생략
- 에셋 값만 갱신 — 코드 변경 없음

[유지] 14차 — 윷 기본 배율 + 부적 덱 필터 구조 (사용자 확정):
- 윷 배율: 도 ×4 / 개 ×3 / 걸 ×4 / 윷 ×9 / 모 ×9
  (`GameBalanceConfig.yutStats` 기본값+에셋 동기화 — 이동 거리 불변)
- `TalismanData.includeInDeck` 신규 — false이면 정의·enum·효과
  처리를 유지하되 새 런 덱에서 제외 (TalismanManager.EnsureDeck 필터)
- 사윷부→윷령부·오모부→모령부 이름 변경(사용자 확인).
  cardId·effect enum 0~11 전부 보존
- 덱 부족 동작: 후보 수·선택 수를 남은 장수로 축소, 0장 생략.
  매 보스전 재지급 없음 (기존 Mathf.Min 로직)
- 검증: dotnet build 0·0. Unity Play 대기 — 덱 14장·제외 3종
  미등장·시작 5택3 복귀·다음 투척 보너스 미잔류

[이전] 2026-10-20 오전 (13차) — 잡기 보상·추혼부 변경 (사용자 확정):
- 기본 잡기 ×2→**×3** + 잡기 성공 시 유효 상성 **×2 고정**
  (`GameBalanceConfig.catchMultiplier`=3, `catchAffinityMult`=2 —
  에셋 조절 가능). 잡기 실패 시 실제 상성·×1
- 추혼부: 잡기 배율 ×6 대체 → **잡기 성공 시 최종 피해 추가 ×4**
  (부적 곱셈 단계에서 `caught` 조건으로 1회 적용 — 대체 로직 제거,
  value 6→4)
- 정산 순서: 이동 보정 → 착지 확정 → 잡기 판정 → 유효 상성 →
  결과별 부적 → 피해 부적 곱셈. 상성 단계는 잡기 후 기록 —
  강제 시 '잡기 상성 ×2' 표시. `ThrowSettlement.actualAffinityMult`
  신규 (실제 상성 별도 기록)
- 잡기 연출 단계의 resultBonusMult 누락 표시 버그 수정
- 속성 부적·천운부는 여전히 실제 속성·실제 약점 검사 —
  잡기만으로 자동 발동 안 함
- 검증: dotnet build 0·0. Unity Play 대기

[이전] 2026-10-19 오후 (12차) — 부적 17종 확장 + 밸런스 상향 (사용자 확정):
- 덱 10종→17종 각 1장 (손패 한도 10 별도). 에셋 TalismanDatabase에
  수치·설명 반영 — cardId·effect enum 0~9 유지, 신규 MoveDelta(10)·
  ResultBonus(11)은 끝에 추가
- 상향: 속성 부적 ×4 / 추혼부 잡기 대체 ×6 / 탐욕 ×2 / 천운 ×4 / 승부 ×2
- 재기부 재정의: 특별 재투척 — 이미 재투척한 윷도 다시 가능(윷당 1회,
  `speciallyRerolled`). 미재투척 윷은 기본 기회 우선 소비, RemainingRerolls
  = 기본+특별 합계. `bonusRerolls`→`specialRerolls`로 필드 교체
- 진격부·후퇴부(MoveDelta): 세션 `moveDelta` 합산 → 정산에서
  Max(0, 기본거리+보정). 도+후퇴=0칸 → 현재 칸 정상 정산. 윷 배율 미변경
- 일도부~오모부(ResultBonus + `targetResult`): 다음 성공 재투척의 전체
  결과 평가(armed→succeeded, 1회) → 최종 결과 동일 시 그 윷 배율 ×2
  (`settlement.resultBonusMult` — 공식: 칸×윷×결과부적×상성×잡기×부적)
- 확정 정정: 한 투척 다장 사용+곱셈 중첩 (미확정→확정)
- Scene/Inspector 작업 없음 — 에셋 데이터만 반영
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-19 오후 (11차) — 게임 설정 데이터 ScriptableObject 분리 (규칙 변경 없음,
저장 위치 통일):
- 데이터 기준 에셋 4종 (`Assets/ScriptableObjects/`):
  `TalismanDatabase`(부적 정의) / `BossDatabase`(보스 목록) /
  `GameBalanceConfig`(투척 횟수·윷 배율·보스 이동 단계) /
  `PresentationConfig`(연출 딜레이) — 클래스는 `Assets/Scripts/Data/`
- YutGameManager 직렬화 설정 필드를 에셋 읽기 프로퍼티로 교체:
  talismanPool/bosses/maxThrows/baseYutStats/bossMoveStageMin·Max/
  moveStepDelay/effectStepDelay — Scene에 설정 목록 저장 금지
- ★ 버그 수정: test2.unity에 저장된 구 talismanPool(50/100/75)이
  덱 기준으로 쓰이던 문제 → 에셋 참조로 해결 (씬의 구 키는
  고아 — Unity 저장 시 자동 제거)
- 에셋 누락 시 ValidateDataAssets 경고 + ConfigReady 가드로
  StartGame/EnterBossBattle 진입 차단 (조용한 대체 없음)
- 탐욕 취소 시 multScoreText 양수 잔존 표시 버그 수정 → 0 표시
- 씬 3개(test2/Test/SampleScene) Gamemanager에 에셋 참조 연결 완료
- 사용자 Unity 작업 없음 — 밸런스 수정은 에셋 파일만 편집
  (Play 종료 → 에셋 편집·저장 → 새 게임 시작으로 반영 확인)
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-19 오후 (10차) — 부적 10종 밸런스 변경 (사용자 확정):
- 피해 부적 = '최종 피해 배율': 화염·수룡·초목 ×2.5 / 탐욕 ×1.5(잡기 성공
  시 0, 모든 증가 효과보다 우선) / 천운 약점 속성 ×3(약점 상성 ×2와 별도
  중첩) / 승부 ×1.5(남은 재투척 전부 포기 유지). 조건 부적 배율은 곱셈
  중첩 — 사용 순서 무관
- 추혼부 = 잡기 배율 ×2→×4 **대체** (부적 곱셈 루프에서 제외 — 중복 없음)
- 인연부 = 덱에서 2장 획득 (카드 value=획득 장수). 사용 카드 먼저 제거 후
  덱 순서대로 AcquireCard. 덱이 장수보다 적으면 발동 불가·미소모
  ⚠ 덱에 1장만 남은 경우(1장 획득 vs 발동 차단)는 미확정 — 임시로 차단
- 봉쇄부 = 다음 최초 던지기의 보스 이동 1회 금지 (`pendingMoveReduction`
  폐기 → `BossManager.pendingMoveSeal`). 봉쇄 이동 0칸 기록, 재투척·확정·
  거부 입력으로 소모 안 함. 보스 전환 해제 유지·중복 중첩 없음 — 둘 다 미확정
- `TalismanData.value` 의미 = 효과별 상이 (피해 부적=배율 / 추혼부=잡기 대체
  배율 / 재기부=횟수 / 인연부=장수) — 주석 참조. `sessionEffects.mult`
- `ThrowSettlement.talismanCancelled` 추가 — 탐욕 취소를 정산 데이터에 보관.
  정산 로그에 실제 잡기 배율 표시 (고정 '잡기2' 문구 제거)
- ⚠ 정정: test2.unity에는 구 talismanPool 수치가 직렬화돼 있었고
  덱 기준으로 사용됐음 — 11차의 에셋 분리로 해결
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-19 오후 (9차) — 부적 손패 한도 10 + 초과 소멸 (사용자 확정):
- `TalismanManager.HandLimit = 10` — 초과 획득분은 덱 복귀 없이 런 소멸,
  기존 손패 보존. 모든 획득은 `AcquireCard` 공통 경로 통과
- 획득 순서 = 선택한 순서 (selectedIndices 추가 순서 — 취소 후 재선택은
  재선택 시점으로 갱신). 기존 후보 인덱스 순서 획득 버그 수정
- 인연부 순서 확정: 덱 검사 → 사용 카드 제거 → 1장 획득 — 손패 10장에서도
  정상 교환. 덱 소진 시 미소모. 새 카드만 소멸 대상
- `talismanNoticeText` 신규 필드 — "손패가 가득 차 부적 N장이 소멸했습니다."
- `talismanHandNameTexts`/`talismanUseButtons` 8→10, `UseTalisman9`/`10` 래퍼
- 씬에 신규 배열 미직렬화 → 기본값 10 적용, 씬 수정 없음
- 후속: `talismanHandDescTexts[10]` 신규 — 손패 카드 설명 Text (이름과 같은
  순서, 빈 슬롯 빈칸, 미연결 무시). 사용자가 설명 Text 10개 연결
- 검증: dotnet build 0·0. Unity Play 검증 대기

[이전] 2026-10-19 오후 (8차) — 소모형 부적 시스템 + 초기 10종 (사용자 확정 4가지: 시작 선택=매
보스전 / 땅=방울 대체 부적 3택1 / 피해 50%=×1.5 증가 / 덱=10종 각 1장·
사용 시 런에서 소멸):
- 신규 `TalismanData.cs` (Data): `TalismanRarity`(범품/진품/영물/신물/미정),
  `TalismanEffectKind` 10종, `TalismanData`(cardId·이름·설명·레어도·효과·
  수치·속성 — Inspector 편집 가능, 이름 문자열 비교로 효과 판별 금지)
- 신규 `TalismanManager.cs` (Core): 런 단위 `drawPile`(10종 셔플)·`hand`,
  선택창 상태(`isChoosing`/`candidates`/`requiredPicks`) — `OpenStartPick`
  (매 보스전 5택3) / `OpenLandPick`(땅 3택1, 방울 대체). 사용 구간 = 최초
  투척 후~확정 전, 실패한 사용은 카드 미소모. `sessionEffects` = '이번
  투척' 피해 효과 — 다음 세션에서 해제
- 정산 순서: 상성 → 잡기 → 부적(사용 순서 순차 곱셈) → 최종 정수화 1회.
  `ThrowSettlement.talismanMult` 기록, mult 표시에 포함. 탐욕부 충돌
  (잡기와 같은 조건 — 임시 해석) 시 공격 0이 모든 증가 효과에 우선
- `YutThrowSession`: `bonusRerolls`(재기부 한도 +1)·`rerollsForfeited`
  (승부부 포기 — 세션 내 번복 불가). `BossManager.pendingMoveReduction`
  (봉쇄부 → 다음 보스 이동 1회 −2, 보스 전환 시 소멸)
- 신규 Inspector 필드: `talismanPool`(10종 정의), `talismanChoicePanel`,
  `talismanOptionNameTexts`/`Desc`/`RarityTexts`[5], `talismanChoiceInfoText`,
  `talismanConfirmButton`, `talismanHandNameTexts`[8], `talismanUseButtons`[8]
- 신규 버튼: `SelectTalismanOption1~5`, `ConfirmTalismanChoice`,
  `UseTalisman1~8` (+int 버전 `SelectTalismanOption`/`UseTalisman`)
- 방울 이벤트 비활성: 땅 착지 → 부적 3택1로 대체 (BellManager·방울 필드는
  호환용 유지, 발동 경로 제거 — 돌려야 하면 FinishPresentation의
  OpenLandPick 호출을 bellManager.OpenBellChoice로 되돌리면 됨)
- ⚠ 미확정 임시 규칙 (GAME_RULES §3 표): 미선택 후보 덱 복귀, 손패
  무제한·다장 사용, 탐욕 충돌=잡기, 봉쇄 1회 −2·합산·최소 0칸, 승부부
  재투척 0이면 불가, 인연부 덱 소진 미소모, 손패 보스 전환 유지
- ⚠ 사용자 작업: 부적 선택창 패널 + 후보 텍스트 + 손패 UI 생성·연결,
  OnClick 연결
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-19 오전(7차) — 보스 말 + 잡기 ×2 + 누적 피해별 이동 단계:
`bossPiece` 필드, `BossManager.bossPieceIndex`·`BossMoveStage`·
`MoveBossPiece`·`ResetBossPiece`(모든 보스전 시작 START 초기화),
`bossMoveStageMin/Max` 배열, 잡기 판정 `caught`/`catchMult` + 잡기 step.

[이전] 2026-10-18 오전(6차) 변경사항 — 착지 정보 + 보스 상성 + 윷 4개 재투척 세션:
- 신규 `YutThrowSession.cs` (Core, POJO): 윷 4개 각 앞·뒤 50%,
  앞면 개수 → 결과 (0=모 1=도 2=개 3=걸 4=윷). 재투척 = 서로 다른 윷
  최대 2개·같은 윷 1회·0/1/2회 후 확정 가능. 확정 전 이동·득점 없음
- 버튼 흐름 변경: `RollYut()` = 최초 4개 투척(세션 시작, 점수 없음)
  → `RerollStick(int)` + `RerollStick1~4()` = 윷별 재투척
  → `ConfirmThrow()` = 결과 확정 → 이동·공격 정산 + 투척 1회 소모
- 신규 Inspector 필드: `stickFaceTexts[4]`(앞/뒤 표시),
  `rerollButtons[4]`(interactable 제어), `throwStatusText`
  ("현재 결과: 개 / 재투척 가능: 2"), `rerollRemainText`,
  `confirmThrowButton`, `landingTileText`("불 - 50점"), `bossAffinityText`
- 보스 상성: `BossAffinity`(기본100/약점200/강점50%에 불·물·풀 셔플 배정)
  — BossManager의 `runAffinities`에 보스별 1회 생성·런 유지, 새 런 재생성.
  공식: `칸 공격점수 × 상성 배율 × 윷 배율` (×0.5는 float 곱셈 후 반올림)
- 상성 표시 2곳: `bossAffinityText`=전투 화면(selectedBossIndex),
  `bossSelectAffinityText`=소개 화면(pendingBossIndex) — 같은 데이터,
  건너뛰기 시 갱신, 미연결 무시
- `rollButton` 필드·자동 표시 제거 (던지기 버튼은 OnClick만으로 동작).
  `숲` 표시명 → `풀` (enum Forest 유지). `multiplier` int→float (실효 배율 표시)
- 가중치 추첨(`DrawYut`/`runtimeWeights`/`baseProbabilities`)은 신규 투척에
  미사용 — API 호환용 유지. 백도·낙은 면 조합에서 발생 불가 (발생 조건 미확정)
- 보스 낙 +2%p 능력: 신규 방식에 적용 불가 → ApplyBossAbility 호출 제거,
  발동 안 함을 로그 안내 (대체 능력 미확정 — 임의 생성 안 함)
- 스페이스바: 세션 진행 중 = ConfirmThrow, 세션 없음 = RollYut
- ⚠ 미확정 보고 유지: 마지막 투척 땅 착지 방울/게임오버 우선순위,
  백도·낙 발생 조건, 낙 능력 대체, 최종 데미지 반올림 규칙(RoundToInt 사용)
- ⚠ 사용자 작업: Test.unity는 소개 패널·30칸 연결 완료 — 재투척 버튼 4개
  (RerollStick1~4)·확정 버튼(ConfirmThrow)·세션 텍스트 6종 연결 필요.
  SampleScene은 boardTiles 18개만 연결 상태
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-18 오전(5차) 변경사항 — 30칸 속성 말판 전환 + 공격 공식 변경:
- YutBoard: TileAttribute enum(Start/Fire/Water/Forest/Earth/Metal),
  TileCount 18→30. BoardTile = index + attribute + AttackScore
  (불·물·숲 50 / START·땅·쇠 0). layoutGenerated 플래그 추가
- BoardManager: 1~29칸 독립 추첨(불25/물25/숲25/땅15/쇠10%,
  최소 개수 보장 없음). 배치는 런당 1회 생성 → 게임오버까지 유지
  (보스 처치·다음 보스·건너뛰기 재추첨 없음). 말 위치·완주는 보스전마다
  START 초기화 (기존 동작). 칸 UI = 속성명 텍스트, BellIcon 자식 = 땅 칸 표시
- ScoreManager: 데미지 = 도착 칸 공격점수 × 윷 배율 — 완주×10·윷 기본점수·
  basicBonus 가산 제거. ThrowSettlement.lapBonus 삭제.
  basicBonus/UpgradeYutStat API는 예약 필드로 유지(공격 미적용)
- 땅 착지 = 데미지 0 + 방울 뽑기 화면 (기존 3택1 재사용).
  bellEventTileCount·별도 방울 칸 지정·bellUsedThisRound 제한 폐기.
  bellChoicePanel 미연결 시 경고 후 이벤트 건너뜀 (보이지 않는 소프트락 방지)
- 씬: boardTiles 30칸으로 확장 (기존 18개 연결 유지 + 빈 슬롯 12개 —
  사용자가 칸 오브젝트 12개 추가·연결 필요). bellEventTileCount/lapBonus
  키 제거. Test.unity도 동일 정리
- ⚠ 미확정 보고: 마지막(5회째) 투척이 땅에 착지하면 소진 판정이 먼저라
  방울 화면 없이 게임오버 — 우선순위 확정 필요
- ⚠ 사용자 작업: 칸 오브젝트 12개 추가 + boardTiles[18~29] 연결,
  칸 자식 Text는 속성명(START/불/물/숲/땅/쇠)이 표시됨
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-18 오전(4차) 변경사항 — 보스 선택 → 순차 소개 + 건너뛰기 전환:
- 5명 직접 선택 폐지: SelectBoss(int)/SelectBoss1~5 제거 (씬에 연결된
  이벤트 없음 확인). 소개 화면에 진행 순서 보스 한 명만 표시
- 버튼: '다음' = EnterBossBattle 재사용 / '건너뛰기' = 신규 SkipBoss()
  → BossManager가 판별: 소개 패널 열림 + Start/Cleared + 다음 보스 존재
  시에만 pendingBossIndex 전진. 전투·보상·처치 기록 없음, 마지막 보스 불가
- 신규 필드 skipBossButton (GameObject, null-safe) — UpdateUI에서
  소개 화면 열림 + CanSkipPending일 때만 표시 (마지막 보스 자동 숨김)
- 소개 화면 필수화: bossSelectPanel 미연결 시 자동 전투 진입 폐기 →
  경고 후 대기
- ResetRunState에 selectedBossIndex=0, pendingBossIndex=0 추가 —
  새 런은 첫 번째 보스부터
- 인덱스 의미: pendingBossIndex=소개 중(건너뛰기 대상) / selectedBossIndex=
  전투 중(EnterBossBattle이 pending 복사). 건너뛰어도 클리어 후
  GoToNextBoss 계산 유지
- ⚠ 사용자 Inspector 연결 필요: Boss Select Panel + 소개 텍스트 3종 +
  '다음' 버튼(→EnterBossBattle) + '건너뛰기' 버튼(→SkipBoss, 필드를
  skipBossButton에도 연결) + '다음 관문' 버튼(→GoToNextBoss,
  nextBossButton에 연결). 패널 미연결 시 게임 진행 불가(대기) — 필수
- 미확정 유지: 보스 수·반복 등장·HP 증가 규칙 미확정. 마지막 보스 이후
  반복·자동 생성 없음, 최종 클리어 유지
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-18 오전(3차) 변경사항 — 라운드·판 구조 폐지 + 재시작 함수 제거:
- 보스전 = 기본 투척 5회의 단일 구간. 중간에 다음 판·투척 재충전 없음.
  판정(연출 종료·스킵 동일): totalScore>=goalScore → 클리어(마지막 투척
  처치도 클리어 우선) → currentThrows>=maxThrows → 게임오버 → 방울 이벤트
- 제거: roundIndex/roundsPerBoss/roundScore 필드, roundProgressText,
  AdvanceToNextRound(), BellManager.ResetForNextRound().
  방울 칸은 보스전당 1회 발동(bellUsedThisRound, SetupBoard 시 리셋),
  ownedBells는 보스전 한정 임시 방울
- 백도 차감 기준: min(totalScore, backDoPenalty) — 구 roundScore는
  totalScore와 항상 동치라 삭제. ⚠보고: 구 '판 시작 직후 차감 상한'
  (그 판 누적까지)은 소실 — 차감량 이내에서 보스전 누적 전체가 대상.
  규칙상 차감량·방식은 미변경
- 함수 제거: RetryBoss(같은 보스 재도전 폐지)·ResetRound·BackToBossSelect.
  RestartRun이 런 초기화+패널 정리+시작 화면 복귀를 직접 수행.
  씬 복귀 버튼 OnClick 2개는 BackToBossSelect → RestartRun으로 재연결됨
- 최종 버튼 역할: StartGame/EnterBossBattle/RollYut/GoToNextBoss/
  RestartRun/SkipPresentation (+SelectBoss1~5, 방울 관련)
- ⚠ 사용자 Inspector 연결 필요: Boss Current HP Text (숫자만 출력됨)
- ⚠ 씬 잔존 오브젝트: TargetSelectOverlay/Panel·TargetYut·FeverPanel은
  비활성 미사용 — 삭제해도 무방 (사용자 결정)
- 검증: dotnet build 오류 0·경고 0. Unity Play 검증 대기

[이전] 2026-10-18 오전(2차) 변경사항 — 보스 현재 체력 표시 + 타겟·피버 시스템 전체 제거:
- bossCurrentHPText = Max(0, goalScore - displayTotal) — 별도 HP 필드 없이
  계산값만. totalScoreText=누적 피해, goalScoreText=최대 체력.
  연출 중 displayTotal 기준 → 정상/스킵 최종 체력 동일. null-safe.
  클리어 판정 totalScore >= goalScore 유지
- 타겟·피버 제거: GameState.TargetSelection 삭제 → 보스전 진입 시 바로
  Playing. 삭제: selectedTarget, feverCombo, feverCap, feverText,
  targetYutText, targetSelectOverlay/Panel, SelectTarget*/ConfirmTargetSelection,
  ThrowSettlement.isTargetHit/feverComboAtRoll, 피버 ScoreEffectStep,
  EffectSourceType.Fever
- SelectableTargets → NormalResults 개칭 (YutRollManager 보스 낙 보정
  비례 차감 대상 — 타겟과 무관). 모든 결과 투척 1회 소모
- 씬 정리: SelectTarget* OnClick 5개 제거 + 삭제 필드 직렬화 키 정리
- ⚠ 보고(미수정): 백도 차감 = totalScore 감소 → 보스 체력이 회복처럼 표시됨.
  규칙 임의 변경 없음

[이전] 2026-10-18 오전(1차) 변경사항 — YutGameManager 책임별 분리 리팩터링 + 1단계 안정화:
- 스크립트 6개 신설 (전부 일반 C# 클래스, YutGameManager.Awake에서 생성 —
  Inspector/OnClick 연결 영향 없음):
  * Scripts/Core/YutRollManager.cs — 추첨·확률 가중치·보스 낙+2%p·윷 스탯
    (YutProbabilityEntry/YutStatEntry 타입 포함)
  * Scripts/Core/BoardManager.cs — 말판 배치·MovePieceVisual·UpdateBoardUI·초기화
  * Scripts/Core/ScoreManager.cs — 점수 정산 유일 경로·피버·저주
    (ScoreEffectStep/ThrowSettlement/EffectSourceType·Target·Op 타입 포함)
  * Scripts/Core/BossManager.cs — BossData·보스 선택·소개 정보·HasNextBoss/IsFinalClear
  * Scripts/Core/BellManager.cs — 방울 후보·선택·보유·판/런 초기화
    (isChoosingBell/selectedBellOption/currentBellOptions가 여기로 이동)
  * Scripts/UI/GameUIManager.cs — UpdateUI·SetPanel/ActivatePanel·PresentSettlement
- YutGameManager: 직렬화 필드 + 상태 전환 + 흐름 조정 + OnClick public 메서드
  전부 유지 (이름·시그니처 동일 — 기존 버튼 연결 그대로 작동)
- 데이터 소유권: 점수·피버·투척 수는 ScoreManager만 쓴다. 말판 데이터는 board
  필드 저장 + BoardManager/YutBoard만 쓴다. 상세 → docs/ARCHITECTURE.md
- 1단계 수정:
  * BuildYutStats() — 기본 YutStatEntry를 참조 공유하던 버그 수정 → 새 객체 복사
    (런타임 강화가 Inspector 기본값을 오염시키지 않음)
  * 씬 maxThrows 3 → 5 (확정 규칙과 일치)
  * bossSelectPanel 미연결 폴백 — 소개 패널이 없으면 경고 후 EnterBossBattle()
    직행 (패널 미생성 상태에서도 플레이 진입 가능, 패널 연결 시 자동 소개 화면)
- 씬 연결 현황 (SampleScene.unity 검사):
  * 연결됨: 텍스트 12종, 패널 6종, boardTiles[18]+playerPiece, rollButton
  * 미연결(오브젝트 없음): bossSelectPanel + Boss Select 텍스트 3종,
    roundProgressText, lapCountText, clearMessageText, nextBossButton,
    bellChoicePanel·bellOption 텍스트·bellSlots/talismanSlots
    (bellEventTileCount=0이라 방울 UI 없어도 진행 무관)
  * OnClick 연결됨: SelectTarget* 5종, RollYut, StartGame, BackToBossSelect×2
  * OnClick 미연결(버튼 자체 없음): EnterBossBattle, GoToNextBoss, RetryBoss,
    SelectBoss1~5, ConfirmTargetSelection, ResetRound, SkipPresentation, 방울 관련
    → 다음 관문 진행·보스 선택은 패널/버튼 제작·연결 후 사용 가능
- 검증: dotnet build Assembly-CSharp.csproj → 오류 0·경고 0 (Unity 미실행 — Play 테스트 필요)
- 다음 단계(2단계) 착수 전 사용자 답 필요: 윷 4면↔결과 대응, 개별 윷 확률,
  백도·낙 프로토타입 포함 여부 (GPT 지시서 선행 질문 참조)

[이전] 2026-10-16 오후 변경사항 요약 - GPT 기획 / Devin 구현:

보스 소개 화면 + 게임오버 전체 초기화 + 판 번호 표시:
- 보스 소개 화면: bossSelectPanel + bossSelectNameText/bossSelectGoalText/
  bossSelectAbilityText 신규 Inspector 필드. GoToNextBoss()는 소개 패널만 표시,
  실제 진입은 새 public EnterBossBattle()(진입 버튼용). StartGame()(게임 시작)도
  같은 패널 경유 — 최초 선택과 다음 관문이 같은 UI 공유. 소개 패널 열린 상태에서
  SelectBoss() 호출 시 내용 갱신. RetryBoss()는 소개 생략·즉시 재시작 유지
- 게임오버 초기화: 새 ResetRunState()가 런 전체 초기화(점수·판·피버·말 위치·
  완주·말판 배치·칸 강화·임시 방울·방울 이벤트·yutStats·확률 효과). '처음으로'
  버튼용 RestartRun() public 추가 = BackToBossSelect()와 동일 동작(전체 초기화 후
  StartPanel 복귀). 관문 이동(GoToNextBoss→EnterBossBattle)은 미경유라 런 성장 유지
- 판 번호: roundProgressText 필드 추가 — "{roundIndex} / {roundsPerBoss}" 출력,
  Start 화면 빈칸
- WarnIfPanelNested 확장: bossSelectPanel이 clearPanel 자식이면 경고
- ⚠ 사용자 Inspector 연결 필요: Boss Select Panel + Boss Select Name/Goal/Ability
  Text + Round Progress Text — 전부 null-safe
- ⚠ 주의: bossSelectPanel을 StartPanel 위를 덮는 전체화면 모달로 만들면 뒤쪽
  보스 선택 버튼이 차단됨 — 재선택 허용하려면 배치/Raycast Target 고려
- 보스전 건너뛰기: 미확정 규칙 — 미구현

[이전] 2026-10-15 변경사항 요약 - GPT 기획 / Devin 구현:

- GoToNextBoss() public 추가: ClearPanel '다음 관문' 버튼용 — Cleared 상태 +
  다음 보스 존재 시에만 동작 → selectedBossIndex++ 후 StartGame() 재사용.
  클리어 즉시 자동 이동 없음 (반드시 버튼 클릭)
- 초기화는 기존 BeginBossFight/SetupBoard가 전부 커버: 총점·판 점수·투척 점수·
  베이직·멀티플·피버·투척 횟수·말 위치(BoardTile_01)·완주·말판 재배치·
  방울 칸·임시 방울 + StopPresentation으로 잔여 연출 정리 + BuildWeights
  재구축으로 낙 +2%p 중복 없음. yutStats 등 런 전체 성장 데이터는 미초기화
  (향후 엽전·윷 강화·부적·영구 방울 유지 구조)
- 최종 클리어: 보스 5 클리어 시 nextBossButton 자동 숨김 + clearMessageText에
  "최종 클리어" 표시
- ⚠ 신규 Inspector 연결: Next Boss Button (GameObject 필드, OnClick→
  GoToNextBoss()), Clear Message Text (Text, 선택 — 최종 클리어 메시지용).
  둘 다 미연결도 null-safe
- 게임오버·보스 선택 복귀(BackToBossSelect)·RetryBoss 변경 없음

GPT 요청(직전 — 이어서 기록): 말판 시작점 규칙 + 보스 UI 단순화.
- 시작점: 별도 시작점 폐기 → BoardTile_01(인덱스 0)이 시작점 겸 일반 점수 칸.
  첫 도 → BoardTile_02. BeginBossFight에서 MovePieceVisual(0)로 말을 시작 칸에 배치.
  완주 판정: BoardTile_18→BoardTile_01 경계 통과마다 +1 (totalMoved 기반)
- 보스 UI 단순화: 보스는 목표 점수 관문 — bossHpText/bossMaxHpText/bossHpBar/
  bossHpSlider 필드와 HP 갱신 로직 제거. bossNameText/bossAbilityText만 유지.
  goalScore(=보스 HP)·클리어/게임오버 판정·낙 +2%p 특수능력 변경 없음.
  ⚠ 씬에 남은 구형 직렬화 키는 무시됨 — Inspector의 해당 참조만 정리하면 됨

GPT 요청(직전 — 이어서 기록): 18칸 원형 말판 + 방울 이벤트.
- 신규 파일 Assets/Scripts/Core/YutBoard.cs: BoardTile/BellData/YutBoard —
  말판 로직을 GameManager와 분리 (UI 참조 없는 순수 데이터+이동)
- 말판: 10~180점 중복 없는 무작위 배치 (보스전 시작 시 SetupBoard → 3판 고정,
  재도전 시 재배치). bellEventTileCount(기본 2)개 방울 칸 무작위 지정
- 베이직 변경: 도착 칸 점수 + 완주×10 + 윷별 강화치(basicBonus, 기본 0).
  구형 윷 기본 베이직 10~50 폐기 — YutStatEntry.basic → basicBonus 개칭,
  멀티플 2~6 유지. UpgradeYutStat(result, bonus, mult) 그대로 사용
- 이동: MovePiece(steps, path) — 도1/개2/걸3/윷4/모5, 마지막 칸 도달·통과=완주,
  이후 18칸마다 추가 완주. 경유 칸을 movePath에 기록 → 연출에서 한 칸씩 이동
  (moveStepDelay 0.15초, Inspector 조절). 말 위치·완주는 3판 유지
- 백도·낙: 말 이동 없음(규칙 미확정 — 임의 적용 안 함, 보고)
- 방울 이벤트: 방울 칸 착지 → 정산 연출 후 BellChoicePanel — 무작위 3개 제시 →
  SelectBellOption1~3 + ConfirmBellChoice() 또는 원클릭 SelectBellAndConfirm1~3.
  획득 → ownedBells + bellSlots 표시. 판당 칸 1회 발동, 판 종료 시 소멸.
  방울 실제 효과·사용 버튼 미구현 (bellPool은 테스트용 더미 6개)
- 연출 순서: 결과명 → 말 이동 → 칸 베이직 → 윷 멀티플 → 효과 단계 → 투척 점수 → 토탈+HP
- ⚠ 신규 Inspector 연결: boardTiles[18] (칸 오브젝트 — 자식 Text=점수, 'BellIcon'
  이름 자식=방울 표시), Player Piece (RectTransform), Lap Count Text,
  Bell Choice Panel + Bell Option Name/Desc Texts[3]씩, Move Step Delay,
  Bell Event Tile Count. 전부 미연결도 null-safe

GPT 요청(직전 — 이어서 기록): 보스전 5종 + 클리어/게임오버 + 정산 연출 완성.
- BossData(이름/maxHP/abilityDescription/nakBonusPercent) + bosses 리스트 5명:
  첫~다섯 번째 저승지기, HP 1000/2000/3000/4000/5000 — 이름·수치 전부 임시 테스트값
- 보스 선택: SelectBoss(int)/SelectBoss1()~SelectBoss5() — Start 상태에서만 동작.
  StartGame() = BeginBossFight()(보스 적용·전투 초기화·특수능력 적용) + EnterTargetSelection()
- 특수능력: 낙 +2%p(퍼센트포인트 가산) — 일반 5종에서 상대 비중 유지하며 비례 차감,
  백도 불변, 음수 보정. BeginBossFight 때마다 BuildWeights() 재구축 → 재도전 중복 없음.
  BackToBossSelect()는 ResetProbabilities()로 효과 제거
- 판 구조: roundsPerBoss=3, roundIndex, roundScore(이번 판 누적=백도 차감 범위 —
  이전 판 확정분 불침범 실체화). totalScore는 보스전 누적(판 넘어 유지)
- 상태: GameState에 Cleared/GameOver 추가(RoundEnd 폐기). 판 끝 → totalScore≥goalScore
  이면 EnterClear(), 아니면 roundIndex≥3이면 EnterGameOver(), 아니면 다음 판 타겟 재선택
- 보스 잔여 수치 = Max(0, goalScore − totalScore) — HP 바/텍스트 UI는 폐기됨
  (위 10-14 오후 항목 참조), 목표·총점 텍스트로 확인
- 정산 연출 완성: displayTotal 도입 — 토탈 텍스트·HP 바는 '토탈 합산' 단계에서만 갱신.
  throwScore 표시도 '투척 점수' 단계에서만 갱신 (정산 시점과 표시 시점 완전 분리)
- 결과 패널: clearPanel/gameOverPanel + 결과 텍스트 5종 (clearBossNameText/clearScoreText/
  gameOverBossNameText/gameOverScoreText/gameOverGoalText) + RetryBoss()/BackToBossSelect()
- ResetRound() 재정의 = RetryBoss() (같은 보스 1판부터 재시작)
- ⚠ 신규 Inspector 연결 필요: 보스 선택 버튼→SelectBoss1~5, 보스 UI(이름·능력),
  clearPanel/gameOverPanel + 결과 텍스트 5종 + clearMessageText + nextBossButton
  (OnClick→GoToNextBoss), 다시 도전→RetryBoss, 돌아가기→BackToBossSelect.
  전부 미연결도 null-safe
- 보스 잠금/해금, 클리어 보상, 판 결과 화면 — 미구현 (기획 대기). 다음 관문 수동
  이동은 GoToNextBoss()로 구현됨

GPT 요청(직전 — 이어서 기록): 투척별 점수 정산 + 연쇄 효과 연출 구조.
- 점수 모델 교체: 누적 풀×배율(baseScorePool×currentMultiplier) 폐기 → 투척마다 독립 정산.
  ThrowScore = 이번 투척 BasicScore × Multiplier → TotalScore에 1회만 합산.
  이번 투척의 베이직/멀티플은 임시값(basicScore/multiplier 필드)이라 매 투척 초기화, 미누적.
- 윷 스탯 테이블: baseYutStats — 도 10×2 / 개 20×3 / 걸 30×4 / 윷 40×5 / 모 50×6 (GPT 지시).
  런타임 yutStats 복사본 + UpgradeYutStat(result, basic, mult) API = 주술 영구 강화용.
  기존 "타겟 적중 시 배수 +1~+5 가산" 방식 폐기됨.
- 효과 기록 구조 (핵심): ScoreEffectStep — 원인(YutResult/Talisman/Bell/Spell/Fever),
  sourceId, 표시명, 대상(Basic/Multiplier), 연산(Add/Multiply + opValue), 적용 전/후 수치,
  증가량(delta), 재생 순서. ×2와 +2처럼 연산 종류와 실제 증가량을 구분 기록.
  ThrowSettlement = 한 투척의 확정 정산(초기/최종 스탯, throwScore, steps[]). currentSettlement 공개.
- 연출 분리: SettleThrow()에서 점수 즉시 확정 → PresentSettlement() 코루틴이 기록을 순서대로
  재생(결과명 → 초기 베이직·멀티플 → 효과 단계 → 투척 점수). effectStepDelay=0.35 테스트값.
  연출 중 isPresenting으로 RollYut 연타 무시 + rollButton 숨김. SkipPresentation() 추가 —
  스킵해도 동일 결과(점수는 이미 확정). 실제 아이템 이펙트 애니메이션은 미구현(구조만).
- MainResult: 윷 결과 이름만 표시(도/개/걸/윷/모/낙/백도). Start/TargetSelection에서
  오브젝트 숨김, Playing 진입 시 빈칸, 안내 문구 전부 제거.
- feverText 신규: FeverLabel() — 0빈칸 / 1 Hit / 2 피버 / 3+ 피버N.
- 백도 페널티 기준 변경: 베이직 풀 → 이번 판 누적 총점(totalScore) 차감/초기화.
- 피버 수식 이식(★검토 필요, 확정 아님): 구 기준 "전체 누적 배율 ×Min(콤보,8)" →
  투척 정산 구조로 "이번 투척 멀티플 ×Min(콤보,8)". 산출 차이 있으므로 수치 확정 필요.
- ⚠ 신규 Inspector 연결 필요: Throw Score Text / Fever Text 필드에 사용자 제작 Text 연결
  (미연결도 null-safe 동작). Base Yut Stats / Effect Step Delay는 Inspector 조정 가능.
- 삭제된 필드: baseScorePool, currentMultiplier (씬에 남은 구형 직렬화 키는 무시됨)

GPT 요청(직전 — 이어서 기록): 타겟 선택 화면을 전체 전환 방식에서 "메인 게임 화면 위에 뜨는 모달 팝업" 방식으로 변경.
시작 화면 → [게임 시작] → 메인 화면 노출 + 반투명 딤 + 중앙 타겟 팝업 → 선택 시 오버레이만 꺼지고 본 게임 진행.

Devin 구현 (YutGameManager.cs 코드만 — 오브젝트 생성·배치는 사용자가 에디터에서 수행):
- 신규 public 필드:
  * gamePanel — 메인 게임 화면 패널
  * targetSelectOverlay — 딤 배경 + 뒤쪽 클릭 차단용 전체화면 오버레이 (TargetSelectPanel의 부모 권장)
  * targetSelectPanel — 중앙 팝업. overlay 자식이므로 미연결도 동작
- 변경된 동작:
  * StartGame() → gamePanel ON + targetSelectOverlay ON + startPanel OFF → GameState.TargetSelection
  * SelectTargetAndStart(int)/개별 함수 → selectedTarget 저장 + overlay OFF → GameState.Playing
  * ResetRound() → overlay 다시 ON → 타겟 재선택
  * Awake() → startPanel ON / gamePanel·overlay·panel OFF 초기 상태 보장
- 타겟 미선택 시 투척 불가 (GameState.Playing 아니면 RollYut 무시 — 기존 로직 유지)
- 진단: 패널 미연결/부모 비활성 시 LogWarning (ActivatePanel 헬퍼, WarnIfPanelNested)
- 블러는 미구현 — 기본 UI Image 반투명 딤으로 대체, 추후 작업 예정

추가 GPT 요청(윷 결과 7종 + 저주 계열) — Devin 구현:
- YutResult enum 7종: Do/Gae/Geol/Yut/Mo(일반) + BackDo/Nak(저주). 타겟 선택은 일반 5종만
- 저주 판별 `IsCurse(YutResult)` + `HandleCurseResult()`로 일반/저주 처리 분리
- 백도: bossIndex ≤5 → 베이직 점수 차감(backDoPenalty) / ≥6 → 점수 풀 초기화. 투척 1회 소모
- 낙: 점수 0, 투척 1회 소모, 누적 점수 유지
- 확률: 인스펙터 편집 List<YutProbabilityEntry> baseProbabilities → 런타임 runtimeWeights 분리
  (ModifyProbability/ResetProbabilities API, 음수 보정, 합계 검증+정규화 추첨)
- 중앙 텍스트: 백도(차감/초기화)·낙 메시지 표시
- 구현 판단(검토 필요): 저주 발생 시 feverCombo 리셋 — 기획 미명시, 비타겟 콤보 초기화 규칙에 맞춰 적용

테스트값(미확정): 확률 개 32.5%/낙 5%, bossIndex=1, backDoPenalty=30 — 전부 Inspector 조정 가능

추가 GPT 요청(UI 텍스트 분리 + 버튼 표시) — Devin 구현:
- throwCountText 제거 → remainText(남은 횟수 숫자)/throwDataText(전체 횟수 숫자) 분리. 라벨·구분자 없이 숫자만
- basicScoreText/multScoreText → 숫자만 출력. targetYutText → 타겟 이름만 (미선택 시 빈 문자열)
- public GameObject rollButton 추가 — Playing 상태에서만 표시 (타겟 선택 모달/시작/종료 화면에서는 숨김)
- 피버 콤보의 텍스트 표시는 제거됨 (feverCombo 필드·로그는 유지)

- 추가 수정(사용자 요청): totalScoreText도 숫자만 출력으로 변경 + goalScoreText(목표 점수 숫자) 필드 추가 — 총점/목표점수 완전 분리

⚠ Inspector 재연결 필요 항목:
- Throw Count Text 필드가 제거됨 → 기존 텍스트를 Remain Text에, 전체 횟수용 텍스트를 Throw Data Text에 연결
- Roll Button 필드에 던지기 버튼 GameObject 연결 (미연결 시 버튼이 항상 보임)
- Goal Score Text 필드에 목표 점수 표시용 텍스트 연결 (기존 Total Score Text 연결은 그대로 유지됨)

추가 GPT 기획 원칙 (UI 확장성 — 구현 없이 기록만):
- 기준 해상도 1920×1080 (16:9), Canvas Scaler = Scale With Screen Size / Match 0.5
- UI는 Anchor로 리사이즈 대응, 배경 이미지와 HUD 요소의 스케일 방식 분리
- 21:9 울트라와이드·20:9 모바일 가로 테스트 가능한 설계 지향, 세로 전용 UI 미구현
- 에디터 수동 배치 + Inspector 연결 원칙 유지, 기존 UI 대규모 재배치 금지
- 주의: 현재 씬 Canvas Scaler는 Constant Pixel Size(800×600) — 적용하려면 에디터에서
  Scale With Screen Size(Reference 1920×1080, Match 0.5)로 변경 필요 (변경 시 기존 UI 배치 재조정됨)

사용자 에디터 작업 필요 (권장 Hierarchy):
  Canvas
  ├─ GamePanel               ← 메인 게임 UI 부모
  ├─ TargetSelectOverlay     ← 전체화면 Image(검정 알파 ~0.75, Raycast Target ON=뒤 클릭 차단)
  │   └─ TargetSelectPanel   ← 중앙 팝업: TitleText + 도/개/걸/윷/모 버튼 5개
  └─ StartPanel              ← 최상단(마지막 자식) 유지
  * UI 렌더 순서 = 형제 순서(나중=위). Overlay가 GamePanel보다 아래(나중)에 있어야 위에 그려짐
  * GameManager Inspector: gamePanel / targetSelectOverlay / targetSelectPanel / startPanel 연결
  * 각 타겟 버튼 OnClick → SelectTargetDo()~SelectTargetMo() 또는 SelectTargetAndStart(0~4)

주의사항:
- 코드로 UI를 동적 생성하는 방식은 폐기된 상태 유지 — 다시 추가하지 말 것
- Devin은 에디터 미실행 — 사용자 연결 후 Play 테스트 필요
- 누적 규칙: maxThrows 5, 투척별 정산(ThrowScore=Basic×Mult→TotalScore), 피버 ×Min(콤보,8)은 투척 멀티플에 적용(이식식, 검토 중)
```

## 현재 상태

- 마지막 작업 일시: 2026-10-18 오전
- 작업 담당 AI: Devin Desktop (Cognition)
- Git 커밋: 본 문서를 포함한 커밋 — `git log -1` 참조
- 현재 브랜치: `develop` (origin: https://github.com/Lee-jinuk/Ryy.git)
- 코드 구조: `YutGameManager` 총괄 + 책임별 POJO 매니저 6개 — `docs/ARCHITECTURE.md` 참조

### [2026-10-20 오후 (17차) - GPT 기획 / Devin 구현]

- 배율 이유 표시 전 단계 확장 (일반 상성 ×1 신규 표기) + 탐욕부 전용 연출 타이밍 — `PresentationConfig.greedStepPreDelay`/`greedStepPostDelay`(0.5s) 신규, `ScoreEffectStep.preApplyDelay`/`postApplyDelay`로 단계별 딜레이 기록. 취소 탐욕부는 상승 연출 없이 취소 안내만
- Inspector 작업 없음 (기존 `PresentationConfig` 에셋에 필드 추가만). 검증: dotnet build 0·0. Unity Play 대기

### [2026-10-20 오후 (16차) - GPT 기획 / Devin 구현]

- 사용 부적 패널 + 배율 이유 표시 (상세는 요약 블록·GAME_RULES 16차 참조): `UsedTalismanRecord`·`ScoreEffectStep.useId`/`reasonText` 신규, 사용 성공만 표시 기록, 단계별 이유→갱신→제거 연출
- `YutGameManager` 필드 4개 신규 (multiplierReasonText·usedTalismanPanel·usedTalismanContent·usedTalismanItemPrefab) + `UsedTalismanItem.prefab` — **사용자 Inspector 연결 필요**
- Scene 작업 없음. 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-20 오후 (15차) - GPT 기획 / Devin 구현]

- 부적 제외 범위 정정 (상세는 요약 블록·GAME_RULES 15차 참조): 14차 '활성 2종'은 범위 오류 — 실제는 일도부·이개부·삼걸부 3종만 제외, 나머지 14종 활성 복원. 에셋 값만 갱신, 코드 변경 없음

### [2026-10-20 오후 (14차) - GPT 기획 / Devin 구현]

- 윷 배율 도4/개3/걸4/윷9/모9 + `includeInDeck` 덱 필터 구조 + 사윷부→윷령부·오모부→모령부 이름 변경 (상세는 요약 블록·GAME_RULES 14차 참조). **'활성 2종' 적용은 15차에서 범위 오류로 정정됨**
- `TalismanData.includeInDeck` 신규 — 제외 카드의 정의·enum·효과 코드는 유지, `EnsureDeck`에서 필터
- Scene 작업 없음. 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-20 오전 (13차) - GPT 기획 / Devin 구현]

- 잡기 보상·추혼부 변경 (상세는 요약 블록·GAME_RULES 13차 참조): 잡기 ×3 + 잡기 시 유효 상성 ×2 고정, 추혼부 = 잡기 성공 시 추가 ×4 (대체 아님)
- `GameBalanceConfig.catchMultiplier`/`catchAffinityMult` 신규 — 에셋 반영 완료. `ThrowSettlement.actualAffinityMult` 신규
- `ScoreManager` 정산 순서 재구성 (잡기 판정 → 유효 상성 → 부적), 상성 단계 강제 표시, resultBonusMult 누락 수정
- Scene 작업 없음. 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-19 오후 (12차) - GPT 기획 / Devin 구현]

- 부적 17종 확장 + 밸런스 상향 (상세는 요약 블록·GAME_RULES 12차·§3 표 참조): 에셋에 수치·신규 카드 반영
- `YutThrowSession` 재작성: `specialRerolls`+`speciallyRerolled[]`(재기부 특별 기회), `moveDelta`(진격/후퇴), `armedResultBonus`/`succeededResultBonus`(일도부~오모부 대기·성공 — `Reroll` 성공 시 1회 평가)
- `ScoreManager`: `moveDelta` 이동 보정, `resultBonusMult` 체인 추가(칸×윷×결과부적×상성×잡기×부적), `AddResultBonusStep`, `AddAffinityStep` 시그니처 float화
- `TalismanData.targetResult` 추가. Scene 작업 없음
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-19 오후 (11차) - GPT 기획 / Devin 구현]

- 게임 설정을 ScriptableObject 에셋으로 분리 (상세는 요약 블록·GAME_RULES 11차 참조): `TalismanDatabase`/`BossDatabase`/`GameBalanceConfig`/`PresentationConfig` + `Assets/ScriptableObjects/*.asset`
- `YutGameManager` 설정 직렬화 필드 → 에셋 읽기 프로퍼티. `ConfigReady` 가드로 에셋 누락 시 진입 차단
- test2.unity의 구 `talismanPool` 직렬화 문제 해결 — 씬의 고아 직렬화 키는 무시되며 다음 씬 저장 시 자동 정리
- 사용자 Unity 작업 없음 — 씬 3개에 에셋 참조 연결 완료. 밸런스 수정 = 에셋 파일 편집만
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-19 오후 (10차) - GPT 기획 / Devin 구현]

- 부적 밸런스 변경 구현 (상세는 요약 블록·GAME_RULES 10차·§3 표 참조): 피해 부적 최종 배율 방식, 추혼부 잡기 ×4 대체, 인연부 2장, 봉쇄부 이동 금지, 탐욕 취소 정산 보관
- ⚠ 남은 미확정: 인연부 덱 1장 경계(임시 차단), 봉쇄 중복·보스 전환 이월, 승부부 재투척 0 불가(임시 유지), 미선택 후보 덱 복귀, 보스 말 이동 방향
- ⚠ 정정: 당시 '씬 미직렬화' 보고는 test2.unity를 누락한 부정확한 판단 — 실제로는 구 수치가 직렬화돼 사용 중이었고 11차 에셋 분리로 해결
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-19 오후 (9차) - GPT 기획 / Devin 구현]

- 손패 한도 10 + 초과 소멸 (상세는 요약 블록·GAME_RULES 9차 참조): `HandLimit`/`AcquireCard` 공통 경로, 선택 순서 획득 수정, 인연부 제거→획득 순서, `talismanNoticeText` 소멸 안내, 손패 UI 배열 8→10 + `UseTalisman9`/`10`
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기 — 손패 9+3 획득, 10장 인연부, 소멸 안내 확인 필요

### [2026-10-19 오후 (8차) - GPT 기획 / Devin 구현]

- 소모형 부적 시스템 구현 (상세는 요약 블록·GAME_RULES 8차·§3 표 참조): `TalismanData.cs`+`TalismanManager.cs` 신설, 덱=10종 각 1장·사용 소멸, 매 보스전 5택3 + 땅 3택1(방울 대체), 사용 구간=최초 투척 후~확정 전, 정산에 부적 단계(상성→잡기→부적 순차 곱셈), 탐욕 충돌 공격 0 우선, 재기·인연·봉쇄·승부부 동작 필드(`bonusRerolls`/`rerollsForfeited`/`pendingMoveReduction`)
- 미확정 임시 규칙 다수 — GAME_RULES §3 표의 미확정 행 참조, 확정 시 코드 주석 '미확정 임시' 검색
- 사용자 Unity 작업: 부적 선택창 패널(후보 이름/설명/레어도 텍스트 5조 + 확정 버튼) + 손패 UI(이름 텍스트·사용 버튼 8조) 생성 후 필드 연결 + OnClick(`SelectTalismanOption1~5`/`ConfirmTalismanChoice`/`UseTalisman1~8`)
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-19 오전 (7차) - GPT 기획 / Devin 구현]

- 보스 말 + 잡기 ×2 + 이동 단계 구현 (상세는 요약 블록·GAME_RULES 7차 참조): `bossPiece` 필드, `BossManager.bossPieceIndex`·`BossMoveStage`·`MoveBossPiece`·`ResetBossPiece`, `bossMoveStageMin/Max` 배열, 잡기 판정·`caught`/`catchMult`·잡기 step, 실효 배율에 잡기 포함
- 보스 말 초기 위치 확정: 모든 보스전 시작 시 `BeginBossFight` → `ResetBossPiece()`로 START 초기화 — 논리+화면 위치 함께, 소개·건너뛰기에서는 호출 없음
- 미확정 임시 구현: 보스 말 이동 방향(플레이어 동일)만 남음 — 확정 대기
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기 — `bossPiece` 연결 필요

### [2026-10-18 오전 (6차) - GPT 기획 / Devin 구현]

- 착지 정보·보스 상성·윷 4개 재투척 세션 구현 (상세는 요약 블록·GAME_RULES 6차 참조): `YutThrowSession.cs` 신설, `RollYut`/`RerollStick(1~4)`/`ConfirmThrow` 버튼 흐름, `BossAffinity` 런타임 상성(런당 보스별 1회 생성), 데미지 = 칸 점수 × 상성 × 윷 배율, `landingTileText`/`bossAffinityText`/세션 UI 필드 5종 추가, `rollButton` 제거, `숲`→`풀` 표시, `multiplier` float화
- 보고(미확정): 백도·낙 발생 조건 미확정으로 신규 경로 미발생(저주 처리 API는 유지), 보스 낙 +2%p 능력 미발동(대체 미확정), 마지막 투척 땅 착지 우선순위 미확정 유지
- 사용자가 `Test.unity`에 보스 소개 패널·30칸·텍스트 대부분 연결 완료 — 이번 세션 신규 UI 필드(`stickFaceTexts`/`rerollButtons`/`throwStatusText`/`rerollRemainText`/`confirmThrowButton`/`landingTileText`/`bossAffinityText`)는 미연결
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-18 오전 (5차) - GPT 기획 / Devin 구현]

- 30칸 속성 말판 전환: `TileAttribute`(START/불/물/풀/땅/쇠) + 독립 추첨(25/25/25/15/10%), 런당 1회 생성·게임오버까지 유지. `BoardTile` 속성 기반 재작성, `lapBonus`/`bonusScore`/`hasBellEvent`/`bellUsedThisRound`/`bellEventTileCount` 삭제
- 공격 = 도착 칸 공격점수(50/0) × 윷 배율 — 완주·`basicBonus` 가산 폐기. 땅 착지 = 방울 뽑기 화면(기존 UI 재사용, 미연결 시 경고 후 건너뜀)
- 씬: `boardTiles` 30칸 확장(18개 연결 유지 + 빈 슬롯 12 — 사용자가 칸 오브젝트 추가 필요)
- 보고: 마지막 투척 땅 착지 시 방울 없이 게임오버(소진 판정 우선) — 미확정
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-18 오전 (4차) - GPT 기획 / Devin 구현]

- 보스 선택 → 순차 소개 전환: `SelectBoss(int)`/`SelectBoss1~5` 제거. 소개 화면 = 진행 순서 보스 한 명 + '다음'(`EnterBossBattle`) / '건너뛰기'(신규 `SkipBoss`) 버튼
- `SkipBoss`: 소개 패널 열림 + Start/Cleared + 다음 보스 존재 시에만 `pendingBossIndex` 전진 — 전투·보상·처치 기록 없음. 신규 `skipBossButton` 필드는 마지막 보스 소개 시 자동 숨김
- `bossSelectPanel` 미연결 폴백(자동 전투 진입) 폐기 → 경고 후 대기. `ResetRunState`에 인덱스 초기화 추가 (새 런 = 첫 보스부터)
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-18 오전 (3차) - GPT 기획 / Devin 구현]

- 라운드·판 구조 폐지: 보스전 = 기본 투척 5회 단일 구간. `FinishPresentation` 판정: 클리어(`totalScore >= goalScore`, 마지막 투척 처치 우선) → 게임오버(5회 소진 미처치) → 방울 이벤트
- 제거: `roundIndex`/`roundsPerBoss`/`roundScore`/`roundProgressText`, `AdvanceToNextRound()`, `BellManager.ResetForNextRound()`. 방울 칸 = 보스전당 1회, 임시 방울 = 보스전 한정
- 백도 차감 기준 `min(totalScore, backDoPenalty)` (구 `roundScore`는 `totalScore`와 동치라 삭제 — 수치 동일). 보고: 구 '판 누적 상한' 소실
- 함수 제거: `RetryBoss`/`ResetRound`/`BackToBossSelect`. `RestartRun`이 런 초기화·패널 정리·시작 화면 복귀를 직접 수행. 씬 복귀 버튼 2개 OnClick → `RestartRun`으로 재연결 완료
- 씬 직렬화 키 `roundsPerBoss`/`roundIndex`/`roundScore` 제거
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-18 오전 (2차) - GPT 기획 / Devin 구현]

- `bossCurrentHPText` 신규 — `Max(0, goalScore - displayTotal)` 계산값 표시 (숫자만, null-safe, 별도 HP 상태 없음)
- 타겟·피버 시스템 전체 제거: `GameState.TargetSelection`·관련 필드/메서드 삭제, `EnterTargetSelection` → `EnterPlaying`. 모든 결과 투척 1회 소모. `SelectableTargets`→`NormalResults` 개칭
- 씬: `SelectTarget*` OnClick 5개·삭제 필드 직렬화 키 정리. `TargetSelectOverlay`/`TargetSelectPanel` 오브젝트 비활성 잔존
- ⚠ 신규 Inspector 연결 필요: `Boss Current HP Text` 필드에 Text 오브젝트 연결
- 보고(미수정): 백도 차감이 보스 체력 회복처럼 표시됨 — 규칙 미변경
- 검증: dotnet build 0 오류·0 경고. Unity Play 검증 대기

### [2026-10-18 오전 - GPT 기획 / Devin 구현]

- `YutGameManager` 책임별 분리 리팩터링: `YutRollManager`/`BoardManager`/`ScoreManager`/`BossManager`/`BellManager`/`GameUIManager` 신설 (일반 C# 클래스 — 직렬화 필드·public 메서드는 `YutGameManager`에 유지해 Inspector·OnClick 보존). 규칙 변경 없음
- 1단계 안정화: `BuildYutStats()` 원본 참조 공유 → 새 객체 복사 수정. 씬 `maxThrows` 3→5. `bossSelectPanel` 미연결 시 `EnterBossBattle()` 직행 폴백
- 씬 검사: 연결 양호. 미연결 = `bossSelectPanel`+텍스트 3종(오브젝트 없음), `roundProgressText`, `lapCountText`, `nextBossButton`, 방울 UI 전체. `EnterBossBattle`/`GoToNextBoss`/`RetryBoss`/`SelectBoss*` 버튼 없음 — 보고 완료
- 검증: dotnet build 0 오류·0 경고. Unity Play 테스트는 사용자 확인 필요

### [2026-10-16 오후 - GPT 기획 / Devin 구현]

- 보스 소개 화면: `bossSelectPanel` + `bossSelectNameText`/`bossSelectGoalText`/`bossSelectAbilityText` 필드, `GoToNextBoss()`는 소개 패널 표시만, `EnterBossBattle()`(진입 버튼)에서 실제 전투 진입. `StartGame()`도 같은 패널 경유 — 최초·다음 관문 UI 공유. `RetryBoss()` 즉시 재시작 유지
- 게임오버 전체 초기화: `ResetRunState()` 신규 — 런 데이터 전부 리셋. `RestartRun()`('처음으로' 버튼) = `BackToBossSelect()`와 동일 동작
- 판 번호 표시: `roundProgressText` — `"1 / 3"` 형식, Start 화면 빈칸
- 사용자 Inspector 연결 필요: `Boss Select Panel`, `Boss Select Name/Goal/Ability Text`, `Round Progress Text` — 전부 null-safe
- 미테스트: Devin 에디터 미실행 — Play 확인 필요. 보스전 건너뛰기 미확정(미구현)

### [2026-10-14 오후 - GPT 기획 / Devin 구현]

- GPT 기획: 타겟 선택을 "메인 화면 위 모달 팝업" 방식으로 전환 (딤 오버레이 + 중앙 팝업, 블러는 추후)
- Devin 구현: `gamePanel`/`targetSelectOverlay` 필드 추가, `StartGame`/`ConfirmTargetSelection`/`ResetRound`/`Awake`의 패널 토글 로직을 모달 흐름으로 갱신
- 사용자 에디터 작업 대기: GamePanel/TargetSelectOverlay/TargetSelectPanel 오브젝트 생성 + Inspector 연결 (권장 Hierarchy는 상단 요약 블록 참조)
- 미테스트: Devin은 에디터 미실행 — 연결 후 Play 확인 필요
- 사용자 확인: 타겟 버튼 무반응은 OnClick 미연결(코드 문제 아님) — 연결 후 정상 작동. 타겟 선택 함수들은 수정 없이 유지됨
- 윷 7종 확장(낙) + 저주 계열: `IsCurse`/`HandleCurseResult`, 확률 시스템 이원화(`baseProbabilities`↔`runtimeWeights`), 백도 보스 분기, 낙 처리 — 전부 코드 완료, 테스트값만 Inspector 조정 대기
- UI 텍스트 분리(GPT 기획): `remainText`/`throwDataText` 분리(숫자만), `basicScoreText`/`multScoreText`/`targetYutText` 라벨 제거, `rollButton` 필드 추가(Playing에서만 표시) — 코드 완료, Inspector 재연결 필요
- GPT 기획 원칙 기록(코드/씬 변경 없음): UI 확장성 가이드 — 기준 1920×1080(16:9), Canvas Scaler `Scale With Screen Size`/Match 0.5, 앵커 기반 리사이즈 대응, 배경 이미지와 HUD 요소 스케일 방식 분리, 21:9 울트라와이드·20:9 모바일 가로 대응 지향, 세로 전용 UI 미구현. 에디터 수동 배치·Inspector 연결 원칙 및 기존 UI 대규모 재배치 금지 유지
  - 참고: 현재 씬 Canvas Scaler = Constant Pixel Size(800×600) — 원칙 적용 시 에디터에서 수동 변경 필요 (자동 변경하면 기존 배치가 재조정되므로 코드/YAML로 임의 적용하지 않음)
- 투척별 점수 정산 + 효과 단계 구조 (GPT 기획 / Devin 구현): `ThrowScore = Basic × Mult` → `TotalScore` 1회 합산. `ScoreEffectStep`/`ThrowSettlement`로 부적·방울·주술·피버 발동 기록 → 코루틴 순차 재생 (`PresentSettlement`, `SkipPresentation`). 윷 스탯 `baseYutStats` (도10×2~모50×6) + `UpgradeYutStat()` 영구 강화 API. MainResult 결과명 전용 + `throwScoreText`/`feverText` 신규 필드. `baseScorePool`/`currentMultiplier` 폐기. 백도 차감 기준 `totalScore`로 변경. 피버 ×Min(콤보,8) 투척 멀티플 적용은 이식식 — 확정 필요
- 보스전 + 클리어/게임오버 (GPT 기획 / Devin 구현): `BossData` 5명 임시(HP 1000~5000), `SelectBoss1~5`, 낙 +2%p 공통 능력(비례 차감·백도 불변·중복 없음), 관문당 3판(`roundsPerBoss`), `roundScore` 판별 누적으로 백도 범위 실체화, `Cleared`/`GameOver` 상태 + 패널, 보스 HP UI(Image/Slider), `displayTotal` 토탈·HP 동기화 연출, `RetryBoss`/`BackToBossSelect`. ResetRound = 보스전 재시작으로 재정의
- 18칸 원형 말판 + 방울 이벤트 (GPT 기획 / Devin 구현): `YutBoard.cs` 분리, 점수 무작위 배치(보스전당), 도착 칸=베이직 + 완주×10 + 강화치, 윷별 1~5칸 이동·경유 연출(`movePath`/`moveStepDelay`), 방울 칸 이벤트(3택1, 판 한정 임시 방울, 판당 1회), 백도·낙 이동 없음(미확정 보고)

### [2026-10-14 오전 - Gemini & Devin 작업분]

- `activeInputHandler` 1→2 (Both) — 입력 런타임 예외 해결
- `maxThrows` 5 확정 반영 (스크립트 + 씬 직렬화 값 모두)
- 피버 배수 상한 8 (`feverCap` 필드, `appliedFever = Mathf.Min(feverCombo, feverCap)`)
- UI 방식 전환: 코드 동적 생성 폐기 → 인스펙터 수동 연결 방식 (사용자가 에디터에서 직접 배치)
  - public 변수: Text 6종(mainResult/basic/mult/total/throwCount/targetYut), 패널 2종(start/targetSelect), 슬롯 배열 2종(bellSlots[3], talismanSlots[4])
  - public 함수: `StartGame`, `SelectTargetAndStart(int)` + 개별 5종, `RollYut`, `ResetRound`
  - 플로우: Start → TargetSelection → Playing → RoundEnd, `GameState.Start` 추가
- `GameManager` 오브젝트 정리, 씬 직렬화 필드명 갱신, stray 스크립트 삭제, 세션 헤더 문서 규칙 추가
- 패널 전환 버그 리포트 대응: `StartGame()`에서 패널 미연결·부모 비활성(startPanel 자식 중첩) 상황을 `Debug.LogWarning`으로 명시, `Awake()`에 `WarnIfPanelNested()` 중첩 경고 추가

### [2026-10-14 - 초기 작업분]

- 로컬 Git 초기화, Unity `.gitignore`, `main` 초기 커밋, `develop` 분리
- 폴더 구조화: `Assets/Scripts/{Core,Data,UI}`, `Assets/Prefabs`, `Assets/ScriptableObjects`
- `YutGameManager.cs` → `Scripts/Core/` 이동 (.meta GUID 유지)
- 단일 타겟 피버 시스템 구현 (당시 지시 기준 — 이후 '검토 중' 분류를 거쳐 상한 8 포함 채택 확정)
- GitHub origin 연결, `main`/`develop` push 완료
- docs/ 협업 문서 체계 구축, `AI_CONTEXT.md` → docs 이관 완료

## 현재 문제점

- ~~`maxThrows = 7` ↔ 확정 규칙 5회 불일치~~ → 해결
- ~~피버 시스템 채택 여부 미결~~ → 해결 후 **폐기** (2026-10-18 오전, 타겟·피버 전체 제거)
- ~~`Input.GetKeyDown` 런타임 예외~~ → 해결 (Both 입력 핸들러)
- ~~`YutGameManager` 씬 미배치 / UI 미연결~~ → 해결 (씬 배치 확인 + 텍스트 필드 연결 유지)
- **UI 수동 연결 대기 (사용자 작업)** — 2026-10-18 `Test.unity` 검사 기준: 보스 소개 패널·소개 텍스트 3종·`skipBossButton`·30칸 `boardTiles`·`playerPiece`·주요 텍스트·OnClick(`StartGame`/`EnterBossBattle`/`SkipBoss`/`GoToNextBoss`/`RestartRun`/`RollYut`)은 **사용자가 연결 완료**. **미연결 = `bossPiece`(보스 말 오브젝트 생성 필요), 투척 세션 UI 7종(`stickFaceTexts[4]`/`rerollButtons[4]`/`throwStatusText`/`rerollRemainText`/`confirmThrowButton`), `landingTileText`, `bossAffinityText`, `bossSelectAffinityText`, `bellChoicePanel`+옵션 텍스트·슬롯(땅 칸 착지 시 경고 후 건너뜀), `lapCountText`, `nextBossButton`** + `RerollStick1~4`/`ConfirmThrow`/`SkipPresentation`/방울 선택 버튼 OnClick. SampleScene은 구버전 — boardTiles 18개만 연결됨, 나머지 미정리 (테스트는 Test.unity 기준)
- **블러 미구현**: 타겟 선택 모달 폐기와 함께 해당 UI는 비활성 잔존 — 씬의 `TargetSelectOverlay`/`TargetSelectPanel`/`TargetYut`/`FeverPanel` 오브젝트는 사용자가 삭제 여부 결정
- **Unity Play 테스트 미수행**: Devin은 에디터 미실행 — dotnet build(0 오류)까지만 검증. 사용자가 Unity에서 컴파일·Play 확인 필요
- **백도 ↔ 보스 체력 표시 (보고)**: 백도 점수 차감 → `totalScore` 감소 → `bossCurrentHPText`가 체력 회복처럼 표시됨 — 규칙 미변경

## 미확정 결정 사항 (사용자 승인 필요)

- ~~피버 적용 수식~~ → 무효 (2026-10-18 오전 피버 시스템 폐기)
- 보스 이름·HP(1000~5000)·특수능력 수치(낙 +2%p) — 전부 임시 테스트값, 밸런스 검토 필요. 말판 도입으로 판당 점수 상승 → 목표 점수 밸런스 조정 대상 (임의 변경 안 함)
- **백도·낙 말 이동 규칙** — 현재 이동 없음으로 구현, 규칙 확정 필요 (역방향 이동·고정 등 기획 대기)
- **방울 실제 효과·사용 방식** — 현재 획득·표시만 (bellPool은 테스트 더미). 12장 덱 구상과의 통합 규칙 미확정
- `effectStepDelay` 연출 간격 (테스트값 0.35초), `moveStepDelay` 말 이동 간격 (0.15초), `bellEventTileCount` (프로토타입 2)
- 백도 페널티 수치 감각 (차감량 `backDoPenalty`는 테스트값)
- 윷 확률 테스트값 확정 여부: 개 32.5%/낙 5% (낙 추가 조정분), `backDoPenalty` 차감량, `bossIndex` 분기 기준 — 전부 테스트값
- ~~저주 발생 시 피버 콤보 리셋 여부~~ → 무효 (피버 폐기)
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. **GPT 6단계 지시서의 2단계 착수 전 선행 질문 답 필요** (사용자 승인 대기): 윷 4면↔결과 대응 규칙, 개별 윷 면 확률(최초 투척·재투척 동일 여부), 백도·낙 프로토타입 포함 여부. 3~6단계 질문도 지시서 표 참조
2. 사용자가 에디터에서 신규 UI 제작·연결: `bossSelectPanel` + 소개 텍스트 3종 + '진입' 버튼(→`EnterBossBattle`), '다음 관문' 버튼(→`GoToNextBoss`)을 `nextBossButton`에 연결, `bossCurrentHPText`(보스 현재 체력 Text), `lapCountText`, 보스 선택 버튼(→`SelectBoss1~5`) → Play 테스트. 잔존 오브젝트 정리 선택: `TargetSelectOverlay`/`TargetSelectPanel`/`TargetYut` 텍스트/`FeverPanel`은 삭제해도 무방(코드 미사용)
3. 백도·낙 말 이동 규칙 확정 → 확정 시 `SettleThrow` 저주 분기에 추가
4. 방울 실제 효과 기획 확정 → `BellData`에 효과 필드 추가 + 사용 버튼/`ScoreEffectStep` 연동
5. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기존 12장 덱 구상과 이벤트 방울 통합 규칙 확정 후 착수
6. 보스 잠금/해금·클리어 보상·다음 보스 자동 이동·판 결과 화면 — 기획 확정 후 착수

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs` — 게임 총괄(상태 전환·흐름·OnClick·직렬화 필드)
- `Assets/Scripts/Core/` — `YutRollManager.cs`, `BoardManager.cs`, `ScoreManager.cs`, `BossManager.cs`, `BellManager.cs`, `YutBoard.cs`
- `Assets/Scripts/UI/GameUIManager.cs` — UI 표시·연출
- `Assets/Scenes/SampleScene.unity` (`Gamemanager` 오브젝트에 `YutGameManager` 컴포넌트 배치됨 — 새 매니저는 POJO라 추가 배치 불필요)
- `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2`)
- `docs/ARCHITECTURE.md` (매니저 역할·데이터 소유권), `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
