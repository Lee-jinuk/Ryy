# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-18 오전 작업 요약 (다음 AI 전달용)]

```
[2026-10-18 오전 (6차) 변경사항 요약 - GPT 기획 / Devin 구현]

★ 최신 — 착지 정보 + 보스 상성 + 윷 4개 재투척 세션 (사용자 승인):
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
- **UI 수동 연결 대기 (사용자 작업)** — 2026-10-18 `Test.unity` 검사 기준: 보스 소개 패널·소개 텍스트 3종·`skipBossButton`·30칸 `boardTiles`·`playerPiece`·주요 텍스트·OnClick(`StartGame`/`EnterBossBattle`/`SkipBoss`/`GoToNextBoss`/`RestartRun`/`RollYut`)은 **사용자가 연결 완료**. **미연결 = 투척 세션 UI 7종(`stickFaceTexts[4]`/`rerollButtons[4]`/`throwStatusText`/`rerollRemainText`/`confirmThrowButton`), `landingTileText`, `bossAffinityText`, `bossSelectAffinityText`, `bellChoicePanel`+옵션 텍스트·슬롯(땅 칸 착지 시 경고 후 건너뜀), `lapCountText`, `nextBossButton`** + `RerollStick1~4`/`ConfirmThrow`/`SkipPresentation`/방울 선택 버튼 OnClick. SampleScene은 구버전 — boardTiles 18개만 연결됨, 나머지 미정리 (테스트는 Test.unity 기준)
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
