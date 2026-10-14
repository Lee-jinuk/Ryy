using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo }

public enum GameState { TargetSelection, Playing, RoundEnd }

public class YutGameManager : MonoBehaviour
{
    [Header("UI")]
    public Text rollResultText;
    public Text roundText;
    public Text calcText;
    public Text totalScoreText;
    public Text targetYutText;

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

    public GameState currentState = GameState.TargetSelection;

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

    private static readonly YutResult[] selectableTargets =
        { YutResult.Do, YutResult.Gae, YutResult.Geol, YutResult.Yut, YutResult.Mo };

    private YutResult lastResult;
    private bool roundCleared;

    private Canvas canvas;
    private GameObject targetPanel;
    private Button rollButton;
    private static Font defaultFont;

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

    // 인스펙터 수동 연결 없이 Play 즉시 플레이 가능하도록 UI를 코드로 보장한다.
    private void Awake()
    {
        EnsureEventSystem();
        EnsureUI();
    }

    private void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();
    }

    private void EnsureUI()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("Canvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        targetYutText = EnsureText(targetYutText, "TargetYutText", new Vector2(0f, 175f), 26);
        roundText = EnsureText(roundText, "RoundText", new Vector2(0f, 55f), 22);
        rollResultText = EnsureText(rollResultText, "RollResultText", new Vector2(0f, 15f), 24);
        calcText = EnsureText(calcText, "CalcText", new Vector2(0f, -25f), 20);
        totalScoreText = EnsureText(totalScoreText, "TotalScoreText", new Vector2(0f, -65f), 22);

        targetPanel = new GameObject("TargetPanel");
        targetPanel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = targetPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, 115f);
        panelRect.sizeDelta = new Vector2(460f, 60f);

        for (int i = 0; i < selectableTargets.Length; i++)
        {
            YutResult target = selectableTargets[i];
            Button button = CreateButton(targetPanel.transform, $"Target{target}", ToKorean(target),
                new Vector2(-176f + i * 88f, 0f), new Vector2(80f, 52f));
            button.onClick.AddListener(() => SelectTarget(target));
        }

        rollButton = CreateButton(canvas.transform, "RollButton", "윷 던지기",
            new Vector2(0f, -125f), new Vector2(220f, 60f));
        rollButton.onClick.AddListener(RollYut);

        Button resetButton = CreateButton(canvas.transform, "ResetButton", "라운드 리셋",
            new Vector2(0f, -200f), new Vector2(160f, 42f));
        resetButton.onClick.AddListener(ResetRound);
    }

    private Text EnsureText(Text text, string name, Vector2 position, int fontSize)
    {
        if (text == null)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(canvas.transform, false);
            text = textObject.AddComponent<Text>();
        }
        RectTransform rect = text.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(700f, 40f);
        text.font = GetDefaultFont();
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static Font GetDefaultFont()
    {
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        return defaultFont;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.94f, 0.87f, 0.68f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        Text labelText = labelObject.AddComponent<Text>();
        labelText.font = GetDefaultFont();
        labelText.text = label;
        labelText.fontSize = 20;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = new Color(0.12f, 0.12f, 0.12f);

        return button;
    }

    // 타겟 선택 버튼 전용: 선택과 동시에 라운드 시작
    public void SelectTarget(YutResult target)
    {
        selectedTarget = target;
        ConfirmTargetSelection();
    }

    // 라운드 시작 시 타겟 윷 1개 선택 확정 (UI Button OnClick 연결용)
    public void ConfirmTargetSelection()
    {
        if (currentState != GameState.TargetSelection)
        {
            return;
        }
        currentState = GameState.Playing;
        Debug.Log($"--- 타겟 선택: {ToKorean(selectedTarget)} ---");
        UpdateUI();
    }

    public void RollYut()
    {
        if (currentState != GameState.Playing)
        {
            Debug.Log("타겟 윷을 먼저 선택하세요. (GameState.TargetSelection)");
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

        Debug.Log($"Yut Result: {ToKorean(selected)} (Probability: {selectedProbability * 100f}%) | Target: {isTarget} | Pool: {baseScorePool} | Multiplier: x{currentMultiplier} | Fever: {feverCombo} | Throws: {currentThrows}/{maxThrows} | Turn Total: {baseScorePool * currentMultiplier}");

        UpdateUI();

        if (currentThrows >= maxThrows)
        {
            EndRound();
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

    // 라운드 초기화 (테스트/재시작용)
    public void ResetRound()
    {
        currentThrows = 0;
        baseScorePool = 0;
        currentMultiplier = 2;
        feverCombo = 0;
        roundCleared = false;
        currentState = GameState.TargetSelection;
        UpdateUI();
    }

    private void UpdateUI()
    {
        int turnTotal = baseScorePool * currentMultiplier;

        if (rollResultText != null)
        {
            rollResultText.text = currentState switch
            {
                GameState.TargetSelection => "타겟 윷을 선택하세요",
                GameState.Playing => $"결과: {ToKorean(lastResult)}",
                _ => roundCleared ? "라운드 클리어!" : "게임 오버",
            };
        }
        if (roundText != null)
        {
            roundText.text = $"남은 투척: {maxThrows - currentThrows} / {maxThrows}";
        }
        if (calcText != null)
        {
            string feverInfo = feverCombo >= 2 ? $" (x{Mathf.Min(feverCombo, feverCap)} 적용)" : "";
            calcText.text = $"베이직: {baseScorePool}  |  배수: x{currentMultiplier}  |  피버: {feverCombo}콤보{feverInfo}";
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = $"현재 점수: {turnTotal} / 목표 {roundTargetScore}";
        }
        if (targetYutText != null)
        {
            targetYutText.text = $"타겟: {ToKorean(selectedTarget)}";
        }
        if (targetPanel != null)
        {
            targetPanel.SetActive(currentState == GameState.TargetSelection);
        }
        if (rollButton != null)
        {
            rollButton.interactable = currentState == GameState.Playing;
        }
    }

    private void Start()
    {
        Debug.Log("--- 게임 시작: 타겟 윷을 선택하세요 ---");
        UpdateUI();
    }

    private void Update()
    {
        // 스페이스바 테스트: 타겟 미선택 시 현재 선택값으로 확정, 이후 윷 던지기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (currentState == GameState.TargetSelection)
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
