using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum YutResult { BackDo, Do, Gae, Geol, Yut, Mo }

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

    private readonly List<YutResult> targetResults = new List<YutResult>();
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

    public void RollYut()
    {
        if (currentThrows >= maxThrows)
        {
            Debug.Log("남은 던지기 횟수가 없습니다.");
            if (rollResultText != null)
            {
                rollResultText.text = "남은 던지기 횟수 없음";
            }
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

        bool isTarget = false;

        if (selected == YutResult.BackDo)
        {
            baseScorePool = 0;
            currentThrows++;
        }
        else
        {
            baseScorePool += BaseScore(selected);
            isTarget = targetResults.Contains(selected);
            if (isTarget)
            {
                // 타겟 적중: 보너스 턴이므로 던지기 횟수 증가 없음
                currentMultiplier += MultiplierBonus(selected);
            }
            else
            {
                currentThrows++;
            }
        }

        lastResult = selected;

        Debug.Log($"Yut Result: {ToKorean(selected)} (Probability: {selectedProbability * 100f}%) | Target: {isTarget} | Pool: {baseScorePool} | Multiplier: x{currentMultiplier} | Throws: {currentThrows}/{maxThrows} | Turn Total: {baseScorePool * currentMultiplier}");

        UpdateUI();
    }

    private void UpdateUI()
    {
        int turnTotal = baseScorePool * currentMultiplier;

        if (rollResultText != null)
        {
            rollResultText.text = $"결과: {ToKorean(lastResult)}";
        }
        if (roundText != null)
        {
            roundText.text = $"{currentThrows} / {maxThrows}";
        }
        if (calcText != null)
        {
            calcText.text = $"베이직: {baseScorePool}  |  배수: x{currentMultiplier}";
        }
        if (totalScoreText != null)
        {
            totalScoreText.text = $"현재 점수: {turnTotal}";
        }
        if (targetYutText != null)
        {
            var names = new List<string>();
            foreach (var target in targetResults)
            {
                names.Add(ToKorean(target));
            }
            targetYutText.text = $"타겟: {string.Join(", ", names)}";
        }
    }

    private void Start()
    {
        // 임시 타겟 패 지정: 윷, 개
        targetResults.Add(YutResult.Yut);
        targetResults.Add(YutResult.Gae);

        Debug.Log("--- 게임 시작 ---");
        UpdateUI();
    }

    private void Update()
    {
        // 스페이스바를 누를 때마다 윷 던지기 테스트
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RollYut();
        }
    }
}
