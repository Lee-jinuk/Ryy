# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-15 작업 요약 (GPT 전달용)]

```
[2026-10-15 변경사항 요약 - GPT 기획 / Devin 구현]

★ 최신 — GPT 요청(보스 클리어 후 다음 관문 진행) — Devin 구현:
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

- 마지막 작업 일시: 2026-10-14 오후
- 작업 담당 AI: Devin Desktop (Cognition)
- Git 커밋: 본 문서를 포함한 커밋 — `git log -1` 참조
- 현재 브랜치: `develop` (origin: https://github.com/Lee-jinuk/Ryy.git)

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
- ~~피버 시스템 채택 여부 미결~~ → 해결 (상한 8 포함 채택, 밸런스 수치는 계속 검토)
- ~~`Input.GetKeyDown` 런타임 예외~~ → 해결 (Both 입력 핸들러)
- ~~`YutGameManager` 씬 미배치 / UI 미연결~~ → 해결 (씬 배치 확인 + 텍스트 필드 연결 유지)
- **UI 수동 연결 대기 (사용자 작업)**: 사용자가 에디터에서 gamePanel/targetSelectOverlay/targetSelectPanel/버튼/슬롯 오브젝트를 만들어 Inspector에 연결할 예정. 연결 전까지는 텍스트가 비어 보이지만 코드는 null-safe로 정상 동작함.
- **블러 미구현**: 타겟 선택 모달 배경은 반투명 딤(Image 알파)만 적용. 실제 블러는 셰이더/패키지 필요로 추후 작업.

## 미확정 결정 사항 (사용자 승인 필요)

- **피버 적용 수식**: 투척 정산 구조로 "이번 투척 멀티플 ×Min(콤보, 8)"로 이식됨 — 구 기준(전체 누적 배율)과 산출 차이 있음. 확정 또는 대안 수치 필요 (GPT 예시의 '+증가' 방식도 구조는 지원됨)
- 보스 이름·HP(1000~5000)·특수능력 수치(낙 +2%p) — 전부 임시 테스트값, 밸런스 검토 필요. 말판 도입으로 판당 점수 상승 → 목표 점수 밸런스 조정 대상 (임의 변경 안 함)
- **백도·낙 말 이동 규칙** — 현재 이동 없음으로 구현, 규칙 확정 필요 (역방향 이동·고정 등 기획 대기)
- **방울 실제 효과·사용 방식** — 현재 획득·표시만 (bellPool은 테스트 더미). 12장 덱 구상과의 통합 규칙 미확정
- `effectStepDelay` 연출 간격 (테스트값 0.35초), `moveStepDelay` 말 이동 간격 (0.15초), `bellEventTileCount` (프로토타입 2)
- 비타겟/백도 페널티 수치 감각 (`GAME_RULES.md` 7절 '검토 중')
- 윷 확률 테스트값 확정 여부: 개 32.5%/낙 5% (낙 추가 조정분), `backDoPenalty` 차감량, `bossIndex` 분기 기준 — 전부 테스트값
- 저주 발생 시 피버 콤보 리셋 여부 (구현은 리셋 — 기획 미명시라 확인 필요)
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. 사용자가 에디터에서 신규 UI 제작·연결: `boardTiles[18]`(칸 오브젝트 — 자식 Text=점수, 자식 `BellIcon`=방울 표시), `playerPiece`, `bellChoicePanel` + 옵션 텍스트·버튼, 보스 선택 버튼(→`SelectBoss1~5`), 클리어·게임오버 패널 + `nextBossButton`(OnClick→`GoToNextBoss()`) + `clearMessageText`(선택) → Play 테스트
2. 피버 적용 수식 확정 (×콤보 vs +증가 등) → 확정 시 `SettleThrow` 피버 단계 한 곳만 수정
3. 백도·낙 말 이동 규칙 확정 → 확정 시 `SettleThrow` 저주 분기에 추가
4. 방울 실제 효과 기획 확정 → `BellData`에 효과 필드 추가 + 사용 버튼/`ScoreEffectStep` 연동
5. 타겟 선택 오버레이 블러 효과 적용 여부 검토 (현재 반투명 딤)
6. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기존 12장 덱 구상과 이벤트 방울 통합 규칙 확정 후 착수
7. 보스 잠금/해금·클리어 보상·다음 보스 자동 이동·판 결과 화면 — 기획 확정 후 착수

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs` (게임 로직 — 점수 정산/효과 기록/상태 전환. UI 동적 생성 없음)
- `Assets/Scenes/SampleScene.unity` (`GameManager` 오브젝트에 매니저 컴포넌트 배치됨)
- `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2`)
- `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
