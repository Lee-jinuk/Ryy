using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo, Nak }

public enum GameState { Start, TargetSelection, Playing, RoundEnd }

// 인스펙터에서 확률을 직접 조정할 수 있는 직렬화 항목
[System.Serializable]
public class YutProbabilityEntry
{
    public YutResult result;
    public float percent;
}

public class YutGameManager : MonoBehaviour
{
    [Header("UI Texts")]
    public Text mainResultText;    // 중앙 결과 화면: 윷 결과 / 피버 / 클리어·게임오버 메시지
    public Text basicScoreText;    // 베이직 점수 (숫자만 출력)
    public Text multScoreText;     // 멀티플 배수 (숫자만 출력)
    public Text totalScoreText;    // 총 점수 / 목표 점수
    public Text remainText;        // 남은 투척 기회 (숫자만 출력)
    public Text throwDataText;     // 전체 투척 기회 (숫자만 출력)
    public Text targetYutText;     // 선택된 타겟 윷 이름 (선택 전 빈 문자열)

    [Header("Panels")]
    public GameObject startPanel;            // '게임 시작' 버튼이 있는 첫 화면
    public GameObject gamePanel;             // 메인 게임 화면 (타겟 선택 모달 뒤에 표시)
    public GameObject targetSelectOverlay;   // 딤 배경 + 뒤쪽 입력 차단 오버레이 (TargetSelectPanel의 부모 권장)
    public GameObject targetSelectPanel;     // 중앙 타겟 선택 팝업 (overlay 자식 — 미연결도 동작)

    [Header("Buttons")]
    public GameObject rollButton;    // 윷 던지기 버튼 오브젝트 — Playing 상태에서만 표시

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

    [Header("Boss / Curse (임시 테스트값)")]
    public int bossIndex = 1;        // 1~5: 백도 점수 차감 / 6~10: 점수 풀 초기화
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

    // 런타임 추첨용 가중치 — 보스/방울/주술의 확률 조작은 여기만 적용해 기본 데이터 오염 방지
    private readonly Dictionary<YutResult, float> runtimeWeights = new Dictionary<YutResult, float>();

    [Header("Target")]
    public YutResult selectedTarget = YutResult.Gae;

    public GameState currentState = GameState.Start;

    // SelectTargetAndStart(int) 인덱스 ↔ 결과 매핑: 0=도 1=개 2=걸 3=윷 4=모
    // 타겟 선택은 일반 결과 5종만 허용 (백도·낙 등 저주 계열 제외)
    private static readonly YutResult[] selectableTargets =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    private YutResult lastResult;
    private bool hasRolled;
    private bool roundCleared;
    private int lastBackDoLoss;

    // 저주(Curse) 계열 판별 — 부적/방울/주술이 이 함수로 저주 결과를 구분한다
    public static bool IsCurse(YutResult result) =>
        result == YutResult.BackDo || result == YutResult.Nak;

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
        YutResult.Nak => "낙",
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

        float totalWeight = 0f;
        foreach (float weight in runtimeWeights.Values)
        {
            totalWeight += weight;
        }
        if (totalWeight <= 0f)
        {
            Debug.LogError("[YutGameManager] 확률 가중치 총합이 0입니다. baseProbabilities를 확인하세요.", this);
            return;
        }

        // 가중치 합으로 나눠 추첨하므로 수정으로 합계가 100을 넘어도 정규화되어 동작
        float roll = Random.value * totalWeight;
        float cumulative = 0f;
        float selectedProbability = 0f;
        YutResult selected = YutResult.Mo;

        // enum 선언 순서대로 순회해 결정적인 추첨 순서 유지
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            cumulative += runtimeWeights[result];
            if (roll < cumulative)
            {
                selected = result;
                selectedProbability = runtimeWeights[result] / totalWeight;
                break;
            }
        }

        bool isTarget = selected == selectedTarget;

        if (IsCurse(selected))
        {
            HandleCurseResult(selected);
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
        BuildWeights();
        WarnIfPanelNested();
        SetPanel(startPanel, true);
        SetPanel(gamePanel, false);
        SetPanel(targetSelectOverlay, false);
        SetPanel(targetSelectPanel, false);
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

    // 보스 특수능력·방울·주술의 확률 조작용 API — runtimeWeights만 변경되고 baseProbabilities는 오염되지 않음
    public void ModifyProbability(YutResult result, float deltaPercent)
    {
        runtimeWeights[result] = Mathf.Max(0f, runtimeWeights[result] + deltaPercent);
    }

    // 런타임 확률 변경을 모두 되돌리고 기본 확률로 재구축
    public void ResetProbabilities() => BuildWeights();

    // 저주 계열 결과 처리: 백도와 낙은 별개 결과이며 둘 다 투척 1회 소모 + 콤보 단절
    private void HandleCurseResult(YutResult result)
    {
        feverCombo = 0;
        currentThrows++;

        if (result == YutResult.BackDo)
        {
            if (bossIndex <= 5)
            {
                // 보스 1~5: 현재 판 베이직 점수에서 차감 (확정된 이전 판 점수는 건드리지 않음)
                lastBackDoLoss = Mathf.Min(baseScorePool, backDoPenalty);
                baseScorePool -= lastBackDoLoss;
            }
            else
            {
                // 보스 6~10: 현재 판 점수 풀 초기화
                lastBackDoLoss = baseScorePool;
                baseScorePool = 0;
            }
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
            return bossIndex <= 5
                ? $"백도! 베이직 -{lastBackDoLoss}"
                : "백도! 점수 풀 초기화";
        }
        if (lastResult == YutResult.Nak)
        {
            return "낙… 점수 획득 없음";
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
            basicScoreText.text = baseScorePool.ToString();
        }
        if (multScoreText != null)
        {
            multScoreText.text = currentMultiplier.ToString();
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = $"점수: {turnTotal} / 목표 {roundTargetScore}";
        }
        if (remainText != null)
        {
            remainText.text = (maxThrows - currentThrows).ToString();
        }
        if (throwDataText != null)
        {
            throwDataText.text = maxThrows.ToString();
        }
        if (targetYutText != null)
        {
            bool targetConfirmed = currentState == GameState.Playing || currentState == GameState.RoundEnd;
            targetYutText.text = targetConfirmed ? ToKorean(selectedTarget) : "";
        }
        // 던지기 버튼은 본 게임(Playing) 상태에서만 표시 — 타겟 선택 모달 뒤에서도 숨김
        SetPanel(rollButton, currentState == GameState.Playing);
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
