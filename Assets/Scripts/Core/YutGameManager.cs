using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo, Nak }

public enum GameState { Start, Playing, Cleared, GameOver }

// 게임 총괄 — 게임 시작/종료, 전체 상태 전환, 시스템 간 실행 순서 조정,
// 투척부터 점수 정산까지의 흐름 제어, Unity Button OnClick 진입점 유지
// 실제 로직은 책임별 매니저에 위임한다 (모두 일반 C# 클래스 — Inspector 연결 영향 없음):
//   YutRollManager(윷 추첨·확률·스탯) / BoardManager(말판·말 이동) / ScoreManager(점수·저주)
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
    public Text throwScoreText;    // 이번 투척 확정 점수 (숫자만 출력)

    [Header("Boss UI")]
    public Text bossNameText;        // 보스 이름
    public Text bossAbilityText;     // 보스 특수능력 설명
    public Text bossCurrentHPText;   // 보스 현재 체력 = Max(0, goalScore - 누적 피해) (숫자만 출력)
    public Text bossAffinityText;    // 전투 중인 보스 상성 정보 — "기본: 불 100%\n약점: 물 200%\n강점: 풀 50%" (미연결 시 무시)
    // 보스 체력은 별도 HP 필드를 두지 않고 goalScore - totalScore로 계산한다.
    // totalScoreText = 보스에게 준 누적 피해, goalScoreText = 보스 최대 체력.

    [Header("Panels")]
    public GameObject startPanel;            // '게임 시작' + 보스 선택이 있는 첫 화면
    public GameObject gamePanel;             // 메인 게임 화면
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
    public Text bossSelectAffinityText;  // 소개 중인 보스 상성 정보 — pendingBossIndex 기준, 전투 화면(bossAffinityText = selectedBossIndex)과 같은 런 고정 데이터 (미연결 시 무시)
    public GameObject skipBossButton;    // 소개 화면 '건너뛰기' 버튼 — 마지막 보스 소개 시 자동 숨김 (미연결 시 무시)

    [Header("Board UI — 말판")]
    public GameObject boardPanel;                    // 원형 말판 부모 (있으면 상태에 맞춰 표시)
    public GameObject[] boardTiles = new GameObject[30];  // 칸 오브젝트 30개 — START → 이동 방향 29칸 순서. 자식의 Text가 속성명 표시, 'BellIcon' 이름의 자식은 땅(방울) 칸 표시
    public RectTransform playerPiece;                // 플레이어 말 — 도착 칸 위치로 이동
    public RectTransform bossPiece;                  // 보스 말 — 플레이어와 별개, 최초 투척 성공 시마다 이동 (미연결 시 경고 후 표시만 생략)
    public Text lapCountText;                        // 완주 횟수 (숫자만 출력, 미연결 시 무시)
    public Text landingTileText;                     // 착지 칸 정보 — "불 - 50점" 형식 (상성·윷 배율 적용 전 칸 기본점수, 미연결 시 무시)
    public float moveStepDelay = 0.15f;              // 말 이동 1칸당 딜레이(초) — Inspector 조절

    [Header("Bell Event — 방울 뽑기 (프로토타입, 땅 칸 착지 시 발동)")]
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
    public List<BellData> ownedBells = new List<BellData>();  // 현재 보스전에서 획득한 임시 방울 (다음 보스전 시작·런 종료 시 소멸)

    [Header("Throw Session UI — 윷 4개 재투척")]
    public Text[] stickFaceTexts = new Text[4];      // 각 윷의 앞/뒤 표시 (재투척 여부는 표시하지 않음 — 제한은 rerolled[]가 판별)
    public Button[] rerollButtons = new Button[4];   // 윷별 재투척 버튼 — 재투척 가능할 때만 interactable
    public Text throwStatusText;                     // 투척 상태창 — "현재 결과: 개 / 재투척 가능: 2" 또는 확정 결과
    public Text rerollRemainText;                    // 남은 재투척 수 (숫자만, 미연결 시 무시)
    public Button confirmThrowButton;                // '결과 확정' 버튼 — 세션 진행 중에만 interactable (미연결 시 무시)

    [Header("Buttons")]
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

    [Header("Throw State")]
    public int maxThrows = 5;
    public int currentThrows = 0;
    public int goalScore = 1000;     // 현재 보스 목표 점수 = 보스 최대 HP

    [Header("Score — 이번 투척 표시용 임시값 (투척마다 초기화)")]
    public int basicScore = 0;      // 이번 투척 계산의 베이직 (효과 단계마다 갱신)
    public float multiplier = 0f;   // 이번 투척 계산의 멀티플 — 윷 배율에 보스 상성까지 반영된 실효 배율 표시 (소수 가능)
    public int throwScore = 0;    // 이번 투척 확정 점수
    public int totalScore = 0;    // 보스전 전체 누적 총점 (보스 HP와 대응 — 백도 차감 기준이기도 함)

    [Header("Boss Move — 보스 말 이동 거리 (밸런스 조절값)")]
    // 누적 피해 비율 단계별 이동 범위 — 단계 i는 피해 ≥ 최대 HP의 i/N 에서 적용 (기본 3단계: <1/3 → 4~7 / ≥1/3 → 3~6 / ≥2/3 → 2~5)
    // 배열 길이 = 단계 수. 변경된 범위는 다음 투척 세션의 최초 던지기부터 적용. 새 보스전(피해 0)은 0단계
    public int[] bossMoveStageMin = { 4, 3, 2 };   // 단계별 이동 최소 칸 수
    public int[] bossMoveStageMax = { 7, 6, 5 };   // 단계별 이동 최대 칸 수 (최소~최대 정수 균등 추첨, 상한 포함)

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
    // basicBonus는 현재 공격 공식(칸 공격점수 × 윷 배율)에 적용되지 않는다 — 향후 강화 효과용으로 예약된 필드
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

    // 30칸 원형 속성 말판 — 런당 1회 생성 후 게임오버까지 유지 (보스 전환·건너뛰기에서 재추첨 없음)
    public YutBoard board = new YutBoard();

    public GameState currentState = GameState.Start;

    // 최근 투척 정산 데이터 — 디버그 인스펙터 노출 + 향후 연출/UI 코드가 읽는다
    public ThrowSettlement currentSettlement;

    // ============ 책임별 매니저 (런타임 생성, 일반 C# 클래스) ============
    internal YutRollManager rollManager;     // 윷 추첨·확률·윷 스탯
    internal BoardManager boardManager;      // 말판·말 이동
    internal ScoreManager scoreManager;      // 점수·저주 정산
    internal BossManager bossManager;        // 보스·관문
    internal BellManager bellManager;        // 방울 이벤트
    internal GameUIManager uiManager;        // UI 표시·연출

    // 표시·연출용 런타임 상태 (매니저들이 접근)
    internal YutResult lastResult;
    internal bool hasRolled;
    internal int displayTotal;          // 화면에 보이는 총점 — 정산 연출의 '토탈 합산' 단계에서만 갱신
    internal bool isPresenting;
    internal Coroutine activePresentation;
    internal string landingTileInfo = "";   // 착지 칸 표시 문자열 — 연출이 말 이동 완료 시점에 채운다

    // 일반 결과 5종 — 저주(백도·낙) 제외. 보스 특수능력의 비례 차감 대상 등에 사용
    internal static readonly YutResult[] NormalResults =
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

    // '게임 시작' 버튼: 현재 진행 순서의 보스 소개 화면을 열고, '다음' 버튼(EnterBossBattle)에서 실제 시작
    public void StartGame()
    {
        if (currentState != GameState.Start)
        {
            return;
        }
        bossManager.ShowBossSelect(bossManager.pendingBossIndex);
    }

    // 보스 소개 화면의 '다음' 버튼 — 소개 중인 보스전을 시작하고 바로 투척 가능 상태로 진행
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

    // 보스 소개 화면의 '건너뛰기' 버튼 — 전투 없이 다음 보스 소개로 이동 (판별·제한은 BossManager)
    public void SkipBoss() => bossManager.SkipBoss();

    // 실제 보스전 시작 (소개 화면 '다음' 버튼에서 호출)
    private void StartBossBattle()
    {
        BeginBossFight();
        EnterPlaying();
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

    // 클리어·게임오버 화면의 '처음으로' 버튼 — 런 데이터를 전부 초기화하고 시작 화면으로 복귀
    public void RestartRun()
    {
        ResetRunState();
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(gamePanel, false);
        GameUIManager.SetPanel(startPanel, true);
        currentState = GameState.Start;
        uiManager.UpdateUI();
    }

    // '윷 던지기' 버튼 — 윷 4개 최초 투척으로 새 세션 시작 (이동·득점은 '결과 확정'에서만 발생)
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
        if (rollManager.session != null && rollManager.session.Active)
        {
            return;   // 진행 중 세션 덮어쓰기 차단 — 재투척 선택 또는 '결과 확정' 필요
        }
        rollManager.StartSession();
        bossManager.MoveBossPiece();   // 최초 투척 성공 시 보스 말 1회 이동 (재투척·확정에서는 이동 없음)
        hasRolled = false;   // 이전 확정 결과 표시 지우기 — 새 세션은 확정 전까지 결과 미표시
        uiManager.UpdateUI();
    }

    // 윷별 재투척 버튼(int): index 0~3 — 선택한 윷 하나만 다시 던진다
    public void RerollStick(int index)
    {
        if (currentState != GameState.Playing || isPresenting || bellManager.isChoosingBell)
        {
            return;
        }
        if (rollManager.session != null && rollManager.session.Reroll(index))
        {
            uiManager.UpdateUI();
        }
    }
    public void RerollStick1() => RerollStick(0);
    public void RerollStick2() => RerollStick(1);
    public void RerollStick3() => RerollStick(2);
    public void RerollStick4() => RerollStick(3);

    // '결과 확정' 버튼 — 확정된 결과로 말 이동·공격 정산 시작 (기본 투척 1회 소모는 여기서만)
    public void ConfirmThrow()
    {
        if (currentState != GameState.Playing || isPresenting || bellManager.isChoosingBell)
        {
            return;
        }
        YutThrowSession session = rollManager.session;
        if (session == null || !session.Active)
        {
            return;   // 확정할 세션 없음 또는 이미 확정 — 연타 차단
        }
        session.Confirm();

        ThrowSettlement settlement = SettleThrow(session.CurrentResult);
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

    // 방울 선택 확정 버튼 — 선택한 방울을 보스전 임시 보유에 추가하고 창 닫기
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
        currentThrows = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;
        hasRolled = false;
        currentSettlement = null;
        bellManager.ResetForNewRun();
        rollManager.session = null;                     // 이전 보스전 투척 세션 정리
        landingTileInfo = "";
        boardManager.SetupBoard();                      // 배치는 런당 1회 생성·유지, 말 위치·완주만 초기화
        boardManager.MovePieceVisual(board.pieceIndex); // 말을 시작점(START) 위치에 배치
        rollManager.BuildWeights();                     // 기본 확률로 재구축 → 보스 효과 중복 누적 방지
        // 보스 낙 확률 능력은 신규 윷 4개 면 조합 방식에 적용 불가 — 적용 호출을 하지 않는다 (미확정 사항 보고)
        if (boss != null && boss.nakBonusPercent > 0f)
        {
            Debug.Log($"참고: {boss.bossName}의 '낙 확률 +{boss.nakBonusPercent}%p' 능력은 신규 투척 방식에서 발동하지 않습니다 (미확정)");
        }
        Debug.Log($"--- 보스전 시작: {boss?.bossName} | 목표 {goalScore} | {maxThrows}회 ---");
    }

    // 본 게임 진입 — 보스전 시작 시 패널 전환 처리
    private void EnterPlaying()
    {
        GameUIManager.SetPanel(gamePanel, true);
        GameUIManager.SetPanel(startPanel, false);
        GameUIManager.SetPanel(clearPanel, false);
        GameUIManager.SetPanel(gameOverPanel, false);
        GameUIManager.SetPanel(bossSelectPanel, false);
        GameUIManager.SetPanel(bellChoicePanel, false);
        bellManager.isChoosingBell = false;
        hasRolled = false;
        currentState = GameState.Playing;
        uiManager.UpdateUI();
    }

    // 보스 클리어: TotalScore >= GoalScore
    private void EnterClear()
    {
        currentState = GameState.Cleared;
        bellManager.isChoosingBell = false;
        GameUIManager.SetPanel(gameOverPanel, false);            // 클리어/게임오버 동시 표시 방지
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

    // 게임오버: 기본 투척 소진 후에도 목표 미달
    private void EnterGameOver()
    {
        currentState = GameState.GameOver;
        bellManager.isChoosingBell = false;
        GameUIManager.SetPanel(clearPanel, false);
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

    // 확정 투척 1회 정산: 확정 결과(윷 4개 면 조합) → 정산 계산(scoreManager) → 정산 데이터 확정
    // 세션은 일반 결과 5종만 생성하므로 저주 경로는 여기서 실행되지 않는다 (HandleCurseResult는 API로 유지)
    private ThrowSettlement SettleThrow(YutResult confirmed)
    {
        // 이전 투척의 표시용 임시값만 초기화 (윷별 영구 스탯·누적 총점은 유지)
        basicScore = 0;
        multiplier = 0f;
        throwScore = 0;

        ThrowSettlement settlement = new ThrowSettlement
        {
            result = confirmed,
        };

        scoreManager.SettleNormalThrow(settlement, confirmed);
        return settlement;
    }

    // 정산 연출 종료 지점 — GameUIManager.PresentSettlement 마지막과 SkipPresentation에서 호출
    internal void FinishPresentation(ThrowSettlement settlement)
    {
        basicScore = settlement.finalBasic;
        multiplier = settlement.finalMult * settlement.affinityMult * settlement.catchMult;   // 실효 배율 (윷 × 상성 × 잡기)
        throwScore = settlement.throwScore;
        displayTotal = totalScore;
        isPresenting = false;
        activePresentation = null;
        uiManager.ShowLandingInfo(settlement);   // 스킵 경로 — 이동 연출 없이도 착지 정보 표시
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
            EnterGameOver();   // 5회 소진 후에도 보스 생존 — 마지막 투척 처치는 위 클리어 판정이 먼저 잡는다
            return;
        }
        // 땅 칸 착지 — 정산 연출이 끝나고 보스전이 계속될 때 방울 뽑기 선택창 표시
        if (settlement.landedOnBellTile)
        {
            bellManager.OpenBellChoice();
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
        rollManager.session = null;          // 진행 중이던 투척 세션 폐기
        landingTileInfo = "";
        selectedBossIndex = 0;                 // 새 런은 첫 번째 보스부터 — 전투·소개 인덱스 모두 초기화
        bossManager.pendingBossIndex = 0;
        bossManager.ResetAffinities();       // 보스 상성은 런 단위 — 새 런에서 새로 생성
        bossManager.ResetBossPiece();        // 보스 말 위치 초기화 — 이전 런 상태 잔존 방지
        currentThrows = 0;
        totalScore = 0;
        displayTotal = 0;
        basicScore = 0;
        multiplier = 0f;
        throwScore = 0;
        boardManager.ResetForNewRun();       // 말판 배치·말 위치·완주 초기화 (다음 런 첫 보스전에서 새 배치 생성)
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
            else if (currentState == GameState.Playing)
            {
                // 세션 진행 중이면 스페이스바 = 결과 확정 (새 최초 투척은 던지지 않는다)
                if (rollManager.session != null && rollManager.session.Active)
                {
                    ConfirmThrow();
                }
                else
                {
                    RollYut();
                }
            }
        }
    }
}
