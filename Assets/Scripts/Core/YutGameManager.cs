using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo }

public enum GameState { Start, TargetSelection, Playing, RoundEnd }

public class YutGameManager : MonoBehaviour
{
    [Header("UI Texts")]
    public Text mainResultText;    // 중앙 결과 화면: 윷 결과 / 피버 / 클리어·게임오버 메시지
    public Text basicScoreText;    // 베이직 점수 풀
    public Text multScoreText;     // 멀티플 배수
    public Text totalScoreText;    // 총 점수 / 목표 점수
    public Text throwCountText;    // 현재 투척 수 / 총 투척 수
    public Text targetYutText;     // 선택된 타겟 윷 + 피버 콤보

    [Header("Panels")]
    public GameObject startPanel;            // '게임 시작' 버튼이 있는 첫 화면
    public GameObject gamePanel;             // 메인 게임 화면 (타겟 선택 모달 뒤에 표시)
    public GameObject targetSelectOverlay;   // 딤 배경 + 뒤쪽 입력 차단 오버레이 (TargetSelectPanel의 부모 권장)
    public GameObject targetSelectPanel;     // 중앙 타겟 선택 팝업 (overlay 자식 — 미연결도 동작)

    [Header("Slots")]
    public GameObject[] bellSlots = new GameObject[3];      // 방울 슬롯 3칸 (방울 선택 결과 표시용)
    public GameObject[] talismanSlots = new GameObject[4];  // 부적 슬롯 4칸

    [Header("Round State")]
    public int maxThrows = 5;
    public int currentThrows = 0;
    public int baseScorePool = 0;
    public int currentMultiplier = 2;
    public int feverCombo = 0;
    public int feverCap = 8;
    public int roundTargetScore = 1000;

    [Header("Target")]
    public YutResult selectedTarget = YutResult.Gae;

    public GameState currentState = GameState.Start;

    // 유물 효과 등으로 런타임에 수정 가능하도록 인스턴스 Dictionary로 관리
    private readonly Dictionary<YutResult, float> probabilityTable = new Dictionary<YutResult, float>
    {
        { YutResult.BackDo, 0.0625f },
        { YutResult.Do, 0.1875f },
        { YutResult.Gae, 0.375f },
        { YutResult.Geol, 0.25f },
        { YutResult.Yut, 0.0625f },
        { YutResult.Mo, 0.0625f },
    };

    // SelectTargetAndStart(int) 인덱스 ↔ 결과 매핑: 0=도 1=개 2=걸 3=윷 4=모
    private static readonly YutResult[] selectableTargets =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    private YutResult lastResult;
    private bool hasRolled;
    private bool roundCleared;

    private static int BaseScore(YutResult result) => result switch
    {
        YutResult.Do => 10,
        YutResult.Gae => 20,
        YutResult.Geol => 30,
        YutResult.Yut => 40,
        YutResult.Mo => 50,
        _ => 0,
    };

    private static int MultiplierBonus(YutResult result) => result switch
    {
        YutResult.Do => 1,
        YutResult.Gae => 2,
        YutResult.Geol => 3,
        YutResult.Yut => 4,
        YutResult.Mo => 5,
        _ => 0,
    };

    private static string ToKorean(YutResult result) => result switch
    {
        YutResult.BackDo => "백도",
        YutResult.Do => "도",
        YutResult.Gae => "개",
        YutResult.Geol => "걸",
        YutResult.Yut => "윷",
        YutResult.Mo => "모",
        _ => result.ToString(),
    };

    // ============ 버튼 OnClick 연결용 Public 함수 ============

    // '게임 시작' 버튼: 시작 화면을 끄고 타겟 선택 화면을 연다
    public void StartGame()
    {
        if (currentState == GameState.Playing)
        {
            return;
        }
        currentState = GameState.TargetSelection;

        // 메인 화면을 먼저 켠 뒤 모달 오버레이를 올리고 시작 화면을 끈다
        SetPanel(gamePanel, true);
        ActivatePanel(targetSelectOverlay, nameof(targetSelectOverlay));
        SetPanel(targetSelectPanel, true);
        if (startPanel != null)
        {
            startPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[YutGameManager] startPanel이 Inspector에 연결되지 않았습니다.", this);
        }

        Debug.Log("--- 게임 시작: 타겟 윷을 선택하세요 ---");
        UpdateUI();
    }

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

    // '윷 던지기' 버튼
    public void RollYut()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("윷을 던질 수 있는 상태가 아닙니다. (GameState.Playing 아님)");
            return;
        }
        if (currentThrows >= maxThrows)
        {
            EndRound();
            return;
        }

        float roll = Random.value;
        float cumulative = 0f;
        float selectedProbability = 0f;
        YutResult selected = YutResult.Mo;

        // enum 선언 순서대로 순회해 결정적인 추첨 순서 유지
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            cumulative += probabilityTable[result];
            if (roll < cumulative)
            {
                selected = result;
                selectedProbability = probabilityTable[result];
                break;
            }
        }

        bool isTarget = selected == selectedTarget;

        if (selected == YutResult.BackDo)
        {
            baseScorePool = 0;
            feverCombo = 0;
            currentThrows++;
        }
        else
        {
            baseScorePool += BaseScore(selected);
            if (isTarget)
            {
                // 타겟 적중: 보너스 턴(횟수 차감 없음) + 배수 합산 + 연속 적중 시 X-Mult 폭발
                feverCombo++;
                currentMultiplier += MultiplierBonus(selected);
                if (feverCombo >= 2)
                {
                    int appliedFever = Mathf.Min(feverCombo, feverCap);
                    currentMultiplier *= appliedFever;
                    Debug.Log($"FEVER x{feverCombo} (적용 x{appliedFever})! Multiplier: x{currentMultiplier}");
                }
            }
            else
            {
                feverCombo = 0;
                currentThrows++;
            }
        }

        lastResult = selected;
        hasRolled = true;

        Debug.Log($"Yut Result: {ToKorean(selected)} (Probability: {selectedProbability * 100f}%) | Target: {isTarget} | Pool: {baseScorePool} | Multiplier: x{currentMultiplier} | Fever: {feverCombo} | Throws: {currentThrows}/{maxThrows} | Turn Total: {baseScorePool * currentMultiplier}");

        UpdateUI();

        if (currentThrows >= maxThrows)
        {
            EndRound();
        }
    }

    // 라운드 리셋 버튼: 타겟 선택 화면으로 되돌아간다
    public void ResetRound()
    {
        currentThrows = 0;
        baseScorePool = 0;
        currentMultiplier = 2;
        feverCombo = 0;
        hasRolled = false;
        roundCleared = false;
        SetPanel(startPanel, false);
        SetPanel(targetSelectOverlay, true);
        SetPanel(targetSelectPanel, true);
        currentState = GameState.TargetSelection;
        UpdateUI();
    }

    // ============ 내부 로직 ============

    private void Awake()
    {
        currentState = GameState.Start;
        WarnIfPanelNested();
        SetPanel(startPanel, true);
        SetPanel(gamePanel, false);
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
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

    private void EndRound()
    {
        int finalScore = baseScorePool * currentMultiplier;
        roundCleared = finalScore >= roundTargetScore;
        currentState = GameState.RoundEnd;

        Debug.Log(roundCleared
            ? $"--- 라운드 클리어! 최종 점수: {finalScore} / 목표 {roundTargetScore} ---"
            : $"--- 게임 오버. 최종 점수: {finalScore} / 목표 {roundTargetScore} ---");

        UpdateUI();
    }

    private string ResultMessage()
    {
        if (!hasRolled)
        {
            return "윷을 던지세요";
        }
        if (lastResult == YutResult.BackDo)
        {
            return "백도! 점수 풀 초기화";
        }
        if (lastResult == selectedTarget)
        {
            return feverCombo >= 2
                ? $"FEVER! {ToKorean(lastResult)} 적중 (x{Mathf.Min(feverCombo, feverCap)} 배수 폭발)"
                : $"타겟 적중! {ToKorean(lastResult)}";
        }
        return $"결과: {ToKorean(lastResult)}";
    }

    private void UpdateUI()
    {
        int turnTotal = baseScorePool * currentMultiplier;

        if (mainResultText != null)
        {
            mainResultText.text = currentState switch
            {
                GameState.Start => "게임 시작을 누르세요",
                GameState.TargetSelection => "타겟 윷을 선택하세요",
                GameState.Playing => ResultMessage(),
                _ => roundCleared ? "라운드 클리어!" : "게임 오버",
            };
        }
        if (basicScoreText != null)
        {
            basicScoreText.text = $"베이직: {baseScorePool}";
        }
        if (multScoreText != null)
        {
            multScoreText.text = $"배수: x{currentMultiplier}";
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {turnTotal} / 목표 {roundTargetScore}";
        }
        if (throwCountText != null)
        {
            throwCountText.text = $"투척: {currentThrows} / {maxThrows}";
        }
        if (targetYutText != null)
        {
            targetYutText.text = $"타겟: {ToKorean(selectedTarget)}  |  피버: {feverCombo}콤보";
        }
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
