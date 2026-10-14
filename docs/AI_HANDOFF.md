# AI_HANDOFF — RYY

AI 간 작업 인수인계 문서. 작업 완료 시마다 갱신한다.
갱신 시 `### [날짜 오전/오후 - 담당 AI]` 세션 헤더로 구분하고, 최상단 요약 블록을 최신 작업으로 교체한다.

## [2026-10-14 오후 작업 요약 (GPT 전달용)]

```
[2026-10-14 오후 변경사항 요약 - GPT 기획 / Devin 구현]

GPT 요청: 타겟 선택 화면을 전체 전환 방식에서 "메인 게임 화면 위에 뜨는 모달 팝업" 방식으로 변경.
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
- 누적 규칙: maxThrows 5, 피버 배수 상한 8 (appliedFever = Min(feverCombo, feverCap))
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

- 코드의 임의 점수/배율/목표 점수 값 채택 여부 (`GAME_RULES.md` 5절 전부 미확정: 도10~모50, 배수+1~+5, 목표 1000)
- 비타겟/백도 페널티 수치 감각 (`GAME_RULES.md` 7절 '검토 중')
- 윷 확률 테스트값 확정 여부: 개 32.5%/낙 5% (낙 추가 조정분), `backDoPenalty` 차감량, `bossIndex` 분기 기준 — 전부 테스트값
- 저주 발생 시 피버 콤보 리셋 여부 (구현은 리셋 — 기획 미명시라 확인 필요)
- 방울 개별 능력, 부적 능력, 중단 시스템 상세 — `GAME_RULES.md` 미확정 목록 참조

## 다음 작업 목표 (제안)

1. 사용자가 에디터에서 모달 UI 배치·인스펙터 연결 완료 후 Play 테스트 → 피버 상한 8·배율 밸런스 감각 보고
2. 타겟 선택 오버레이 블러 효과 적용 여부 검토 (현재 반투명 딤)
3. `GAME_RULES.md` 5절 점수 계산 규칙 확정 → 코드 임의값 반영 여부 결정
4. 방울 덱 데이터 구조 설계 (`ScriptableObject` 후보) — 기획 확정 후 착수. `bellSlots`/`talismanSlots` 배열은 이미 준비됨

## 관련 파일

- `Assets/Scripts/Core/YutGameManager.cs` (게임 로직 + UI 자동 생성)
- `Assets/Scenes/SampleScene.unity` (`GameManager` 오브젝트에 매니저 컴포넌트 배치됨)
- `ProjectSettings/ProjectSettings.asset` (`activeInputHandler: 2`)
- `docs/GAME_DESIGN.md`, `docs/GAME_RULES.md`, `docs/DEVELOPMENT_LOG.md`
- `AGENTS.md`
