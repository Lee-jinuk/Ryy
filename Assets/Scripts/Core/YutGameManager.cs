using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo, Nak }

public enum GameState { Start, TargetSelection, Playing, Cleared, GameOver }

// 인스펙터에서 확률을 직접 조정할 수 있는 직렬화 항목
[System.Serializable]
public class YutProbabilityEntry
{
    public YutResult result;
    public float percent;
}

// 윷 결과별 영구 스탯 — 주술 등으로 런타임에 강화하면 다음 투척부터 적용
[System.Serializable]
public class YutStatEntry
{
    public YutResult result;
    public int basic;
    public int mult;
}

// 보스 데이터 — 이름·HP·특수능력 수치를 데이터로 분리해 보스별 다른 능력 추가에 대비
[System.Serializable]
public class BossData
{
    public string bossName = "저승지기";
    public int maxHP = 1000;                    // 목표 점수 = 보스 최대 HP
    [TextArea] public string abilityDescription = "낙 확률 +2%p";
    public float nakBonusPercent = 2f;          // 현재 공통 특수능력 수치 (낙 확률 +N%p)
}

public enum EffectSourceType { YutResult, Talisman, Bell, Spell, Fever }
public enum EffectTarget { Basic, Multiplier }
public enum EffectOp { Add, Multiply }

// 효과 적용 1단계 기록 — 향후 부적/방울/주술/피버 발동 연출은 이 목록을 순서대로 재생한다
[System.Serializable]
public class ScoreEffectStep
{
    public EffectSourceType source;  // 효과 발생 원인 (윷 결과/부적/방울/주술/피버)
    public string sourceId;          // 효과를 발생시킨 아이템·능력 식별자
    public string displayName;       // 화면에 표시할 효과 이름
    public EffectTarget target;      // 적용 대상: 베이직 or 멀티플
    public EffectOp op;              // 연산 종류: +고정값 or ×배율 (×2와 +2는 서로 다른 연산)
    public int opValue;              // 연산값 (×2이면 2, +3이면 3)
    public int beforeValue;          // 적용 전 수치
    public int afterValue;           // 적용 후 수치
    public int delta;                // 실제 증가량 (after - before)
    public int order;                // 연출 재생 순서
}

// 한 번의 투척에 대한 확정 정산 — 점수는 여기서 계산되고, 연출은 이 데이터만 재생한다
[System.Serializable]
public class ThrowSettlement
{
    public YutResult result;
    public bool isTargetHit;
    public int baseBasic;            // 효과 적용 전 초기 베이직 (윷 스탯 기준)
    public int baseMult;             // 효과 적용 전 초기 멀티플
    public int finalBasic;           // 모든 효과 적용 후 베이직
    public int finalMult;            // 모든 효과 적용 후 멀티플
    public int throwScore;           // finalBasic × finalMult — TotalScore에 1회만 합산
    public int feverComboAtRoll;     // 이 투척 시점의 콤보 횟수
    public List<ScoreEffectStep> steps = new List<ScoreEffectStep>();
}

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

    [Header("Boss UI")]
    public Text bossNameText;        // 보스 이름
    public Text bossHpText;          // 보스 현재 HP (잔여 목표 점수, 숫자만)
    public Text bossMaxHpText;       // 보스 최대 HP (숫자만)
    public Text bossAbilityText;     // 보스 특수능력 설명
    public Image bossHpBar;          // 체력바 — Image(Filled 타입) 연결 시 fillAmount 갱신
    public Slider bossHpSlider;      // 체력바 대안 — Slider 연결 시 value 갱신 (둘 중 연결된 것만 사용)

    [Header("Panels")]
    public GameObject startPanel;            // '게임 시작' + 보스 선택이 있는 첫 화면
    public GameObject gamePanel;             // 메인 게임 화면 (타겟 선택 모달 뒤에 표시)
    public GameObject targetSelectOverlay;   // 딤 배경 + 뒤쪽 입력 차단 오버레이 (TargetSelectPanel의 부모 권장)
    public GameObject targetSelectPanel;     // 중앙 타겟 선택 팝업 (overlay 자식 — 미연결도 동작)
    public GameObject clearPanel;            // 보스 클리어 패널
    public GameObject gameOverPanel;         // 게임오버 패널

    [Header("Result Texts (숫자·이름만 출력)")]
    public Text clearBossNameText;       // 클리어한 보스 이름
    public Text clearScoreText;          // 클리어 시 최종 점수
    public Text gameOverBossNameText;    // 도전한 보스 이름
    public Text gameOverScoreText;       // 최종 획득 점수
    public Text gameOverGoalText;        // 목표 점수

    [Header("Buttons")]
    public GameObject rollButton;    // 윷 던지기 버튼 오브젝트 — Playing 상태 + 연출 중이 아닐 때만 표시

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

    [Header("Yut Stats — 윷별 베이직/멀티플 기준값 (테스트값)")]
    public List<YutStatEntry> baseYutStats = new List<YutStatEntry>
    {
        new YutStatEntry { result = YutResult.Do,   basic = 10, mult = 2 },
        new YutStatEntry { result = YutResult.Gae,  basic = 20, mult = 3 },
        new YutStatEntry { result = YutResult.Geol, basic = 30, mult = 4 },
        new YutStatEntry { result = YutResult.Yut,  basic = 40, mult = 5 },
        new YutStatEntry { result = YutResult.Mo,   basic = 50, mult = 6 },
    };

    [Header("Presentation (임시 테스트값)")]
    public float effectStepDelay = 0.35f;   // 정산 연출 단계 간 딜레이(초)

    // 런타임 추첨용 가중치 — 보스 특수능력·방울·주술의 확률 조작은 여기만 적용해 기본 데이터 오염 방지
    private readonly Dictionary<YutResult, float> runtimeWeights = new Dictionary<YutResult, float>();

    // 런타임 윷 스탯 — 주술 등의 영구 강화는 여기만 적용해 기본 데이터 오염 방지
    private readonly Dictionary<YutResult, YutStatEntry> yutStats = new Dictionary<YutResult, YutStatEntry>();

    [Header("Target")]
    public YutResult selectedTarget = YutResult.Gae;

    public GameState currentState = GameState.Start;

    // 최근 투척 정산 데이터 — 디버그 인스펙터 노출 + 향후 연출/UI 코드가 읽는다
    public ThrowSettlement currentSettlement;

    // SelectTargetAndStart(int) 인덱스 ↔ 결과 매핑: 0=도 1=개 2=걸 3=윷 4=모
    // 타겟 선택은 일반 결과 5종만 허용 (백도·낙 등 저주 계열 제외)
    private static readonly YutResult[] selectableTargets =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    private YutResult lastResult;
    private bool hasRolled;
    private int lastBackDoLoss;
    private int displayTotal;          // 화면에 보이는 총점 — 정산 연출의 '토탈 합산' 단계에서만 갱신
    private bool isPresenting;
    private Coroutine activePresentation;

    // 저주(Curse) 계열 판별 — 부적/방울/주술이 이 함수로 저주 결과를 구분한다
    public static bool IsCurse(YutResult result) =>
        result == YutResult.BackDo || result == YutResult.Nak;

    private static string ToKorean(YutResult result) => result switch
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

    private BossData CurrentBoss =>
        bosses != null && bosses.Count > 0
            ? bosses[Mathf.Clamp(selectedBossIndex, 0, bosses.Count - 1)]
            : null;

    // ============ 버튼 OnClick 연결용 Public 함수 ============

    // 보스 선택 버튼(int): index 0~4
    public void SelectBoss(int index)
    {
        if (currentState != GameState.Start)
        {
            return;   // 보스전 진행 중에는 변경 불가 — HP 표시가 다른 보스로 바뀌는 것 방지
        }
        if (bosses == null || index < 0 || index >= bosses.Count)
        {
            Debug.LogWarning($"SelectBoss: 잘못된 인덱스 {index} (0~{(bosses?.Count ?? 0) - 1})");
            return;
        }
        selectedBossIndex = index;
        goalScore = bosses[index].maxHP;   // 선택 화면에서 목표 점수 미리보기
        Debug.Log($"--- 보스 선택: {bosses[index].bossName} (HP {bosses[index].maxHP}, {bosses[index].abilityDescription}) ---");
        UpdateUI();
    }

    // 보스 선택 버튼(개별): 인스펙터에서 파라미터 없이 바로 연결 가능
    public void SelectBoss1() => SelectBoss(0);
    public void SelectBoss2() => SelectBoss(1);
    public void SelectBoss3() => SelectBoss(2);
    public void SelectBoss4() => SelectBoss(3);
    public void SelectBoss5() => SelectBoss(4);

    // '게임 시작' 버튼: 선택한 보스전을 시작하고 타겟 선택으로 진행
    public void StartGame()
    {
        if (currentState == GameState.Playing)
        {
            return;
        }
        BeginBossFight();
        EnterTargetSelection();
    }

    // 클리어·게임오버 화면의 '다시 도전' 버튼 — 같은 보스로 보스전 재시작
    public void RetryBoss()
    {
        SetPanel(clearPanel, false);
        SetPanel(gameOverPanel, false);
        StartGame();
    }

    // 클리어·게임오버 화면의 '보스 선택으로 돌아가기' 버튼
    public void BackToBossSelect()
    {
        StopPresentation();
        SetPanel(clearPanel, false);
        SetPanel(gameOverPanel, false);
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
        SetPanel(gamePanel, false);
        SetPanel(startPanel, true);
        currentState = GameState.Start;
        // 보스전 종료 — 특수능력 제거(기본 확률 복원) + 점수 초기화로 선택 화면 미리보기 정상화
        ResetProbabilities();
        totalScore = 0;
        displayTotal = 0;
        roundScore = 0;
        UpdateUI();
    }

    // 라운드 리셋 버튼: 현재 보스전을 처음(1판)부터 다시 시작한다
    public void ResetRound() => RetryBoss();

    // 켜야 할 패널 전용: 미연결이면 경고, 켰는데도 안 보이면 부모 비활성 경고
    private static void ActivatePanel(GameObject panel, string fieldName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[YutGameManager] {fieldName}이(가) Inspector에 연결되지 않았습니다.");
            return;
        }
        panel.SetActive(true);
        if (!panel.activeInHierarchy)
        {
            Debug.LogWarning($"[YutGameManager] {fieldName}은(는) 켜졌지만 부모가 비활성 상태라 화면에 보이지 않습니다. Hierarchy를 확인하세요.", panel);
        }
    }

    // 타겟 선택 버튼(int): yutIndex 0=도 1=개 2=걸 3=윷 4=모
    public void SelectTargetAndStart(int yutIndex)
    {
        if (yutIndex < 0 || yutIndex >= selectableTargets.Length)
        {
            Debug.LogWarning($"SelectTargetAndStart: 잘못된 인덱스 {yutIndex} (0~4)");
            return;
        }
        selectedTarget = selectableTargets[yutIndex];
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
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
        hasRolled = false;
        currentState = GameState.Playing;
        Debug.Log($"--- 타겟 선택: {ToKorean(selectedTarget)} ---");
        UpdateUI();
    }

    // '윷 던지기' 버튼 — 정산은 즉시 확정되고, 화면 연출은 정산 데이터를 그대로 재생한다
    public void RollYut()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("윷을 던질 수 있는 상태가 아닙니다. (GameState.Playing 아님)");
            return;
        }
        if (isPresenting)
        {
            return;   // 정산 연출 재생 중 연타 방지 — 중복 투척/중복 득점 불가
        }
        if (currentThrows >= maxThrows)
        {
            return;
        }

        ThrowSettlement settlement = SettleThrow();
        currentSettlement = settlement;
        lastResult = settlement.result;
        hasRolled = true;

        activePresentation = StartCoroutine(PresentSettlement(settlement));
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

    // ============ 내부 로직 ============

    private void Awake()
    {
        currentState = GameState.Start;
        BuildWeights();
        BuildYutStats();
        if (CurrentBoss != null)
        {
            goalScore = CurrentBoss.maxHP;   // 선택 화면 미리보기
        }
        WarnIfPanelNested();
        SetPanel(startPanel, true);
        SetPanel(gamePanel, false);
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
        SetPanel(clearPanel, false);
        SetPanel(gameOverPanel, false);
    }

    // 보스전 시작: 선택 보스 적용 + 전투 상태 초기화 + 보스 특수능력 적용
    private void BeginBossFight()
    {
        StopPresentation();
        BossData boss = CurrentBoss;
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
        BuildWeights();                    // 기본 확률로 재구축 → 보스 효과 중복 누적 방지
        if (boss != null)
        {
            ApplyBossAbility(boss);
        }
        Debug.Log($"--- 보스전 시작: {boss?.bossName} | 목표 {goalScore} | {roundsPerBoss}판 x {maxThrows}회 ---");
    }

    // 타겟 선택 화면 진입 — 패널 전환 공통 처리
    private void EnterTargetSelection()
    {
        SetPanel(gamePanel, true);
        ActivatePanel(targetSelectOverlay, nameof(targetSelectOverlay));
        SetPanel(targetSelectPanel, true);
        SetPanel(startPanel, false);
        SetPanel(clearPanel, false);
        SetPanel(gameOverPanel, false);
        currentState = GameState.TargetSelection;
        UpdateUI();
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
        Debug.Log($"--- {roundIndex}판 시작 | 누적 총점 {totalScore} / 목표 {goalScore} ---");
        EnterTargetSelection();
    }

    // 보스 클리어: TotalScore >= GoalScore
    private void EnterClear()
    {
        currentState = GameState.Cleared;
        SetPanel(gameOverPanel, false);            // 클리어/게임오버 동시 표시 방지
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
        ActivatePanel(clearPanel, nameof(clearPanel));
        UpdateUI();
        BossData boss = CurrentBoss;
        Debug.Log($"--- 보스 클리어! {boss?.bossName} | 최종 점수 {totalScore} / 목표 {goalScore} ---");
    }

    // 게임오버: 3판 종료 후에도 목표 미달
    private void EnterGameOver()
    {
        currentState = GameState.GameOver;
        SetPanel(clearPanel, false);
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
        ActivatePanel(gameOverPanel, nameof(gameOverPanel));
        UpdateUI();
        BossData boss = CurrentBoss;
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

    // 기본 확률 → 런타임 가중치 복사. 누락 결과는 0, 음수는 0으로 보정
    private void BuildWeights()
    {
        runtimeWeights.Clear();
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            runtimeWeights[result] = 0f;
        }
        foreach (YutProbabilityEntry entry in baseProbabilities)
        {
            if (entry == null)
            {
                continue;
            }
            runtimeWeights[entry.result] += Mathf.Max(0f, entry.percent);
        }
        ValidateWeights();
    }

    private void ValidateWeights()
    {
        float sum = 0f;
        foreach (float weight in runtimeWeights.Values)
        {
            sum += weight;
        }
        if (Mathf.Abs(sum - 100f) > 0.01f)
        {
            Debug.LogWarning($"[YutGameManager] 확률 합계 {sum}% (100% 아님) — 추첨 시 정규화되어 동작하지만 값 확인 필요.", this);
        }
    }

    // 보스 특수능력 적용: 낙 +N%p. 증가분은 일반 결과 5종에서 상대 비중 유지하며 비례 차감 (백도 불변)
    private void ApplyBossAbility(BossData boss)
    {
        float bonus = Mathf.Max(0f, boss.nakBonusPercent);
        if (bonus <= 0f)
        {
            return;
        }
        runtimeWeights[YutResult.Nak] += bonus;

        float normalSum = 0f;
        foreach (YutResult result in selectableTargets)
        {
            normalSum += runtimeWeights[result];
        }
        if (normalSum <= 0f)
        {
            return;
        }
        foreach (YutResult result in selectableTargets)
        {
            float share = runtimeWeights[result] / normalSum;
            runtimeWeights[result] = Mathf.Max(0f, runtimeWeights[result] - bonus * share);
        }
        Debug.Log($"보스 특수능력 적용: {boss.bossName} — 낙 +{bonus}%p (일반 결과 비례 차감, 백도 불변)");
    }

    // 보스 특수능력·방울·주술의 확률 조작용 API — runtimeWeights만 변경되고 baseProbabilities는 오염되지 않음
    public void ModifyProbability(YutResult result, float deltaPercent)
    {
        runtimeWeights[result] = Mathf.Max(0f, runtimeWeights[result] + deltaPercent);
    }

    // 런타임 확률 변경을 모두 되돌리고 기본 확률로 재구축 (보스 효과도 함께 제거됨)
    public void ResetProbabilities() => BuildWeights();

    // 기본 윷 스탯 → 런타임 스탯 복사 (누락 결과는 0/0)
    private void BuildYutStats()
    {
        yutStats.Clear();
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            yutStats[result] = new YutStatEntry { result = result };
        }
        foreach (YutStatEntry entry in baseYutStats)
        {
            if (entry != null)
            {
                yutStats[entry.result] = entry;
            }
        }
    }

    // 주술 등 영구 강화용 API — 다음 투척부터 적용. baseYutStats는 오염되지 않음
    public void UpgradeYutStat(YutResult result, int newBasic, int newMult)
    {
        yutStats[result].basic = Mathf.Max(0, newBasic);
        yutStats[result].mult = Mathf.Max(0, newMult);
    }

    private YutResult DrawYut()
    {
        float totalWeight = 0f;
        foreach (float weight in runtimeWeights.Values)
        {
            totalWeight += weight;
        }
        if (totalWeight <= 0f)
        {
            Debug.LogError("[YutGameManager] 확률 가중치 총합이 0입니다. baseProbabilities를 확인하세요.", this);
            return YutResult.Nak;
        }

        // 가중치 합으로 나눠 추첨하므로 수정으로 합계가 100을 넘어도 정규화되어 동작
        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        // enum 선언 순서대로 순회해 결정적인 추첨 순서 유지
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            cumulative += runtimeWeights[result];
            if (roll < cumulative)
            {
                return result;
            }
        }
        return YutResult.Mo;
    }

    // 투척 1회 정산: 결과 추첨 → 초기 베이직·멀티플 → 효과 단계 기록 → ThrowScore 확정 → 총점 1회 합산
    private ThrowSettlement SettleThrow()
    {
        // 이전 투척의 표시용 임시값만 초기화 (윷별 영구 스탯·누적 총점은 유지)
        basicScore = 0;
        multiplier = 0;
        throwScore = 0;

        YutResult selected = DrawYut();
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
            HandleCurseResult(selected);
            Debug.Log($"Curse Result: {ToKorean(selected)} | Throws: {currentThrows}/{maxThrows} | Round: {roundScore} | Total: {totalScore}");
            return settlement;
        }

        // 1) 윷 결과의 초기 베이직·멀티플 — 주술 강화가 반영된 런타임 스탯 사용
        YutStatEntry stat = yutStats[selected];
        settlement.baseBasic = stat.basic;
        settlement.baseMult = stat.mult;
        settlement.finalBasic = stat.basic;
        settlement.finalMult = stat.mult;

        // 2) 타겟 적중: 보너스 턴(횟수 차감 없음) + 콤보 증가. 2연속 적중부터 피버 효과 기록
        if (isTarget)
        {
            feverCombo++;
            settlement.feverComboAtRoll = feverCombo;
            if (feverCombo >= 2)
            {
                int appliedFever = Mathf.Min(feverCombo, feverCap);
                int before = settlement.finalMult;
                settlement.finalMult *= appliedFever;
                AddStep(settlement.steps, EffectSourceType.Fever, "fever",
                    $"피버 x{appliedFever}", EffectTarget.Multiplier, EffectOp.Multiply,
                    appliedFever, before, settlement.finalMult);
            }
        }
        else
        {
            feverCombo = 0;
            currentThrows++;
        }

        // 3) 향후 부적·방울·주술 효과는 이 지점에서 settlement.steps에 순차 추가된다

        // 4) 이번 투척 점수 확정 + 이번 판 점수·보스전 총점 1회 합산
        //    표시용 throwScore 필드는 여기서 갱신하지 않는다 — '투척 점수 표시' 단계에서만 보인다
        settlement.throwScore = settlement.finalBasic * settlement.finalMult;
        roundScore += settlement.throwScore;
        totalScore += settlement.throwScore;

        Debug.Log($"Yut Result: {ToKorean(selected)} | Target: {isTarget} | {settlement.finalBasic} x {settlement.finalMult} = {settlement.throwScore} | Fever: {feverCombo} | Throws: {currentThrows}/{maxThrows} | Round: {roundScore} | Total: {totalScore}");

        return settlement;
    }

    private static void AddStep(List<ScoreEffectStep> steps, EffectSourceType source, string sourceId,
        string displayName, EffectTarget target, EffectOp op, int opValue, int before, int after)
    {
        steps.Add(new ScoreEffectStep
        {
            source = source,
            sourceId = sourceId,
            displayName = displayName,
            target = target,
            op = op,
            opValue = opValue,
            beforeValue = before,
            afterValue = after,
            delta = after - before,
            order = steps.Count,
        });
    }

    // 정산 데이터를 화면에 순서대로 재생 — 점수 계산과 분리되어 스킵해도 같은 결과
    private IEnumerator PresentSettlement(ThrowSettlement settlement)
    {
        isPresenting = true;

        // 1) 윷 결과 표시 (이전 투척의 베이직·멀티플·투척점수는 0으로 초기화된 상태)
        UpdateUI();
        yield return new WaitForSeconds(effectStepDelay);

        // 2) 초기 베이직·멀티플 표시
        basicScore = settlement.baseBasic;
        multiplier = settlement.baseMult;
        UpdateUI();

        // 3) 발동 효과를 기록 순서대로 재생
        foreach (ScoreEffectStep step in settlement.steps)
        {
            yield return new WaitForSeconds(effectStepDelay);
            if (step.target == EffectTarget.Basic)
            {
                basicScore = step.afterValue;
            }
            else
            {
                multiplier = step.afterValue;
            }
            Debug.Log($"[효과 {step.order + 1}] {step.displayName} — {step.target} {step.beforeValue} → {step.afterValue} ({step.op} {step.opValue}, 증가량 {(step.delta >= 0 ? "+" : "")}{step.delta})");
            UpdateUI();
        }

        // 4) 이번 투척 점수 표시
        yield return new WaitForSeconds(effectStepDelay);
        throwScore = settlement.throwScore;
        UpdateUI();

        // 5) 토탈 합산 표시 — 보스 HP 바 감소와 동기화 (displayTotal 기준으로 표시)
        yield return new WaitForSeconds(effectStepDelay);
        FinishPresentation(settlement);
    }

    private void FinishPresentation(ThrowSettlement settlement)
    {
        basicScore = settlement.finalBasic;
        multiplier = settlement.finalMult;
        throwScore = settlement.throwScore;
        displayTotal = totalScore;
        isPresenting = false;
        activePresentation = null;
        UpdateUI();

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
        }
    }

    // 저주 계열 결과 처리: 백도와 낙은 별개 결과이며 둘 다 투척 1회 소모 + 콤보 단절
    private void HandleCurseResult(YutResult result)
    {
        feverCombo = 0;
        currentThrows++;

        if (result == YutResult.BackDo)
        {
            // 보스 번호 = 선택 순번 + 1 (1~5: 이번 판 점수 차감 / 6~10: 이번 판 점수 초기화)
            int bossNumber = selectedBossIndex + 1;
            if (bossNumber <= 5)
            {
                // 이번 판 누적 점수에서만 차감 — 이전 판 확정분은 불침범
                lastBackDoLoss = Mathf.Min(roundScore, backDoPenalty);
                roundScore -= lastBackDoLoss;
                totalScore -= lastBackDoLoss;
            }
            else
            {
                lastBackDoLoss = roundScore;
                totalScore -= roundScore;
                roundScore = 0;
            }
            totalScore = Mathf.Max(0, totalScore);
        }
        // 낙: 획득 점수 0 + 누적 점수 유지 — 추가 처리 없음 (투척 소모·콤보 단절은 위 공통 처리)
    }

    // 타겟 선택 UI가 startPanel 자식이면 부모가 꺼질 때 같이 숨겨지므로 미리 경고한다
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
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private string FeverLabel()
    {
        if (feverCombo <= 0)
        {
            return "";
        }
        if (feverCombo == 1)
        {
            return "Hit";
        }
        if (feverCombo == 2)
        {
            return "피버";
        }
        return $"피버{feverCombo}";
    }

    private void UpdateUI()
    {
        if (mainResultText != null)
        {
            // 윷 결과 이름만 표시 — Start/TargetSelection에서는 오브젝트 자체를 숨긴다
            bool showResult = currentState == GameState.Playing
                || currentState == GameState.Cleared
                || currentState == GameState.GameOver;
            mainResultText.gameObject.SetActive(showResult);
            mainResultText.text = showResult && hasRolled ? ToKorean(lastResult) : "";
        }
        if (basicScoreText != null)
        {
            basicScoreText.text = basicScore.ToString();
        }
        if (multScoreText != null)
        {
            multScoreText.text = multiplier.ToString();
        }
        // 총점 표시는 연출 단계에 맞춰 displayTotal 사용 — '토탈 합산' 단계에서만 증가
        if (totalScoreText != null)
        {
            totalScoreText.text = displayTotal.ToString();
        }
        if (goalScoreText != null)
        {
            goalScoreText.text = goalScore.ToString();
        }
        if (remainText != null)
        {
            remainText.text = (maxThrows - currentThrows).ToString();
        }
        if (throwDataText != null)
        {
            throwDataText.text = maxThrows.ToString();
        }
        if (throwScoreText != null)
        {
            throwScoreText.text = throwScore.ToString();
        }
        if (feverText != null)
        {
            feverText.text = FeverLabel();
        }
        if (targetYutText != null)
        {
            bool targetConfirmed = currentState != GameState.Start && currentState != GameState.TargetSelection;
            targetYutText.text = targetConfirmed ? ToKorean(selectedTarget) : "";
        }

        // 보스 정보 — 선택 화면에서는 미리보기(만피), 전투 중에는 displayTotal과 동기화된 잔여 HP
        BossData boss = CurrentBoss;
        if (boss != null)
        {
            int remainingHp = currentState == GameState.Start
                ? boss.maxHP
                : Mathf.Max(0, goalScore - displayTotal);
            if (bossNameText != null)
            {
                bossNameText.text = boss.bossName;
            }
            if (bossHpText != null)
            {
                bossHpText.text = remainingHp.ToString();
            }
            if (bossMaxHpText != null)
            {
                bossMaxHpText.text = boss.maxHP.ToString();
            }
            if (bossAbilityText != null)
            {
                bossAbilityText.text = boss.abilityDescription;
            }
            float hpFill = boss.maxHP > 0 ? (float)remainingHp / boss.maxHP : 0f;
            if (bossHpBar != null)
            {
                bossHpBar.fillAmount = hpFill;
            }
            if (bossHpSlider != null)
            {
                bossHpSlider.value = hpFill;
            }

            // 클리어·게임오버 결과 텍스트 (패널이 꺼져 있어도 값은 항상 최신)
            if (clearBossNameText != null)
            {
                clearBossNameText.text = boss.bossName;
            }
            if (clearScoreText != null)
            {
                clearScoreText.text = totalScore.ToString();
            }
            if (gameOverBossNameText != null)
            {
                gameOverBossNameText.text = boss.bossName;
            }
            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = totalScore.ToString();
            }
            if (gameOverGoalText != null)
            {
                gameOverGoalText.text = goalScore.ToString();
            }
        }

        // 던지기 버튼은 Playing 상태에서만, 그리고 연출 재생 중에는 숨긴다
        SetPanel(rollButton, currentState == GameState.Playing && !isPresenting);
    }

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        // 스페이스바 테스트: 화면 플로우를 순서대로 진행
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.Start)
            {
                StartGame();
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
