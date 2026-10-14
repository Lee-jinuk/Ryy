using System.Collections.Generic;
using UnityEngine;
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
    public int maxThrows = 7;
    public int currentThrows = 0;
    public int baseScorePool = 0;
    public int currentMultiplier = 2;
    public int feverCombo = 0;
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

    private YutResult lastResult;

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
                    currentMultiplier *= feverCombo;
                    Debug.Log($"FEVER x{feverCombo}! Multiplier: x{currentMultiplier}");
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
        bool cleared = finalScore >= roundTargetScore;
        currentState = GameState.RoundEnd;

        Debug.Log(cleared
            ? $"--- 라운드 클리어! 최종 점수: {finalScore} / 목표 {roundTargetScore} ---"
            : $"--- 게임 오버. 최종 점수: {finalScore} / 목표 {roundTargetScore} ---");

        if (rollResultText != null)
        {
            rollResultText.text = cleared ? "라운드 클리어!" : "게임 오버";
        }
    }

    // 라운드 초기화 (테스트/재시작용)
    public void ResetRound()
    {
        currentThrows = 0;
        baseScorePool = 0;
        currentMultiplier = 2;
        feverCombo = 0;
        currentState = GameState.TargetSelection;
        UpdateUI();
    }

    private void UpdateUI()
    {
        int turnTotal = baseScorePool * currentMultiplier;

        if (rollResultText != null && currentState == GameState.Playing)
        {
            rollResultText.text = $"결과: {ToKorean(lastResult)}";
        }
        if (roundText != null)
        {
            roundText.text = $"{currentThrows} / {maxThrows}";
        }
        if (calcText != null)
        {
            calcText.text = $"베이직: {baseScorePool}  |  배수: x{currentMultiplier}  |  피버: {feverCombo}콤보";
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = $"현재 점수: {turnTotal} / 목표 {roundTargetScore}";
        }
        if (targetYutText != null)
        {
            targetYutText.text = $"타겟: {ToKorean(selectedTarget)}";
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
