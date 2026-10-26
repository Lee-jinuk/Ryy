// ──────────────────────────────────────────────
// FortuneUpgradeManager.cs — 복주머니 점괘 강화
//
// 담당: 강화 후보 추첨·선택 대기 상태·획득 횟수·누적 강화 적용·조회·런 초기화.
//
// 규칙 (사용자 확정):
//   - 복주머니 구매 성공 시 점괘 후보 3개 중 1개 선택 → 강화 1회 적용
//   - 같은 강화를 반복 획득할 수 있고, 보스 전환에도 유지되며 새 런에서 초기화
//   - 수치 강화: 윷 배율 +0.5 / 불 착지 배율 +0.5 / 물 연계 각 단계 +0.5 /
//     풀 도트 최초·추가 비율 각각 +0.5%p
//   - 확률 강화(윷 결과·속성 발생 +5%p)는 추첨 정책 미확정 — 데이터만 정의,
//     effectEnabled=0으로 후보 제외. 미완성 상태로 활성화하지 않는다
//   - 윷 배율 강화는 런타임 스탯 복사본에 적용 (방울 성장과 같은 경로 —
//     GameBalanceConfig 원본·설정 에셋 오염 없음)
//   - 불 착지 배율 강화는 이 클래스의 attrMultBonus에 별도 보관 —
//     화령방울 성장(BellManager.attrMultBonus)과 합산되어 적용
//   - 후보는 구매 성공 시 1회 추첨해 보관 — UI 갱신·재표시로 재추첨하지 않는다
//   - 선택 대기 중 상점 거래·리프레시·닫기·보스 진입 차단은 ShopManager·
//     YutGameManager가 isChoosingFortune을 검사해 처리한다 (2·3단계)
//
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class FortuneUpgradeManager
{
    private readonly YutGameManager gm;
    private readonly System.Random rng = new System.Random();

    // 강화별 획득 횟수 — upgradeId → 누적 선택 수 (기록·표시용, 런 단위 상태)
    public readonly Dictionary<string, int> acquiredCounts = new Dictionary<string, int>();

    // 복주머니 선택 대기 — 구매 성공 시 후보를 보관하고 선택까지 유지.
    // 상점 거래·리프레시·닫기·보스 진입 차단의 판정 기준 (미완료 구매 소실 방지)
    public bool isChoosingFortune;
    public readonly List<FortuneUpgradeData> pendingCandidates = new List<FortuneUpgradeData>();

    // 누적 강화 — (효과 종류, 대상) 기준 저장소. upgradeId가 아닌 kind+target으로 합산하므로
    // 같은 종류의 정의를 다른 ID로 추가해도 효과가 누적된다 (ID는 정의 식별·기록·표시 전용).
    // 대상 키: 윷 결과/속성은 해당 enum 정수값, 대상 없는 종류(물 연계·풀 도트)는 0.
    // 설정 에셋 원본과 분리된 런 상태 — 보스 전환 유지, 새 런 초기화
    private readonly Dictionary<FortuneUpgradeKind, Dictionary<int, float>> cumulativeBonus =
        new Dictionary<FortuneUpgradeKind, Dictionary<int, float>>();

    public FortuneUpgradeManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 획득 횟수 조회 — 정보판·디버그용
    public int GetCount(string upgradeId) =>
        acquiredCounts.TryGetValue(upgradeId, out int count) ? count : 0;

    // 누적 강화의 대상 키 — 대상 있는 종류는 enum 정수값, 대상 없는 종류는 0
    private static int TargetKey(FortuneUpgradeData data)
    {
        switch (data.kind)
        {
            case FortuneUpgradeKind.YutMult:
            case FortuneUpgradeKind.YutProb:
                return (int)data.targetResult;
            case FortuneUpgradeKind.AttrMult:
            case FortuneUpgradeKind.AttrProb:
                return (int)data.targetAttribute;
            default:
                return 0;
        }
    }

    // 누적 강화 조회 — 같은 kind·대상의 모든 정의가 합산된 값
    private float GetBonus(FortuneUpgradeKind kind, int targetKey) =>
        cumulativeBonus.TryGetValue(kind, out Dictionary<int, float> byTarget) &&
        byTarget.TryGetValue(targetKey, out float bonus) ? bonus : 0f;

    // 누적 강화 기록 — 적용 성공한 선택만 호출
    private void AddBonus(FortuneUpgradeData data)
    {
        if (!cumulativeBonus.TryGetValue(data.kind, out Dictionary<int, float> byTarget))
        {
            byTarget = new Dictionary<int, float>();
            cumulativeBonus[data.kind] = byTarget;
        }
        int key = TargetKey(data);
        byTarget[key] = (byTarget.TryGetValue(key, out float cur) ? cur : 0f) + data.amount;
    }

    // 복주머니 속성 착지 배율 강화분 — 화령방울 성장분(BellManager.attrMultBonus)과 별도로 합산해 적용
    public float GetAttrMultBonus(TileAttribute attr) =>
        GetBonus(FortuneUpgradeKind.AttrMult, (int)attr);

    // 물 연계 각 단계 배율 강화분 — 1회 강화 시 [1,2,4,8,16] → [1.5,2.5,4.5,8.5,16.5]
    public float GetWaterComboBonus() => GetBonus(FortuneUpgradeKind.WaterComboMult, 0);

    // 풀 도트 등록 비율 강화분(%p) — 최초·추가 비율에 같은 양을 합산, 이후 등록분부터 적용
    public float GetForestDotBonus() => GetBonus(FortuneUpgradeKind.ForestDotRate, 0);

    // 실제 적용 가능한 효과인지 — 활성 여부와 별개로 구현된 종류·유효 대상만 후보·적용에 사용한다.
    // 미구현 확률 강화(YutProb/AttrProb)·잘못된 대상(백도·낙 윷 배율, 불 외 속성 배율)은
    // effectEnabled여도 후보에 등장하지 않고 적용되지 않는다 — ValidateDataAssets가 경고한다
    internal static bool IsSupported(FortuneUpgradeData data)
    {
        if (data == null)
        {
            return false;
        }
        switch (data.kind)
        {
            case FortuneUpgradeKind.YutMult:
                return data.targetResult == YutResult.Do || data.targetResult == YutResult.Gae
                    || data.targetResult == YutResult.Geol || data.targetResult == YutResult.Yut
                    || data.targetResult == YutResult.Mo;   // 도·개·걸·윷·모만 — 백도·낙은 무효 대상
            case FortuneUpgradeKind.WaterComboMult:
            case FortuneUpgradeKind.ForestDotRate:
                return true;
            case FortuneUpgradeKind.AttrMult:
                return data.targetAttribute == TileAttribute.Fire;   // 불 착지 배율만 적용 경로 존재 — 물·풀 속성 배율은 미구현
            default:
                return false;   // YutProb·AttrProb 등 미구현 종류
        }
    }

    // 후보로 제시 가능한 강화 수 — 활성 + 적용 가능 종류·유효 대상. 상점 상품 추첨 가능 여부 공용
    public int SupportedCandidateCount()
    {
        if (gm.fortuneDatabase == null || gm.fortuneDatabase.upgrades == null)
        {
            return 0;
        }
        int count = 0;
        foreach (FortuneUpgradeData u in gm.fortuneDatabase.upgrades)
        {
            if (u != null && u.effectEnabled && IsSupported(u))
            {
                count++;
            }
        }
        return count;
    }

    // 점괘 후보 추첨 — 활성(effectEnabled)이면서 실제 적용 가능한 강화 중 후보 수만큼 무작위.
    // 한 후보 집합 안에서는 같은 강화를 뽑지 않는다 — 다음 구매의 새 후보에는 다시 등장 가능.
    // (집합 내 중복 허용 여부는 정책 확인 대상 — 확정 전까지 중복 없이 구성)
    public void RollCandidates(int count = 3)
    {
        pendingCandidates.Clear();
        List<FortuneUpgradeData> pool = new List<FortuneUpgradeData>();
        if (gm.fortuneDatabase != null && gm.fortuneDatabase.upgrades != null)
        {
            foreach (FortuneUpgradeData u in gm.fortuneDatabase.upgrades)
            {
                if (u != null && u.effectEnabled && IsSupported(u))
                {
                    pool.Add(u);
                }
            }
        }
        int picks = Mathf.Min(count, pool.Count);
        for (int i = 0; i < picks; i++)
        {
            int j = rng.Next(pool.Count);
            pendingCandidates.Add(pool[j]);
            pool.RemoveAt(j);
        }
    }

    // 복주머니 선택 시작 — 후보를 추첨하고 선택 대기 상태로 전환 (구매 성공 경로에서만 호출).
    // 이미 대기 중이면 재추첨·교체하지 않고, 유효 후보가 부족하면 대기 상태를 만들지 않는다.
    // 반환 false = 선택 준비 실패 — 호출 측은 돈 차감·구매 완료 처리를 하지 않는다
    public bool BeginChoice(int candidateCount = 3)
    {
        if (isChoosingFortune)
        {
            return false;   // 이미 선택 대기 중 — 재호출로 후보를 재추첨·교체하지 않는다
        }
        if (candidateCount <= 0)
        {
            Debug.LogWarning($"[YutGameManager] 점괘 후보 수 {candidateCount} — 0 이하의 후보 수로는 선택을 시작할 수 없습니다. ShopConfig.fortuneCandidateCount를 확인하세요.", gm);
            return false;
        }
        RollCandidates(candidateCount);
        if (pendingCandidates.Count < candidateCount)
        {
            int available = pendingCandidates.Count;
            pendingCandidates.Clear();
            Debug.LogWarning($"[YutGameManager] 점괘 후보 부족 — 활성 강화가 {available}개로 {candidateCount}택1을 구성할 수 없습니다. FortuneUpgradeDatabase의 effectEnabled를 확인하세요.", gm);
            return false;
        }
        isChoosingFortune = true;
        return true;
    }

    // 점괘 선택 확정 — 후보 인덱스 검증 후 강화를 정확히 1회 적용하고 대기 상태 해제.
    // 실패 시 상태를 바꾸지 않는다 (연타로 중복 적용 방지는 상태 해제가 담당)
    public bool ChooseUpgrade(int index)
    {
        if (!isChoosingFortune || index < 0 || index >= pendingCandidates.Count)
        {
            return false;
        }
        FortuneUpgradeData chosen = pendingCandidates[index];
        if (!ApplyUpgrade(chosen))
        {
            return false;   // 미구현 효과 등 — 선택창은 유지 (취소 경로 없이 다른 후보 선택 가능)
        }
        int total = GetCount(chosen.upgradeId);
        pendingCandidates.Clear();
        isChoosingFortune = false;
        Debug.Log($"점괘 강화 선택: {chosen.upgradeName} — 누적 {total}회");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 선택 대기 상태 해제 — 후보 정리 (선택 취소는 제공하지 않는다 — 구매는 확정됨).
    // 구매 실패 환불 경로·런 초기화에서 사용 — 대기 상태만 닫는 전용 경로
    public void ClearChoice()
    {
        pendingCandidates.Clear();
        isChoosingFortune = false;
    }

    // 강화 1회 적용 — 누적 저장소에 기록하고 종류별 추가 경로를 실행한다.
    // 미구현 확률 강화는 false 반환. 같은 kind의 다른 ID 데이터도 자동으로 누적된다
    private bool ApplyUpgrade(FortuneUpgradeData data)
    {
        switch (data.kind)
        {
            case FortuneUpgradeKind.YutMult:
                // 런타임 윷 스탯 복사본에 적용 — 도걸·윷령방울 성장과 같은 경로, 설정 원본 미변경
                gm.rollManager.GetStat(data.targetResult).mult += data.amount;
                break;
            case FortuneUpgradeKind.AttrMult:
                if (data.targetAttribute != TileAttribute.Fire &&
                    data.targetAttribute != TileAttribute.Water &&
                    data.targetAttribute != TileAttribute.Forest)
                {
                    return false;   // 공격 속성 외 대상은 받아들이지 않는다
                }
                break;
            case FortuneUpgradeKind.WaterComboMult:
            case FortuneUpgradeKind.ForestDotRate:
                break;   // 누적 저장소 기록만으로 적용 — GetWaterComboBonus·GetForestDotBonus가 조회
            default:
                Debug.LogWarning($"[YutGameManager] 점괘 강화 {data.upgradeId}는 미구현 효과입니다 — 적용하지 않습니다.", gm);
                return false;
        }
        AddBonus(data);
        acquiredCounts[data.upgradeId] = GetCount(data.upgradeId) + 1;
        return true;
    }

    // 런 초기화 — 획득 횟수·누적 강화·선택 대기 전부 정리.
    // 윷 배율 강화는 런타임 스탯이 BuildYutStats에서 함께 초기화된다
    public void ResetForNewRun()
    {
        acquiredCounts.Clear();
        ClearChoice();
        cumulativeBonus.Clear();
    }
}
