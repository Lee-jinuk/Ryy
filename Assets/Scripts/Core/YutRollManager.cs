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
// baseScore: 윷 기본점수 — 공격 공식의 출발점 (점수제 전환, 2026-10-22 확정).
// basicBonus: 예약 필드 — 의미 변경 없이 보존 (현재 미적용)
[System.Serializable]
public class YutStatEntry
{
    public YutResult result;
    public int basicBonus;
    public int baseScore;   // 윷 기본점수 — 도20/개30/걸30/윷50/모60 (GameBalanceConfig 편집)
    public float mult;      // 윷 기본 배율 — 방울 성장(+0.5) 지원을 위한 소수 배율 — 정수화는 정산 마지막에 1회만
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
        // 방울 연동 세션 설정 — 세션 생성 시점에 보유 방울을 반영한다
        // (확률 판정은 실행 시점에만 — 세션 중 보유 상태는 변하지 않는다)
        // 윤회령 — 유효 확률(공령명 배율 반영)을 세션에 보관, Reroll 실행 시 추첨
        OwnedBell freeRerollBell = gm.bellManager.GetBell(BellEffectKind.FreeReroll);
        session.freeRerollChance = freeRerollBell != null ? gm.bellManager.EffectiveChance(freeRerollBell) : 0f;
        // 삼고령 — 기본 재투척 기회 가산 (같은 윷 1회 제한·특별 기회는 별도 유지)
        OwnedBell extraRerollBell = gm.bellManager.GetBell(BellEffectKind.ExtraReroll);
        session.maxRerolls = YutThrowSession.BaseRerolls + (extraRerollBell != null ? extraRerollBell.bonusCount : 0);
        // 결단령 — 모든 재투척 금지 (다른 방울이 낙·승부부의 금지를 해제하지 않는 것과 같은 방향의 독립 상태)
        session.rerollsBanned = gm.bellManager.HasBell(BellEffectKind.RerollBan);
        // 양면령 + 재투척 면 점괘 — 기본값(미장착 0.5·장착 시 방울 설정)에
        // 앞면 − 뒷면 강화 누적을 더하고 20~80%로 제한한 최종값을 세션에 보관한다
        // (기본·특별 재투척 모두 적용, 최초 투척에는 미적용 — 공령명 증폭 대상 아님)
        float faceDelta = (gm.fortuneManager.GetRerollFrontBonus() - gm.fortuneManager.GetRerollBackBonus()) / 100f;
        session.rerollFrontUpChance = Mathf.Clamp(RerollFrontBaseChance() + faceDelta, 0.2f, 0.8f);
        // 걸윷령·쌍개령·극단령 + 윷 확률 점괘 — 최초 투척 결과 확률 보정 분포를 세션에 보관
        // (추첨은 InitialRoll에서 1회만 — 재투척·UI 갱신에는 적용되지 않는다)
        session.initialResultWeights = BuildInitialResultWeights();
    }

    // 재투척 앞면 확률의 기본값 — 양면령 장착 시 방울 설정(현재 0.7), 미장착 0.5.
    // 점괘 보정·클램프 전의 원본 — HasEffect 판정과 세션 설정이 같은 경로를 쓴다
    public float RerollFrontBaseChance()
    {
        OwnedBell frontBiasBell = gm.bellManager.GetBell(BellEffectKind.RerollFrontBias);
        return frontBiasBell != null ? frontBiasBell.frontUpChance : 0.5f;
    }

    // 최초 투척 결과 확률 보정 — 장착 확률 방울과 윷 확률 점괘의 증가량을 모아
    // 한 번 계산한다 (합산이라 장착·획득 순서와 무관). 보정 대상이 없으면 null —
    // 세션은 기본 면 분포를 사용한다.
    // 일반 경우: 대상에 +N%p를 적용하고 남은 확률을 비대상의 기존 비율로 분배.
    // 예외(모든 결과가 대상이거나 대상 확률 합계 ≥ 100%): 전체를 함께 100%로 정규화 —
    // 비대상을 강제로 0으로 만들지 않는다 (2026-10-26 지시서 §7 → 2026-10-27 점괘 통합)
    public Dictionary<YutResult, float> BuildInitialResultWeights()
    {
        Dictionary<YutResult, float> bonus = CollectInitialProbBonus();
        return bonus.Count == 0 ? null : ComputeAdjustedResultWeights(bonus);
    }

    // 최초 투척 결과별 증가량 수집 — 확률 방울(걸윷·쌍개·극단) + 윷 확률 점괘를
    // 같은 맵에 합산한다. 두 출처를 구분하지 않으므로 적용 순서에 결과가 달라지지 않는다
    private Dictionary<YutResult, float> CollectInitialProbBonus()
    {
        var bonus = new Dictionary<YutResult, float>();
        foreach (OwnedBell bell in gm.bellManager.ownedBells)
        {
            if (bell.probBonusPercent <= 0f)
            {
                continue;
            }
            AddProbTarget(bonus, bell.targetResult, bell.probBonusPercent);
            if (bell.targetResult2 != bell.targetResult)   // 같은 대상 중복 가산 방지
            {
                AddProbTarget(bonus, bell.targetResult2, bell.probBonusPercent);
            }
        }
        foreach (YutResult result in YutGameManager.NormalResults)
        {
            float fortuneBonus = gm.fortuneManager.GetYutProbBonus(result);
            if (fortuneBonus > 0f)
            {
                AddProbTarget(bonus, result, fortuneBonus);
            }
        }
        return bonus;
    }

    // 보정 분포 계산 — 결과별 기본 확률 + 증가량을 ApplyPercentBonuses 규칙으로 조정.
    // 후보 효과 판정(WouldChangeInitialWeights)과 실제 세션 설정이 같은 경로를 쓴다
    private static Dictionary<YutResult, float> ComputeAdjustedResultWeights(
        Dictionary<YutResult, float> bonus)
    {
        var baseP = new float[YutGameManager.NormalResults.Length];
        var bonusP = new float[baseP.Length];
        for (int i = 0; i < baseP.Length; i++)
        {
            baseP[i] = BaseResultPercent(YutGameManager.NormalResults[i]);
            bonusP[i] = bonus.TryGetValue(YutGameManager.NormalResults[i], out float v) ? v : 0f;
        }
        float[] adjusted = ApplyPercentBonuses(baseP, bonusP);
        var weights = new Dictionary<YutResult, float>();
        for (int i = 0; i < adjusted.Length; i++)
        {
            weights[YutGameManager.NormalResults[i]] = adjusted[i];
        }
        return weights;
    }

    // 점괘 후보의 실효 판정 — 대상 결과에 +pct를 가상 적용했을 때 보정 분포가
    // 바뀌는지 비교한다. 순수 조회 — 방울·세션·추첨 상태를 바꾸지 않는다
    public bool WouldChangeInitialWeights(YutResult target, float pct)
    {
        if (pct <= 0f || target == YutResult.BackDo || target == YutResult.Nak)
        {
            return false;
        }
        Dictionary<YutResult, float> bonus = CollectInitialProbBonus();
        Dictionary<YutResult, float> current = ComputeAdjustedResultWeights(bonus);
        bonus[target] = bonus.TryGetValue(target, out float v) ? v + pct : pct;
        Dictionary<YutResult, float> next = ComputeAdjustedResultWeights(bonus);
        foreach (YutResult result in YutGameManager.NormalResults)
        {
            if (Mathf.Abs(current[result] - next[result]) > 0.0001f)
            {
                return true;
            }
        }
        return false;
    }

    // 확률 %p 보정의 공통 규칙 — 대상에 증가량을 더하고, 남은 확률을 비대상의
    // 기존 비율로 분배해 합계 100%를 유지한다. 모든 대상이 증가 대상이거나 대상
    // 합계가 100% 이상이면 전체를 함께 정규화한다 (비대상 강제 0 없음).
    // 최초 투척 결과 보정·말판 속성 발생 보정이 이 구현을 공용한다
    public static float[] ApplyPercentBonuses(float[] basePcts, float[] bonusPcts)
    {
        int n = basePcts.Length;
        var adjusted = new float[n];
        float targetSum = 0f;
        float totalSum = 0f;
        float nonTargetBase = 0f;
        bool allTargeted = n > 0;
        for (int i = 0; i < n; i++)
        {
            float w = Mathf.Max(0f, basePcts[i] + bonusPcts[i]);
            adjusted[i] = w;
            totalSum += w;
            if (bonusPcts[i] > 0f)
            {
                targetSum += w;
            }
            else
            {
                allTargeted = false;
                nonTargetBase += Mathf.Max(0f, basePcts[i]);
            }
        }
        if (allTargeted || targetSum >= 100f)
        {
            if (totalSum > 0f)
            {
                for (int i = 0; i < n; i++)
                {
                    adjusted[i] = adjusted[i] / totalSum * 100f;
                }
            }
            return adjusted;
        }
        float remaining = 100f - targetSum;
        for (int i = 0; i < n; i++)
        {
            if (bonusPcts[i] > 0f)
            {
                continue;
            }
            float b = Mathf.Max(0f, basePcts[i]);
            adjusted[i] = nonTargetBase > 0f ? b * remaining / nonTargetBase : 0f;
        }
        return adjusted;
    }

    // 확률 가산 대상 등록 — 저주 결과(백도·낙)는 일반 결과 분포의 대상이 아니다
    private static void AddProbTarget(Dictionary<YutResult, float> bonus, YutResult result, float pct)
    {
        if (result == YutResult.BackDo || result == YutResult.Nak)
        {
            return;
        }
        bonus[result] = bonus.TryGetValue(result, out float v) ? v + pct : pct;
    }

    // 실제 4개 윷의 기본 면 분포 — 앞면 k개의 조합 수 C(4,k) / 16 (%)
    // 도(앞1)=25, 개(앞2)=37.5, 걸(앞3)=25, 윷(앞4)=6.25, 모(앞0)=6.25
    private static float BaseResultPercent(YutResult result)
    {
        int combos = result switch
        {
            YutResult.Do => 4,
            YutResult.Gae => 6,
            YutResult.Geol => 4,
            YutResult.Yut => 1,
            YutResult.Mo => 1,
            _ => 0,
        };
        return combos * 100f / 16f;
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
                    baseScore = entry.baseScore,
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
