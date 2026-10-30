# DEVELOPMENT_LOG — RYY

실제 Unity 프로젝트를 검사해 기록한 구현 현황. 코드를 확인하지 않은 기능은 구현 완료로 기록하지 않는다.

## 파일 구조 (2026-10-18 검사)

- `Assets/Scripts/Core/` — `YutGameManager.cs`(게임 총괄) + 책임별 매니저 `YutRollManager.cs`, `BoardManager.cs`, `ScoreManager.cs`, `BossManager.cs`, `BellManager.cs` + 데이터 `YutBoard.cs`, `YutThrowSession.cs`(윷 4개 투척 세션)
- `Assets/Scripts/UI/` — `GameUIManager.cs` (UI 갱신 조정·정산 연출) + `BellSlotUI.cs`(슬롯) + 화면별 UI 컴포넌트 16종 (`StartScreenUI`·`BossSelectScreenUI`·`GameScreenUI`·`ClearScreenUI`·`ShopScreenUI`·`GameOverScreenUI`·`YutThrowUI`·`ScoreUI`·`BossCombatUI`·`BoardUI`·`TalismanHandUI`·`EquippedBellUI`·`TalismanChoiceUI`·`MoveAdjustUI`·`FortuneChoiceUI`·`UsedTalismanUI`)
- `Assets/Scripts/Editor/` — `UITransferTool.cs` (레거시 UI 참조 → 컴포넌트 이전 도구, 에디터 메뉴)
- `Assets/Scripts/Data/` — `TalismanData.cs` (부적 카드 정의 자료형 — `TalismanRarity`/`TalismanEffectKind`/`TalismanData`)
- `Assets/Prefabs/`, `Assets/ScriptableObjects/` — `.gitkeep`만 존재
- `Assets/Scenes/SampleScene.unity` — Canvas, StartPanel, GamePanel 계열, TargetSelectOverlay/Panel, Clearpanel, Gameoverpanel, 보드 칸 18개 + PlayerPiece, EventSystem 확인
- 매니저 역할·데이터 소유권 상세: `docs/ARCHITECTURE.md`

### [2026-10-29 (59차) - 사용자 지시 / Devin 구현 — 방울 슬롯 기본 4·최대 8 + 전투 화면 자동 배치]

- **`ShopConfig`**: `maxBells`(4) → `baseBellSlots`(4, `FormerlySerializedAs` 이전) + `maxBellSlots`(8) — 기존 에셋 값 자동 승계
- **`BellManager`**: `openBellSlots` 런타임 상태 추가 — 보유 수(`ownedBells.Count`)와 별도 관리. `BaseBellSlots`/`MaxBellSlots`/`OpenSlots` 조회, `TryAddBell` 한도 검사가 `OpenSlots` 기준. `ResetForNewRun`에서 기본 4칸으로 리셋. 확장 공용 경로 `ExpandBellSlots(amount)` — **확장 아이템 종류·수치·지속은 미확정으로 임의 추가 없음**, `YutGameManager.ExpandBellSlots()`(+1)는 테스트·OnClick용 래퍼
- **`EquippedBellUI` 자동 배치**: 슬롯 배열 8칸 + `slotContainer`(미연결 시 첫 슬롯 부모) + `bigSlotSize`/`smallSlotSize`/`slotGap`/`rowGap` Inspector 조절. 열린 수 기준: ≤4 = 큰 크기 한 줄, 5~8 = 작은 크기 두 줄(5→3+2·6→3+3·7→4+3·8→4+4, 각 줄 가운데). 슬롯 앵커를 컨테이너 중앙으로 정규화 후 배치. `i >= open` 슬롯은 `SetActive(false)` 잠김 숨김 — 열린 빈 칸은 표시. 배치 기준이 열린 수라 **판매 시 재배치 없음**
- **`ShopScreenUI`**: `shopBellSlots` 4→8칸(보유 수만큼 채움, 최대 8 처리), 구매 가능 검사 `MaxBells`→`OpenSlots`. 구매 후보 `shopOfferSlots` 4칸은 별개 — 유지
- **`ShopManager`**: 실패 로그 `MaxBells`→`OpenSlots`
- 씬 변경 없음 — `equipBellSlots` 배열 길이는 Inspector에서 8로 늘려 슬롯 오브젝트 연결 필요 (현 씬 4칸 직렬화 유지, null-safe). test3 배치·이미지 보존 — 배치는 코드가 덮어씀
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — 열린 4/5/6/7/8 배치·빈 슬롯 표시·장착 한도·구매/판매 후 갱신·`ExpandBellSlots` 테스트 확인 필요

### [2026-10-29 (58차) - 사용자 지시 / Devin 구현 — 손패 구 배열 제거 + test2 카드 슬롯 이전]

- **제거**: `TalismanHandUI`의 `talismanHandNameTexts`/`talismanHandDescTexts`/`talismanUseButtons` 필드 + `RefreshSlots`(구 텍스트 슬롯 경로) + `talismanUseButtons` 기반 런타임 자동 부착 폴백. `YutGameManager`의 동명 접근자 3개 + `legacyTalismanHandNameTexts`/`legacyTalismanHandDescTexts`/`legacyTalismanUseButtons` 필드. `talismanNoticeText` 계열은 유지
- **test2.unity 씬 이전** (구 배열에 실참조가 있던 유일한 씬): 10개 `HandCard` GO에 `TalismanCardUI` 컴포넌트 신규 부착(`&900000001`~`&900000010`) — background=카드 Image·nameText=기존 이름 Text·button=기존 Button·slotIndex·handUI 연결. `TalismanHandUI.cardSlots`에 10개 연결 후 구 배열·YGM legacy 배열 삭제. 카드 안 설명 Text 오브젝트 10개는 `m_IsActive: 0` (공용 설명창 방식 — 코드가 더 이상 비우지 않으므로 숨김 처리). 카드 Button의 `OnClick→UseTalismanN`은 유지 — test2엔 사용 영역이 없어 제거 시 사용 불가가 됨
- **test3/Test/SampleScene**: 전부 빈 참조 배열만 있어 행 삭제만. `_Recovery` 백업 씬은 미수정
- **UITransferTool**: `legacy`+필드명 리플렉션 매핑이라 양쪽 필드 제거 시 자동 스킵 — 무수정
- `PrepareCards`는 이제 연결된 `cardSlots`만 준비(부모·원래 Y·형제 순서 기록) — 미연결 슬롯은 건너뜀
- 검증: `dotnet build` 오류 0·경고 0 + 씬 YAML 대조(cardSlots 10칸 연결·구 필드명 0건·desc GO 비활성). **Unity Play 미검증** — 카드 표시·설명·정상 사용·불가 카드·드래그 취소·사용 후 재정렬 확인 필요

### [2026-10-29 (57차) - 사용자 지시 / Devin 구현 — 부적 손패 이미지 카드화 + 위로 드래그 사용]

- **신규 `TalismanCardUI.cs`** (`Assets/Scripts/UI/`) — 카드 1장 컴포넌트(Image+Button 오브젝트에 부착). `IPointerClickHandler`=클릭 설명, `IBeginDrag/IDrag/IEndDrag`=드래그. 드래그 발생 시 EventSystem이 PointerClick을 보내지 않아 클릭·드래그가 자동 분리됨. 실제 사용 판정·소모·효과는 손대지 않고 `gm.UseTalisman(인덱스)` 경유
- **`TalismanHandUI` 카드 모드 추가** — 신규 필드: `cardSlots[10]`·`yellowSprite`/`whiteSprite`·`cardContainer`·`descriptionPanel`+`descriptionNameText`+`descriptionText`·`useDropArea`·`handCountText`·`cardGap`·`descriptionOffset`·`dragUseMinPixels`(80px)
  - **표시**: 보유 카드만 활성, 빈 슬롯 `SetActive(false)`. 카드엔 제목 Text만 (설명은 카드 안에 두지 않음 — 카드 자식의 구 설명 Text는 빈칸으로 비움). 배경은 `TalismanData.appearance`(노란부적→yellowSprite/하얀부적→whiteSprite/미정→배치 스프라이트 유지). 이름·색상으로 사용 가능 여부를 판단하지 않음 — `useTiming`/`CanUseCard()` 경로 유지
  - **배치**: 모든 카드 같은 Y(처음 배치 Y)·회전·크기 — X 간격만 조절. `spacing = min(카드너비+gap, (컨테이너너비−카드너비)/(장수−1))` — 넘치면 음수로 균등 겹침. 획득·사용·보스전 리셋 시 `Refresh` 경유 자동 재정렬
  - **클릭**: 공용 설명창을 해당 카드 상단 위에 배치 — 이름+설명 표시, 캔버스 경계 클램프. 같은 카드 재클릭=닫기. 사용 불가 카드도 설명 표시 (선택≠사용 가능). `descriptionPanel` 미연결 시 무시
  - **드래그 사용(이번 대화에서 제안한 신규 입력 방식)**: 카드가 포인터를 따라가고 `SetAsLastSibling`으로 맨 앞 표시. 드롭 시 `위로 dragUseMinPixels 이상 + useDropArea 안`이면 사용 시도 — 손패 변동에 대비해 인덱스가 아닌 **카드 인스턴스(`boundCard`)를 `hand.IndexOf`로 재검사** 후 `gm.UseTalisman` 호출. 영역 밖·거리 부족·조건 불충족이면 재정렬로 원위치 복귀(미소모). `useDropArea` 미연결 시 드래그 사용 없음
  - **마이그레이션**: `cardSlots` 빈 칸은 `talismanUseButtons[i]`에 `TalismanCardUI`를 런타임 자동 부착(배경=버튼 Image·이름=구 nameTexts) — `test2.unity`는 씬 수정 없이 카드 모드로 전환. 카드가 하나도 없는 씬만 구 `RefreshSlots`(텍스트 슬롯) 경로
- **test2.unity 동작 변화**: 카드 숨김·겹침 배치·설명 비움이 적용되나, `descriptionPanel`/`useDropArea`가 없어 클릭=구 OnClick(UseTalismanN)으로 사용되는 기존 동작 유지, 드래그는 원위치 복귀만. **주의** — test2에 설명창·사용 영역을 추가할 때는 HandCard의 `OnClick→UseTalismanN` 연결을 제거해야 클릭 시 사용+설명 이중 동작이 생기지 않음
- **test3.unity**: 손패 오브젝트 미존재 — 사용자가 명세 계층(TalismanHandPanel>CardContainer>Card1~10 + TalismanUseArea + TalismanDescriptionPanel) 구성 후 새 필드 연결 예정. 카드 Button에는 OnClick 연결 금지, NameText Raycast Target 해제
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — 손패 0·1·10장, 노란/하얀 배경, 설명창 표시·화면 클램프, 정상 사용, 드래그 취소/영역 밖 복귀, 사용 불가 카드 설명, 카드 제거 후 재정렬 확인 필요

### [2026-10-29 (56차) - 사용자 지시 / Devin 구현 — 불꽃 횟수 표시 + 버튼 눌림 스프라이트]

- `YutThrowUI`: `flameImages[7]` 필드 추가 — `Refresh`에서 `maxThrows - currentThrows`만큼 왼쪽부터 `enabled` 토글 표시 (0이면 전부 숨김). 7칸은 표시 용량 — 투척 횟수 규칙 변경 없음. `UpdateUI` 경유라 확정 차감·새 보스전·재시작 시 자동 갱신
- `YutGameManager`: `flameImages` 접근자 추가 (컴포넌트 전용 — 신규 필드라 레거시 없음)
- `test2.unity` 던지기·확정 버튼(`&1361701825`·`&1820903027`): `m_Transition` 1→2(Sprite Swap) + `m_PressedSprite` = `버튼3 눌림_0` — OnClick·활성화 조건 원본 유지, 눌림용 별도 코드 없음. 두 버튼 배경은 현재 기본 UISprite — 눌림 스프라이트만 지정됨 (배경 이미지 적용 시 눌림 매칭 재확인 필요)
- `test3.unity`: `flameImages`는 배치 후 Inspector에서 Image 7개를 순서대로 연결
- 검증: `dotnet build` 오류 0·경고 0 + 씬 YAML 확인(Transition=2·PressedSprite 연결). **Unity Play 미검증** — 불꽃 차감/초기화·버튼 눌림/해제 확인 필요

### [2026-10-29 (55차) - 사용자 지시 / Devin 구현 — 윷 이미지 클릭 방식 정리]

- 씬 구조 감사 결과: `test2.unity`의 `Button (Legacy)~(3)`는 별도 재투척 버튼이 아니라 **윷 오브젝트 자체**(자식 `Image` = `stickFaceImages[i]` 면 표시) — 제거 대상인 "윷과 따로 배치된 재투척 버튼"은 존재하지 않았음
- `OnClick → RerollStick1~4`·Button 컴포넌트는 원본 유지 — stickFaceImages 순서와 버튼 순서 일치 확인 (`images[0]`부모=RerollStick1 … `[3]`=RerollStick4)
- `test2.unity` `YutThrowUI.rerollButtons` 빈 참조 → 윷 자체의 Button 4개 연결 (`1768085321`/`1158747532`/`1185707446`/`825652101`) — 재투척 불가 시 `interactable=false` 비활성화 경로 완성
- `stickFaceTexts` 제거는 54차에서 완료 — 이번 변경은 씬 참조 연결만
- `test3.unity`: `rerollButtons`·면 이미지·OnClick 모두 미연결 상태 — 사용자가 배치 중인 씬이라 수동 연결 안내 (윷 오브젝트에 Button+OnClick `RerollStick1~4` 부착 후 `rerollButtons` 배열에 연결)
- 검증: `dotnet build` 오류 0·경고 0 + YAML 대조(OnClick 타겟=YGM `&467659387`·순서 일치). **Unity Play 미검증** — 윷 클릭 재투척·횟수 소진 시 비활성화 확인 필요

### [2026-10-29 (54차) - 사용자 지시 / Devin 구현 — stickFaceTexts 제거 (면 표시 이미지 전용)]

- `YutThrowUI`: `stickFaceTexts` 필드 + Refresh의 앞·뒤 Text 갱신 블록 제거 — 면 표시는 `stickFaceImages` + 앞·뒤 스프라이트로 통일 (낙·미투척 숨김 판정은 이미지 경로에 유지)
- `YutGameManager`: `stickFaceTexts` 접근자 + `legacyStickFaceTexts` 필드 제거
- `UITransferTool`: 수정 불필요 — 리플렉션으로 `legacy`+필드명을 매핑하므로 양쪽 필드 제거 시 자동 스킵
- 씬 정리: `test2`·`test3`의 `stickFaceTexts` 4칸 빈 참조 행, `Test`·`SampleScene`의 `[]` 행 제거 — 전 씬에서 실 연결 없었음(모두 `{fileID: 0}`/`[]`). `stickFaceImages`·스프라이트·`rerollButtons` 연결 보존
- 검증: `dotnet build` 오류 0·경고 0 + 전 코드/씬 `stickFaceTexts` 잔여 0건. **Unity Play 미검증** — 면 PNG 표시·재투척 버튼 동작 확인 필요

### [2026-10-29 (53차) - 사용자 지시 / Devin 구현 — 결과·재투척 표시 형식]

- `YutThrowUI.Refresh`: `mainResultText`를 `gm.lastResult`(확정값) → **`session.CurrentResult`(세션 실시간 판정값)**로 변경 — 최초 투척·개별 재투척 직후 현재 조합 결과 즉시 표시(도/개/걸/윷/모/낙), 확정 후 세션 생존으로 확정 결과 유지, `rolled=false` 준비 세션에서 비움. 재추첨 없이 `frontUp` 판정값만 사용. 표시 상태(Playing/Cleared/GameOver에서만 활성)는 유지
- `rerollRemainText`: 숫자만 → `N회` 형식 (`RemainingRerolls + "회"`, `0회` 포함). 세션 없으면 빈 문자열 유지
- `hasRolled`·`lastResult`는 표시 소비처가 없어져 쓰기 전용 내부 상태가 됨 — 로직상 제거하지 않고 유지
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증**

### [2026-10-28 (52차) - 사용자 지시 / Devin 구현 — UI 참조 이전 검증 + Legacy 정리]

**실제 Scene 직렬화 감사 → 누락 이전 → 검증 → Legacy 연결 정리 (규칙·OnClick 변경 없음)**

- **감사 결과 (test2.unity)**: 사용자가 Unity에서 이전 도구를 실행해 16개 컴포넌트 부착·참조 복사가 완료돼 있었음. 스크립트로 `legacy*` 74필드 전체와 대응 컴포넌트 필드를 1:1 대조 — 불일치 0건 (빈 배열 vs 0참조 차이만 형식상 존재, 실질 동일)
- **미이전 씬 발견**: `Assets/Scenes/SampleScene.unity`·`Assets/Test.unity`는 구 필드명 직렬화 그대로 → YAML 스크립트로 이전 실행: 16개 컴포넌트 MonoBehaviour 문서 추가(fileID 2000000000~) + 호스트 GameObject `m_Component` 등록 + 구 필드 참조 복사(배열 포함 그대로) + 매니저에 `xxxUI` 참조 16종 추가 + 구 필드명 행 삭제. 호스트 규칙은 이전 도구와 동일(해당 패널 GO, 없으면 매니저 GO — Test.unity는 팝업·상점 패널 부재로 매니저 GO에 부착)
- **`Assets/_Recovery/0.unity`·`0 (1).unity`**: Unity 자동 복구 백업 — 의도적으로 미수정. `legacy*` 필드가 코드에 남아 있어 `[FormerlySerializedAs]` 경유 폴백으로 계속 동작
- **Legacy 정리**: `test2.unity`의 `legacy*` 직렬화 행 182줄 삭제 — 읽기 기준이 컴포넌트로 단일화. `SampleScene`/`Test.unity`는 구 필드명 행 제거로 동일 효과
- **코드 측 레거시 실사용 경로 검사**: `legacy*`를 접근하는 코드는 `YutGameManager`의 폴백 접근자뿐 — 다른 매니저·UI에서 직접 접근 없음. `UITransferTool`은 미이전 씬 대비로 유지
- **Missing Script 검사**: 3개 씬 전체 스크립트 GUID 대조 — 미해석 GUID는 전부 UGUI·URP 패키지 스크립트(Text/Image/Button/LayoutGroup/Light 2D), 프로젝트 스크립트 누락 0건. 프리팹에 YGM 참조 없음
- **OnClick**: 직렬화 도구가 `m_PersistentCalls`를 건드리지 않음 — `RollYut` 등 YGM 대상 바인딩 원본 유지 확인
- 검증: `dotnet build` 오류 0·경고 0 + 3개 씬 YAML 정적 검증(필드 일치·fileID 충돌 없음·배열 보존). **Unity Play 미검증** — SampleScene·Test.unity는 구 데이터(18칸 보드·사라진 필드 잔재)가 남은 스냅숏으로 실사용 여부 확인 필요
- 임시 호환 경로: `legacy*` 필드는 코드에 유지(미이전 씬·백업 폴백). 제거 조건 — 모든 실사용 씬이 새 구조로 저장 확인 후 필드·접근자·이전 도구 일괄 삭제

### [2026-10-28 (50차) - 사용자 지시 / Devin 구현 — 윷 면 PNG 표시]

- `YutGameManager`: `stickFaceImages[4]`·`stickFrontSprite`·`stickBackSprite` Inspector 필드 추가 (기존 `stickFaceTexts` 보존)
- `GameUIManager.UpdateUI`: 면 루프에서 `session.frontUp[i]`에 따라 스프라이트 교체 — 표시용 재추첨 없음. `session == null || !rolled || isNak`이면 `enabled=false`로 숨김(레이아웃 유지)
- 갱신 경로는 기존 `UpdateUI()` 호출 그대로 — 최초 투척(`RollYut`)·개별 재투척(`RerollStick`) 후 즉시 반영, 별도 호출 추가 없음
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — Inspector 연결 필요: `stickFaceImages` 4칸(Image)+`stickFrontSprite`/`stickBackSprite` 2종

### [2026-10-28 (51차) - 사용자 지시 / Devin 구현 — YutGameManager UI 구조 분리]

**규칙·실행 순서 변경 없음 — UI 참조 소유와 표시 작성자만 화면별 컴포넌트로 이전**

- 신규 UI 컴포넌트 16종 (`Assets/Scripts/UI`, MonoBehaviour — 표시 전용, 규칙 계산 없음):
  - 화면 6: `StartScreenUI`·`BossSelectScreenUI`·`GameScreenUI`·`ClearScreenUI`·`ShopScreenUI`·`GameOverScreenUI`
  - 게임 화면 하위 6: `YutThrowUI`·`ScoreUI`·`BossCombatUI`·`BoardUI`·`TalismanHandUI`·`EquippedBellUI`
  - 팝업 4: `TalismanChoiceUI`(7택5/3택1 공용)·`MoveAdjustUI`·`FortuneChoiceUI`·`UsedTalismanUI`(동적 항목)
- `YutGameManager`: 기존 UI 필드 전부 `legacy*`로 개명 + `[FormerlySerializedAs]`로 기존 Scene 연결 보존. `public` 접근자(이름 동일)가 **컴포넌트 연결 우선 → 레거시 폴백**을 단일 경로로 제공 — `gm.xxx` 호출부(코어 매니저·UI) 전부 무수정. `EnsureUIComponents()`가 Awake에서 미부착 컴포넌트를 해당 패널(없으면 매니저 오브젝트)에 자동 부착 — 이전 도구 미실행 씬도 동작, 컴포넌트 필드 비어 있으면 레거시 폴백
- `GameUIManager` → 얇은 조정자: `UpdateUI`는 각 컴포넌트 `Refresh(gm)` 호출만. 개별 Text 작성은 컴포넌트 소유 — `ScoreUI`(점수·능력치·배율 이유), `YutThrowUI`(면/버튼/상태), `BossCombatUI`(보스 정보+목표·현재 HP), `BoardUI`(칸 표시·말 위치·예고·완주·착지), `TalismanChoiceUI`(후보+확정), `TalismanHandUI`(손패), `EquippedBellUI`(장착), `ShopScreenUI`(상점 슬롯), `FortuneChoiceUI`(점괘), `BossSelectScreenUI`(소개+건너뛰기), `GameScreenUI`(보유 냥), `ClearScreenUI`/`GameOverScreenUI`(결과), `UsedTalismanUI`(`usedItems` 맵 이관 — 항목 생성·단계별 제거·정산 정리)
- `BoardManager`: `UpdateBoardUI`·`MovePieceVisual`·`MoveBossPieceVisual`·`MoveBossPreviewVisual`은 이름 유지 래퍼 → `BoardUI` 위임 (호출부 전부 무수정)
- `GameUIManager.PresentSettlement`·`ShowLandingInfo`는 정산 연출 경로로 유지 — `ThrowSettlement`/`ScoreEffectStep` 재생만, useId별 카드 제거는 `UsedTalismanUI` 경유
- `BellSlotUI.UsesText` — 남은 발동 표시 공용 헬퍼 (장착·상점 슬롯 공용)
- 이전 도구 `Assets/Scripts/Editor/UITransferTool.cs` — 메뉴 `YUTROLL/UI 참조 이전 (열린 씬) — 미리보기/실행`: 컴포넌트 자동 부착 + `legacy*` 참조를 **비어 있는 새 필드에만 복사**(덮어쓰기 금지·멱등·Undo 지원·씬 오브젝트 미삭제). 실행 후 Ctrl+S로 씬 저장
- Scene·Prefab·OnClick 변경 없음 — 버튼 공개 함수 시그니처 전부 유지
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — 시작 7택5·땅/START 3택1·환위부·점괘·상점·정산 연출·보스 HP·완주 표시·새 런/재진입 등 기존 흐름 전체 확인 필요
- 임시 호환 경로: `legacy*` 필드 — 모든 참조가 컴포넌트로 이전 확인 후 비우기·정리 가능(제거 조건: 씬·프리팹 전부 이전 도구 실행 + 새 참조 정상 확인)

### [2026-10-27 오후 (49차) - 사용자 지시 / Devin 구현 — 부적 25종 전면 개편]

- `TalismanEffectKind` 신규 추가(끝에만): `WaterComboBonus`(24)·`MirageMove`(25)·`StopMove`(26) — 기존 번호 유지. `TalismanAppearance` 노란/하얀 활용
- `YutThrowSession`: `rerollAttempted`(승부부 판정 — 일반·특별·윤회령 무료 모두 기록)·`catchWidenRange`(포위부)·`stopMove`(정지부)·`resultBasedMoveDelta`(환영부 — 최종 결과 기준 도·개·걸 +2/윷·모 −3)
- `TalismanManager`: 차례 지속 효과 `turnEffects`(족쇄부·추적부 — `PrepareTurn`에서 `OnTurnStart` 1회 차감, UI 조회로 차감 없음) + 다음 차례 확률 예약 `pendingProbBoosts`(도걸부·모윷부 — `PrepareSession`에서 1회 소비, 보스 전환 소멸) + `IsMoveDisplayEffect`(환영부·정지부 이동 표시 포함)
- `ScoreManager`: **윷 기본배율 경로 분리** — `EffectTarget.YutMult`로 결과별 부적·추적부를 기초 피해 쪽 유효 윷 배율에 곱연산, 전체배율 곱연산 부적은 피해 부적만. `resultBonusMult`는 기록용으로만 유지. 잡기 판정에 포위부 범위(원형 최단 거리) 통합 — 일반·포위 잡기 동일 판정, 추혼부·탐욕부 동일 적용. 승부부는 `rerollAttempted==false`일 때 곱연산
- `TileEffectManager`: 물 착지 시 `WaterComboBonus` 추가 연계 +1(연수령과 독립) / 풀 착지 시 `ForestDotBoost`로 이번 등록·증가분만 ×value
- `YutRollManager`: `ApplyPendingProbBoosts` — 예약 확률 부적을 이번 세션 결과 가중치에 곱한 뒤 100% 정규화(방울·점괘 보정분에 후적용, 재투척·낙 미적용)
- `BossManager.ApplyMoveReduction`: 속박령 + `talismanManager.BossMoveReduction` 합산 후 최소 1칸 — 봉쇄부 0칸 미부활 유지. `BoardManager.ReshuffleAttributes` — START·위치·연계·도트·완주 유지 속성 재추첨
- `GameUIManager`: 이동 표시 제거 대상을 `IsMoveDisplayEffect`로 확장 — 환영부·정지부 카드도 이동 연출과 함께 표시·제거
- `TalismanDatabase.asset`: 25종 `includeInDeck: 1`·외형(노란 16/하얀 9)·`useTiming` 정리 — 재기부·승부부·결과별 5종·정지부 하얀. 수룡부 효과 `WaterComboKeep`→`WaterComboBonus`·설명 전면 지시서 문구로. 신규 `t_mirage`·`t_stop` 추가 (총 정의 30종 — 제외 5종 `includeInDeck: 0` 유지)
- 검증: `dotnet build` 오류 0·경고 0 + 에셋 정적 검증(덱 25종·타이밍 16/9). **Unity Play 미검증** — 25종 각각 실사용·지속 만료·확률 100% 정규화·역행부 상태 보존·족쇄/봉쇄 예고 일치 확인 필요

### [2026-10-27 (48차-3단계) - 사용자 지시 / Devin 구현 — 최초 투척 낙 1%]

- `GameBalanceConfig.initialNakPercent`(%, 기본 1) + 에셋 명시 — 접수된 최초 투척에만 1회, 도트 처치 시 추첨 생략
- `YutThrowSession.isNak` + `InitialRollNak`(면 추첨 없이 결과 고정·준비 부적 보존) + `CurrentResult` 낙 반환 + `CanReroll`/`RemainingRerolls` 낙 차단
- `YutGameManager.RollYut` — 도트 처리 후 `initialNakPercent` 판정으로 `InitialRollNak`/`InitialRoll` 분기, `MoveBossPiece`는 기존 1회 경로 유지. `SettleThrow` → `SettleNakThrow` 전용 분기
- `ScoreManager.SettleNakThrow` — 이동·착지·잡기·선택·도트·피해 단계 없이 공격 0 + `currentThrows++` 1회 + 사용 부적 안내 단계만 재생
- `TalismanManager.CanUseAfterRoll` — 낙 세션 차단(환위부 우회 불가). `GameUIManager` — 상태창 "낙 — 확정만 가능"·면 텍스트 "-"
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증**

### [2026-10-27 (48차-2단계) - 사용자 지시 / Devin 구현 — START 착지 공격·부적 3택1]

- `ScoreManager`: `attackLanded`에 `TileAttribute.Start` 추가, `landedOnBellTile` = `Start || (Earth && !earthNoPick)` — START는 땅 전용 방울 억제 대상이 아니므로 항상 부적 선택 발동
- `TileEffectManager`: `case TileAttribute.Start` 신설 — 말판 ×1·단계 기록·물 연계 끊김/청류령 방어 공통 경로·도트 미등록. `default`(쇠)는 피해 0 유지
- 상성 단계 조건을 불·물·풀 긍정 판정으로 정리 — 땅·START·쇠 제외 (`AffinityMultiplier` 비공격 속성 ×1 기존 유지)
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — START 착지 공격·부적 창·잡기(보스 START 위치)·물 연계 끊김 확인 필요

### [2026-10-27 (48차-1단계) - 사용자 지시 / Devin 구현 — 시작 부적 7택5]

- `TalismanManager`: `OpenStartPick` → `OpenPick(7,5)` — 규격 상수화(`StartCandidateCount`/`StartPickCount`/`LandCandidateCount`/`LandPickCount`/`OptionSlotMax`), 땅 3택1 불변
- UI 슬롯 안전 장치: `AvailableOptionSlots`(3종 배열 최소 길이)로 필요 선택 수 상한 — 슬롯 부족 시 갇힘 방지, 0이면 선택 생략·덱 미소모. `GameUIManager` 후보 루프 상한 5→`OptionSlotMax`
- `YutGameManager`: `talismanOptionNameTexts` 등 3종 배열 기본값 5→7 (기존 씬 직렬화 보존 — 사용자가 7칸 확장 + `SelectTalismanOption(5)`·`(6)` 버튼 추가 필요)
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 미검증** — 배선 확장 후 6·7번째 후보 선택·5슬롯 장면의 5택5 축소 동작 확인 필요

### [2026-10-27 (47차) - 사용자 지시 / Devin 구현 — 점괘 24종 확장·매 보스전 재배치·상점 할인]

- `FortuneUpgradeKind` 신규 8종 (`YutBaseScore`·`RerollFrontProb`·`RerollBackProb`·`ShopDiscount`) + `FortuneUpgradeManager` 조회 경로 — 기본점수는 `YutStatEntry.baseScore` 가산(설정 원본 미변경), 나머지는 조회 시 합산
- 최초 투척 결과 확률: `BuildInitialResultWeights`가 방울 보정 + `GetYutProbBonus`를 별개 버킷으로 합산 → `ApplyPercentBonuses`(%p 증분·비대상 비례 차감·전체 증가 시 정규화) 1회 적용. 방울 보정·점괘는 `CollectInitialProbBonus` 단일 버킷에 합산 후 1회 적용 — 순서 무관, 방울 %p 의미 보존
- 속성 발생 확률: `BoardManager.DrawAttribute`가 `GetAttrProbBonus`를 같은 `ApplyPercentBonuses` 규칙으로 적용 — `WouldChangeAttrWeights` 순수 조회로 실효 판정
- 재투척 면: 세션 `rerollFrontUpChance` = Clamp(양면령 기본값(없으면 50%) + `GetRerollFrontBonus` − `GetRerollBackBonus`, 20~80%) — 세션 생성 시 계산 — 세션 생성 시 계산해 `rerollFrontUpChance`에 기록, 기본·특별 재투척 공통
- 상점 할인: `FortuneUpgradeManager`가 `ShopDiscountPending`↔`shopDiscountActive` 이원 관리, `OnBossCleared`는 `YutGameManager.EnterClear`에서만 호출 — 상점 재진입·건너뛰기로 활성화 안 됨. `ShopManager.PriceFor`(Ceil, 상한 `ShopConfig.shopDiscountMaxPercent`)를 슬롯 표시·구매 가능 판정·차감 공용 — `GameUIManager` 상점 슬롯은 `offer.price`(정가) 대신 조회값 표시
- **매 보스전 말판 재배치**: `SetupBoard`의 `layoutGenerated` 게이트 제거 — 매 실제 보스전 시작 시 배치 재생성(34차 확정·구현 대기 규칙 구현). 소개·상점·점괘 선택·건너뛰기에서는 재배치 없음. 누적 완주·성장 유지
- `HasEffect`(선택 전후 실제 변화 판정, 순수 조회) + `ShopManager.FortunePouchAvailable`/`CanSellFortunePouch`로 유효 후보 부족 복주머니를 구매 불가 표시·미차감 거부 — 후보 부족 시 돈 미차감·선택 대기 미발생 유지
- 에셋: `FortuneUpgradeDatabase` — u_do/gae/geol/yut/mo_base·u_front_face·u_back_face·u_shop_discount 신규 8종 + 확률 8종 `effectEnabled: 1` + 설명 정정("기본점수 +0.5"→"기본 배율 +0.5" 등). `ShopConfig.asset`에 `shopDiscountMaxPercent: 50` 명시
- 검증: `dotnet build` 오류 0·경고 0 + 수치 검증(도 25→30·개 37.5→35·걸·윷·모 분포, 앞면 52/74/클램프 80, 할인 20→19/19→19/20→10). **Unity Play 미검증** — 24종 후보 노출·효과·보스 전환 유지·재배치 반영·할인 적용 시점·후보 제외는 Play에서 확인 필요
- Inspector 작업 없음

### [2026-10-27 (46차) - 사용자 지시 / Devin 구현 — 방울 4종 효과 분류·표시 문구]

**규칙 변경 (사용자 확정 지시서 + 문구 보완 지시)** — 20종 구현 유지, 4종의 계산 분류·수치·설명만 변경:

- **화염령** — 방울 피해 곱연산 → **말판 곱연산 ×2**로 분류 이동 (불 조건·20% 확률·공령명 부스트 유지). `ScoreManager.BellDamageMultFor`에서 제거하고 `TileEffectManager` 불 칸 경로가 `EffectiveChance` 추첨 후 `mult *= damageMult` — `tileEffectMult`에 포함돼 최종 피해는 기존과 동일. 출처는 방울로 기록 — `AddBellDamageStep`·`AddBellInfoStep`을 internal 공용화 (미발동도 단계 기록)
- **결단령** — 방울 피해 ×2 → **전체배율 방울가산 +2**. 에셋 `damageMult: 2→0` + `totalMultAdd: 2` — `ScoreManager.BellAddFor`(평면 가산)가 공격 칸에서 `bellAddMult`에 가산. 재투척 금지·재기부 거부·삼고령 차단 경로는 무관하게 유지. 실제 피해 변경 (불 기준 480→720)
- **폭염령** — 불 말판 자체 배율 +1(×2→×3) → **불 공격 시 전체배율 말판가산 +1**. `BellData`/`OwnedBell`에 `tileMultAdd` 신규(구 `tileMultBonus`는 미사용 주석 처리) + 에셋 `tileMultBonus: 1→0`·`tileMultAdd: 1`. `ScoreManager.BellTileAddFor`가 `tile.attribute == targetAttribute` 조건으로 `tileAddMult`에 가산 — 비불 공격은 0. `CurrentAttrMult`의 tileMultBonus 합산 제거로 정보판·계산 동일하게 ×2 유지
- **윤환령** — 런 누적 완주 ×`perLapBonus` 가산은 45차 구현 그대로 (설명 문구만 변경)
- **단계 표시** — `화염령 ×2`(Multiply·방울 출처·말판 구간), `결단령 +2`·`폭염령 +1`(Add·방울 출처·가산 구간). UI 코드 변경 없음 — 기존 재생 경로가 `op`/`reasonText`/`afterValueF` 사용
- **에셋 이전** — `BellDatabase.asset`: 결단령 `damageMult 2→0`+`totalMultAdd: 2` / 폭염령 `tileMultBonus 1→0`+`tileMultAdd: 1` / 4종 `description`을 사용자 제공 문구로 교체 (가산·곱연산 용어 없음 — 화염 "불속성 공격시 일정확률로 크리티컬데미지(2배)를 발동한다"·결단 "재투척을 없애는 대신 모든공격력에 2배 공격력을 증가시킨다"·폭염 "불속성 공격시 1배의 공격력을 증가하여 공격한다."·윤환 "현재 말판의 바퀴수만큼 전체배율이 증가된다."). 다른 방울·필드 미변경. **코드 기본값만으로 완료 처리하지 않고 저장 에셋에 명시 이전**
- **공식 표기 확장** — 가산 항에 잡기·말판·상성 가산 자리 명시. `ThrowSettlement`에 `catchAddMult`/`affinityAddMult` 예약 필드(현재 적용 효과 없음·0)
- **수정 파일** — `ScoreManager.cs`(가산 버킷·BellAddFor·BellTileAddFor·AddAddStep·bell 단계 헬퍼 internal화·AddBellInfoStep 유효값 인자화), `TileEffectManager.cs`(불 경로 화염령 배치·runningForSteps 사용), `BellData.cs`(tileMultAdd·enum 주석), `BellManager.cs`(tileMultAdd 복사), `BellDatabase.asset`, `docs/` 4종
- 검증: `dotnet build` 오류 0·경고 0. 수식 대조(기초 120·불 ×2): 기본 240 / 결단 (1+2)×2=6→720 / 폭염 (1+1)×2=4→480 / 결단+폭염 (1+2+1)×2=8→960 / 위 둘+화염 발동 (1+2+1)×2×2=16→1,920 — 5종 전부 일치. 중복 적용 없음(BellDamageMultFor에서 화염·결단 제거·폭염은 가산 경로만). **Unity Play 대기**: 불/비불 조건·화염령 0/100%·결단령 재투척 차단 재확인·정상/스킵 동일 실측
- Inspector 작업 없음 — 신규 필드·버튼·패널 없음 (`tileMultAdd`는 Inspector 표시만 추가), 기존 연결 전부 보존

### [2026-10-27 (45차) - 사용자 지시 / Devin 구현 — 전체배율 가산 우선 구조·런 누적 완주]

**규칙 변경 (사용자 확정 지시서)** — 전체배율은 가산 항목을 먼저 합산한 뒤 곱연산 항목을 곱하는 구조로 개편 + 완주 횟수를 런 단위로 변경:

- **런 누적 완주 (1단계)** — `YutBoard.lapCount` 보스전 단위 → 런 단위. `MovePiece`는 `totalMoved`(보스전 진행도)가 START 경계(30칸 배수)를 넘은 횟수만 가산 — `while(totalMoved >= TileCount*(lapCount+1))` 구식 누적 판정 제거(진행도 리셋과 누적 횟수 분리 불가 문제 해소). `BoardManager.SetupBoard`는 `pieceIndex`/`totalMoved`만 리셋하고 `lapCount` 유지 — `ResetForNewRun`에서만 0. START 강제 배치·이동 0·UI 갱신 미증가, 미완주 부분 진행은 이월하지 않음. `Lap Count Text` = 런 누적 완주 (동일 필드 읽음 — 코드 변경 불요)
- **전체배율 공통 경로 (2단계)** — `ThrowSettlement`에 `lapBonus`/`talismanAddMult`/`bellAddMult`/`AdditiveTotal` 추가, `baseTotalMult`는 설정값만 보관. `SettleNormalThrow` 재구성: 가산 수집(기본→윤환령→가산 부적→가산 방울) → 곱연산 부적(결과별→피해 부적) → 잡기 → 곱연산 방울 → 말판 → 상성. 부적 조건은 `TalismanDamageApplies`에서 1회 판정 후 가산/곱연산 경로 공용
- **가산/곱연산 데이터 구조** — `TalismanData.totalMultOp`(EffectOp, 기본 Multiply — 기존 에셋 항목은 누락 필드라 생성자 기본값 Multiply 적용)·`ActiveTalismanEffect.totalMultOp` 복사 / `BellData`·`OwnedBell.totalMultAdd`(기본 0 — 평면 가산, `ScoreManager.BellAddFor`에서 평가, 현재 20종 전부 0). 기존 효과의 연산·수치는 전부 유지 — ×N을 +N으로 바꾼 효과 없음
- **취소·단계 일치** — 탐욕부 취소 시 `runningMult`를 0으로 고정해 이후 잡기·방울·말판·상성 단계 전후 0 표시(기존 방울 단계의 비0 표시 누락도 함께 정리). `TileEffectManager.ResolveLandingEffect`는 `runningForSteps` 인자로 공통 누적값을 받아 별도 공식 재조립 폐기 — `AddTileStep`/`AddInfoTileStep`/`AddWaterComboGuardStep` 전부 전달값 사용. 상성 단계는 말판 다음 마지막 항목으로 이동
- **표시 순서 (3단계)** — 단계 기록 순서 = 확정 공식 순서와 동일(기본→윤환령 +N→가산 부적 +N→가산 방울 +N→곱연산 부적 ×N→잡기 ×N→곱연산 방울 ×N→말판 ×N→상성 ×N). `EffectOp.Add` 단계는 `AddAddStep`으로 전후 누적 합계 기록 — 이유 Text·전체배율 Text·정산 로그가 같은 단계 재생. UI 코드 변경 없음(기존 재생 경로가 `afterValueF` 표시)
- **디버그 로그** — 최종 로그를 가산 합계→결과부적→부적→잡기→방울→착지→상성 순서로 갱신
- 검증: `dotnet build` 오류 0·경고 0. 수식 대조: 가산 효과가 없는 기존 공격은 곱연산 순서 재배치만으로 변경 전과 동일 피해(필수 예시 5종 일치). **Unity Play 대기**: 첫 보스 완주 2회 → 다음 보스 2 유지·추가 완주 3·건너뛰기 유지·새 런 0 실측 / 윤환령 판매→가산 제거·재장착→누적 적용 / 가산 단계(+N) 표시 / 탐욕부 취소 후 전 단계 0 표시 / 정상·스킵 동일
- Inspector 작업 없음 — 신규 필드·버튼·패널 없음, 기존 연결 전부 보존 (`totalMultOp`/`totalMultAdd`는 데이터 필드라 Inspector 표시만 생김 — 씬·프리팹 연결 변경 아님)

### [2026-10-26 (44차-6단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 6단계: 전체 활성·회귀 점검]

**규칙 적용 (사용자 확정 지시서)** — 20종 전체 상점 활성 + 회귀 코드 점검, Unity Play 실측 대기:

- **활성·상점 풀 확인** — `BellDatabase.asset` 20종 전부 `effectEnabled: 1`·ID 중복 0건·구매 20/판매 10 통일. `ShopManager.BuildCandidatePool`은 `effectEnabled`+미보유(`OwnsKind`) 필터라 20종이 상품 후보에 등장 — 슬롯당 방울·복주머니 50:50 추첨·리프레시 비용 구조 미변경
- **장착 규칙** — `TryAddBell`의 `MaxBells`(shopConfig 기본 4)+`OwnsKind` 중복 종류 차단 유지. `BellSlotUI`는 이름·설명 데이터 주도라 20종 별도 경로 불요
- **수명** — 보유 방울은 런 단위(`ResetForNewRun`에서만 정리)라 보스 전환 유지. 세션 방울 설정(`maxRerolls`/`rerollsBanned`/`rerollFrontUpChance`/`freeRerollChance`/`initialResultWeights`)은 `PrepareSession` 단일 경로에서 차례마다 반영 — 상점 구매는 보스 사이에만 가능해 세션 중 보유 변동 없음. 확률 방울 추첨은 `InitialRoll`·`Reroll` 실행 시점에만 — UI 갱신·버튼 조회 미추첨
- **정상 연출=스킵 동일성** — 모든 방울 판정(확률·배율·수입)은 정산(`SettleNormalThrow`/`ResolveLandingEffect`/`ApplyTurnStartDot`)에서 1회 계산·`ScoreEffectStep`으로 기록 — `PresentSettlement`·스킵은 같은 단계를 재생하므로 피해·확률·보상 동일
- **기존 기능 무결** — 재투척 버튼 `session.CanReroll` 연동(결단령 시 전부 비활성·잔여 0 표시), 보스 이동 예고 `PlannedEffectiveSteps`가 봉쇄(봉쇄부·목박령)·속박령을 동일 반영, 땅 카드 선택은 `landedOnBellTile` 억제로만 생략(땅 공격 유지), 최초 투척은 세션 결과 5종만 생성(백도·낙 도달 불가 기존 유지)
- **잔여 표기 정리** — `YutGameManager` bellDatabase 주석 4종→20종, `TalismanData` 족쇄부 주석 족쇄방울→속박령
- 검증: `dotnet build` 오류 0·경고 0 + 에셋 정합(정의 20·ID 중복 0·가격 통일)·상점 풀 필터 확인. **Unity Play 대기**: 20종 구매·최대 4 장착·중복 거부·판매 해제·보스 전환 유지·새 런 초기화·확률 방울 0/100% 강제 테스트·정상/스킵 동일 실측
- Inspector 작업 없음 — 신규 필드·버튼·패널 없음, 기존 연결 전부 보존

### [2026-10-26 (44차-5단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 5단계: 확률 통합]

**규칙 적용 (사용자 확정 지시서 §7)** — 최초 투척 결과 확률 방울 3종 + 공령명 완성으로 **20종 전부 구현·활성**, 6단계 전체 회귀 승인 대기:

- **`Core/YutRollManager.cs` `BuildInitialResultWeights`** — 장착 방울 중 `probBonusPercent`가 설정된 것(걸윷령·쌍개령·극단령)의 `targetResult`/`targetResult2` 대상에 증가량을 합산 (합산이라 장착 순서 무관, 같은 대상 중복 가산 방지, 백도·낙은 대상 제외). 기준 확률은 실제 4개 윷의 면 분포(`BaseResultPercent`: 도 25·개 37.5·걸 25·윷 6.25·모 6.25 = C(4,k)/16). **일반 경우**: 대상 = 기본+증가, 비대상은 남은 확률을 기본 비율로 분배(합계 100%). **정규화 예외**(모든 결과가 대상이거나 대상 합계 ≥ 100%): 전체를 함께 100%로 정규화 — 비대상 강제 0 처리 없음
- **`Core/YutThrowSession.cs`** — `initialResultWeights` 세션 필드(PrepareSession 보관) + `InitialRoll` 분기: 가중치 있으면 `RollWeightedFaces`(결과 선추첨 → `FrontCountFor`로 앞면 개수 확정 → 위치 균등 배치 — **보정 결과와 화면 면이 항상 일치**), 없으면 기존 0.5 면 추첨. 가중치 오류 시 기본 분포로 복귀. 재투척은 기존 면 추첨·실제 면 판정 — 확률 방울 미적용
- **공령명** — `EffectiveChance`가 지정 5종(잔향 20→40 / 윤회 5→10 / 화염 20→40 / 목박 10→20 / 연수 10→20) 전부에 연결됨을 확인. 원본 `chance` 미변경·조회 시점 배율·양면령 70%·확률 방울 +10%p는 비대상 — 추가 활성만으로 완성
- **`ScriptableObjects/BellDatabase.asset`** — 공령명·걸윷·쌍개·극단 `effectEnabled: 1` → **20종 전체 활성**
- 검증: `dotnet build` 오류 0·경고 0 + 정규화 수치 확인(쌍개령: 개 47.5·나머지 비례 축소 합계 100 / 걸윷+극단: 대상 합계 102.5 → 예외 정규화, 비대상 개 26.79 유지 / 3종 동시: 전원 대상 예외 정규화). **Unity Play 대기** — 보정 투척 결과·면 일치·재투척 미적용·공령명 2배 실측 필요
- Inspector 작업 없음 — 신규 필드·버튼 없음, 기존 연결 보존

### [2026-10-26 (44차-4단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 4단계: 피해·보상]

**규칙 적용 (사용자 확정 지시서)** — 피해·보상 방울 6종 구현, 5~6단계 승인 대기:

- **화염령** (`Core/ScoreManager.cs` `BellDamageMultFor`) — 불 공격 착지 시 `EffectiveChance`(기본 0.2, 공령명 반영)로 `damageMult` ×2를 방울 피해 배율에 곱한다. 정산 계산 시점에 1회 추첨 — 미발동도 `AddBellInfoStep`으로 안내 단계 기록
- **접전령** (같은 경로) — `BoardDistance`(원형 말판 최단 거리 = 양방향 차의 작은 쪽, `YutBoard.TileCount` 기준)로 최종 착지↔보스 실제 위치를 측정, `proximityRange`(3) 이내면 `damageMult` ×2. 거리 0(잡기 칸)도 충족 — 잡기 배율과 중첩. 화면 좌표·경유 칸·예고 위치 미사용. 범위 초과도 안내 단계 기록
- **윤환령** (`ScoreManager.SettleNormalThrow`) — `settlement.baseTotalMult += lapCount × perLapBonus`. 이번 공격 이동으로 완료한 완주는 `MovePiece`가 먼저 증가시키므로 최종 착지 시점 횟수에 자연스럽게 반영. 완주 데이터는 읽기 전용 — 보스전 시작 초기화(SetupBoard의 `lapCount=0`) 규칙 유지. 가산 시 안내 단계 기록
- **지맥령·금맥령** (`ScoreManager` + `Core/TileEffectManager.cs`) — `earthNoPick`으로 `landedOnBellTile`을 내려 `OpenLandPick` 카드 선택을 제거 (땅은 공격 칸으로 유지, START 미적용). 땅 착지: 지맥령 `tileMult`(2) 말판 배율 + `AddTileStep` / 금맥령 `bonusMoney`(20)를 `EconomyManager.AddMoney` 경로로 정산당 1회 지급 + 안내 단계. 동시 장착 시 선택 없음·×2·20냥 모두 적용. `ResolveLandingEffect`는 확정 정산에서만 호출 — 스킵·UI 갱신 중복 수입 없음
- **폭염령** (`TileEffectManager.CurrentAttrMult`) — 불 말판 배율에 `tileMultBonus`(1) 가산 — 실제 데미지 계산과 정보판 표시가 같은 조회 경로 (기본 ×2 → ×3). 착지 시 안내 단계 기록
- **`ScriptableObjects/BellDatabase.asset`** — 화염·윤환·지맥·금맥·접전·폭염 `effectEnabled: 1` (활성 16종)
- 검증: `dotnet build` 오류 0·경고 0 + 필수 예시 수식 확인(기본 전체배율 1·무효과: 도·불=20×6×2=**240** / +폭염령=20×6×3=**360** / +화염령 발동=20×6×2×2=**480** / 도·땅+지맥령=20×6×2=**240** / 도·불+결단령+접전령+화염령 발동=20×6×8×2=**1,920**). **Unity Play 대기** — 지맥+금맥 동시 장착(선택 없음·×2·20냥)·윤환령 완주 가산·접전령 거리 경계(3칸/4칸) 실측 필요
- Inspector 작업 없음 — 신규 필드·버튼 없음, 기존 연결 보존

### [2026-10-26 (44차-3단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 3단계: 속성·도트]

**규칙 적용 (사용자 확정 지시서)** — 속성·도트 방울 2종 신규 구현 + 청류령·잔향령 검증·정합, 4~6단계 승인 대기:

- **연수령** (`Core/TileEffectManager.cs`) — 물 착지 시 정상 연계 증가 후 `EffectiveChance`(공령명 반영 유효 확률, 기본 0.1)로 `waterComboCount +1`. 증가된 횟수로 이번 공격 배율을 즉시 계산(첫 착지 발동 시 2연계 ×2), 증가분은 이후 연계에도 유지, 추가 횟수로 재판정·재귀 실행 없음, 연계 배율 목록 초과는 기존 클램프 유지. `ScoreEffectStep` 안내 단계 기록
- **청류령 정합** — 연계 새 시작 판정을 `waterComboCount == 1`에서 `comboStart`(증가 전 0)로 변경: 연수령으로 첫 착지가 2연계가 돼도 방어 1회가 정상 준비된다. 기존 방어 소모·충전·보스 전환 초기화 의미는 불변
- **목박령** (`Core/YutGameManager.cs` `ApplyTurnStartDot`) — 도트 지급마다 `EffectiveChance`(기본 0.1)로 `bossManager.pendingMoveSeal = true`. 정상 지급과 잔향령 추가 지급에서 각각 판정(추가 지급 시 기회 2회). 봉쇄부와 같은 봉쇄 경로: `PlannedEffectiveSteps` 0(예고 표식 즉시 반영) → `MoveBossPiece`가 상태 소모하며 0칸 처리 → 속박령 최소 1칸 보정이 0을 되살리지 않음. bool 플래그라 같은 다음 이동을 막는 효과가 중첩해 봉쇄 턴을 추가하지 않고, 보스 전환 시 기존 경로(`pendingMoveSeal = false`)로 해제. 도트 처치 시 `EnterClear`로 후속 판정 중단(기존)
- **잔향령 정합** — 직접 `chance` 조회를 `EffectiveChance`로 전환 (공령명 5단계 구현 시 자동 적용, 현재 기본 0.2 동일). 저장 도트량 불변·추가분 재판정 없음·도트 0이면 미추첨 유지
- **`Core/BossManager.cs`** — `pendingMoveSeal`·봉쇄 소모 경로의 주석·로그를 봉쇄부+목박령 공용 표기로 갱신 (의미 변경 없음)
- **`ScriptableObjects/BellDatabase.asset`** — 목박·연수 `effectEnabled: 1` (활성 10종)
- 검증: `dotnet build` 오류 0·경고 0. 조합 확인: 청류+연수(연계 시작 방어 유지) / 잔향 추가 지급에서 목박 판정·잔향 재귀 없음 / 목박+속박(0칸 미해제) / 목박+봉쇄부(같은 플래그 — 중첩 없음). **Unity Play 대기** — 연수령 첫 착지 2연계·도트 지급 시 이동 예고 0 표시·실제 이동 0칸 실측 필요
- Inspector 작업 없음 — 신규 필드·버튼 없음, 기존 연결 보존

### [2026-10-26 (44차-2단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 2단계: 투척·이동]

**규칙 적용 (사용자 확정 지시서)** — 투척·이동 방울 5종 구현 + 속박령 데이터화, 3~6단계 승인 대기:

- **`Core/YutThrowSession.cs`** — 세션 필드: `maxRerolls`(기본 `BaseRerolls`=1)·`rerollsBanned`·`rerollFrontUpChance` + 기존 `freeRerollChance` 유지. `CanReroll`: `rerollsBanned` 즉시 거부, 기본 기회 상한 `maxRerolls`. `Reroll`: 기본/특별 판정 후 윤회령 추첨(차감만 생략·`rerolled`/`speciallyRerolled` 기록·결과 평가 정상) → 양면령 앞면 확률로 면 추첨(`rerollFrontUpChance` 0이면 0.5). `RemainingRerolls`: 포기·금지 시 0. `InitialRoll`은 0.5 고정 — 최초 투척 미적용
- **`Core/YutRollManager.cs`** — `PrepareSession`에서 보유 방울을 세션 필드로 반영: 윤회령 `EffectiveChance`(공령명 배율 반영 유효 확률) / 삼고령 `BaseRerolls+bonusCount` / 결단령 `rerollsBanned` / 양면령 `rerollFrontUpChance`. 세션 생성 경로는 이곳뿐 — 중복 설정 없음
- **`Core/TalismanManager.cs`** — 재기부(`ExtraReroll`): 결단령 보유 시 `false` 반환으로 발동 불가 — 카드 미소모(`hand.RemoveAt` 전 차단). 승부부 포기·낙 금지는 어느 방울도 해제하지 않음
- **`Core/ScoreManager.cs`** — 축지령: 이동 계산을 `MoveSteps(selected)+moveDelta+bonusCount`로 합산 (윷 기본점수·윷 배율·부적 `moveDelta`와 독립, 최소 0칸) + 안내 단계 기록. 결단령: `ApplyBellDamageMults`/`BellDamageMultFor`/`AddBellDamageStep` 신규 — 공격 칸에서만 평가, 조건 만족 방울의 `damageMult`를 `bellDamageMult`에 순차 곱하고 `EffectSourceType.Bell` 단계로 전후 배율 기록. 화염령·접전령 조건은 4단계에서 추가
- **`Core/BossManager.cs`** — 속박령: `ApplyMoveReduction`이 `GetBell().bonusCount` 사용(−1, 최소 1) — 봉쇄부 0칸 미해제·예고=실제 이동 동일 계획 유지, 의미 변화 없음
- **`ScriptableObjects/BellDatabase.asset`** — 삼고·축지·결단·양면 `effectEnabled: 1` (활성 8종)
- 검증: `dotnet build` 오류 0·경고 0. 조합 확인: 결단령+삼고령+재기부 = 재투척 전면 차단 / 삼고령 = 기본 기회만 +1(특별 기회 분리) / 윤회령 = CanReroll·UI·거부 입력 미추첨 / 양면령 = 최초 투척 미적용. **Unity Play 대기** — 버튼 비활성 표시·이동 예고·정산 단계 표시 실측 필요
- Inspector 작업 없음 — 신규 필드·버튼 없음, 기존 연결 보존

### [2026-10-26 (44차-1단계) - 사용자 지시 / Devin 구현 — 방울 20종 확장 1단계: 데이터 재편]

**규칙 변경 (사용자 확정 지시서)** — 방울 4종 → 20종 체계의 1단계만 구현, 2~6단계 승인 대기:

- **`Data/BellData.cs`** — `BellEffectKind` 신규 16종 추가(11~26번): `ExtraReroll` 삼고령 / `MoveBonus` 축지령 / `FireChanceMult` 화염령 / `DotSealChance` 목박령 / `WaterComboChance` 연수령 / `RerollBan` 결단령 / `RerollFrontBias` 양면령 / `ChanceBoost` 공령명 / `ProbGeolYut` 걸윷령 / `ProbGae` 쌍개령 / `ProbMoDo` 극단령 / `LapTotalMult` 윤환령 / `EarthNoPickMult` 지맥령 / `EarthNoPickGold` 금맥령 / `ProximityMult` 접전령 / `FireMultBonus` 폭염령. 구 4종(3·5·8·9) 번호 유지, 폐기 번호(1·2·4·6·7·10) 재사용 금지
- **`BellData` 필드 확장** — `chanceBoostable`(공령명 대상 bool) + 효과별 전용 수치: `bonusCount`(재투척/이동/연계 가산)·`damageMult`(방울 피해 배율)·`tileMultBonus`(말판 가산)·`tileMult`(말판 곱셈)·`bonusMoney`(착지 수입)·`frontUpChance`(앞면 확률)·`proximityRange`(판정 거리)·`probBonusPercent`(결과 확률 +%p)·`chanceBoostMult`(공령명 배율)·`perLapBonus`(완주당 가산). 사용하지 않는 `growthAmount` 제거 — `OwnedBell`에 동일 필드 복사
- **`Core/BellManager.cs`** — `TryAddBell`이 신규 필드 전부 복사 / `EffectiveChance(OwnedBell|kind)` 신규: 공령명 보유 시 `chanceBoostable` 방울만 `chanceBoostMult` 배 유효 확률 반환 (원본 `chance` 미변경 — 조회 시점 계산)
- **`ScriptableObjects/BellDatabase.asset`** — 4종 → **20종**. 기존 ID 재사용으로 이름 변경: `b_suryeong` 수령방울→**청류령** / `b_mogryeong` 목령방울→**잔향령** / `b_jokswae` 족쇄방울→**속박령** / `b_cheonun` 천운방울→**윤회령**(`chance` 0.07→**0.05**). 신규 16종: `b_samggo`·`b_chukji`·`b_hwayeom`·`b_mokbak`·`b_yeonsu`·`b_gyeoldan`·`b_yangmyeon`·`b_gongmyeong`·`b_geolyut`·`b_ssanggae`·`b_geukdan`·`b_yunhwan`·`b_jimaek`·`b_geummaek`·`b_jeopjeon`·`b_pokyeom`. 전부 구매 20·판매 10. 확률 5종(잔향·윤회·화염·목박·연수)만 `chanceBoostable: 1`, 공령명 `chanceBoostMult: 2`
- **활성화 정책**: 구현 완료 4종만 `effectEnabled: 1`, 신규 16종은 `0` — 각 단계에서 효과 구현 후 활성화 (미구현 활성화 금지 원칙 준수)
- 검증: `dotnet build` 오류 0·경고 0 + 에셋 정의 20종·ID 중복 0건·활성 4종. Inspector 작업 없음 — 기존 슬롯·직렬화 보존
- 대기: 2단계 투척·이동(삼고·윤회·양면·결단·축지·속박) / 3단계 속성·도트 / 4단계 피해·보상 / 5단계 확률 통합 / 6단계 전체 활성·회귀

### [2026-10-26 (43차) - 사용자 지시 / Devin 구현 — 방울 4종 재편·효과 전체 구현]

**규칙 변경 (사용자 확정 지시서)** — 방울을 4종(수령·목령·족쇄·천운)으로 재편하고 전 효과를 실제 상태·정산 경로에 구현:

- **1단계 — 데이터·삭제 대상 정리**
  - `Data/BellData.cs` — `BellEffectKind`를 4종+`None`으로 정리: `BossMoveReduce=3`·`FreeReroll=5`·`WaterComboGuard=8`·`DotDoubleChance=9` 유지, 구 1·2·4·6·7·10번은 주석으로 폐기 명시 (번호 재사용 금지 — 직렬화 의미 보존). `targetResult/2`·`targetAttribute`·`growthAmount` 필드는 유지(현 4종 미사용, 주석 갱신)
  - `ScriptableObjects/BellDatabase.asset` — 14종 → 4종(`b_suryeong`·`b_mogryeong`·`b_jokswae`·`b_cheonun`)으로 재작성, 전부 `effectEnabled: 1`·구매 20·판매 10·목령 `chance 0.2`·천운 `chance 0.07`. 스크립트 GUID 확인으로 에셋 바인딩 보존
  - `Core/BellManager.cs` — 재작성: `ownedBells`·`OwnsKind`·`HasBell`·`GetBell`·`TryAddBell`·`RemoveBell`·`ResetForNewRun`만 유지. 제거: `ApplyGrowth`/`GrowthTrigger`/`attrAttackBonus`/`attrMultBonus`/`FrogAffinityMult`/구 `GetChance` 경로
  - `Core/ScoreManager.cs` — 제거: 정산의 방울 성장 루프+`AddBellStep`(성장 단계), 청개구리 상성 분기(`frogActive`·`FrogAffinityMult` — `AddAffinityStep` 시그니처에서 `frogActive` 삭제), 재복방울 약점 수입 블록
  - `Core/TileEffectManager.cs` — `CurrentAttrMult`에서 화령방울 성장분(`bellManager.GetAttrMultBonus`) 제거 — 불 기본 ×2·복주머니 `AttrMult` 강화는 유지
  - 기타 잔여 주석 정리: `FortuneUpgradeManager`·`FortuneUpgradeData`·`BossManager`·`YutGameManager`의 구 방울명 참조 주석
- **2단계 — 수령방울** (`Core/TileEffectManager.cs`)
  - `waterComboGuardArmed` 신규(보스전 단위) — 물 착지로 `waterComboCount` 0→1 시 방울 보유 중이면 방어 준비 + 정산 안내 단계
  - `BreakOrGuardWaterCombo` 신규 — 물 외 모든 착지(불·풀·땅·START·기타) 공통 처리: 방어 준비+방울 보유 시 소모해 연속 기록 보존(true), 아니면 연속 초기화(false). 보존 시 횟수 미증가·비물 칸 효과 정상 적용·물 배율 미적용
  - 연계 중 물 재착지로는 방어 충전 안 됨 — `waterComboCount == 1`일 때만 준비. `ResetForBoss`에서 연계·방어 함께 초기화
- **3단계 — 목령방울** (`Core/YutGameManager.cs`)
  - `ApplyTurnStartDot` — 도트>0 지급 시 `bellManager.GetBell(DotDoubleChance)`의 `chance`(0.2)로 `extra=dot` 추가 지급. 저장 도트량(`forestDot`) 불변·추가분 재추첨·연쇄 없음·도트 0이면 미추첨·처치 클리어 처리 기존 유지. 이유 표시 "풀 도트 피해 +N +N (목령방울)"
- **4단계 — 천운방울** (`Core/YutThrowSession.cs`·`Core/YutRollManager.cs`)
  - `YutThrowSession.freeRerollChance` 신규 — `YutRollManager.PrepareSession`이 보유 방울 `chance`(0.07)를 세션 생성 시 보관 (확률은 세션 단위, 판정은 실행 시점)
  - `Reroll` — 기회 종류(기본/특별) 판정 후 `freeReroll` 추첨, 무료면 그 차감(`rerollUsed++` 또는 `specialRerolls--`)만 생략. 면·결과·`rerolled`/`speciallyRerolled` 기록·`EvaluateArmedBonuses`는 정상 — 같은 윷 반복 차단 유지. `CanReroll`·UI 조회·최초 투척·거부 입력·승부부 포기(`rerollsForfeited`)에서는 추첨 없음
- **5단계 — 족쇄방울 검증·활성화** — `BossManager.ApplyMoveReduction`(−1, 최소 1) 기존 유지 확인: 봉쇄부 0칸은 보정 전 처리로 미해제, 이동 계획(예고)·실제 이동 동일 경로, 고정 범위(4~7) 모드 유지, 판매 시 `HasBell` false로 즉시 해제
- **유지 확인**: `ShopManager` 후보 풀(`effectEnabled`+미보유 필터) 구조 변경 없음 — 4종 정의만으로 상점에 4종만 등장. `GameUIManager` 방울 표시는 이름·설명 데이터 주도라 별도 경로 삭제 불요. 복주머니 8종 강화·보스 상성·잡기 ×4·전체배율 정산 경로 미변경
- 검증: `dotnet build` 오류 0·경고 0 + 라이브 참조 grep 정리(제거 enum·방울명·bellId 전부 0건 — 문서 이력 주석 제외). **Unity Play 대기**: 4종 구매·장착 표시·수령 물→불→물→풀 예시·목령 0/100%·천운 0/100%(판매 복귀 후 7%)·족쇄 예고=실제·판매 해제·보스 전환 유지·새 런 초기화
- Inspector: **신규 필드·버튼 없음** — 기존 상점/슬롯/버튼 연결 그대로
- 엑셀 반영 예정: `16_방울목록` 10종 제거(또는 삭제 이력)+4종 구현 상태 / `01_현재게임룰` 4종 체계·성장 방울 폐기 / `02_미확정_결정` 구현 3종 정리·수룡부 중첩 유지 / `14_데이터설정` BellDatabase 4종·목령 0.2·천운 0.07. 패널·버튼 시트 변경 없음

### [2026-10-26 (42차-1단계) - 사용자 지시 / Devin 구현 — 투척·말판·부적 선택·낙 밸런스 변경 1단계: 기본 수치·보스 이동·완주 확인]

**규칙 변경 (사용자 확정 지시서)** — 1단계만 구현, 2~4단계는 승인 대기:

- **기본 투척 5→6** — `GameBalanceConfig.maxThrows` 클래스 기본값·에셋·`YutGameManager.maxThrows` 폴백 전부 6으로. 개별 재투척 1회 규칙은 별개 유지. 마지막 투척 처치 클리어 우선 판정은 `currentThrows >= maxThrows`라 자동 추종
- **불 착지 ×1.5→×2** — `fireTileMult` 클래스·에셋·`TileEffectManager` 폴백(2f) 전부 반영. 관련 주석·폴백 잔재(ScoreManager·BellData·BellManager·YutGameManager 주석) 정리
- **보스 이동 — 피해 비율 단계 비활성화(구조 보존)** — `GameBalanceConfig`에 `useBossMoveDamageStages`(기본 false)·`bossMoveMin 4`·`bossMoveMax 7` 추가. `DrawBossMoveSteps`는 비활성 시 고정 범위 추첨, 활성 시 기존 단계 배열 경로 그대로. `BossMoveStage`는 비활성 시 항상 0. 족쇄방울 −1(최소 1)·봉쇄부 0·이동 예고/계획 소비 경로 변경 없음. `ValidateDataAssets`는 단계 배열 검사를 단계 모드에서만 수행 + 고정 범위 오류 경고
- **완주 검증** — `YutBoard.MovePiece`의 `totalMoved` 경계 통과 증가 구조 재사용: 29→0 통과·정확한 START 도착 모두 +1, 0칸 이동 증가 없음, 6번 모(30칸) 시 완주 1. 초기화 = 보스전 `SetupBoard`·런 `ResetForNewRun` (기존). 표시는 기존 `lapCountText` 필드 + `UpdateBoardUI` 동기화 경로 사용 — 신규 완주 보너스·방울 효과 없음
- 검증: `dotnet build` 오류 0·경고 0 + 수식: 도·불(무효과) = 20×6×2 = **240**. Unity Play 대기 — 6회 소진 종료·보스 이동 4~7칸·완주 표시 확인 필요
- 엑셀: `01` 투척 6회·불 ×2·보스 이동 고정 4~7 / `04` 불 ×2·투척 6 / `14` `maxThrows` 6·`fireTileMult` 2·신규 `useBossMoveDamageStages`·`bossMoveMin/Max` / `09` `lapCountText` 연결 안내

### [2026-10-26 (41차) - 사용자 지시 / Devin 구현 — 잡기 배율 단일화]

**규칙 변경**: 잡기 성공 배율 `×3 + 별도 보너스 ×2(=×6)` → **`×4` 단일**.

- `Data/GameBalanceConfig.cs` + 에셋 — `catchMultiplier` 3→4, `catchBonusMult` 필드·에셋 저장값 제거
- `Core/ScoreManager.cs` — `ThrowSettlement.catchBonusMult` 제거 / `catchMult` 폴백 4f / 잡기 연출 단계를 "보스 잡기" 하나로 (구 '보스 말 잡기 ×3'+'잡기 보너스 ×2' 두 단계 폐기) / `runningMult`·디버그 로그에서 보너스 제거 / 공식 주석 갱신
- `Core/TileEffectManager.cs` — 착지 단계 전후값의 `catchBonusMult` 인자 제거
- 유지: 추혼부 추가 ×4, 탐욕부 잡기 취소 = 피해 0·전체배율 0, 공격 없는 칸에서도 잡기 단계 기록
- 검증: `dotnet build` 오류 0·경고 0 + 수식 대조 — 윷·땅·잡기 1,200 / +추혼부 4,800 / 윷·불·잡기 1,800 / +추혼부 7,200. Unity Play 대기
- 엑셀: 잡기 규칙 ×6→×4, 계산기 잡기 보너스 항목 제거, `14_데이터설정` catchMultiplier 3→4·catchBonusMult 행 제거

### [2026-10-23 오전 (40차) - 사용자 지시 / Devin 구현 — 점검 발견 오류 4건 보완]

**전체 점검에서 발견한 검증 누락 4건 수정** (규칙 변경 없음 — 정상 후보 8종 동작 보존):

- **A. 윷 강화 대상 검증** — `IsSupported`가 `YutMult`의 `targetResult`가 도·개·걸·윷·모인지 확인 — 백도·낙 대상 강화 데이터는 활성화해도 후보·적용에서 제외 (이전엔 무의미하게 누적만 됨)
- **B. 속성 강화 대상 검증** — `AttrMult`를 불만 유효로 제한 — 물·풀 속성 배율 강화는 적용 경로(`CurrentAttrMult` 호출 지점)가 없어 효과 없는 후보가 됨. 물·풀 배율 경로 신설 여부는 사용자 결정 대기
- **C. 후보 수 검사 불일치 해소** — `HasSupportedCandidates`(≥1) → `SupportedCandidateCount`로 교체, `CanSellFortunePouch` 신규: 출현 확률>0 + 데이터 연결 + **선택창 UI 준비(`FortuneChoiceReady`) + 후보 수 ≥ fortuneCandidateCount**를 구매와 동일 기준으로 검사. 활성 강화가 1~2개거나 점괘 UI 미연결이면 복주머니는 "생성 불가" — 새로고침이 구매 불가 상품만 만드는 경우 차감 없이 실패
- **D. 선택창 UI 판정 공통화·중첩 경고** — `FortuneChoiceReady(need, warn)`로 패널·슬롯·선택 버튼 검사를 구매 거부와 상품 추첨 판정이 공유. `WarnIfPanelNested`가 `fortuneChoicePanel`의 clearPanel 자식 배치를 경고 (부모 비활성 시 선택 잠금 방지)
- **설정 경고** — `ValidateDataAssets`에 점괘 검사 추가: 활성이지만 지원되지 않는 정의(`IsSupported` 실패)를 개별 경고 + 지원 후보가 1~`need-1`개면 후보 부족 경고
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 백도 대상·물 속성 배율 데이터 추가 시 후보 미등장 + 경고 / 활성 강화 2개 구성에서 복주머니 새로고침·구매 차단

### [2026-10-23 오전 (39차) - 사용자 지시 / Devin 구현 — 후속 오류 4건 + 회귀 정정]

**복주머니·상점·탐욕부 취소·첫 실행 초기화 오류 수정** (규칙 변경 없음):

- **1단계: 복주머니 선택 불가 상태 방지** — `FortuneUpgradeManager.IsSupported` 공통 경로 신규 (적용 가능 종류만 인정: 미구현 YutProb·AttrProb·비공격 속성 AttrMult는 활성화해도 후보 제외·적용 거부) + `HasSupportedCandidates` 조회. `BeginChoice`가 `candidateCount <= 0` 거부 — `BeginChoice(0)`의 후보 없는 대기 상태 제거. `BuyFortunePouch`가 후보 수만큼의 슬롯·선택 버튼 연결을 사전 검사 — 패널만 연결·슬롯 누락·버튼 누락이면 돈 미차감으로 구매 거부
- **2단계: 보스 소개 재진입·상점 상태 보호** — `GoToNextBoss`에 상점 열림·점괘 선택 가드 추가. `ShowBossSelect`가 이미 표시 중인 동일 보스 재표시(`bossSelectPanel` 활성 + 같은 인덱스)면 `ResetForBoss` 생략 — 소개 재진입으로 상품·리프레시 비용이 초기화되지 않음. 실제 보스 구간 전환(다른 인덱스)은 기존대로 초기화
- **3단계: 탐욕부 취소 정산·표시 통일** — `settlement.totalMult`를 취소 시 0으로 확정 (이후 말판 단계가 양수 복귀하지 않음 — 착지 단계 기록도 취소 후 0→0). `FinishPresentation`의 윷 배율 표시를 취소 시에도 `finalMult` 유지로 정정 (윷 배율은 정산값, 취소는 전체배율만 0). 풀 도트는 기존 `preEffectDamage > 0` 가드로 피해 0 시 미등록 유지 — 변경 없음 확인
- **4단계-A: 기본 전체배율 첫 실행 초기화** — `ScoreManager` 생성자에서 `balanceConfig.baseTotalMult`로 초기화 — 첫 실행부터 설정값 반영 (기존엔 새 런 재시작 후에만 반영). `ResetForNewRun`과 동일 경로·중복 없음
- **4단계-B: 생성 가능 상품 기준 새로고침** — `CanGenerateOffers` 공통 판정 신규: 복주머니(출현 확률>0 + `HasSupportedCandidates`) 또는 방울(복주머니 확률<1 + 미보유·활성 후보). `CanRefresh`·`RefreshShop`이 같은 판정 사용 — 방울 후보 없음+복주머니 확률 0%라도 차감 없이 실패
- **5단계 정정**: 이전 보고의 "도·불·화염부·잡기 → 전체배율 36·피해 4,320"은 오기 — 정답은 전체배율 27·피해 3,240 (잡기 ×3 + 보너스 ×2)
- 검증: `dotnet build` 오류 0·경고 0 + 회귀 사례 8건 수식 대조 (도·땅 120 / 도·불 180 / +화염부 540 / +잡기 3,240 / 윷·땅·잡기 1,800 / +추혼부 7,200 / 윷·불·탐욕 675 / 탐욕+잡기 0). Unity Play 대기

### [2026-10-23 오전 (38차) - 사용자 지시 / Devin 구현 — 전체배율 개편 2단계: UI 표시 연결]

- **`YutGameManager.totalMultText` 필드 신규** — 전체배율 표시 Text (미연결 시 무시). 화면 구성이 `윷 기본점수(basicScoreText) × 윷 배율(multScoreText) × 전체배율(totalMultText)`로 정리됨
- 연출 재생은 1단계 라우팅으로 자동 동작 — 윷 배율 성장 단계는 윷 배율 표시에, 상성·잡기·부적·착지 단계는 전체배율 표시에 전후값을 반영. 전체배율은 기본값(×1)에서 시작해 발동 효과별로 갱신
- 탐욕부 잡기 취소는 취소 단계의 전체배율 → 0 + 이유 "탐욕부 — 잡기 성공으로 피해 0"으로 명확히 표시 — 양수 배율만 남아 성공처럼 보이지 않음
- 잡기는 내부 기록 `×3`+보너스 `×2`를 보존하고 화면 이유에 각각 표시 (묶어서 `×6` 표시는 선택 사항이라 기존 2단계 유지)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: `totalMultText` 연결 후 `기본점수×윷 배율×전체배율`과 최종 피해 일치 확인. (39차에서 정정: 이전 보고 예시 "도·불·화염부·잡기 = 전체배율 36·피해 4,320"은 오기 — 정답은 잡기 ×3·보너스 ×2 반영 전체배율 **27**, 피해 **3,240**)

### [2026-10-23 오전 (37차) - 사용자 지시 / Devin 구현 — 전체배율 계산·표시 구조 개편 1단계]

**기초 피해 × 전체배율 계산 구조 명문화** — 최종 피해 결과·발동 조건·규칙 전부 불변, 계산 단위 분리만:

- **`GameBalanceConfig.baseTotalMult`** (×1) + `ScoreManager.baseTotalMult` 런타임 복사본 — 보스 전환 유지·`ResetForNewRun`에서만 설정값 초기화. 추후 전체배율 강화 경로 예약
- **`ThrowSettlement` 필드 추가** — `baseTotalMult`(이번 정산 출발 전체배율)·`totalMult`(말판 배율까지 포함한 최종 전체배율)·`bellDamageMult`(별도 방울 피해 배율, 현재 ×1). `finalMult`는 성장 반영 윷 배율 의미로 유지
- **`EffectTarget` 재정의** — `Multiplier` → `YutMult`(윷 배율 단계: 방울 윷 성장·화령 안내) / `TotalMult`(전체배율 단계: 결과별 부적·상성·잡기·잡기보너스·부적·말판·재복 안내·탐욕 취소). `ScoreManager`의 runningMult 시작값에서 `stat.mult` 제거 — 윷 배율과 전체배율을 같은 수치로 섞지 않음
- **피해 공식 재배치** — `preEffectDamage = finalBasic × finalMult × runningMult(전체배율) × bellDamageMult`, 말판 배율은 `ResolveLandingEffect` 경유 후 `settlement.totalMult` 확정. 수치상 `baseTotalMult=1`·`bellDamageMult=1`로 기존 공식과 동치
- 표시 저장소 분리 — `gm.multiplier`=윷 배율, `gm.totalMult`=전체배율 (신규 런타임 필드). 연출 재생은 단계 `target`으로 라우팅, `FinishPresentation`은 정산 데이터만 읽음. **전체배율 Text 필드는 2단계에서 추가** — 현재 `multScoreText`는 윷 배율 표시로 의미 정리됨
- 검증: `dotnet build` 오류 0·경고 0. 기대 사례 수식 대조: 도·땅=120 / 도·불=180 / 도·불+화염부=540 / 윷·땅·잡기=1800 / +추혼부=7200 / 윷·불·탐욕=675 / 탐욕+잡기=0 — 전부 유지. Unity Play 대기: 물 연계·풀 도트·성장 중첩·보스 전환 유지·새 런 초기화

### [2026-10-23 오전 (36차) - 사용자 지시 / Devin 구현 — 혼합 상점·점괘 선택 구현 (지시서 1~3단계)]

**혼합 상점 + 복주머니 구매·점괘 3택1 UI** — 데이터·거래·표시가 하나의 기능이라 연동 구현:

- **`ShopOffer` 런타임 자료형 신규** (`Data/ShopOffer.cs`) — `ShopOfferKind`(Bell/FortunePouch) enum으로 상품 종류를 구분 (이름 문자열·임시 방울 판정 없음). `ShopManager.offers`를 `List<BellData>` → `List<ShopOffer>`로 전환
- **`ShopConfig` 확장** — `fortunePouchPrice: 20`, `fortunePouchChance: 0.5`, `fortuneCandidateCount: 3`, 복주머니 이름·설명 필드 (에셋 YAML에 실제 저장값 반영)
- **혼합 추첨** — `RollOffers`가 슬롯당 복주머니 확률로 독립 추첨 (구성 보장 없음, 4칸 전부 같은 종류 가능). 방울 칸은 미보유·활성 후보에서 목록 내 중복 없이 뽑고 **방울 후보가 없으면 그 칸을 비움** — 복주머니 대체 여부는 미확정으로 사용자 결정 대기. 목록 수명·구매 완료·리프레시 규칙 보존
- **복주머니 구매** (`ShopManager.BuyFortunePouch`) — 장착 한도와 무관하게 잔액·선택창·후보 확보 순서로 검사. `BeginChoice`로 후보 3개를 먼저 확보한 뒤 `TrySpend` — 실패 시 `ClearChoice`로 후보만 정리하고 돈·구매 완료 표시 없음. 성공 시 차감·구매 완료 각 1회, 선택 대기 상태로 상점 잠금
- **선택 중 차단** — `isChoosingFortune` 동안 `BuyOffer`·`SellBellInstance`·`RefreshShop`·`CloseShop`·`EnterBossBattle`·`SkipBoss`를 메서드 수준에서 거부 + UI 버튼 비활성 (양쪽 차단)
- **점괘 UI** — `YutGameManager`에 `fortuneChoicePanel`·`fortuneOptionSlots[3]`(BellSlotUI 재사용)·`fortuneChoiceText` 필드 추가. `GameUIManager.SyncFortuneChoicePanel`이 `pendingCandidates`를 표시만 — 선택 버튼 리스너는 `Bind`가 교체 방식으로 코드 자동 연결 (수동 OnClick 불필요). 상점 구매 슬롯은 `ShopOffer` 종류별 이름·설명·가격 표시, 복주머니는 방울 한도 무관 잔액만 검사
- 검증: `dotnet build` 오류 0·경고 0. **Unity Play 대기 — 사용자 Inspector 작업 필요** (패널·슬롯 3칸 배치). 확률 강화 8종·말판 재배치는 미구현 유지

### [2026-10-23 오전 (35차) - 사용자 지시 / Devin 구현 — 후속 지시서 1단계: 문제 3건 수정]

**복주머니 추가 후 확인된 문제 3건 수정** (규칙 변경 없음):

- **A. 화령방울 성장 안내에 복주머니 강화 누락** — `TileEffectManager.CurrentAttrMult(attr)` 공통 조회 경로 신규 (설정 기본 + 화령 성장 + 복주머니). `BellManager`의 AttrMultGrowth 안내가 성장 전후를 이 경로로 조회 → 복주머니로 2.0인 상태면 "2 → 2.5"로 표시. 정보판·실제 계산도 같은 경로 사용 — 계산식 복제 제거
- **B. 물·풀 강화의 특정 ID 의존 제거** — `FortuneUpgradeManager`의 누적 저장소를 `(kind, target)` 키의 `cumulativeBonus`로 재구성. `upgradeId`는 정의 식별·획득 기록 전용, 같은 kind의 새 ID 데이터도 자동 누적. `GetDef`·`CountedBonus`·하드코딩 ID 조회 제거
- **C. 후보 0개 선택 대기 진입 방지** — `BeginChoice`가 bool 반환: 대기 중 재호출·활성 후보가 candidateCount 미만이면 후보를 비우고 false — 끝낼 수 없는 선택 상태 미생성. 구매 연결 시 실패면 돈 미차감·상품 미처리 경로로 사용
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 복주머니+화령 조합 안내 일치 / 동일 kind 다른 ID 적용 / 후보 부족 시 상태 정리

### [2026-10-23 오전 (34차) - 사용자 지시 / Devin 구현 — 복주머니 강화·혼합 상점·말판 재배치 1단계]

**점괘 강화 데이터·런타임 상태·확정 수치 적용** (지시서 5단계 중 1단계):

- 신규: `FortuneUpgradeData`(`FortuneUpgradeKind` 6종 + 대상·수치·활성 필드) / `FortuneUpgradeDatabase`(SO) / `FortuneUpgradeManager`(획득 횟수·누적 강화·선택 대기·조회·런 초기화) / `FortuneUpgradeDatabase.asset` 16종
- `YutGameManager` — `fortuneDatabase` 에셋 필드 + `fortuneManager` 생성 + `ResetForNewRun`에 리셋 연결 (보스 전환에서는 미호출 — 런 단위 수명)
- 확정 수치 적용 경로: 윷 배율 = 런타임 `stat.mult` 가산(도걸·윷령방울과 동일 경로·원본 미변경) / 불 = `fortuneManager.attrMultBonus` 별도 보관 → `TileEffectManager`가 `fireTileMult`+화령+복주머니 합산 / 물 연계·풀 도트 비율 = 횟수×수치 `CountedBonus` 조회
- `GameUIManager` — 불 배율·물 연계 표시에 복주머니 강화분 합산 (표시=계산 일치)
- **활성 강화 8종**: 도/개/걸/윷/모 배율·불 배율·물 연계·풀 도트. **비활성 8종**: 윷·속성 확률 — 추첨 정책(차감 방식·상한·적용 범위 등) 사용자 확인 대기
- **Inspector 작업**: `FortuneUpgradeDatabase` 에셋 → `YutGameManager.Fortune Database` 연결
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 같은 강화 2회 누적 / 물 단계 전체·풀 비율 / 보스 전환 유지·새 런 초기화 / 방울 성장 합산

### [2026-10-23 오전 (33차) - 사용자 지시 / Devin 구현 — 유지보수 정리 1단계]

**확인된 오류 5건 수정** (규칙 변경 없음, 동작 보존 — 표시·방어 정합성 개선):

- **A. 잡기 후 부적 배율 표시 누락** — `ScoreManager.SettleNormalThrow`의 `runningMult` 시작값에 `catchBonusMult` 추가. 윷·땅·잡기·추혼부에서 단계 전후가 36→144로 실제 피해(7200)와 일치
- **B. 화령방울 성장 표시 단위 혼동** — `GrowthTrigger.isInfo` 신규: `AttrMultGrowth` 단계는 안내 전용으로 점수판 배율을 바꾸지 않고, 기록 수치는 속성 배율 단위(before/after = 1.5→2)로 보존. 이유 표시 "화령방울 — 불 착지 배율 1.5 → 2". `AddBellStep`에 `displayedMult` 인자 추가
- **C. 빈 상점 목록 무료 갱신** — `ShopManager.offersGenerated` 플래그 신규: 목록 생성 여부와 상품 개수를 구분. 빈 목록도 보스 구간 동안 유지, 판매 후 새 상품은 유료 리프레시 경로만. `ResetForBoss`에서만 초기화
- **D. 보스 이동 배열 방어** — `DrawBossMoveSteps`가 null·빈 배열·길이 불일치를 검사하고 `-1` 반환 → `PrepareBossMove`가 계획을 만들지 않고 `MoveBossPiece`가 이동을 생략(봉쇄부도 미소모). `ValidateDataAssets` 경고 문구를 실제 동작(이동 차단)과 일치
- **E. 보스 상성 검증** — `HasAffinityAbility`가 약점·강점을 `CombatAttributes`(불·물·풀) 멤버십 + 서로 다름으로 검사. 비공격 속성 상성은 조회·적용·표시 모두에서 거부 — 임의 대체 없음
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: A~C 재현 케이스 / 빈 상점 판매→재진입 / 정상 배열 이동 단계 유지

### [2026-10-23 오전 (32차) - 사용자 지시 / Devin 구현 — 유지보수 정리 0단계]

**현재 동작 기준 확보** — 코드 변경 없이 기준점 기록만 수행:

- **Git 기준점**: `develop` 커밋 `0a0f7c9` — 방울 8종 1단계·환위부 선택 수정까지의 동작 확정본 (미수정 오류 포함 상태를 식별 가능하게 보존)
- **테스트 Scene**: `Assets/test2.unity` — `moveAdjustPanel` 연결 + 환위부 +1/−1/취소 OnClick 연결 **이미 완료** 확인. `rollButton`은 `{fileID: 0}` 미연결 (실제 차단은 `RollYut` 검사가 처리). `SampleScene.unity`에는 신규 연결 없음 — 실제 사용 Scene은 test2로 간주, 사용자 확인 필요
- **활성 구성**: 부적 8종(화염·추혼·탐욕·재기·인연·봉쇄·승부·환위) / 방울 4종(도걸·윷령·화령·족쇄). 수령·목령·천운·지령 = 정의 준비·비활성
- **기준 사례 검증** (코드 대조 — 사용자 지시서와 일치): 도·불=180 / 윷·땅·잡기=1800 / +추혼부=7200 / 윷·불·탐욕부=675 / 탐욕부+잡기=0 / 풀 300→400→200 도트 60→80→90 / 물 90 연속 90→180→360→720→1440 / 화령 첫 발동 불×2·도=240 / 보상 10+남은 투척×5냥
- **확인된 오류 (1단계 수정 대상 — 코드 확인 완료)**:
  - A. `ScoreManager` 부적 곱셈의 `runningMult` 시작값에 `catchBonusMult` 누락 — 잡기 성공 시 부적 단계의 전후 배율이 실제와 불일치 (실제 피해는 정상)
  - B. 화령방울 성장 단계가 누적 성장분(0→0.5)을 전체 실효 배율 before/after로 기록 — 표시 단위 혼동
  - C. `ShopManager`가 `offers.Count == 0`을 미생성으로 판단 — 빈 목록도 재진입 시 재생성 경유 → 방울 판매 후 무료 상품 갱신 가능
  - D. `DrawBossMoveSteps`가 `bossMoveStageMin[stage]`를 null/빈 배열 방어 없이 접근 — `BossMoveStage`는 min 길이로 방어하나 null 자체는 미방어
  - E. `HasAffinityAbility`가 `weak != strong`만 검사 — 땅·쇠 등 비공격 상성도 유효 통과
- **문서-코드 충돌 없음 확인**: 상점 후보는 규칙 문서에 '8종 목표·1단계 시점 4종 활성'으로 기록돼 구현 상태와 일치
- 미확정 유지: 수룡부 × 수령방울 중첩, 인연부 덱 1장 부족 처리, 탐욕부 충돌=잡기 임시 해석 — 임의 변경 없음

### [2026-10-22 오후 (31차) - 사용자 지시 / Devin 구현 — 환위부 선택 완료 불가 버그 수정]

**버그 수정 (규칙 변경 없음)**: `ChooseMoveAdjust`가 `CanUseAfterRoll()`을 재호출했는데, 그 안의 `isChoosingMoveAdjust` 검사가 열린 선택창 자체를 거부 사유로 만들어 +1/−1 선택이 항상 실패.

- `TalismanManager.ChooseMoveAdjust` — '사용 개시' 판정과 '선택 완료' 판정 분리: `isChoosingMoveAdjust` 검사 없이 Playing·세션 투척 완료·미확정·연출 중 아님·땅 선택창 없음·대기 카드 유효만 재검증. `delta != ±1` 거부 추가. 성공 시 보정·기록·소모 1회 후 선택 해제, 취소는 기존 경로 유지
- `YutGameManager` — `rollButton`(Button) 필드 신규: 기존에 최초 던지기 버튼은 참조 경로가 없어 표시상 활성 ↔ 실제 차단이 어긋났음
- `GameUIManager` — `rollButton.interactable`을 `RollYut`의 차단 조건과 동일 판정으로 제어 (Playing·미투척 세션·연출/선택창 없음·남은 횟수)
- **Inspector 작업 필요**: '윷 던지기' 버튼을 `YutGameManager.Roll Button`에 연결 (미연결 시 기존 동작 유지 — 실제 차단은 `RollYut` 검사가 처리)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: +1/−1 선택 후 창 닫힘·확정 가능 / 취소 후 세션 그대로 / 재투척 소진 후 환위부 사용 가능 / 연타 중복 없음 / 선택창 중 투척 입력 차단

### [2026-10-22 오후 (30차) - 사용자 지시 / Devin 구현 — 방울 8종 전환 1단계]

**상점 후보 8종 전환 + 성장형 방울 3종 구현** (사용자 지시서, 4단계 중 1단계):

- `BellData`/`OwnedBell` — `targetResult2`(쌍 대상 결과)·`chance`(확률 효과) 필드 추가. `BellEffectKind` 끝에 `AttrMultGrowth`·`WaterComboGuard`·`DotDoubleChance`·`GoldOnEarth` 추가 — 기존 값·순서 보존
- `BellManager` — `attrMultBonus`(속성 착지 배율 런타임 성장분) 신규 + `GetAttrMultBonus`. `YutMultGrowth` 쌍 대상 지원(targetResult2 ≠ BackDo이면 두 결과 공유 한도), `AttrMultGrowth` 케이스 — 발동 정산당 1회 성장·발동 횟수 소모는 기존 공용 경로. `ResetForNewRun`에 성장분 초기화 추가
- `TileEffectManager` — 불 배율 = `fireTileMult` + `GetAttrMultBonus(Fire)`. `GameUIManager` 속성 정보판 동일 계산
- `BellDatabase` — 도걸방울(b_dogeol)·지령방울(b_jiryeong) 신규. b_yutryeong 쌍 대상(윷+모)·b_hwaryeong AttrMultGrowth 전환 활성. 수령·목령·천운·지령 = 정의 준비·비활성(2~4단계 대기). 도깨비·개화·걸음·모령·재복 제외(정의 보존)
- **활성 상점 후보(1단계 시점): 도걸·윷령·화령·족쇄 4종** — 나머지 4종은 단계 구현 후 활성화
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 상점 후보 / 도·걸·윷·모 각각 해당 결과만 성장·합산 5회 소멸 / 화령 첫 불 착지 ×2(1.5→2) / 소멸·판매·보스 전환 후 성장 유지·새 런 초기화 / 정보판·남은 횟수 일치
- 미확정: 수룡부 × 수령방울 중첩 — 사용자 결정 대기

### [2026-10-22 오후 (29차) - 사용자 지시 / Devin 구현 — 부적 20종 전환 2단계]

**환위부(백색 부적) 사용 시점 + ±1 선택 UI** (사용자 승인):

- `TalismanManager` — `isChoosingMoveAdjust` + `pendingMoveAdjustHandIndex` 선택 대기 상태. `CanUseAfterRoll()` 신규 — `AfterRollBeforeConfirm` 카드는 `session.rolled && !confirmed` 구간(재투척 전·사이·소진 후 포함)에만 사용 가능. `CanUseCard` public 전환 — 손패 버튼 활성화와 실행 재검증이 같은 판정 공유. `UseTalisman`의 MoveAdjust 경로는 선택창만 열고 카드 미소모 → `ChooseMoveAdjust(delta)`에서 세션·카드 재검증 후 `session.moveDelta += delta`·`RecordUse`·소멸(기존 MoveDelta 합산 경로 재사용 — 최소 이동 0칸 처리 유지), `CancelMoveAdjust`로 미소모 취소
- `YutGameManager` — `moveAdjustPanel`(GameObject) 필드 + `ChooseMoveAdjustPlus`/`ChooseMoveAdjustMinus`/`CancelMoveAdjust` 버튼 진입점. `RollYut`·`RerollStick`·`ConfirmThrow`에 `isChoosingMoveAdjust` 차단 추가, 패널 전환 지점 4곳에서 선택창 숨김
- `GameUIManager` — 손패 사용 버튼을 카드별 `CanUseCard`로 개별 판정(기존: 전부 준비 단계 한정). `throwInputOpen`에 선택창 차단 추가, `SetPanel(moveAdjustPanel, …)` 동기화
- `TalismanDatabase` — t_hwanwi `includeInDeck: 1` 활성화 (활성 8종)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 환위부가 준비 단계에서 비활성·투척 후 활성 / +1·−1 최종 착지 변경 / 후퇴 시 최소 0칸 현재 칸 정산 / 재투척 소진 후에도 사용 가능 / 선택창 열린 동안 투척·재투척·확정 차단 / 취소 시 미소모·재사용 가능 / 정상·스킵 결과 일치

### [2026-10-22 오후 (28차) - 사용자 지시 / Devin 구현 — 부적 20종 전환 1단계]

**부적 20종 전환 1단계 — 데이터 정리·확정 수치 변경** (사용자 지시서 + 해석 확정 9건):

- `TalismanData.cs` — `TalismanEffectKind` 끝에 신규 12종 추가(기존 enum 값 불변): `WaterComboKeep`·`ForestDotBoost`·`CatchWiden`·`MoveAdjust`·`NextTurnProbBoost`·`BoardReshuffle`·`BossMoveReduce`·`ProximityBaseBoost`·`NoRerollBaseBoost`·`TriAttributeCombo`·`PrevResultMatch`·`ResultGamble`. `TalismanUseTiming.AfterRollBeforeConfirm` 추가(환위부 전용). 필드 신규: `targetResult2`(도걸·모윷부 두 번째 목표)·`duration`(지속 턴 수)·`range`(거리 조건)
- `TalismanDatabase.asset` — 정의 총 28종(기존 17 + 신규 11). 수치 변경: 화염부 ×4→×3 / 탐욕부·승부부 ×2→×1.5. 수룡부·초목부 효과를 WaterComboKeep·ForestDotBoost로 전환(구현 전 비활성). 덱 제외: 진격부·후퇴부·윷령부·모령부·천운부(정의·효과 보존). 신규 11종 정의 추가 — 전부 `includeInDeck: 0`
- 활성 덱 7종: 화염부·추혼부·탐욕부·재기부·인연부·봉쇄부·승부부. 미구현 효과는 `ApplyEffect`가 false 반환 — 강제 사용 시도도 카드 미소모로 안전
- 해석 확정(사용자 답변): 수룡부=물 외 착지 시 연속 기록 유지+현재 연계 배율 적용 / 초목부=이번 등록·증가분만 ×2 / 족쇄부=사용 차례 포함 3턴·족쇄방울과 합산 / 추적부=전후 각 2칸 이내 / 고집부=차례별 판정 / 삼재부=3턴 내 불·물·풀 수집 완성 시 세 번째 착지 ×6 / 도걸·모윷부=면 조합 가중 추첨 / 역행부=속성 재섞기 / 도박부=결과 선택→최종 일치 시 보스전 윷 배율 ×1.2
- 카드 ID 대응: 기존 유지 t_fire·t_water·t_forest·t_pursuit·t_greed·t_retry·t_bond·t_seal·t_allinnothing. 신규 t_siege(포위부)·t_hwanwi(환위부)·t_dogeol·t_moyut·t_reverse·t_shackle·t_chase·t_stubborn·t_samjae·t_cycle·t_gamble
- 검증: `dotnet build` 오류 0·경고 0. **사용자 검토 후 2단계(환위부 백색·±1 선택 UI) 진행 예정**

### [2026-10-22 오후 (27차) - 사용자 지시 / Devin 구현]

**풀 도트 첫 발동 시점 수정** (사용자 지시):

- 도트 지급 시점을 **정산 종료 → 다음 최초 던지기 정상 접수 시점**으로 이동 — `FinishPresentation`의 즉시 틱과 `TalismanManager.ConfirmChoice`의 지연 틱 호출 제거, `ApplyTurnStartDot`은 `RollYut`에서만 호출
- `YutGameManager` — `dotPaidThisTurn` 필드 신규: `PrepareTurn`에서 리셋, `RollYut`의 모든 검사(상태·연출·선택창·남은 횟수·미투척) 통과 후 `InitialRoll`·`MoveBossPiece` 전에 차례당 1회 지급. 도트로 처치 시 `EnterClear` 후 즉시 return — 투척 소모·보스 이동 없음
- 풀 착지는 즉시 피해 D + 도트 등록/누적만 처리 — 정산 종료 직후 추가 피해 없음. 땅 선택창이 열려 있으면 투척 자체가 차단되므로 "선택 완료 후 도트" 순서 유지
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 풀 착지 직후 도트 미지급 / 다음 최초 던지기 1회 지급 / 재투척·확정·선택창·거부 입력 미지급 / 도트 처치 즉시 클리어·투척 미소모 / 보스 전환 잔류 없음 / 정상·스킵 일치

### [2026-10-22 오후 (26차) - 사용자 지시 / Devin 구현]

**능력치 정보판 줄바꿈** (사용자 지시):

- `GameUIManager` — `currentYutStatsText`·`currentAttributeStatsText` 항목 구분자를 `" / "`에서 `"\n"`으로 변경 — 윷 5행·속성 3행 세로 표시. 수치·조회 경로·계산·Inspector 연결 변경 없음
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기 — 각 항목이 줄 단위로 표시되는지 확인

### [2026-10-22 오전 (25차) - 사용자 지시 / Devin 구현]

**부적 덱·손패 보스전 단위 초기화 + 땅 착지 공격 허용** (사용자 확정 지시 2건):

- `TalismanManager` — `ResetForBoss()` 신규(기존 런 초기화와 같은 정리: 덱 재구성 예약·손패·후보·선택·사용 기록·세션 효과 비움), `ResetForNewRun()`은 `ResetForBoss()` 위임. 헤더 주석·미확정 목록 정리(손패 유지 임시 규칙 → 확정 해제)
- `YutGameManager` — `BeginBossFight`에서 `talismanManager.ResetForBoss()` 호출 — 실제 전투 진입 시 보스전당 1회. `OpenStartPick`은 새 덱 위에서 동작(사용·소멸 카드 복귀). 돈·방울·런 성장·말판은 유지
- `ScoreManager` — `attackLanded`에 `Earth` 추가(땅 공격 허용). 상성 기록 단계는 공격 속성에서만 — `tile.attribute != Earth` 조건 추가
- `TileEffectManager` — `ResolveLandingEffect`에 `Earth` 케이스: 물 연속 초기화 + 피해 ×1 + "공격 ×1 — 부적 선택" 정보 단계. 도트 등록·증가 없음. default는 START·쇠만 0
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 땅 공격 도=120·승부부=240·탐욕 취소=0 / 땅 부적 선택 유지 / 다음 보스 새 덱 복귀·손패 미유지 / 소개·상점·건너뛰기 미초기화 / 돈·방울·성장 유지

### [2026-10-22 오전 (24차) - 사용자 지시 / Devin 구현]

**점수제·속성 말판 전환 + 보스 상성 선택 적용** (사용자 지시서 + 정책 확정 8건):

- `YutStatEntry` — `baseScore` 추가(도20/개30/걸30/윷50/모60) + 배율 변경(6/3/4/6/5), `basicBonus` 예약 보존. `BuildYutStats` 복사 경로 포함
- `BossData` — `useAffinity`+`affinityWeak`/`affinityStrong`/`affinityWeakMult`/`affinityStrongMult` 추가, BossDatabase 전 보스 `useAffinity: 0`. `BossManager`: `EnsureAffinity` 무작위 생성·`runAffinities` 삭제 → `HasAffinityAbility`/`GetAffinity`(null=비상성) 데이터 주도로 전환, `AffinityMultiplier`/`BuildAffinityText` 데이터 사용, 상성 없는 보스 상성 텍스트 숨김
- `TileEffectManager.cs` 신규 — 물 연속(보스전)·풀 도트(보스전) 상태 + `ResolveLandingEffect`(불×1.5·물 연계·풀 도트·비공격 0+물 초기화, 착지 효과 단계 기록) + `TurnStartDotDamage`
- `ScoreManager` — 공식 전환: baseBasic = 윷 기본점수, 칸 점수·속성 성장 합산 제거, `attackLanded` 판정(구 tileScore>0 대체), 잡기 보너스(`catchBonusMult`) 별도 단계, 상성 단계는 능력 보스만, 천운부·재복방울은 `GetAffinity` null 시 조건 불충족, 최종 = `tileEffects.ResolveLandingEffect`. `EffectSourceType.TileEffect` 추가, `settlement.tileScore` 제거 → `attackLanded`/`tileEffectMult`/`tileEffectNote`/`catchBonusMult`
- `YutGameManager` — `tileEffects` 등록, `ApplyTurnStartDot()`(도트 틱→totalScore, 처치 시 즉시 클리어), `FinishPresentation`에서 땅 선택창 없으면 즉시 틱 / `TalismanManager.ConfirmChoice`에서 지연 틱, `BeginBossFight`·`ResetRunState`에서 리셋, `ResetAffinities` 호출 제거
- `BellManager` — `FrogAffinityMult` null 안전, `GameUIManager` — 정보판(윷 점수×배율 / 속성 효과 상태)·착지 설명·상성 숨김 전환
- `GameBalanceConfig`+에셋 — `fireTileMult 1.5`·`waterComboMults {1,2,4,8,16}`·`forestDotInitialPercent 20`·`forestDotAddPercent 5`·`catchBonusMult 2`(catchAffinityMult 대체)
- `BellDatabase` — 화령·수령·목령·청개구리 `effectEnabled: 0`
- 확정 정책: 잡기 보너스 ×2 별도 유지 / 땅·START 공격 0 / 물 연속 보스전 단위 / 도트 처치 즉시 클리어 / 땅 선택 후 도트 / 등록 시 반올림 / 피해0 풀 미등록 / 청개구리 상점 제외
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 도 불 180 / 개 물 90→180→360→720→1440 / 풀 300→도트60,400→80,200→90 / 도트 틱 중복 없음 / 비상성 보스 상성 ×1·잡기 ×6 / 천운부·재복 미발동 / 보스 전환 상태 제거

### [2026-10-21 오전 (23차) - 사용자 지시 / Devin 구현]

**능력치 정보판 + 보스 클리어 금전 보상** (사용자 지시):

- `YutBoard.BaseAttackScore(attr)` 정적 접근자 신규 — `BoardTile.AttackScore`가 위임. 정산과 표시가 같은 기본값 경로 사용 (UI에 50점 계산식 복제 없음)
- `YutGameManager` — `currentYutStatsText`·`currentAttributeStatsText` 필드 추가, `BeginBossFight`에 `economyManager.ResetClearReward()`, `EnterClear`에 `GrantBossClearReward()` 호출
- `GameUIManager` — 정보판 표시(도·개·걸·윷·모 배율 / 불·물·풀 공격력) + `FormatStat`(정수면 소수점 생략)·`AttrAttack` 헬퍼 — 조회 전용
- `EconomyManager` — `clearRewardPaid` 플래그·`ResetClearReward`·`GrantBossClearReward()` — 보스전당 1회 지급, 기본+남은 투척 보상 로그 기록
- `ShopConfig` + 에셋 — `bossClearBaseReward=10`·`bossClearPerThrowReward=5` 추가
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 새 런 기본값(도4/개3/걸4/윷9/모9·불물풀50) / 성장 +0.5·+5 갱신 / 판매·소멸 후 유지 / 보상 4회남음=30·2회=20·0회=10 / 연출 스킵 동일 / 재진입·게임오버·건너뛰기 미지급

### [2026-10-21 오전 (22차) - 사용자 지시 / Devin 구현]

**방울 UI 고정 슬롯 전환 + 상점 리프레시** (사용자 승인 + 정책 확정 3건):

- `BellSlotUI.cs` 신규(Scripts/UI) — 슬롯 컴포넌트(nameText/descText/statusText/actionButton/actionLabel 명시 참조). `Bind`로 내용·버튼 표시·리스너 교체(누적 없음), `Clear`로 빈 칸
- `YutGameManager` — `equipBellSlots[4]`·`shopOfferSlots[4]`·`shopBellSlots[4]`·`refreshButton`·`refreshCostText` 필드 신규 + `RefreshShop()` 버튼 함수. 구형 필드 제거: `bellContent`·`bellItemPrefab`·`shopOfferContent`·`shopBellContent`·`shopItemPrefab` (Scene 직렬화 잔여분 무해 — Unity가 무시. `BellItem`/`ShopItem` Prefab 에셋은 삭제하지 않음 — 미사용 보고)
- `ShopManager` — 상품 목록 보스 구간 유지(`offers.Count == 0`일 때만 추첨), `BuildCandidatePool` 분리, `CurrentRefreshCost`(long 계산+int 상한), `CanRefresh`·`RefreshShop()`(후보 없음·잔액 부족 시 차감 없이 실패), `SellBellInstance(instanceId)` 인스턴스 판매로 교체, `ResetForBoss()`(목록·비용 초기화) — `BossManager.ShowBossSelect`에서 호출(다음 보스 소개 = 보스 전환)
- `ShopConfig` + 에셋 — `refreshBaseCost=2`·`refreshCostMultiplier=2` 추가
- `GameUIManager` — `SyncBellPanel`/`SyncShopPanel`을 고정 슬롯 바인딩으로 재작성 — 구형 동적 목록 생성·스냅샷 비교·Prefab 경로 제거. 상품 슬롯: 구매 완료 표시·비활성 조건(잔액·한도), 판매 슬롯: 인스턴스 ID 바인딩, 리프레시 비용 텍스트·버튼 활성
- 정책 확정(사용자): 리프레시 비용 보스 전환 시 초기화 / 상점 닫아도 목록 유지 / 판매 방울 재구매 시 횟수 새 상품 초기화
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 0~4개 보유 표시 / 구매·판매 인스턴스 정확성 / 리프레시 비용 2→4→8 / 잔액 부족 유지 / 닫기·재진입 목록 유지 / 보스 전환 초기화

### [2026-10-21 오전 (21차) - 사용자 지시 / Devin 구현]

**보스 다음 이동 위치 미리보기** (사용자 승인):

- `BossManager` — 이동 계획 분리: `movePlanReady`·`plannedBaseSteps` 필드 + `PrepareBossMove()`(차례 준비에 거리 1회 추첨·보관) + `PlannedEffectiveSteps`·`PlannedDestination`(족쇄방울·봉쇄부를 계산 시점에 반영 — 봉쇄 미소모). `MoveBossPiece()`는 준비된 계획을 1회 소비해 실행(재추첨 없음). `ApplyMoveReduction`으로 족쇄 로직 공용화. `ResetBossPiece`가 계획도 함께 폐기
- `YutGameManager` — `bossMovePreview`(RectTransform) 필드 추가, `PrepareTurn`에서 `PrepareBossMove()` 호출, `BeginBossFight` 순서 정리(ResetBossPiece→PrepareTurn — 새 보스 기준 계획), Awake에서 표식 Raycast Target 해제·초기 숨김
- `BoardManager` — `MoveBossPreviewVisual(tileIndex)` 추가 — 표식을 예정 칸 위치에 배치
- `GameUIManager` — UpdateUI에 예고 표시 제어: `Playing` + 미투척 세션 + `movePlanReady`일 때만 표시, 표시 중엔 `PlannedDestination`으로 위치 갱신 — 추첨·상태 변경은 UI에서 하지 않음
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기: 첫 보스전 첫 투척 전 예고 표시 / 예고 칸=실제 이동 일치 / 재투척·확정 추가 이동 없음 / 봉쇄부 사용 즉시 예고 현재 칸 갱신·미소모 / 다음 차례 예고 복귀 / 족쇄 적용 일치 / 클리어·게임오버·보스 전환 잔류 없음 / 정상·스킵 동일

### [2026-10-21 오전 (20차) - 사용자 지시 / Devin 구현]

**정산 후 부적 버튼 재활성화 누락 수정**:

- `YutGameManager.FinishPresentation` — `UpdateUI`가 `PrepareTurn`·땅 선택창 처리 전에 실행돼 손패 버튼이 이전 확정 세션 기준 비활성으로 남던 문제. `PrepareTurn`+땅 선택 처리 후 최종 `UpdateUI`를 추가 — 버튼 강제 활성이 아니라 새 준비 세션·선택창 상태를 반영한 뒤 `CanUseNow`+카드 `useTiming`으로 활성 판정. `PrepareTurn` 중복 호출 없음(준비 효과 초기화 방지), 클리어·게임오버 경로는 준비 세션 미생성
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 대기: 일반 칸 정산 후 버튼 활성 / 2·3번째 차례 반복 / 땅 선택 완료 후 활성 / 투척 후·재투척·정산 중 비활성 / 정상·스킵 동일 동작

### [2026-10-21 오전 (19차) - GPT 기획 / Devin 구현]

**기본 재투척 1회·말판 확률·부적 사용 시점 변경** (사용자 승인):

- `YutThrowSession` — `MaxRerolls` 2→1. `rolled` 플래그·`RollInProgress` 추가: 세션은 차례 준비에 생성(rolled=false=준비 단계), `InitialRoll`은 면과 재투척 기록만 초기화하고 준비 때 사용한 부적 효과(특별 재투척·moveDelta·armed/succeeded 결과 부적·포기) 보존. `CanReroll`에 `!rolled` 가드
- `BoardManager` — 확률 30/30/30/10/쇠0, 추첨 예외 반환값 Metal→Earth (쇠는 정의·표시 유지, 생성만 제외)
- `TalismanData` — `TalismanUseTiming` enum(BeforeFirstThrow)·`useTiming` 필드 + `TalismanAppearance` enum(미정/노란부적/하얀부적)·`appearance` 필드 추가. 사용 시점을 카드 데이터로 판정 — 색상·이름 문자열 판정 금지. `TalismanDatabase.asset` 17종 전부 `useTiming: 0`·`appearance: 0` 명시
- `TalismanManager` — `CanUseNow`를 '세션 존재 + 미투척(rolled=false) + 미확정'으로 변경(준비 단계만 허용), 카드별 `CanUseCard` = 구간 + `useTiming` 데이터 검사. 승부부·재기부 등 준비 사용 효과는 세션 필드에 보관돼 투척 후 유지
- `YutRollManager` — `StartSession` → `PrepareSession` (세션 생성만, 투척 안 함)
- `YutGameManager` — `PrepareTurn()` 신규(세션 생성 + `ClearSessionEffects` — 차례당 1회): `BeginBossFight`·정산 종료 후 다음 차례에서 호출. `RollYut`는 준비 세션의 `InitialRoll`만 실행 + 보스 이동, `rolled` 세션 차단. `ConfirmThrow`는 `RollInProgress`만 허용. 스페이스바 분기도 동일 기준
- `GameUIManager` — `throwInputOpen`을 `RollInProgress` 기준으로 변경, 면 표시는 `rolled` 이후만, 준비 상태 메시지 추가, 손패 사용 버튼은 준비 구간 활성
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기: 재투척 기본 1회 / 재기부 사용 시 2회 / 쇠 없는 30칸 말판 / 준비 단계 부적 사용→투척 후 효과 유지(봉쇄·재기·승부·결과별) / 투척 후 사용 차단 / 다음 차례 재사용 가능 / 정상·스킵 결과 일치

### [2026-10-20 오후 (18차) - GPT 기획 / Devin 구현]

**방울 장착·상점·경제 시스템** (사용자 승인 — 과거 방울 선택 이벤트 완전 대체):

- 신규 파일: `Scripts/Data/BellData.cs`(BellEffectKind·BellData·OwnedBell)·`BellDatabase.cs`·`ShopConfig.cs`, `Scripts/Core/EconomyManager.cs`·`ShopManager.cs`, `Prefabs/BellItem.prefab`·`ShopItem.prefab`, 에셋 `BellDatabase.asset`(12종)·`ShopConfig.asset`(50냥·4칸·후보 4종)
- `BellManager` 재작성 — 장착 인스턴스 관리·성장 처리(`ApplyGrowth` → GrowthTrigger 목록 반환, 한도 소진 인스턴스만 소멸)·속성 공격력 런 성장·청개구리 유효 상성(`FrogAffinityMult`)·지속 효과 조회(`HasBell`). 과거 선택 이벤트 코드 전부 제거
- `YutGameManager` — 레거시 필드(`bellPool`·`bellChoicePanel`·`bellOption*`·`ownedBells`·`bellSlots`·`talismanSlots`)와 방울 선택 버튼 함수 제거. 신규 필드: `bellDatabase`·`shopConfig`·`moneyText`·`shopMoneyText`·`bellContent`·`bellItemPrefab`·`shopPanel`·`shopOfferContent`·`shopBellContent`·`shopItemPrefab`. 신규 버튼 함수: `OpenShop`·`CloseShop`. `EnterBossBattle`·`SkipBoss`는 상점 열림 중 차단. 보스전 시작에서 방울 초기화 제거(런 단위 유지)
- `ScoreManager` — 정산에서 방울 성장 발동(이번 공격부터)·`AddBellStep` 기록(Basic/Multiplier), 청개구리 유효 상성(잡기 강제보다 우선), 재복방울 약점 공격 +1냥(정산당 1회). `baseMult`·`finalMult` float화
- `YutRollManager` — `YutStatEntry.mult` int→float, `UpgradeYutStat` 시그니처 float
- `BossManager` — `MoveBossPiece`에 족쇄방울 −1칸(최소 1칸, 봉쇄부 경로 미간섭)
- `GameUIManager` — `SyncBellPanel`(스냅샷 비교 후 재구성)·`SyncShopPanel`(상품·보유 목록+잔액, 버튼 onClick 코드 연결) 신규. 방울 선택창·bellSlots 동기화 제거
- `Assembly-CSharp.csproj` — 신규 5개 스크립트 등록. `SampleScene.unity` — `bellDatabase`·`shopConfig` 에셋 참조 연결 (구형 필드 직렬화 잔여분은 무해 — Unity가 무시)
- 미구현·미확정: 천운방울 효과(확률 미확정 — effectEnabled=0), 재복방울 세부 해석(원래 약점 기준·피해 0 시에도 지급 — 임시 적용), 방울 재구매 횟수 처리
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기: 상점 진입/구매/판매/잔액 표시, 성장 방울 발동·소멸·성장 유지, 족쇄/청개구리/재복 실효과, 런 초기화

### [2026-10-20 오후 (17차) - GPT 기획 / Devin 구현]

**배율 이유 표시 범위 확장 + 탐욕부 연출 타이밍** (사용자 승인 — 표시만, 계산·카드 소모·피해 계산 미변경):

- `ScoreManager`: 상성 이유 텍스트 세분화 — 일반 상성 "일반 상성 ×1"(기존 "상성 ×1"), 약점·강점·잡기 강제는 기존 유지. `ScoreEffectStep.preApplyDelay`/`postApplyDelay` 신규(기본 -1 = 일반 타이밍) — 탐욕부 비취소 단계에만 `PresentationConfig`의 탐욕부 딜레이 기록. 취소 탐욕부는 상승 연출 없이 취소 안내 단계만 재생
- `PresentationConfig.cs` + `.asset`: `greedStepPreDelay`·`greedStepPostDelay` 신규(0.5s) — 탐욕부 이유 표시→갱신 대기, 갱신→카드 제거 대기
- `GameUIManager`: 단계 재생 시 `preApplyDelay>0`이면 이유 표시 후 대기 뒤 갱신, `postApplyDelay>0`이면 그 시간 사용(아니면 `effectStepDelay`)
- 이유 표시 전체 범위: 윷 기본 배율 → 결과별 부적 → 상성(일반/약점/강점/잡기 강제) → 잡기 → 피해 부적 → 조건 불충족 → 취소 — 감소·유지 단계 포함 전 단계 표시
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기: 탐욕부 0.5s+0.5s 타이밍 / 취소 시 상승 연출 없음 / 정상·스킵 최종 일치

### [2026-10-20 오후 (16차) - GPT 기획 / Devin 구현]

**사용 부적 패널 + 배율 변화 이유 표시** (사용자 승인 — 표시 기능만, 계산 규칙·카드 소모 시점·피해 계산 미변경):

- 신규 `Assets/Scripts/Data/UsedTalismanRecord.cs` — 사용 시점 데이터 복사(useId·cardId·이름·설명·효과·수치·resultNote·displayConsumed). `TalismanManager.usedThisThrow` 보관, `ClearSessionEffects`·`ResetForNewRun`에서 정리
- `TalismanManager`: `ActiveTalismanEffect.useId` 추가, 성공한 사용만 기록(실패 시 기록 없음·미소모). 인연부 획득 결과·재기부·봉쇄부·진격/후퇴 안내문을 `resultNote`로 기록
- `YutThrowSession`: `armed/succeededResultBonus` 튜플에 useId 추가 — 결과별 부적 단계와 사용 기록 연결
- `ScoreManager`: `ScoreEffectStep.useId`·`reasonText` 신규. 상성(실제/잡기 강제)·잡기·부적·결과별 부적 단계에 이유 텍스트 기록. 조건 불충족 피해 카드 `AddTalismanFailStep` + 비피해·미발동 카드 `AddInfoStep`(×1 — Mult 미변경) 추가 — 진격부·후퇴부는 이동 연출 구간에서 처리. 탐욕 취소 시 카드 제거는 취소 단계가 담당
- `GameUIManager`: `SyncUsedTalismanPanel`(동적 항목 생성·소비분 정리·패널 토글)·`RemoveUsedItem`·`SetMultiplierReason`·`ClearUsedTalismanFeedback` 신규. 연출 순서 = 이유 → Mult 갱신 → 간격 → 카드 제거. 이동 단계에서 진격부·후퇴부 안내+제거. 미연결 시 경고 후 표시 생략(게임 진행 유지)
- `YutGameManager` 신규 필드: `multiplierReasonText`·`usedTalismanPanel`·`usedTalismanContent`·`usedTalismanItemPrefab` — 전부 미연결 시 무시. `FinishPresentation`에 `ClearUsedTalismanFeedback` + 플레이어 말 최종 착지 스냅 추가(이동 중 스킵 대응)
- 신규 `Assets/Prefabs/UsedTalismanItem.prefab` — NameText(첫 자식)+DescText(둘째 자식, 선택) 구조, `GetComponentsInChildren<Text>` 순서로 채움
- `Assembly-CSharp.csproj`에 UsedTalismanRecord.cs 등록
- 검증: `dotnet build` 오류 0·경고 0 — 코드 검증 완료. Unity Play 검증 대기: 사용 성공만 표시·다장 표시·순차 제거·이유 텍스트·조건 불충족·스킵 정리·비피해 카드 잔여 없음·사용 카드 없는 투척

### [2026-10-20 오후 (15차) - GPT 기획 / Devin 구현]

**부적 제외 범위 정정** (사용자 승인 정정 — 14차의 '활성 2종'은 범위 전달 오류):

- `TalismanDatabase.asset` 정정: 복구 12종 `includeInDeck: 0→1` — 활성 **14종** = t_fire·t_water·t_forest·t_pursuit·t_greed·t_retry·t_bond·t_seal·t_fortune·t_allinnothing·t_advance·t_retreat·t_yut·t_mo / 제외 유지 **3종** = t_do·t_gae·t_geol만
- 코드 변경 없음 — `includeInDeck` 필터·윷 배율·잡기 규칙·추혼부·이름(윷령부/모령부) 등 14차 변경은 전부 유지
- 기대 동작 (코드 기준): 새 런 덱 14장 각 1장 / 시작 선택 후보 5장 중 3장 복귀 / 땅 선택 덱 충분 시 3택1 / 일도부·이개부·삼걸부는 모든 획득 경로에서 미등장 / 윷령부·모령부의 결과별 ×2 보너스 유지
- 검증: 에셋 값 확인으로 대체 (코드 미변경 — 빌드 불필요). Unity Play 검증 대기: 덱 14장 구성·시작 5택3·제외 3종 미등장·새 런 기준 손패

### [2026-10-20 오후 (14차) - GPT 기획 / Devin 구현]

**윷 기본 배율 변경 + 부적 구성 2종 축소** (사용자 승인 변경):

- `GameBalanceConfig` yutStats: 도4/개3/걸4/윷9/모9 — 클래스 기본값과 `.asset` 모두 갱신 (이동 거리 불변)
- `TalismanData.includeInDeck` 필드 신규 — 카드 정의는 유지하되 덱 구성에서 제외할 수 있게 함
- `TalismanManager.EnsureDeck`: `includeInDeck: false` 카드 필터링 추가 (null 가드 포함). 후보 부족 시 `Mathf.Min` 축소·0장 생략 로직은 기존 그대로
- `TalismanDatabase.asset`: 윷령부(t_yut, 구 사윷부)·모령부(t_mo, 구 오모부) 이름 변경(사용자 확인). cardId·effect enum(0~11)·value 전부 유지. **당시 15종을 `includeInDeck: 0`으로 제외했으나 15차에서 범위 오류로 정정 — 실제 제외는 일도부·이개부·삼걸부 3종**
- 덱 축소 시 동작 (기존 코드 기준): 시작 선택 후보 2장 중 2장 획득 → 덱 소진. 이후 땅 선택·다음 보스 시작 선택은 생략. 사용 카드 소멸·재지급 없음
- 수식 검증 (코드 기준, 50·일반·잡기 없음): 도**200**/개**150**/걸**200**/윷**450**/모**450** / 잡기 성공(상성2·잡기3): 도1200/개900/걸1200/윷2700/모2700 / 결과별 부적 성공 윷·모: 50×18 = **900**
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 새 런에서 윷령부·모령부만 등장, 후보 2/1/0장 선택창 비정지, 다음 투척 보너스 미잔류

### [2026-10-20 오전 (13차) - GPT 기획 / Devin 구현]

**잡기 보상 ×3 + 잡기 시 유효 상성 ×2 고정 + 추혼부 추가 배율** (사용자 승인 변경):

- `GameBalanceConfig`: `catchMultiplier = 3`(잡기 배율)·`catchAffinityMult = 2`(잡기 시 강제 상성) 신규 — 클래스 기본값과 `.asset` 모두 반영
- `ScoreManager`: 정산 순서 재구성 — 착지 확정 → **잡기 판정** → 실제 상성 조회(`actualAffinityMult`) → 유효 상성 결정(잡기면 2 고정) → 잡기 배율(잡기면 3) → 결과별 부적 → 상성·잡기 단계 기록(강제 시 '잡기 상성 ×2' 표시) → 부적 곱셈. `pursuitMult` 잡기 배율 대체 로직 제거 — `CatchPursuit`는 `caught` 조건의 일반 곱셈 카드로 처리(×4, 잡기 성공 시에만). `ThrowSettlement.actualAffinityMult` 추가
- 잡기 연출 단계 `beforeValueF`에 `resultBonusMult` 누락 수정 — 윷×결과부적×상성까지 반영 후 잡기 배율 표시
- `TalismanDatabase.asset` 추혼부: value 6→4, 설명 '잡기 배율 ×6 변경' → '잡기 성공 시 최종 피해 추가 ×4'. `TalismanData` 주석 갱신
- 수식 검증 (코드 기준, 50·개): 잡기 성공 = 50×3×2×3 = **900** (원래 상성 무관) / +추혼 = **3,600** / +이개부 성공 = **7,200** / 잡기 실패 추혼부 사용 = 무효·조건 불충족 / 탐욕+잡기 = **0** / 약점+천운+추혼+잡기 = **14,400**
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — test2 씬에서 조합별 피해·'잡기 상성 ×2' 표시·스킵 일치 확인 필요

### [2026-10-19 오후 (12차) - GPT 기획 / Devin 구현]

**부적 17종 확장 + 밸런스 상향** (사용자 승인 변경):

- 1단계 — 기존 10종 상향: `TalismanDatabase.asset`의 value·description 갱신 (속성 ×4 / 추혼 ×6 / 탐욕 ×2 / 천운 ×4 / 승부 ×2). cardId·enum 0~9 유지
- 2단계 — 재기부 특별 재투척: `YutThrowSession` 재작성 — `bonusRerolls`→`specialRerolls`, `speciallyRerolled[]` 추가. `CanReroll`: 미재투척 윷은 기본 기회(서로 다른 2개·윷당 1회) 우선, 이미 재투척·기본 소진이면 특별 기회(윷당 1회) 소비. `RemainingRerolls` = 기본 잔여 + 특별 합계. 승부부 포기 시 둘 다 차단(기존 forfeited 가드 유지)
- 3단계 — 진격부·후퇴부: `TalismanEffectKind.MoveDelta`(10) + 세션 `moveDelta` 합산. `SettleNormalThrow`에서 `Max(0, MoveSteps + moveDelta)` — 후퇴 도 → 0칸 `MovePiece(0)`으로 현재 칸 정상 정산(경로 비움·판정 유지). 합산 상쇄 가능
- 4단계 — 일도부~오모부: `TalismanEffectKind.ResultBonus`(11) + `TalismanData.targetResult`. 세션 `armedResultBonus`/`succeededResultBonus` (Dictionary<YutResult,(mult,name)> — 카드별 독립). `Reroll` 성공 시 `EvaluateArmedBonuses` — 목표 일치 성공 기록 후 전부 해제(1회 평가). 정산에서 `succeeded[확정결과]`면 `settlement.resultBonusMult` ×2 — `AddResultBonusStep` 기록, `runningMult`·공식·로그·`FinishPresentation` 실효 배율에 포함
- 에셋: 신규 7종 추가 — t_advance/t_retreat(MoveDelta ±1), t_do~t_mo(ResultBonus, targetResult 1~5, value 2). 덱 17종, 손패 10 유지
- 수식 검증 (코드 기준): 걸+승부 = 50×4×2=400 / 개+약점+속성+천운 = 50×3×2×4×4=4800 / +추혼 잡기 = 4800×6=28800 / 도 보너스 성공 = 배율 2→4
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 특별 재투척 소비 순서, 후퇴 0칸 정산, 결과별 부적 성공/실패 분기 확인 필요

### [2026-10-19 오후 (11차) - GPT 기획 / Devin 구현]

**게임 설정 데이터 ScriptableObject 분리 + test2.unity 구 부적 수치 이전** (사용자 승인 변경 — 규칙 변경 없이 데이터 저장 위치만 이동):

- **원인**: `test2.unity`의 Gamemanager에 구 `talismanPool`(value=50/100/75 — 밸런스 변경 전 수치)이 직렬화돼 있어 실제 덱이 구 수치로 구성됐음. 10차의 "씬 미직렬화" 판단은 `test2`를 포함하지 않아 부정확했음 — 정정
- 신규 `Assets/Scripts/Data/`: `TalismanDatabase.cs`, `BossDatabase.cs`, `GameBalanceConfig.cs`, `PresentationConfig.cs` (각 `CreateAssetMenu` — RYY 메뉴)
- 신규 에셋 `Assets/ScriptableObjects/`: `TalismanDatabase.asset`(부적 10종 최신 수치: 배율 2.5/4/1.5/1/2/0/3/1.5), `BossDatabase.asset`(5종 1000~5000, 낙+2%p), `GameBalanceConfig.asset`(maxThrows 5, 윷 배율 2/3/4/5/6, 이동 단계 4-7/3-6/2-5), `PresentationConfig.asset`(0.15/0.35)
- `YutGameManager`: 에셋 참조 4개 직렬화 필드 추가(`talismanDatabase`/`bossDatabase`/`balanceConfig`/`presentationConfig`). `talismanPool`/`bosses`/`maxThrows`/`baseYutStats`/`bossMoveStageMin/Max`/`moveStepDelay`/`effectStepDelay`를 읽기 프로퍼티로 전환 — 씬의 구 직렬화 키는 고아가 돼 Unity 저장 시 자동 제거(수동 삭제 불필요). `ConfigReady` + `ValidateDataAssets`(Awake) — 미연결·빈 목록·배열 길이 불일치·maxThrows≤0 경고. `StartGame`/`EnterBossBattle`에 ConfigReady 가드 — 에셋 누락 시 진입 차단(조용한 대체 없음)
- 탐욕 취소 표시 수정: `FinishPresentation`에서 `talismanCancelled`이면 실효 배율 0 표시 (기존은 양수 mult 잔존)
- `YutRollManager.BuildYutStats`/`TalismanManager.EnsureDeck`에 에셋 null 가드
- 씬 3개(`test2`/`Test`/`SampleScene`) Gamemanager에 에셋 참조 직렬화 연결 — UI 참조·OnClick 변경 없음
- 유지된 직렬화 설정: `backDoPenalty`, `baseProbabilities` (레거시 — 미사용, 데이터화 대상 아님), `bellPool`/`ownedBells`(방울 호환), `selectedBossIndex`, `goalScore`/`currentThrows` 등 실행 상태 — 실행 상태와 설정 구분은 헤더·주석으로 표기
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 에셋 값 변경 반영 확인(Play 종료 → 편집·저장 → 새 게임), 에셋 누락 시 진입 차단, 기존 게임 진행 동일성 확인 필요

### [2026-10-19 오후 (10차) - GPT 기획 / Devin 구현]

**부적 10종 밸런스 변경** (사용자 승인 변경):

- `TalismanData`: `value` 의미 재정의 주석 — 피해 부적=최종 피해 배율(2.5/1.5/3), 추혼부=잡기 배율 대체값(4), 재기부=횟수(1), 인연부=장수(2), 봉쇄부=미사용
- `YutGameManager`: `talismanPool` 10종 수치·설명 신규 규칙으로 갱신 — 씬에 미직렬화라 이전 불필요. `pendingMoveReduction` 참조를 `pendingMoveSeal`로 교체
- `TalismanManager`: `ActiveTalismanEffect.valuePercent` → `mult` (최종 피해 배율). 인연부 = `card.value` 장수만큼 덱 순서대로 획득 — 사용 카드 먼저 제거 후 `AcquireCard`, 덱이 장수보다 적으면 발동 불가·미소모(덱 1장 경계는 미확정). 봉쇄부 = `bossManager.pendingMoveSeal = true`
- `BossManager`: `pendingMoveReduction` 폐기 → `pendingMoveSeal` — 다음 `MoveBossPiece`에서 이동 생략 + `lastBossMoveSteps = 0` 기록 후 소모
- `ScoreManager`: 잡기 배율 = 추혼부 사용 시 `fx.mult`(4)로 대체(부적 곱셈 루프에서 CatchPursuit 제외로 중복 방지), 일반 잡기 ×2, 실패 ×1. 부적 루프는 `fx.mult` 직접 곱셈 — 순서 무관 동일 결과. 탐욕 취소를 `settlement.talismanCancelled`에 보관(연출·스킵 후 일치). 로그 `잡기{실제 배율}` 표시. 부적 step 표시명 `×{mult}`
- 수식 검증 (코드 기준, 개 배율 3·칸 50): 약점 천운부 = 50×3×2×3=900, 약점+속성부적+천운부+추혼부 잡기 = 50×3×2×4×2.5×3=9000, 탐욕 잡기 실패 = 225, 탐욕 잡기 성공 = 0
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 조합별 피해·봉쇄 1회 금지·인연부 2장 획득 확인 필요

### [2026-10-19 오후 (9차 후속) - GPT 기획 / Devin 구현]

**손패 카드 설명 표시** (사용자 승인 변경):

- `YutGameManager`: `talismanHandDescTexts[10]` 추가 — 이름 슬롯과 같은 순서로 손패 카드 `description` 표시, 빈 슬롯 빈칸, 미연결 null-safe. `talismanHandNameTexts[10]` 유지
- `GameUIManager`: 손패 루프에서 이름과 같은 인덱스로 설명 갱신 — 사용·획득으로 순서가 바뀌면 함께 갱신됨
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기

### [2026-10-19 오후 (9차) - GPT 기획 / Devin 구현]

**부적 손패 한도 10 + 초과 소멸** (사용자 승인 변경):

- `TalismanManager`: `HandLimit = 10` 상수, `AcquireCard` 공통 획득 경로(여유→보유 / 초과→런 소멸, `pendingOverflowNotice` 누적). `ConfirmChoice`를 **선택 순서(selectedIndices 추가 순서)대로 획득**으로 수정 — 기존은 후보 인덱스 순서였음. 취소·재선택 시 재선택 시점이 순서가 됨(기존 토글 구조가 자연히 충족)
- 인연부: 덱 검사 → 사용 카드 `hand.RemoveAt` → `AcquireCard` — 손패 10장에서도 정상 교환(9→10), 덱 소진 시 미소모. 새로 뽑은 카드만 소멸 대상
- `YutGameManager`: `talismanHandNameTexts`/`talismanUseButtons` 8→10, `talismanNoticeText` 추가, `UseTalisman9`/`UseTalisman10` 래퍼
- `GameUIManager`: 손패 루프 `HandLimit` 기준, 소멸 안내 "손패가 가득 차 부적 N장이 소멸했습니다." 표시(다음 선택창에서 해제)
- 씬 확인: `Test.unity`·`SampleScene`에 신규 부적 배열 미직렬화 — 스크립트 기본값(10) 적용으로 씬 수정 불필요
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 손패 9+3 획득, 10장 상태 인연부, 소멸 안내 확인 필요

### [2026-10-19 오후 (8차) - GPT 기획 / Devin 구현]

**소모형 부적 시스템 + 초기 10종** (사용자 승인 변경 — 핵심 4가지 확정: 시작 선택=매 보스전 / 땅=방울 대체 부적 / 피해 50%=×1.5 증가 / 덱=10종 각 1장·사용 소멸):

- `TalismanData.cs` 신설 (Data): `TalismanRarity`(범품/진품/영물/신물/미정 — 배정·추첨 확률 미확정), `TalismanEffectKind` 10종, `TalismanData`(cardId·이름·설명·레어도·효과·수치·속성 — Inspector 편집, 문자열 비교 금지)
- `TalismanManager.cs` 신설 (Core): `drawPile`(런 시작 시 10종 셔플, 사용 시 소멸)·`hand`·`candidates`/`selectedIndices`/`requiredPicks`/`isChoosing` — `OpenStartPick`(5택3, `StartBossBattle` 끝)·`OpenLandPick`(3택1, 땅 착지 시 방울 대체). `CanUseNow`(Playing+세션 활성+비선택·비연출) / `UseTalisman`(발동 실패 시 미소모) / `sessionEffects`('이번 투척' 피해 효과, `ClearSessionEffects`로 다음 세션 해제)
- `YutThrowSession`: `bonusRerolls`(재기부 세션 한도) + `rerollsForfeited`(승부부 포기) — `MaxRerollCount` = 2+보너스, `CanReroll`은 포기 상태에서 차단
- `BossManager`: `pendingMoveReduction` — 봉쇄부 감소분을 다음 `MoveBossPiece` 1회에 적용 후 소모(`Max(0, steps-감소)`), 이미 이동한 건 되돌리지 않음
- `ScoreManager`: 정산 4단계 = 세션 부적 효과 순차 곱셈(상성→잡기→부적) — 속성 조건부(화염·수룡·초목)/잡기 조건부(추혼)/약점 조건부(천운)/무조건(탐욕·승부). `AddTalismanStep`(적용 카드별 `EffectSourceType.Talisman` 기록, sourceId=cardId) + `AddTalismanCancelStep`(탐욕 충돌 시 공격 0 — 모든 증가 효과보다 우선). `ThrowSettlement.talismanMult` 추가, `FinishPresentation` 실효 배율에 포함
- `YutGameManager`: `talismanPool`(10종 정의 기본값) + 부적 UI 필드 8종. 가드에 `talismanManager.isChoosing` 추가(RollYut/RerollStick/ConfirmThrow — 선택 완료 전 투척 차단). `FinishPresentation` 땅 착지 → `OpenLandPick()`(방울 호출 제거, `BellManager`·방울 필드는 호환용 유지). `BeginBossFight` 세션 효과 해제+미소비 봉쇄 소멸, `ResetRunState` 덱 초기화
- `GameUIManager`: 후보 이름/설명/레어도 5슬롯 표시, 선택 진행 `n/3` 안내, 확정 버튼 interactable, 선택창 패널 자동 토글, 손패 이름 8슬롯 + 사용 버튼 8개(사용 가능 구간만 interactable)
- 버튼 진입점: `SelectTalismanOption(int)`+래퍼 1~5, `ConfirmTalismanChoice`, `UseTalisman(int)`+래퍼 1~8
- 미확정 임시 규칙(GAME_RULES §3 표 참조): 미선택 후보 덱 복귀, 손패 무제한·다장 사용, 탐욕 충돌=잡기, 봉쇄 1회 −2·합산·최소 0칸, 승부부 재투척 0이면 불가, 인연부 덱 소진 미소모, 손패 보스 전환 유지
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 선택창 패널·후보 텍스트·손패 UI 연결 필요

### [2026-10-19 오전 (7차) - GPT 기획 / Devin 구현]

**보스 말 + 잡기 ×2 + 누적 피해별 이동 단계** (사용자 승인 변경):

- `YutGameManager`: `bossPiece`(RectTransform), `bossMoveStageMin[3]={4,3,2}`/`bossMoveStageMax[3]={7,6,5}` 필드. `RollYut()` 가드 통과 후 `bossManager.MoveBossPiece()` 1회 호출. `ResetRunState`에 `ResetBossPiece()` 추가
- `BossManager`: `bossPieceIndex`/`lastBossMoveSteps` — 플레이어 말과 독립, `%30` 순환. `BossMoveStage` — `totalScore×N ≥ i×goalScore` 정수 비교로 단계 판정(displayTotal 아닌 실제 누적). `DrawBossMoveSteps` — 단계 배열에서 최소~최대 정수 균등(상한 포함), min>max·min<1 보정 경고. `ResetBossPiece`(새 런 초기화)
- `BoardManager`: `MoveBossPieceVisual` — 즉시 위치 변경(연출 없음), `bossPiece` 미연결 시 1회 경고 후 생략
- 잡기: `SettleNormalThrow`에서 `landing == bossPieceIndex` 판정(칸 인덱스, 통과 미발동) → `settlement.caught`/`catchMult`(×2/×1) + 잡기 `ScoreEffectStep` 기록. 최종식 `RoundToInt(칸 점수 × 상성 × 윷 배율 × 잡기)` — 곱셈 완료 후 정수화. `FinishPresentation`의 `multiplier` 실효 배율에 잡기 포함. `ScoreEffectStep.beforeValueF` 추가
- 미확정 임시 구현: 보스 말 START 시작·플레이어 방향·보스 전환 시 위치 유지 — 확정 대기로 문서에 명시
- 후속 확정: **모든 보스전 시작 시 보스 말 START 초기화** — `BeginBossFight`에서 `ResetBossPiece()` 호출(논리 `bossPieceIndex=0` + 화면 위치 함께). 소개·건너뛰기에서는 호출 없음 → 보스 전환 위치는 '매 보스전 초기화'로 확정, 남은 미확정은 이동 방향뿐
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — `bossPiece` 오브젝트 생성·연결 필요

### [2026-10-18 오전 (6차) - GPT 기획 / Devin 구현]

**착지 정보 표시 + 보스 상성 + 윷 4개 재투척 세션** (사용자 승인 변경):

- `YutThrowSession.cs` 신설 (Core, POJO): 윷 4개 `frontUp[4]`/`rerolled[4]`/`rerollUsed`/`confirmed`. 앞면 개수 → 결과(0=모 1=도 2=개 3=걸 4=윷), 재투척 = 서로 다른 윷 최대 2개·같은 윷 1회. `DrawYut` 가중치 추첨은 신규 투척 경로에서 미사용 (API 유지)
- `YutRollManager`: `session` 필드 + `StartSession()` 소유
- `YutGameManager`: `RollYut()`=최초 투척(진행 중 세션 덮어쓰기 차단), `RerollStick(int)`+래퍼1~4, `ConfirmThrow()`=확정→`SettleThrow(result)`→정산 연출 (투척 1회 소모는 확정에서만). 신규 필드 `stickFaceTexts[4]`/`rerollButtons[4]`/`throwStatusText`/`rerollRemainText`/`confirmThrowButton`/`landingTileText`/`bossAffinityText`. `rollButton` 필드·자동 표시 제거. `multiplier` int→float. `ApplyBossAbility` 호출 제거(낙 능력 미적용 안내 로그). 스페이스바: 세션 중=확정, 없음=최초 투척
- `BossManager`: `BossAffinity`(normal/weak/strong) 런타임 데이터 — `runAffinities` 딕셔너리에 보스별 1회 생성·저장(`EnsureAffinity`, `ShowBossSelect`에서 첫 소개 시 생성). `AffinityMultiplier`(약점×2/강점×0.5/기본×1, 비공격 속성은 ×1), `BuildAffinityText`, `ResetAffinities`(새 런)
- `ScoreManager`: `최종 데미지 = 칸 공격점수 × 상성 배율 × 윷 배율` — float 곱셈 후 `RoundToInt`(×0.5 정수화 0 방지). 공격 속성 칸 착지 시 상성 단계를 `steps`에 기록(신규 `EffectSourceType.Boss` — enum 끝에 추가해 기존 값 유지, `opValueF`/`afterValueF` 소수 기록 필드 추가). `ThrowSettlement.affinityMult` 추가
- `GameUIManager`: 세션 UI(면 `앞/뒤`만 표시 — 재투척 여부 미표기, 제한은 `rerolled[]`가 판별, 재투척 버튼 `interactable`, 상태창 `현재 결과: 개 / 재투척 가능: 2`→`확정 결과`, 남은 재투척, 확정 버튼 `interactable`), `bossAffinityText` 표시(기본/약점/강점 3행), `ShowLandingInfo`(착지 후 `불 - 50점`, 스킵 경로에서도 호출). `rollButton` 표시 로직 제거
- `BoardManager`: `숲` 표시명 → `풀` (enum·값·순서 유지 — `TileAttribute.Forest` 그대로)
- 씬: `rollButton` 직렬화 키 제거(SampleScene/Test). `Assembly-CSharp.csproj`에 신규 파일 등록(유니티 재생성 전 임시). **사용자가 Test.unity에 보스 소개 패널·30칸 전체·텍스트 대부분 연결 완료 확인** — 신규 세션 UI 필드는 미연결
- **보고(미확정)**: 백도·낙은 면 조합에서 발생 불가 — `HandleCurseResult`·`IsCurse`는 API로 유지하되 도달 불가. 보스 낙 +2%p 능력은 발동하지 않음(표시는 데이터 그대로 — 임의 대체 안 함)
- 후속 추가: `bossSelectAffinityText` 필드 — `FillBossSelectInfo`에서 `pendingBossIndex` 상성 표시 (`BuildAffinityText`가 `EnsureAffinity`의 저장값 재사용 — 재추첨 없음). 건너뛰기→`ShowBossSelect` 재호출로 자동 갱신. 미연결 null-safe
- 후속 수정: `stickFaceTexts`의 `(재)` 접미사 제거 — `앞`/`뒤`만 표시 (재투척 제한·버튼 활성화 로직은 `rerolled[]` 기준으로 유지)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — 재투척 버튼 4개·확정 버튼·세션 텍스트·소개 화면 상성 텍스트 연결 필요

### [2026-10-18 오전 - GPT 기획 / Devin 구현]

**YutGameManager 책임별 분리 리팩터링 + 1단계 안정화** — 게임 규칙 변경 없음. 직렬화 필드는 전부 `YutGameManager`에 유지하고 로직만 일반 C# 매니저로 분리 (Inspector·OnClick 연결 영향 없음):

- 신규 매니저 (모두 일반 C# 클래스, `Awake`에서 생성): `YutRollManager`(추첨·확률·윷 스탯), `BoardManager`(말판 배치·이동 연출·말판 UI), `ScoreManager`(점수 정산·피버·저주·효과 기록), `BossManager`(보스 선택·소개 정보·관문 조건), `BellManager`(방울 후보·선택·보유·초기화), `GameUIManager`(텍스트 갱신·패널 토글·정산 연출 코루틴 — `Scripts/UI/`)
- `YutGameManager`는 상태 전환·실행 순서·투척→정산 흐름·관문 진입·OnClick 진입점만 유지. 기존 public 메서드 전부 보존(내부는 매니저 위임)
- 1단계 수정: `BuildYutStats()`가 기본 `YutStatEntry`를 참조 공유하던 문제 수정 → 새 객체로 복사 (런타임 강화가 Inspector 기본 스탯을 오염시키지 않음)
- 씬 `maxThrows` 3 → 5 (확정 규칙 5회와 일치 — 이전엔 테스트값 3으로 저장돼 있어 보고 대상이었음)
- 보스 소개 패널 미연결 폴백: `bossSelectPanel == null`이면 소개를 건너뛰고 바로 `EnterBossBattle()` — 패널 미생성 상태에서도 플레이 진입 가능, 패널 연결 시 자동으로 소개 화면 사용
- 씬 연결 검증 결과: 연결됨 = 텍스트 12종(mainResult~feverText, bossName/Ability, 결과 텍스트 5종), 패널 6종(start/game/targetSelectOverlay/targetSelectPanel/clear/gameOver/board), boardTiles 18개 + playerPiece, rollButton. **미연결 = `bossSelectPanel` + 소개 텍스트 3종(오브젝트 자체가 없음), `roundProgressText`, `lapCountText`, `clearMessageText`, `nextBossButton`, bellChoicePanel·옵션 텍스트·슬롯(bellEventTileCount=0이라 현재 무관)**
- OnClick 검증: `SelectTarget*` 5종·`RollYut`·`StartGame`·`BackToBossSelect`×2 연결 확인 — 리팩터링 후에도 호환. `EnterBossBattle`/`GoToNextBoss`/`RetryBoss`/`SelectBoss*`를 호출하는 버튼은 씬에 아직 없음 → 다음 관문 수동 진행·보스 선택은 UI 연결 후 사용 가능 (폴백으로 플레이 진입은 가능)
- 검증: `dotnet build Assembly-CSharp.csproj` 오류 0·경고 0. Devin 에디터 미실행 — Play 테스트는 사용자 확인 필요

### [2026-10-18 오전 (5차) - GPT 기획 / Devin 구현]

**30칸 속성 말판 전환 + 공격 공식 변경** (사용자 승인 변경):

- `YutBoard.cs` 재작성: `TileAttribute` enum(Start/Fire/Water/Forest/Earth/Metal) 추가, `TileCount` 18→30, `BoardTile`을 속성 기반으로 변경 — `baseScore`/`bonusScore`/`hasBellEvent`/`bellUsedThisRound` 삭제, `AttackScore` 속성(불·물·숲 50 / 나머지 0). `layoutGenerated` 플래그 추가
- `BoardManager`: 1~29칸 독립 추첨(불25/물25/숲25/땅15/쇠10%)로 런당 1회 생성(`SetupBoard`가 `layoutGenerated` 확인 — 보스 전환·건너뛰기에서 재추첨 없음), 말 위치·완주는 보스전마다 START로 초기화(기존 동작). `UpdateBoardUI`는 속성명(START/불/물/숲/땅/쇠) 표시 + 'BellIcon' 자식 = 땅 칸 표시
- `ScoreManager`: 공격 데미지 = `도착 칸 공격점수 × 윷 배율` — 완주×10·윷 기본점수·`basicBonus` 가산 제거. `ThrowSettlement.lapBonus` 필드 삭제. `basicBonus`/`UpgradeYutStat` API는 예약 필드로 유지(공격 미적용 — 보고)
- `BellManager`: 땅 칸 착지 → 방울 뽑기 화면(기존 3택1 재사용). 별도 방울 칸 무작위 지정·칸별 발동 제한 폐기. `bellChoicePanel` 미연결 시 `isChoosingBell`로 멈추지 않도록 경고 후 건너뜀
- `YutGameManager`: `bellEventTileCount` 필드 삭제, `boardTiles` 18→30, 땅 착지 발동 단순화
- 씬: `boardTiles` 배열 30칸으로 확장(기존 연결 유지 + 빈 슬롯 12개), `bellEventTileCount`·`lapBonus` 직렬화 키 제거 (두 씬 모두)
- **보고(미확정)**: 마지막(5회째) 투척이 땅에 착지해도 투척 소진 판정이 먼저라 방울 화면 없이 게임오버 — 우선순위 결정 필요
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기 — `boardTiles` 빈 슬롯 12개에 칸 오브젝트 추가·연결 필요

### [2026-10-18 오전 (4차) - GPT 기획 / Devin 구현]

**보스 선택 → 순차 소개 + 건너뛰기 전환** (사용자 승인 변경):

- 5명 직접 선택 UI 폐지: `SelectBoss(int)`/`SelectBoss1~5` 제거 — 두 씬·코드에 연결된 OnClick 없음 확인 후 삭제 (끊어진 이벤트 없음)
- 소개 화면은 진행 순서의 보스 한 명만 표시: '다음' = `EnterBossBattle` 재사용, '건너뛰기' = 신규 `SkipBoss()`(BossManager 판별)
- `SkipBoss` 규칙: `bossSelectPanel` 열림 + Start/Cleared 상태 + 다음 보스 존재 시에만 `ShowBossSelect(pendingBossIndex + 1)` — 전투·보상·처치 기록 없음, 연속 사용 시 순서대로 전진, 마지막 보스 불가
- 신규 필드 `skipBossButton` (GameObject, null-safe) — `UpdateUI`에서 소개 화면 열림 + `CanSkipPending`일 때만 표시 → 마지막 보스 소개 시 자동 숨김
- 소개 화면 필수화: `bossSelectPanel` 미연결 시 자동 전투 진입 폴백 폐기 → 경고 후 대기
- 새 런 첫 보스부터: `ResetRunState`에 `selectedBossIndex = 0`/`pendingBossIndex = 0` 추가
- 인덱스 분리 유지: `pendingBossIndex` = 소개 중(건너뛰기가 전진), `selectedBossIndex` = 전투 중(`EnterBossBattle`에서 pending을 복사) — 건너뛰어도 클리어 후 `GoToNextBoss`의 다음 보스 계산은 유지
- 스페이스바 흐름 유지: Start + 소개 패널 열림 → `EnterBossBattle` / Cleared + 패널 → `EnterBossBattle` / 그 외 → `RollYut`
- 씬 변경 없음 (신규 버튼·필드는 사용자가 에디터에서 연결)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기

### [2026-10-18 오전 (3차) - GPT 기획 / Devin 구현]

**라운드·판 구조 폐지 + 미사용 재시작 함수 제거** (사용자 승인 변경):

- 새 진행 규칙: 보스전 = 기본 투척 5회의 단일 구간 — 중간에 다음 판 진행·투척 재충전 없음. `FinishPresentation` 판정 순서: `totalScore >= goalScore` → `EnterClear`(마지막 투척 처치 포함, 클리어 우선) → `currentThrows >= maxThrows` → `EnterGameOver` → 방울 이벤트. 스킵(`FinishPresentation` 경유)도 동일 판정
- 제거: `roundIndex`/`roundsPerBoss`/`roundScore` 필드와 `roundProgressText`, `AdvanceToNextRound()`, `BellManager.ResetForNextRound()`(판별 임시 방울 소멸·방울 칸 판당 리셋). 방울 칸 `bellUsedThisRound`는 보스전당 1회 발동 플래그로 의미 조정(`SetupBoard` 칸 재생성 시 리셋), `ownedBells`는 보스전 한정 임시 방울이 됨
- 백도 차감 기준: `min(roundScore, backDoPenalty)` → `min(totalScore, backDoPenalty)`. 라운드 폐지 후 `roundScore`는 `totalScore`와 보스전 내 항상 동치라 삭제해도 수치 동일. 보고: 구 '판 시작 직후 차감 상한'(그 판 누적까지)은 소실 — 차감량 이내에서 보스전 누적 전체가 대상이 됨 (규칙상 차감량·방식은 미변경)
- 함수 제거: `RetryBoss()`(같은 보스 재도전 폐지)·`ResetRound()`(별칭)·`BackToBossSelect()`. `RestartRun()`이 `ResetRunState` + 패널 정리 + `GameState.Start` 복귀를 직접 수행. 씬의 복귀 버튼 OnClick 2개는 `BackToBossSelect` → `RestartRun`으로 재연결 (버튼 오브젝트·타겟 유지, 메서드명만 교체)
- 최종 버튼 역할: `StartGame`/`EnterBossBattle`/`RollYut`/`GoToNextBoss`/`RestartRun`/`SkipPresentation` (+보스 선택 `SelectBoss1~5`, 방울 관련)
- 씬 직렬화 키 정리: `roundsPerBoss`/`roundIndex`/`roundScore` 제거 (`roundProgressText`는 씬에 키가 없었음 — 미연결 상태였음)
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기

### [2026-10-18 오전 (2차) - GPT 기획 / Devin 구현]

**보스 현재 체력 표시 + 타겟·피버 시스템 제거** (사용자 승인 변경):

- `bossCurrentHPText` 신규 필드 — 보스 현재 체력 = `Max(0, goalScore - displayTotal)` (숫자만, null-safe). 별도 HP 상태 필드 없이 계산값만 표시. `goalScoreText`=최대 체력, `totalScoreText`=누적 피해 유지. 연출 중엔 `displayTotal` 기준이라 '토탈 합산' 단계에서 총점과 동시 갱신 → 정상 연출과 스킵의 최종 체력 동일. 새 보스전 시작 시 최대 체력, 처치 시 0. 클리어 판정 `totalScore >= goalScore` 유지
- 타겟·피버 제거: `GameState.TargetSelection` 삭제 — 보스전 진입·다음 판 시작 시 바로 `Playing` (`EnterTargetSelection` → `EnterPlaying`). `selectedTarget`/`feverCombo`/`feverCap`/`feverText`/`targetYutText`/`targetSelectOverlay`/`targetSelectPanel` 필드와 `SelectTargetAndStart`/`SelectTargetDo~Mo`/`ConfirmTargetSelection` 메서드 제거. `SelectableTargets` → `NormalResults` 개칭(`YutRollManager` 보스 낙 보정의 비례 차감 대상으로 계속 사용). `ThrowSettlement.isTargetHit`/`feverComboAtRoll` 및 피버 `ScoreEffectStep` 제거. 일반·저주 결과 모두 투척 1회 소모
- `ResetRound`/`RetryBoss`는 보스전 재시작 기능으로 유지 (타겟 함수 아님)
- 씬 정리: `SelectTarget*` OnClick 5개 제거 + 삭제된 필드의 직렬화 키 정리. `TargetSelectOverlay`/`TargetSelectPanel` 오브젝트는 비활성 상태로 씬에 남아 있음 (삭제 여부 사용자 결정)
- **보고(미수정)**: 백도 점수 차감 → `totalScore` 감소 → 보스 현재 체력이 회복처럼 표시됨. 규칙 임의 변경 없음
- 검증: `dotnet build` 오류 0·경고 0. Unity Play 검증 대기

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

- 책임별 매니저 분리 (2026-10-18): `YutGameManager`(총괄·OnClick·직렬화 필드) + POJO 매니저 `YutRollManager`/`BoardManager`/`ScoreManager`/`BossManager`/`BellManager`/`GameUIManager` — 역할·데이터 소유권은 `docs/ARCHITECTURE.md`
- `YutResult` enum 7종 (BackDo + Nak 포함, 2026-10-14 오후 확장)
- 인스펙터 편집 확률 리스트 `baseProbabilities` → 런타임 `runtimeWeights` 분리 구조 (테스트값: 백도 6.25 / 도 18.75 / 개 32.5 / 걸 25 / 윷 6.25 / 모 6.25 / 낙 5 %)
- 보스 현재 체력 표시: `bossCurrentHPText` = `Max(0, goalScore - displayTotal)` (2026-10-18)
- ~~단일 타겟 선택·피버 콤보~~ → 폐기 (2026-10-18 오전, 사용자 승인으로 타겟·피버 시스템 전체 제거 — `GameState.TargetSelection` 삭제, 모든 결과 투척 1회 소모)
- 18칸 원형 말판(`YutBoard`/`BoardTile` — `YutBoard.cs` 분리): 10~180 무작위 배치(보스전 시작 시), 윷별 1~5칸 이동, 도착 칸 점수=베이직, 완주 ×10 보너스, 말 이동 연출(`moveStepDelay`)
- 방울 이벤트: 방울 칸 착지 → 3개 제시 → 1개 선택 → 판 한정 `ownedBells` + `bellSlots` 표시 (실제 효과 미구현)
- 투척별 독립 정산: `ThrowScore = BasicScore × Multiplier` → `TotalScore` 1회 합산 (투척 임시값 매 투척 초기화)
- 윷 스탯: 멀티플 도×2~모×6 + `basicBonus`(칸 점수에 가산되는 윷별 강화치, 기본 0) — `baseYutStats` → 런타임 `yutStats` + `UpgradeYutStat()` 영구 강화 API
- 효과 단계 기록·재생: `ScoreEffectStep`/`ThrowSettlement` — 부적·방울·주술·피버 순차 발동 연출 구조 (정산 확정 후 코루틴 재생, 스킵 가능)
- ~~피버 콤보: 타겟 적중 시 `feverCombo++`, 2연속부터 이번 투척 멀티플 ×Min(콤보, 8)~~ → 폐기 (2026-10-18 오전)
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

- **투척 횟수**: ~~코드 `maxThrows = 7` ↔ 확정 규칙 5회~~ → 해결 (2026-10-14 오전, 코드·씬 모두 5로 수정). ~~씬 직렬화값이 테스트용 3으로 되돌아가 있던 것~~ → 해결 (2026-10-18 오전, 씬 3→5)
- **타겟/피버 시스템**: ~~채택 확정 (2026-10-14 오전)~~ → **폐기** (2026-10-18 오전, 사용자 승인으로 코드·규칙에서 전체 제거)
- **점수·배율·목표 점수**: ~~전부 미확정 규칙이나 코드에 임의값으로 구현됨 (도10~모50, 배수+1~+5, 목표 1000)~~ → 투척 정산 구조·윷 스탯 확정 (2026-10-14 오후, GPT 기획). 목표 1000은 여전히 테스트값.
- **피버 적용식**: ~~이식식 산출 차이 — 확정 필요~~ → 무효 (2026-10-18 오전, 피버 시스템 폐기)
- **백도 ↔ 보스 체력 표시**: 백도 차감은 `totalScore` 감소로 이어져 `bossCurrentHPText`상 체력 회복처럼 표시됨 — 규칙 미변경, 보고만 (2026-10-18 오전)
- **`Update()` 입력**: ~~`activeInputHandler: 1` 환경에서 `Input.GetKeyDown` 런타임 예외~~ → 해결 (2026-10-14 오전, `activeInputHandler: 2` Both로 변경)

## 미구현 기능

- 방울 덱빌딩 (덱 12개, 6개 제시 → 3개 선택, 상점 구매/제거) — 전부 미구현
- 부적 시스템 — 미구현
- 판(여러 라운드) 진행 구조 — 미구현
- 중단/점수 확정 시스템 — 미구현
- ~~씬 연동: `YutGameManager` 미배치~~ → 해결 (2026-10-14 오전). `GameManager` 오브젝트에 컴포넌트 존재 확인.
- **사용자 수동 배치 대기**: `bossSelectPanel` + 소개 텍스트 3종 + '진입' 버튼(OnClick→`EnterBossBattle`), '다음 관문' 버튼(→`GoToNextBoss`), `roundProgressText`, `lapCountText` 등 — 상세 목록은 2026-10-18 세션 항목 참조. 미연결 항목은 전부 null-safe (보스 소개 패널은 미연결 시 자동 바로 진입 폴백).

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
| 2026-10-18 오전 | `YutGameManager` 책임별 분리 — POJO 매니저 6개(`YutRollManager`/`BoardManager`/`ScoreManager`/`BossManager`/`BellManager`/`GameUIManager`), 직렬화 필드·public 메서드 전부 유지. `BuildYutStats` 원본 공유 버그 수정, 씬 `maxThrows` 3→5, 보스 소개 패널 미연결 시 바로 진입 폴백 | GPT 기획 / Devin 구현. 규칙 변경 없음, dotnet build 0오류. 매니저 구조는 `docs/ARCHITECTURE.md` |
| 2026-10-18 오전 | 보스 현재 체력 표시(`bossCurrentHPText` = 목표−누적 피해 계산값), 타겟·피버 시스템 전체 제거(타겟 선택 단계 폐지, 모든 결과 투척 1회 소모), 씬 OnClick·직렬화 키 정리 | GPT 기획 / Devin 구현. dotnet build 0오류, Unity Play 검증 대기 |
