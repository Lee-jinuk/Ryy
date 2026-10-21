using System.Collections.Generic;
using UnityEngine;

// 인스펙터에서 확률을 직접 조정할 수 있는 직렬화 항목
[System.Serializable]
public class YutProbabilityEntry
{
    public YutResult result;
    public float percent;
}

// 윷 결과별 영구 스탯 — 주술 등으로 런타임에 강화하면 다음 투척부터 적용
// basicBonus: 윷별 베이직 강화치 — 현재 공격 공식(칸 공격점수 × 윷 배율)에는 미적용, 강화 API 예약용
[System.Serializable]
public class YutStatEntry
{
    public YutResult result;
    public int basicBonus;
    public float mult;   // 방울 성장(+0.5) 지원을 위한 소수 배율 — 정수화는 정산 마지막에 1회만
}

// 윷 투척과 확률 담당 — 윷 4개 투척 세션(면 조합·재투척), 기본/런타임 확률 관리(예약 API),
// 윷별 기본 능력치 및 강화값 관리. 게임 상태와 UI 표시는 다루지 않는다.
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class YutRollManager
{
    private readonly YutGameManager gm;

    // 현재 차례의 투척 세션 — 차례 준비(PrepareSession)에 생성되고 rolled=false면 최초 투척 전.
    // 최초 투척/재투척/확정은 YutGameManager 버튼 경유
    public YutThrowSession session;

    // 런타임 추첨용 가중치 — 보스 특수능력·방울·주술의 확률 조작은 여기만 적용해 기본 데이터 오염 방지
    // 참고: 신규 윷 4개 면 조합 투척 경로는 가중치 추첨을 사용하지 않는다 (DrawYut는 API 호환용으로 유지)
    public readonly Dictionary<YutResult, float> runtimeWeights = new Dictionary<YutResult, float>();

    // 런타임 윷 스탯 — 주술 등의 영구 강화는 여기만 적용해 기본 데이터 오염 방지
    public readonly Dictionary<YutResult, YutStatEntry> yutStats = new Dictionary<YutResult, YutStatEntry>();

    public YutRollManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 차례 준비 — 새 세션을 생성만 하고 던지지 않는다 (rolled=false = 준비 단계).
    // 준비 단계에서 사용하는 부적 효과가 세션 필드에 보관되도록 최초 투척 전에 만들어 둔다
    public void PrepareSession()
    {
        session = new YutThrowSession();
    }

    // 기본 확률 → 런타임 가중치 복사. 누락 결과는 0, 음수는 0으로 보정
    public void BuildWeights()
    {
        runtimeWeights.Clear();
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            runtimeWeights[result] = 0f;
        }
        foreach (YutProbabilityEntry entry in gm.baseProbabilities)
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
            Debug.LogWarning($"[YutGameManager] 확률 합계 {sum}% (100% 아님) — 추첨 시 정규화되어 동작하지만 값 확인 필요.", gm);
        }
    }

    // 보스 특수능력 적용: 낙 +N%p. 증가분은 일반 결과 5종에서 상대 비중 유지하며 비례 차감 (백도 불변)
    public void ApplyBossAbility(BossData boss)
    {
        float bonus = Mathf.Max(0f, boss.nakBonusPercent);
        if (bonus <= 0f)
        {
            return;
        }
        runtimeWeights[YutResult.Nak] += bonus;

        float normalSum = 0f;
        foreach (YutResult result in YutGameManager.NormalResults)
        {
            normalSum += runtimeWeights[result];
        }
        if (normalSum <= 0f)
        {
            return;
        }
        foreach (YutResult result in YutGameManager.NormalResults)
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
    public void BuildYutStats()
    {
        yutStats.Clear();
        foreach (YutResult result in System.Enum.GetValues(typeof(YutResult)))
        {
            yutStats[result] = new YutStatEntry { result = result };
        }
        if (gm.baseYutStats == null)
        {
            return;   // balanceConfig 미연결 — 경고는 ValidateDataAssets가 출력
        }
        foreach (YutStatEntry entry in gm.baseYutStats)
        {
            if (entry != null)
            {
                // 새 객체로 복사 — 원본(Inspector 기본값)을 공유하면 런타임 강화가 기본 스탯을 오염시킨다
                yutStats[entry.result] = new YutStatEntry
                {
                    result = entry.result,
                    basicBonus = entry.basicBonus,
                    mult = entry.mult,
                };
            }
        }
    }

    // 주술 등 영구 강화용 API — 다음 투척부터 적용. baseYutStats는 오염되지 않음
    // basicBonus는 예약 필드(현재 미적용), mult는 해당 윷의 멀티플
    public void UpgradeYutStat(YutResult result, int newBasicBonus, float newMult)
    {
        yutStats[result].basicBonus = Mathf.Max(0, newBasicBonus);
        yutStats[result].mult = Mathf.Max(0f, newMult);
    }

    public YutStatEntry GetStat(YutResult result) => yutStats[result];

    // 런타임 가중치 기반 윷 결과 추첨
    public YutResult DrawYut()
    {
        float totalWeight = 0f;
        foreach (float weight in runtimeWeights.Values)
        {
            totalWeight += weight;
        }
        if (totalWeight <= 0f)
        {
            Debug.LogError("[YutGameManager] 확률 가중치 총합이 0입니다. baseProbabilities를 확인하세요.", gm);
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
}
