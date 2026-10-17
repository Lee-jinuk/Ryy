using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo, Nak }

public enum GameState { Start, TargetSelection, Playing, Cleared, GameOver }

// 게임 총괄 — 게임 시작/종료, 전체 상태 전환, 시스템 간 실행 순서 조정,
// 투척부터 점수 정산까지의 흐름 제어, Unity Button OnClick 진입점 유지
// 실제 로직은 책임별 매니저에 위임한다 (모두 일반 C# 클래스 — Inspector 연결 영향 없음):
//   YutRollManager(윷 추첨·확률·스탯) / BoardManager(말판·말 이동) / ScoreManager(점수·피버·저주)
//   BossManager(보스·관문) / BellManager(방울 이벤트) / GameUIManager(UI 표시·연출)
public class YutGameManager : MonoBehaviour
{
    [Header("UI Texts")]
    public Text mainResultText;    // 중앙 결과: 윷 결과 이름만 표시 (전투 중에만 보임)
    public Text basicScoreText;    // 이번 투척 베이직 (숫자만 출력)
    public Text multScoreText;     // 이번 투척 멀티플 (숫자만 출력)
    public Text totalScoreText;    // 누적 총점 (숫자만 출력)
    public Text goalScoreText;     // 목표 점수 = 보스 최대 HP (숫자만 출력)
    public Text remainText;        // 남은 투척 기회 (숫자만 출력)
    public Text throwDataText;     // 전체 투척 기회 (숫자만 출력)
    public Text targetYutText;     // 선택된 타겟 윷 이름 (선택 전 빈 문자열)
    public Text throwScoreText;    // 이번 투척 확정 점수 (숫자만 출력)
    public Text feverText;         // 피버 상태 (빈칸 / Hit / 피버 / 피버N)
    public Text roundProgressText; // 판 진행도 "현재 판 / 전체 판" (예: 1 / 3)

    [Header("Boss UI")]
    public Text bossNameText;        // 보스 이름
    public Text bossAbilityText;     // 보스 특수능력 설명
    // 보스는 HP를 깎는 적이 아니라 목표 점수 관문 — 체력바/HP 텍스트는 사용하지 않음
    // (잔여 수치는 goalScoreText(목표)와 totalScoreText(누적)로 확인)

    [Header("Panels")]
    public GameObject startPanel;            // '게임 시작' + 보스 선택이 있는 첫 화면
    public GameObject gamePanel;             // 메인 게임 화면 (타겟 선택 모달 뒤에 표시)
    public GameObject targetSelectOverlay;   // 딤 배경 + 뒤쪽 입력 차단 오버레이 (TargetSelectPanel의 부모 권장)
    public GameObject targetSelectPanel;     // 중앙 타겟 선택 팝업 (overlay 자식 — 미연결도 동작)
    public GameObject clearPanel;            // 보스 클리어 패널
    public GameObject gameOverPanel;         // 게임오버 패널
    public GameObject bossSelectPanel;       // 보스 소개/진입 패널 — 최초 선택과 클리어 후 다음 보스 소개에 공용 사용

    [Header("Result Texts (숫자·이름만 출력)")]
    public Text clearBossNameText;       // 클리어한 보스 이름
    public Text clearScoreText;          // 클리어 시 최종 점수
    public Text clearMessageText;        // 클리어 메시지 — 마지막 보스 클리어 시 "최종 클리어" 표시 (선택 사항)
    public Text gameOverBossNameText;    // 도전한 보스 이름
    public Text gameOverScoreText;       // 최종 획득 점수
    public Text gameOverGoalText;        // 목표 점수

    [Header("Boss Select UI — 보스 소개 화면")]
    public Text bossSelectNameText;      // 진입 예정 보스 이름
    public Text bossSelectGoalText;      // 진입 예정 보스 목표 점수 (= 보스 HP, 숫자만 출력)
    public Text bossSelectAbilityText;   // 진입 예정 보스 특수능력 설명

    [Header("Board UI — 말판")]
    public GameObject boardPanel;                    // 원형 말판 부모 (있으면 상태에 맞춰 표시)
    public GameObject[] boardTiles = new GameObject[18];  // 칸 오브젝트 18개 — 자식의 Text가 점수 표시, 'BellIcon' 이름의 자식은 방울 칸 표시
    public RectTransform playerPiece;                // 플레이어 말 — 도착 칸 위치로 이동
    public Text lapCountText;                        // 완주 횟수 (숫자만 출력, 미연결 시 무시)
    public float moveStepDelay = 0.15f;              // 말 이동 1칸당 딜레이(초) — Inspector 조절

    [Header("Bell Event — 방울 이벤트 (프로토타입)")]
    public int bellEventTileCount = 2;   // 보스전당 방울 칸 수 — Inspector 조정 가능
    public List<BellData> bellPool = new List<BellData>  // 방울 후보 풀 (테스트용 더미 — 실제 효과 미구현)
    {
        new BellData { bellName = "테스트 방울 1", description = "효과 미구현 (테스트용)" },
        new BellData { bellName = "테스트 방울 2", description = "효과 미구현 (테스트용)" },
        new BellData { bellName = "테스트 방울 3", description = "효과 미구현 (테스트용)" },
        new BellData { bellName = "테스트 방울 4", description = "효과 미구현 (테스트용)" },
        new BellData { bellName = "테스트 방울 5", description = "효과 미구현 (테스트용)" },
        new BellData { bellName = "테스트 방울 6", description = "효과 미구현 (테스트용)" },
    };
    public GameObject bellChoicePanel;                // 방울 3개 선택창 — 사용자 생성
    public Text[] bellOptionNameTexts = new Text[3];  // 후보 3개 이름 표시
    public Text[] bellOptionDescTexts = new Text[3];  // 후보 3개 설명 표시
    public List<BellData> ownedBells = new List<BellData>();  // 이번 판 획득한 임시 방울 (판 종료 시 소멸)

    [Header("Buttons")]
    public GameObject rollButton;       // 윷 던지기 버튼 오브젝트 — Playing 상태 + 연출/선택 중이 아닐 때만 표시
    public GameObject nextBossButton;   // 클리어 패널의 '다음 관문' 버튼 — 마지막 보스 클리어 시 자동 숨김 (미연결 시 무시)

    [Header("Slots")]
    public GameObject[] bellSlots = new GameObject[3];      // 방울 슬롯 3칸 (방울 선택 결과 표시용)
    public GameObject[] talismanSlots = new GameObject[4];  // 부적 슬롯 4칸

    [Header("Boss — 임시 테스트 데이터 (이름·수치 변경 가능)")]
    public List<BossData> bosses = new List<BossData>
    {
        new BossData { bossName = "첫 번째 저승지기",   maxHP = 1000 },
        new BossData { bossName = "두 번째 저승지기",   maxHP = 2000 },
        new BossData { bossName = "세 번째 저승지기",   maxHP = 3000 },
        new BossData { bossName = "네 번째 저승지기",   maxHP = 4000 },
        new BossData { bossName = "다섯 번째 저승지기", maxHP = 5000 },
    };
    public int selectedBossIndex = 0;   // 보스 선택 화면에서 선택한 보스 (0~4)

    [Header("Round State")]
    public int maxThrows = 5;
    public int currentThrows = 0;
    public int roundsPerBoss = 3;    // 관문(보스전)당 판 수 — 3판 내 목표 미달 시 게임오버
    public int roundIndex = 1;       // 현재 판 (1부터 시작)
    public int feverCombo = 0;
    public int feverCap = 8;
    public int goalScore = 1000;     // 현재 보스 목표 점수 = 보스 최대 HP
    public int roundScore = 0;       // 이번 판 누적 점수 — 백도 차감 범위 (이전 판 확정분 불침범)

    [Header("Score — 이번 투척 표시용 임시값 (투척마다 초기화)")]
    public int basicScore = 0;    // 이번 투척 계산의 베이직 (효과 단계마다 갱신)
    public int multiplier = 0;    // 이번 투척 계산의 멀티플 (효과 단계마다 갱신)
    public int throwScore = 0;    // 이번 투척 확정 점수
    public int totalScore = 0;    // 보스전 전체 누적 총점 (판을 넘어 유지 — 보스 HP와 대응)

    [Header("Boss / Curse (임시 테스트값)")]
    public int backDoPenalty = 30;   // 보스 1~5 백도 차감량 (미확정 수치 — 테스트값)

    [Header("Probabilities — 임시 테스트값 (합계 100% 유지)")]
    // 낙 추가로 개 37.5→32.5 조정분과 낙 5%는 테스트값 (확정 확률 아님)
    public List<YutProbabilityEntry> baseProbabilities = new List<YutProbabilityEntry>
    {
        new YutProbabilityEntry { result = YutResult.BackDo, percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Do,     percent = 18.75f },
        new YutProbabilityEntry { result = YutResult.Gae,    percent = 32.5f },
        new YutProbabilityEntry { result = YutResult.Geol,   percent = 25f },
        new YutProbabilityEntry { result = YutResult.Yut,    percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Mo,     percent = 6.25f },
        new YutProbabilityEntry { result = YutResult.Nak,    percent = 5f },
    };

    [Header("Yut Stats — 윷별 베이직 강화치/멀티플 (테스트값)")]
    // basicBonus는 도착 칸 점수에 더해지는 윷별 강화치 (기본 0 — 칸 점수가 베이직 기준)
    public List<YutStatEntry> baseYutStats = new List<YutStatEntry>
    {
        new YutStatEntry { result = YutResult.Do,   basicBonus = 0, mult = 2 },
        new YutStatEntry { result = YutResult.Gae,  basicBonus = 0, mult = 3 },
        new YutStatEntry { result = YutResult.Geol, basicBonus = 0, mult = 4 },
        new YutStatEntry { result = YutResult.Yut,  basicBonus = 0, mult = 5 },
        new YutStatEntry { result = YutResult.Mo,   basicBonus = 0, mult = 6 },
    };

    [Header("Presentation (임시 테스트값)")]
    public float effectStepDelay = 0.35f;   // 정산 연출 단계 간 딜레이(초)

    // 원형 말판 — 보스전 시작 시 재구축, 같은 보스전 3판 동안 유지
    public YutBoard board = new YutBoard();

    [Header("Target")]
    public YutResult selectedTarget = YutResult.Gae;

    public GameState currentState = GameState.Start;

    // 최근 투척 정산 데이터 — 디버그 인스펙터 노출 + 향후 연출/UI 코드가 읽는다
    public ThrowSettlement currentSettlement;

    // ============ 책임별 매니저 (런타임 생성, 일반 C# 클래스) ============
    internal YutRollManager rollManager;     // 윷 추첨·확률·윷 스탯
    internal BoardManager boardManager;      // 말판·말 이동
    internal ScoreManager scoreManager;      // 점수·피버·저주 정산
    internal BossManager bossManager;        // 보스·관문
    internal BellManager bellManager;        // 방울 이벤트
    internal GameUIManager uiManager;        // UI 표시·연출

    // 표시·연출용 런타임 상태 (매니저들이 접근)
    internal YutResult lastResult;
    internal bool hasRolled;
    internal int displayTotal;          // 화면에 보이는 총점 — 정산 연출의 '토탈 합산' 단계에서만 갱신
    internal bool isPresenting;
    internal Coroutine activePresentation;

    // SelectTargetAndStart(int) 인덱스 ↔ 결과 매핑: 0=도 1=개 2=걸 3=윷 4=모
    // 타겟 선택은 일반 결과 5종만 허용 (백도·낙 등 저주 계열 제외)
    internal static readonly YutResult[] SelectableTargets =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    // 저주(Curse) 계열 판별 — 부적/방울/주술이 이 함수로 저주 결과를 구분한다
    public static bool IsCurse(YutResult result) =>
        result == YutResult.BackDo || result == YutResult.Nak;

    internal static string ToKorean(YutResult result) => result switch
    {
        YutResult.BackDo => "백도",
        YutResult.Do => "도",
        YutResult.Gae => "개",
        YutResult.Geol => "걸",
        YutResult.Yut => "윷",
        YutResult.Mo => "모",
        YutResult.Nak => "낙",
        _ => result.ToString(),
    };

    // ============ 버튼 OnClick 연결용 Public 함수 ============

    // 보스 선택 버튼(int): index 0~4
    public void SelectBoss(int index) => bossManager.SelectBoss(index);

    // 보스 선택 버튼(개별): 인스펙터에서 파라미터 없이 바로 연결 가능
    public void SelectBoss1() => SelectBoss(0);
    public void SelectBoss2() => SelectBoss(1);
    public void SelectBoss3() => SelectBoss(2);
    public void SelectBoss4() => SelectBoss(3);
    public void SelectBoss5() => SelectBoss(4);

    // '게임 시작' 버튼: 보스 소개 화면을 열고, 진입 버튼(EnterBossBattle)에서 실제 시작
    public void StartGame()
    {
        if (currentState != GameState.Start)
        {
            return;
        }
        bossManager.ShowBossSelect(selectedBossIndex);
    }

    // 보스 소개 화면의 '진입' 버튼 — 소개 중인 보스전을 시작하고 타겟 선택으로 진행
    public void EnterBossBattle()
    {
        if (currentState != GameState.Start && currentState != GameState.Cleared)
        {
            return;
        }
        if (bosses == null || bossManager.pendingBossIndex < 0 || bossManager.pendingBossIndex >= bosses.Count)
        {
            Debug.LogWarning($"EnterBossBattle: 잘못된 보스 인덱스 {bossManager.pendingBossIndex}");
            return;
        }
        selectedBossIndex = bossManager.pendingBossIndex;
        GameUIManager.SetPanel(bossSelectPanel, false);
        StartBossBattle();
    }

    // 실제 보스전 시작 (소개 화면 '진입'과 '다시 도전'이 공용 사용)
    private void StartBossBattle()
    {
        BeginBossFight();
        EnterTargetSelection();
    }

    // 클리어·게임오버 화면의 '다시 도전' 버튼 — 소개 없이 같은 보스로 즉시 재시작
    public void RetryBoss()
    {
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        StartBossBattle();
    }

    // 클리어 화면의 '다음 관문' 버튼 — 다음 보스 소개 화면 표시 (전투 진입은 진입 버튼에서)
    public void GoToNextBoss()
    {
        if (currentState != GameState.Cleared || !bossManager.HasNextBoss)
        {
            return;   // 클리어 상태에서만, 다음 보스가 있을 때만
        }
        GameUIManager.SetPanel(clearPanel, false);
        bossManager.ShowBossSelect(selectedBossIndex + 1);
    }

    // 게임오버 화면의 '처음으로' 버튼 — 런 전체 데이터 초기화 후 시작(보스 선택) 화면으로 복귀
    public void RestartRun() => BackToBossSelect();

    // 클리어·게임오버 화면의 '보스 선택으로 돌아가기' 버튼 — 런 데이터를 전부 초기화하고 시작 화면으로 복귀
    public void BackToBossSelect()
    {
        ResetRunState();
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(targetSelectOverlay, false);
        GameUIManager.SetPanel(targetSelectPanel, false);
        GameUIManager.SetPanel(gamePanel, false);
        GameUIManager.SetPanel(startPanel, true);
        currentState = GameState.Start;
        uiManager.UpdateUI();
    }

    // 라운드 리셋 버튼: 현재 보스전을 처음(1판)부터 다시 시작한다
    public void ResetRound() => RetryBoss();

    // 타겟 선택 버튼(int): yutIndex 0=도 1=개 2=걸 3=윷 4=모
    public void SelectTargetAndStart(int yutIndex)
    {
        if (yutIndex < 0 || yutIndex >= SelectableTargets.Length)
        {
            Debug.LogWarning($"SelectTargetAndStart: 잘못된 인덱스 {yutIndex} (0~4)");
            return;
        }
        selectedTarget = SelectableTargets[yutIndex];
        ConfirmTargetSelection();
    }

    // 타겟 선택 버튼(개별): 인스펙터에서 파라미터 없이 바로 연결 가능
    public void SelectTargetDo() => SelectTargetAndStart(0);
    public void SelectTargetGae() => SelectTargetAndStart(1);
    public void SelectTargetGeol() => SelectTargetAndStart(2);
    public void SelectTargetYut() => SelectTargetAndStart(3);
    public void SelectTargetMo() => SelectTargetAndStart(4);

    // 타겟 선택 확정: 타겟 선택 화면을 끄고 본 게임 시작
    public void ConfirmTargetSelection()
    {
        if (currentState != GameState.TargetSelection)
        {
            return;
        }
        GameUIManager.SetPanel(targetSelectOverlay, false);
        GameUIManager.SetPanel(targetSelectPanel, false);
        hasRolled = false;
        currentState = GameState.Playing;
        Debug.Log($"--- 타겟 선택: {ToKorean(selectedTarget)} ---");
        uiManager.UpdateUI();
    }

    // '윷 던지기' 버튼 — 정산은 즉시 확정되고, 화면 연출은 정산 데이터를 그대로 재생한다
    public void RollYut()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("윷을 던질 수 있는 상태가 아닙니다. (GameState.Playing 아님)");
            return;
        }
        if (isPresenting || bellManager.isChoosingBell)
        {
            return;   // 정산 연출 재생 중 또는 방울 선택 중 연타 방지 — 중복 투척/득점 불가
        }
        if (currentThrows >= maxThrows)
        {
            return;
        }

        ThrowSettlement settlement = SettleThrow();
        currentSettlement = settlement;
        lastResult = settlement.result;
        hasRolled = true;

        activePresentation = StartCoroutine(uiManager.PresentSettlement(settlement));
    }

    // 연출 스킵 — 정산 데이터는 이미 확정되어 있으므로 스킵해도 같은 결과가 표시된다
    public void SkipPresentation()
    {
        if (!isPresenting || currentSettlement == null)
        {
            return;
        }
        if (activePresentation != null)
        {
            StopCoroutine(activePresentation);
        }
        FinishPresentation(currentSettlement);
    }

    // 방울 후보 선택 버튼(int): index 0~2 — 선택만 하고 확정은 ConfirmBellChoice()
    public void SelectBellOption(int index) => bellManager.SelectBellOption(index);
    public void SelectBellOption1() => bellManager.SelectBellOption(0);
    public void SelectBellOption2() => bellManager.SelectBellOption(1);
    public void SelectBellOption3() => bellManager.SelectBellOption(2);

    // 방울 선택 확정 버튼 — 선택한 방울을 이번 판 임시 보유에 추가하고 창 닫기
    public void ConfirmBellChoice() => bellManager.ConfirmBellChoice();

    // 원클릭 버전 — 옵션 버튼에 바로 연결하면 선택과 확정을 한 번에 처리
    public void SelectBellAndConfirm(int index) => bellManager.SelectBellAndConfirm(index);
    public void SelectBellAndConfirm1() => bellManager.SelectBellAndConfirm(0);
    public void SelectBellAndConfirm2() => bellManager.SelectBellAndConfirm(1);
    public void SelectBellAndConfirm3() => bellManager.SelectBellAndConfirm(2);

    // 보스 특수능력·방울·주술의 확률 조작용 API — runtimeWeights만 변경되고 baseProbabilities는 오염되지 않음
    public void ModifyProbability(YutResult result, float deltaPercent) =>
        rollManager.ModifyProbability(result, deltaPercent);

    // 런타임 확률 변경을 모두 되돌리고 기본 확률로 재구축 (보스 효과도 함께 제거됨)
    public void ResetProbabilities() => rollManager.ResetProbabilities();

    // 주술 등 영구 강화용 API — 다음 투척부터 적용. baseYutStats는 오염되지 않음
    public void UpgradeYutStat(YutResult result, int newBasicBonus, int newMult) =>
        rollManager.UpgradeYutStat(result, newBasicBonus, newMult);

    // ============ 내부 로직 (게임 흐름 조정) ============

    private void Awake()
    {
        // 매니저 생성 — 참조 보관 순서 (roll → board/score → boss/bell → ui)
        rollManager = new YutRollManager(this);
        boardManager = new BoardManager(this);
        scoreManager = new ScoreManager(this);
        bossManager = new BossManager(this);
        bellManager = new BellManager(this);
        uiManager = new GameUIManager(this);

        currentState = GameState.Start;
        rollManager.BuildWeights();
        rollManager.BuildYutStats();
        if (bossManager.CurrentBoss != null)
        {
            goalScore = bossManager.CurrentBoss.maxHP;   // 선택 화면 미리보기
        }
        WarnIfPanelNested();
        GameUIManager.SetPanel(startPanel, true);
        GameUIManager.SetPanel(gamePanel, false);
        GameUIManager.SetPanel(targetSelectOverlay, false);
        GameUIManager.SetPanel(targetSelectPanel, false);
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
    }

    // 보스전 시작: 선택 보스 적용 + 전투 상태 초기화 + 보스 특수능력 적용
    private void BeginBossFight()
    {
        StopPresentation();
        BossData boss = bossManager.CurrentBoss;
        goalScore = boss != null ? boss.maxHP : 0;
        roundIndex = 1;
        currentThrows = 0;
        roundScore = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;
        feverCombo = 0;
        hasRolled = false;
        currentSettlement = null;
        bellManager.ResetForNewRun();
        boardManager.SetupBoard();                      // 18칸 점수·방울 칸 무작위 배치 + 말 위치 초기화
        boardManager.MovePieceVisual(board.pieceIndex); // 말을 시작점(BoardTile_01) 위치에 배치
        rollManager.BuildWeights();                     // 기본 확률로 재구축 → 보스 효과 중복 누적 방지
        if (boss != null)
        {
            rollManager.ApplyBossAbility(boss);
        }
        Debug.Log($"--- 보스전 시작: {boss?.bossName} | 목표 {goalScore} | {roundsPerBoss}판 x {maxThrows}회 ---");
    }

    // 타겟 선택 화면 진입 — 패널 전환 공통 처리
    private void EnterTargetSelection()
    {
        GameUIManager.SetPanel(gamePanel, true);
        GameUIManager.ActivatePanel(targetSelectOverlay, nameof(targetSelectOverlay));
        GameUIManager.SetPanel(targetSelectPanel, true);
        GameUIManager.SetPanel(startPanel, false);
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        bellManager.isChoosingBell = false;
        currentState = GameState.TargetSelection;
        uiManager.UpdateUI();
    }

    // 다음 판 진행 — 누적 총점은 유지, 판 단위 카운터만 초기화 후 타겟 재선택
    private void AdvanceToNextRound()
    {
        roundIndex++;
        currentThrows = 0;
        roundScore = 0;
        feverCombo = 0;
        hasRolled = false;
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;
        bellManager.ResetForNextRound();   // 임시 방울 소멸 + 방울 칸 판당 발동 제한 리셋
        Debug.Log($"--- {roundIndex}판 시작 | 누적 총점 {totalScore} / 목표 {goalScore} ---");
        EnterTargetSelection();
    }

    // 보스 클리어: TotalScore >= GoalScore
    private void EnterClear()
    {
        currentState = GameState.Cleared;
        bellManager.isChoosingBell = false;
        GameUIManager.SetPanel(gameOverPanel, false);            // 클리어/게임오버 동시 표시 방지
        GameUIManager.SetPanel(targetSelectOverlay, false);
        GameUIManager.SetPanel(targetSelectPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        GameUIManager.ActivatePanel(clearPanel, nameof(clearPanel));
        // 다음 관문 버튼: 마지막 보스 클리어(최종 클리어)면 숨기고 메시지 표시
        GameUIManager.SetPanel(nextBossButton, !bossManager.IsFinalClear);
        if (clearMessageText != null)
        {
            clearMessageText.text = bossManager.IsFinalClear ? "최종 클리어" : "";
        }
        uiManager.UpdateUI();
        BossData boss = bossManager.CurrentBoss;
        Debug.Log($"--- 보스 클리어! {boss?.bossName} | 최종 점수 {totalScore} / 목표 {goalScore}{(bossManager.IsFinalClear ? " | 최종 클리어" : "")} ---");
    }

    // 게임오버: 3판 종료 후에도 목표 미달
    private void EnterGameOver()
    {
        currentState = GameState.GameOver;
        bellManager.isChoosingBell = false;
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(targetSelectOverlay, false);
        GameUIManager.SetPanel(targetSelectPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        GameUIManager.ActivatePanel(gameOverPanel, nameof(gameOverPanel));
        uiManager.UpdateUI();
        BossData boss = bossManager.CurrentBoss;
        Debug.Log($"--- 게임 오버. {boss?.bossName} | 최종 점수 {totalScore} / 목표 {goalScore} ---");
    }

    private void StopPresentation()
    {
        if (activePresentation != null)
        {
            StopCoroutine(activePresentation);
            activePresentation = null;
        }
        isPresenting = false;
    }

    // 투척 1회 정산 흐름: 추첨(rollManager) → 정산 계산(scoreManager) → 정산 데이터 확정
    private ThrowSettlement SettleThrow()
    {
        // 이전 투척의 표시용 임시값만 초기화 (윷별 영구 스탯·누적 총점은 유지)
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;

        YutResult selected = rollManager.DrawYut();
        bool isTarget = selected == selectedTarget;

        ThrowSettlement settlement = new ThrowSettlement
        {
            result = selected,
            isTargetHit = isTarget,
            feverComboAtRoll = feverCombo,
        };

        if (IsCurse(selected))
        {
            // 저주: 점수 계산 없이 고정 처리 (백도 이번 판 점수 페널티 / 낙 무득점)
            scoreManager.HandleCurseResult(selected);
            Debug.Log($"Curse Result: {ToKorean(selected)} | Throws: {currentThrows}/{maxThrows} | Round: {roundScore} | Total: {totalScore}");
            return settlement;
        }

        scoreManager.SettleNormalThrow(settlement, selected, isTarget);
        return settlement;
    }

    // 정산 연출 종료 지점 — GameUIManager.PresentSettlement 마지막과 SkipPresentation에서 호출
    internal void FinishPresentation(ThrowSettlement settlement)
    {
        basicScore = settlement.finalBasic;
        multiplier = settlement.finalMult;
        throwScore = settlement.throwScore;
        displayTotal = totalScore;
        isPresenting = false;
        activePresentation = null;
        uiManager.UpdateUI();

        if (currentState != GameState.Playing)
        {
            return;
        }
        if (totalScore >= goalScore)
        {
            EnterClear();
            return;
        }
        if (currentThrows >= maxThrows)
        {
            if (roundIndex >= roundsPerBoss)
            {
                EnterGameOver();
            }
            else
            {
                AdvanceToNextRound();
            }
            return;
        }
        // 방울 이벤트 칸 착지 — 정산 연출이 끝나고 판이 계속될 때 선택창 표시
        if (settlement.landedOnBellTile && settlement.landingTileIndex >= 0)
        {
            BoardTile tile = board.tiles[settlement.landingTileIndex];
            if (tile.hasBellEvent && !tile.bellUsedThisRound)
            {
                tile.bellUsedThisRound = true;
                boardManager.UpdateBoardUI();
                bellManager.OpenBellChoice();
            }
        }
    }

    // 런 전체 초기화 — 게임오버/보스 선택 복귀로 런이 끝날 때 호출
    // 관문 이동(GoToNextBoss→EnterBossBattle)은 여기를 거치지 않으므로 런 성장 요소가 유지된다
    private void ResetRunState()
    {
        StopPresentation();                  // 이전 보스전의 잔여 연출 정리
        bellManager.ResetForNewRun();        // 방울 선택창·임시 방울·방울 이벤트 상태
        hasRolled = false;                   // 이전 윷 결과 표시용 데이터
        currentSettlement = null;
        scoreManager.lastBackDoLoss = 0;
        feverCombo = 0;
        currentThrows = 0;
        roundIndex = 1;
        roundScore = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;
        boardManager.ResetForNewRun();       // 말판 점수 배치·칸 강화·방울 칸·말 위치·완주 초기화
        // 런 전체 성장 요소 초기화 — 런이 끝났으므로 윷 강화·보스 특수능력 원복
        rollManager.BuildYutStats();
        rollManager.ResetProbabilities();
        goalScore = bossManager.CurrentBoss != null ? bossManager.CurrentBoss.maxHP : 0;   // 선택 화면 미리보기
    }

    // 패널 중첩 경고 — 부모가 꺼지면 함께 숨겨지는 배치를 미리 진단한다
    private void WarnIfPanelNested()
    {
        if (startPanel == null)
        {
            return;
        }
        if (targetSelectOverlay != null && targetSelectOverlay.transform.IsChildOf(startPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] targetSelectOverlay가 startPanel의 자식(또는 같은 오브젝트)입니다. startPanel이 꺼지면 함께 숨겨지니 Canvas 바로 아래로 분리하세요.", targetSelectOverlay);
        }
        if (targetSelectPanel != null && targetSelectPanel.transform.IsChildOf(startPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] targetSelectPanel이 startPanel의 자식(또는 같은 오브젝트)입니다. startPanel이 꺼지면 함께 숨겨지니 Canvas 바로 아래로 분리하세요.", targetSelectPanel);
        }
        if (bossSelectPanel != null && clearPanel != null && bossSelectPanel.transform.IsChildOf(clearPanel.transform))
        {
            Debug.LogWarning("[YutGameManager] bossSelectPanel이 clearPanel의 자식입니다. 다음 관문 클릭 시 clearPanel이 꺼지면 함께 숨겨지니 Canvas 바로 아래로 분리하세요.", bossSelectPanel);
        }
    }

    private void Start()
    {
        uiManager.UpdateUI();
    }

    private void Update()
    {
        // 스페이스바 테스트: 화면 플로우를 순서대로 진행
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.Start)
            {
                // 보스 소개 화면이 열려 있으면 스페이스바가 진입 버튼 역할
                if (bossSelectPanel != null && bossSelectPanel.activeSelf)
                {
                    EnterBossBattle();
                }
                else
                {
                    StartGame();
                }
            }
            else if (currentState == GameState.Cleared && bossSelectPanel != null && bossSelectPanel.activeSelf)
            {
                EnterBossBattle();
            }
            else if (currentState == GameState.TargetSelection)
            {
                ConfirmTargetSelection();
            }
            else
            {
                RollYut();
            }
        }
    }
}
