# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-14 오후 작업 요약 (GPT 전달용)]

```
[2026-10-14 오후 변경사항 요약 - GPT 기획 / Devin 구현]

★ 최신 — GPT 요청(투척별 점수 정산 + 연쇄 효과 연출 구조) — Devin 구현:
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

- 마지막 작업 일시: 2026-10-14 오전
- 작업 담당 AI: Devin Desktop (Cognition)
- Git 커밋: 본 문서를 포함한 커밋 — `git log -1` 참조 (직전: `0ddd3b7`)
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
- 라운드 목표 점수 값 (현재 테스트값 1000)
- `effectStepDelay` 연출 간격 (테스트값 0.35초)
- 비타겟/백도 페널티 수치 감각 (`GAME_RULES.md` 7절 '검토 중')
- 윷 확률 테스트값 확정 여부: 개 32.5%/낙 5% (낙 추가 조정분), `backDoPenalty` 차감량, `bossIndex` 분기 기준 — 전부 테스트값
- 저주 발생 시 피버 콤보 리셋 여부 (구현은 리셋 — 기획 미명시라 확인 필요)
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. 사용자가 `ThrowScoreText`/`FeverText` Text 오브젝트를 에디터에서 만들어 Inspector 연결 후 Play 테스트 → 점수 정산·연출 순서 확인
2. 피버 적용 수식 확정 (×콤보 vs +증가 등) → 확정 시 `SettleThrow` 피버 단계 한 곳만 수정
3. 타겟 선택 오버레이 블러 효과 적용 여부 검토 (현재 반투명 딤)
4. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기획 확정 후 착수. `bellSlots`/`talismanSlots` 배열과 `ScoreEffectStep` 기록 구조는 이미 준비됨
5. 부적·방울 실제 아이템 구현 시 `SettleThrow()`의 "3) 향후 효과" 지점에 단계 추가 + 연출에서 아이템 이미지 강조/진동 확장

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs` (게임 로직 — 점수 정산/효과 기록/상태 전환. UI 동적 생성 없음)
- `Assets/Scenes/SampleScene.unity` (`GameManager` 오브젝트에 매니저 컴포넌트 배치됨)
- `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2`)
- `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
