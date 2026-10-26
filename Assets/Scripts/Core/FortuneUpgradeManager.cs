// ──────────────────────────────────────────────
// FortuneUpgradeManager.cs — 복주머니 점괘 강화
//
// 담당: 강화 후보 추첨·선택 대기 상태·획득 횟수·누적 강화 적용·조회·런 초기화.
//
// 규칙 (사용자 확정):
//   - 복주머니 구매 성공 시 점괘 후보 3개 중 1개 선택 → 강화 1회 적용
//   - 같은 강화를 반복 획득할 수 있고, 보스 전환에도 유지되며 새 런에서 초기화
//   - 수치 강화: 윷 배율 +0.5 / 불 착지 배율 +0.5 / 물 연계 각 단계 +0.5 /
//     풀 도트 최초·추가 비율 각각 +0.5%p / 윷 기본점수 +5
//   - 확률 강화: 최초 투척 결과 +5%p(비대상 기존 비율 차감)·속성 칸 발생 +5%p
//     (이후 말판 생성부터) — 2026-10-27 정책 확정으로 구현·활성화
//   - 재투척 면 강화: 앞면·뒷면 각 +2%p — 양면령 기본값에 보정 후 20~80% 제한
//   - 상점 할인: +5%p, 누적 50% 상한 — 획득 상점이 아닌 다음 보스 처치 후부터 적용
//   - 윷 배율·기본점수 강화는 런타임 스탯 복사본에 적용 (방울 성장과 같은 경로 —
//     GameBalanceConfig 원본·설정 에셋 오염 없음)
//   - 불 착지 배율 강화는 이 클래스의 attrMultBonus에 별도 보관 —
//     설정 기본 배율과 합산되어 적용 (TileEffectManager.CurrentAttrMult가 유일한 조회 경로)
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
            case FortuneUpgradeKind.YutBaseScore:
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

    // 복주머니 속성 착지 배율 강화분 — 설정 기본 배율과 별도로 합산해 적용
    public float GetAttrMultBonus(TileAttribute attr) =>
        GetBonus(FortuneUpgradeKind.AttrMult, (int)attr);

    // 물 연계 각 단계 배율 강화분 — 1회 강화 시 [1,2,4,8,16] → [1.5,2.5,4.5,8.5,16.5]
    public float GetWaterComboBonus() => GetBonus(FortuneUpgradeKind.WaterComboMult, 0);

    // 풀 도트 등록 비율 강화분(%p) — 최초·추가 비율에 같은 양을 합산, 이후 등록분부터 적용
    public float GetForestDotBonus() => GetBonus(FortuneUpgradeKind.ForestDotRate, 0);

    // 최초 투척 결과 확률 강화분(%p) — 대상 결과별 누적치. YutRollManager의 보정 분포에 합산
    public float GetYutProbBonus(YutResult result) =>
        GetBonus(FortuneUpgradeKind.YutProb, (int)result);

    // 속성 칸 발생 확률 강화분(%p) — 대상 속성별 누적치. 이후 말판 생성 추첨에 합산
    public float GetAttrProbBonus(TileAttribute attr) =>
        GetBonus(FortuneUpgradeKind.AttrProb, (int)attr);

    // 재투척 앞면·뒷면 확률 강화분(%p) — 앞면 − 뒷면 순증가분이 최종 보정치
    public float GetRerollFrontBonus() => GetBonus(FortuneUpgradeKind.RerollFrontProb, 0);
    public float GetRerollBackBonus() => GetBonus(FortuneUpgradeKind.RerollBackProb, 0);

    // 상점 할인 — 누적 획득분과 현재 적용분을 구분한다.
    // 누적은 선택 즉시 쌓이고, 적용은 획득 이후 첫 보스 처치(OnBossCleared)에서 이전된다
    private float shopDiscountActive;   // 현재 구매가 계산에 적용 중인 할인율(%)

    // 누적 획득 할인율(%) — 적용 대기분 포함, 설정 상한으로 제한
    public float ShopDiscountPending =>
        Mathf.Min(GetBonus(FortuneUpgradeKind.ShopDiscount, 0), ShopDiscountMaxPercent);

    // 현재 적용 할인율(%) — 구매가 계산의 유일한 기준 (가격 표시·구매 판정·차감 공용)
    public float ShopDiscountPercent => shopDiscountActive;

    // 할인율 상한(%) — ShopConfig 설정, 기본 50
    private float ShopDiscountMaxPercent =>
        gm.shopConfig != null ? Mathf.Max(0f, gm.shopConfig.shopDiscountMaxPercent) : 50f;

    // 보스 처치 — 적용 대기 중인 누적 할인을 현재 적용 할인으로 이전한다.
    // EnterClear에서만 호출 (건너뛰기·상점 재진입·점괘 창 종료로는 활성화되지 않는다).
    // 같은 값 재대입은 무해하므로 중복 호출 방지 상태가 필요 없다
    public void OnBossCleared()
    {
        shopDiscountActive = ShopDiscountPending;
    }

    // 실제 적용 가능한 효과인지 — 활성 여부와 별개로 구현된 종류·유효 대상만 후보·적용에 사용한다.
    // 잘못된 대상(백도·낙 윷 스탯, 지원 외 속성)은 effectEnabled여도 후보에 등장하지 않고
    // 적용되지 않는다 — ValidateDataAssets가 경고한다
    internal static bool IsSupported(FortuneUpgradeData data)
    {
        if (data == null)
        {
            return false;
        }
        switch (data.kind)
        {
            case FortuneUpgradeKind.YutMult:
            case FortuneUpgradeKind.YutBaseScore:
            case FortuneUpgradeKind.YutProb:
                return data.targetResult == YutResult.Do || data.targetResult == YutResult.Gae
                    || data.targetResult == YutResult.Geol || data.targetResult == YutResult.Yut
                    || data.targetResult == YutResult.Mo;   // 도·개·걸·윷·모만 — 백도·낙은 무효 대상
            case FortuneUpgradeKind.WaterComboMult:
            case FortuneUpgradeKind.ForestDotRate:
            case FortuneUpgradeKind.RerollFrontProb:
            case FortuneUpgradeKind.RerollBackProb:
            case FortuneUpgradeKind.ShopDiscount:
                return true;
            case FortuneUpgradeKind.AttrMult:
                return data.targetAttribute == TileAttribute.Fire;   // 불 착지 배율만 적용 경로 존재 — 물·풀 속성 배율은 미구현
            case FortuneUpgradeKind.AttrProb:
                return data.targetAttribute == TileAttribute.Fire
                    || data.targetAttribute == TileAttribute.Water
                    || data.targetAttribute == TileAttribute.Forest;   // 발생 확률 추첨 대상 속성 — 땅·쇠는 미지원
            default:
                return false;
        }
    }

    // 후보로 제시 가능한 강화 수 — 활성 + 적용 가능 종류·유효 대상 + 현재 상태에서 실제 효과 있음.
    // 상점 상품 추첨 가능 여부와 실제 후보 구성이 같은 판정을 공용한다
    public int SupportedCandidateCount()
    {
        if (gm.fortuneDatabase == null || gm.fortuneDatabase.upgrades == null)
        {
            return 0;
        }
        int count = 0;
        foreach (FortuneUpgradeData u in gm.fortuneDatabase.upgrades)
        {
            if (u != null && u.effectEnabled && IsSupported(u) && HasEffect(u))
            {
                count++;
            }
        }
        return count;
    }

    // 현재 상태에서 선택 시 실제 변화가 생기는지 — IsSupported와 별개의 상태 판정.
    // 순수 조회 — 스탯·돈·확률·추첨 상태를 바꾸지 않는다. UI 노출 판정과 실제 후보
    // 구성이 같은 경로를 쓰므로 '표시됐는데 효과 없음'이 발생하지 않는다
    private bool HasEffect(FortuneUpgradeData data)
    {
        switch (data.kind)
        {
            case FortuneUpgradeKind.AttrProb:
                // 다음 보스 말판 생성 시 실제 발생 확률이 바뀌는지 — 배치 추첨과 같은 규칙으로 가상 비교
                return gm.boardManager.WouldChangeAttrWeights(data.targetAttribute, data.amount);
            case FortuneUpgradeKind.YutProb:
                // 실제 보정 분포가 바뀌는지 — 방울 보정과 같은 경로로 가상 적용해 비교
                return gm.rollManager.WouldChangeInitialWeights(data.targetResult, data.amount);
            case FortuneUpgradeKind.RerollFrontProb:
            case FortuneUpgradeKind.RerollBackProb:
            {
                // 양면령 기본값 + 누적 보정의 클램프 결과가 바뀌는지 — 상한 도달 시 해당 방향은 무료
                float baseChance = gm.rollManager.RerollFrontBaseChance();
                float delta = GetRerollFrontBonus() - GetRerollBackBonus();
                float sign = data.kind == FortuneUpgradeKind.RerollFrontProb ? 1f : -1f;
                float now = Mathf.Clamp(baseChance + delta / 100f, 0.2f, 0.8f);
                float next = Mathf.Clamp(baseChance + (delta + sign * data.amount) / 100f, 0.2f, 0.8f);
                return next != now;
            }
            case FortuneUpgradeKind.ShopDiscount:
                return ShopDiscountPending < ShopDiscountMaxPercent;   // 누적 상한 도달 시 효과 없음
            default:
                return true;   // 윷 배율·기본점수·속성 배율·물 연계·도트 — 반복 획득 시 항상 유효
        }
    }

    // 점괘 후보 추첨 — 활성(effectEnabled)·적용 가능·현재 상태에서 실제 효과가 있는
    // 강화 중 후보 수만큼 무작위. 한 후보 집합 안에서는 같은 강화를 뽑지 않는다 —
    // 다음 구매의 새 후보에는 다시 등장 가능 (선택 대기 중 재추첨 없음)
    public void RollCandidates(int count = 3)
    {
        pendingCandidates.Clear();
        List<FortuneUpgradeData> pool = new List<FortuneUpgradeData>();
        if (gm.fortuneDatabase != null && gm.fortuneDatabase.upgrades != null)
        {
            foreach (FortuneUpgradeData u in gm.fortuneDatabase.upgrades)
            {
                if (u != null && u.effectEnabled && IsSupported(u) && HasEffect(u))
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
                // 런타임 윷 스탯 복사본에 적용 — 설정 원본은 미변경
                gm.rollManager.GetStat(data.targetResult).mult += data.amount;
                break;
            case FortuneUpgradeKind.YutBaseScore:
                // 기본점수 강화 — 배율과는 별도 값에 같은 런타임 스탯 경로로 적용
                gm.rollManager.GetStat(data.targetResult).baseScore += Mathf.RoundToInt(data.amount);
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
            case FortuneUpgradeKind.YutProb:
            case FortuneUpgradeKind.AttrProb:
            case FortuneUpgradeKind.RerollFrontProb:
            case FortuneUpgradeKind.RerollBackProb:
            case FortuneUpgradeKind.ShopDiscount:
                break;   // 누적 저장소 기록만으로 적용 — 각 조회 경로(GetYutProbBonus·
                         // GetAttrProbBonus·GetRerollFront/BackBonus·ShopDiscountPending)가 계산 시점에 반영
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
        shopDiscountActive = 0f;   // 누적은 cumulativeBonus와 함께 소거, 적용분도 초기화
    }
}
