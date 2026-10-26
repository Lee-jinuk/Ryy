// ──────────────────────────────────────────────
// ShopManager.cs — 방울 상점
//
// 담당: 상점 열기/닫기, 무작위 상품 후보 구성, 구매·판매 거래.
//
// 규칙 (사용자 확정):
//   - 진입 조건: 보스 소개 화면이 열려 있고 GameState.Cleared — 첫 보스전 이전에는 이용 불가
//   - 상품 = effectEnabled 방울 중 무작위 shopOfferCount종 (이미 보유한 종류는 제외)
//   - 구매: 잔액·장착 한도·중복·판매 완료 검사 후 돈 차감+획득을 한 번에 처리
//   - 판매: 보유 인스턴스 ID 기준 — 방울 제거+판매가 지급을 한 번에 처리
//   - 실패한 거래는 돈·상품 어느 쪽도 변경하지 않는다
//   - 상점이 열린 동안 보스전 진입·건너뛰기는 차단 (YutGameManager가 검사)
//   - 상품 목록은 보스 구간 동안 유지 — 상점을 닫아도 같은 목록 (사용자 확정)
//   - 리프레시: 비용 2냥부터 성공마다 2배 (ShopConfig), 보스 전환 시 2냥으로 초기화 (사용자 확정)
//   - 판매한 종류의 재구매는 새 인스턴스 — 발동 횟수는 새 상품 기준으로 초기화 (사용자 확정)
//
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class ShopManager
{
    private readonly YutGameManager gm;
    private readonly System.Random rng = new System.Random();

    public bool isOpen;
    public bool offersGenerated;    // 이 보스 구간에 상품 목록을 만들었는지 — 상품 0종인 빈 목록도 '생성됨'으로 유지
    public readonly List<ShopOffer> offers = new List<ShopOffer>();   // 현재 상품 후보 — 방울·복주머니 혼합, 보스 구간 동안 유지
    public readonly HashSet<int> soldOfferIndices = new HashSet<int>();   // 판매 완료된 후보 인덱스
    public int refreshCount;    // 이번 보스 구간의 성공한 리프레시 횟수 — 다음 비용 = 시작 비용 × 배수^횟수

    public ShopManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 상점 이용 가능 조건 — 보스 소개 화면 표시 중 + Cleared 상태 (첫 보스전 이전 Start 상태 차단)
    public bool CanUseShop =>
        !isOpen &&
        gm.currentState == GameState.Cleared &&
        gm.bossSelectPanel != null && gm.bossSelectPanel.activeSelf;

    // '상점' 버튼 — 보스 소개 화면에서 진입. 상품 목록은 보스 구간 동안 유지 (첫 진입에만 추첨)
    public void OpenShop()
    {
        if (!CanUseShop)
        {
            return;
        }
        if (gm.shopPanel == null)
        {
            Debug.LogWarning("[YutGameManager] shopPanel 미연결 — 상점을 표시할 수 없습니다. 씬에 Shop Panel을 만들어 연결하세요.", gm);
            return;
        }
        if (!offersGenerated)
        {
            RollOffers();   // 목록은 보스 구간 동안 유지 — 빈 목록도 생성된 상태이므로 닫기·재진입으로 무료 갱신되지 않는다
        }
        isOpen = true;
        GameUIManager.SetPanel(gm.shopPanel, true);
        gm.uiManager.UpdateUI();
        Debug.Log($"--- 상점 진입 — 보유 {gm.economyManager.money}냥, 상품 {offers.Count}종 ---");
    }

    // '상점 닫기' 버튼 — 보스 소개 화면으로 복귀 (bossSelectPanel은 유지되어 있음)
    public void CloseShop()
    {
        if (!isOpen || gm.fortuneManager.isChoosingFortune)
        {
            return;   // 점괘 선택 대기 중 닫기 차단 — 미완료 구매가 사라지지 않게 한다
        }
        isOpen = false;
        GameUIManager.SetPanel(gm.shopPanel, false);
        gm.uiManager.UpdateUI();
    }

    // 상품 후보 추첨 — 슬롯당 방울·복주머니를 fortunePouchChance 확률로 독립 추첨 (구성 보장 없음).
    // 방울 칸은 미보유·활성 후보에서 목록 내 중복 없이 뽑고, 방울 후보가 없으면 그 칸은 비운다
    // (복주머니 대체 여부는 미확정 — 임의 확정하지 않음). 판매 완료 표시를 초기화하고 전체를 새로 구성
    private void RollOffers()
    {
        offers.Clear();
        soldOfferIndices.Clear();
        List<BellData> pool = BuildCandidatePool();
        float pouchChance = gm.shopConfig != null ? Mathf.Clamp01(gm.shopConfig.fortunePouchChance) : 0.5f;
        int pouchPrice = gm.shopConfig != null ? gm.shopConfig.fortunePouchPrice : 20;
        int slots = gm.shopConfig != null ? gm.shopConfig.shopOfferCount : 4;
        for (int i = 0; i < slots; i++)
        {
            if (rng.NextDouble() < pouchChance)
            {
                offers.Add(ShopOffer.FortunePouch(pouchPrice));
                continue;
            }
            if (pool.Count == 0)
            {
                continue;   // 방울 후보 없음 — 해당 칸은 비움 (미확정 정책, 대체 여부는 사용자 결정 대기)
            }
            int j = rng.Next(pool.Count);
            offers.Add(ShopOffer.Bell(pool[j]));
            pool.RemoveAt(j);
        }
        offersGenerated = true;   // 빈 목록도 유효한 생성 결과 — 재진입·판매 후 재추첨으로 새 상품을 만들지 않는다
    }

    // 후보 풀 — 활성 방울 중 현재 미보유 종류 전부 (판매한 종류는 미보유이므로 다시 포함된다).
    // 같은 종류가 목록 안에 중복되지 않는 것은 중복 장착 금지 규칙이 보장한다
    private List<BellData> BuildCandidatePool()
    {
        List<BellData> pool = new List<BellData>();
        if (gm.bellDatabase != null && gm.bellDatabase.bells != null)
        {
            foreach (BellData bell in gm.bellDatabase.bells)
            {
                if (bell == null || !bell.effectEnabled || gm.bellManager.OwnsKind(bell.bellId))
                {
                    continue;
                }
                pool.Add(bell);
            }
        }
        return pool;
    }

    // 현재 리프레시 비용 — 시작 비용 × 배수^성공 횟수. 오버플로는 int 최대값으로 제한해 음수·무료 방지
    public int CurrentRefreshCost
    {
        get
        {
            int baseCost = gm.shopConfig != null ? Mathf.Max(0, gm.shopConfig.refreshBaseCost) : 2;
            int mult = gm.shopConfig != null ? Mathf.Max(1, gm.shopConfig.refreshCostMultiplier) : 2;
            long cost = baseCost;
            for (int i = 0; i < refreshCount; i++)
            {
                cost *= mult;
                if (cost > int.MaxValue)
                {
                    return int.MaxValue;
                }
            }
            return (int)cost;
        }
    }

    // 점괘 선택창이 후보 전부를 표시·선택할 수 있는 구성인지 — 구매 거부와 상품 추첨 가능 여부가
    // 같은 판정을 사용한다. warn=true면 거부 사유를 로그로 남긴다 (구매 경로 전용)
    private bool FortuneChoiceReady(int need, bool warn)
    {
        if (gm.fortuneChoicePanel == null)
        {
            if (warn) Debug.LogWarning("[YutGameManager] fortuneChoicePanel 미연결 — 돈이 차감되고 선택할 수 없는 상태를 막기 위해 구매를 거부합니다.", gm);
            return false;
        }
        if (gm.fortuneOptionSlots == null || gm.fortuneOptionSlots.Length < need)
        {
            if (warn) Debug.LogWarning($"[YutGameManager] fortuneOptionSlots {gm.fortuneOptionSlots?.Length ?? 0}/{need} — 후보를 전부 표시할 수 없어 구매를 거부합니다.", gm);
            return false;
        }
        for (int i = 0; i < need; i++)
        {
            BellSlotUI slot = gm.fortuneOptionSlots[i];
            if (slot == null || slot.actionButton == null)
            {
                if (warn) Debug.LogWarning($"[YutGameManager] 점괘 슬롯 {i + 1}번 미연결(슬롯 또는 선택 버튼 없음) — 선택 불가 상태를 막기 위해 구매를 거부합니다.", gm);
                return false;
            }
        }
        return true;
    }

    // 복주머니가 실제 판매 가능한 상품인지 — 출현 확률·선택창 구성·후보 수를 구매와 같은 기준으로 검사
    private bool CanSellFortunePouch()
    {
        int need = gm.shopConfig != null ? gm.shopConfig.fortuneCandidateCount : 3;
        float pouchChance = gm.shopConfig != null ? Mathf.Clamp01(gm.shopConfig.fortunePouchChance) : 0.5f;
        return need > 0 && pouchChance > 0f && gm.fortuneDatabase != null
            && FortuneChoiceReady(need, false)
            && gm.fortuneManager.SupportedCandidateCount() >= need;
    }

    // 실제로 생성 가능한 상품이 있는지 — 상품 종류별 출현 확률과 후보 유효성을 함께 검사한다.
    // 복주머니: CanSellFortunePouch / 방울: 출현 확률 > 0(복주머니 확률 < 1) + 미보유·활성 후보 존재
    private bool CanGenerateOffers()
    {
        float pouchChance = gm.shopConfig != null ? Mathf.Clamp01(gm.shopConfig.fortunePouchChance) : 0.5f;
        bool bellPossible = pouchChance < 1f && BuildCandidatePool().Count > 0;
        return CanSellFortunePouch() || bellPossible;
    }

    // 리프레시 가능 — 상점 열림 + 점괘 선택 중이 아님 + 실제 생성 가능 상품이 있을 때
    public bool CanRefresh =>
        isOpen && !gm.fortuneManager.isChoosingFortune && CanGenerateOffers();

    // '새로고침' 버튼 — 비용 차감 후 상품 전체(구매 완료 칸 포함)를 새로 구성하고 비용을 증가시킨다.
    // 잔액 부족·상점 닫힘·선택 대기 중·후보 없음이면 돈·상품·비용을 바꾸지 않는다
    public bool RefreshShop()
    {
        if (!isOpen || gm.fortuneManager.isChoosingFortune)
        {
            return false;   // 점괘 선택 대기 중 — 미완료 구매를 새 상품으로 덮지 않는다
        }
        if (!CanGenerateOffers())   // CanRefresh와 같은 판정 — 생성 가능 상품이 없으면 차감 없이 실패
        {
            Debug.Log("상점 리프레시 실패 — 생성 가능한 상품이 없습니다 (방울 후보 없음 + 복주머니 출현 불가 또는 선택 가능 강화 없음).");
            return false;
        }
        int cost = CurrentRefreshCost;
        if (!gm.economyManager.TrySpend(cost))
        {
            Debug.Log($"상점 리프레시 실패 — 잔액 부족 ({gm.economyManager.money}냥 < {cost}냥)");
            return false;
        }
        refreshCount++;
        RollOffers();
        Debug.Log($"상점 리프레시: -{cost}냥 → 상품 {offers.Count}종 (잔액 {gm.economyManager.money}냥, 다음 비용 {CurrentRefreshCost}냥)");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 상품 구매(int): index = 상품 목록 순번 — 상품 종류별로 방울·복주머니 경로를 분기한다.
    // 점괘 선택 대기 중에는 어떤 상품도 구매하지 않는다 (미완료 구매 보호)
    public bool BuyOffer(int index)
    {
        if (!isOpen || index < 0 || index >= offers.Count || soldOfferIndices.Contains(index)
            || gm.fortuneManager.isChoosingFortune)
        {
            return false;
        }
        ShopOffer offer = offers[index];
        if (offer.kind == ShopOfferKind.FortunePouch)
        {
            return BuyFortunePouch(index, offer);
        }
        BellData bell = offer.bell;
        // 장착 한도·중복은 TryAddBell이 최종 검사 — 잔액 부족이면 차감 없이 실패
        if (!gm.economyManager.TrySpend(offer.price))
        {
            Debug.Log($"상점 구매 실패: {bell.bellName} — 잔액 부족 ({gm.economyManager.money}냥 < {offer.price}냥)");
            return false;
        }
        if (!gm.bellManager.TryAddBell(bell))
        {
            // 한도·중복 실패 — 차감된 돈을 되돌려 거래를 원자적으로 되돌린다
            gm.economyManager.AddMoney(offer.price);
            Debug.Log($"상점 구매 실패: {bell.bellName} — 장착 불가 (한도 {gm.bellManager.MaxBells} 또는 중복 종류)");
            gm.uiManager.UpdateUI();
            return false;
        }
        soldOfferIndices.Add(index);   // 같은 상품 재구매 차단 — 연타도 여기서 막힌다
        Debug.Log($"상점 구매: {bell.bellName} — {offer.price}냥 (잔액 {gm.economyManager.money}냥)");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 복주머니 구매 — 장착 한도와 무관하게 구매 가능. 점괘 후보를 먼저 확보하고
    // 후보·선택창 준비가 안 되면 돈을 차감하지 않는다 (미완료 구매 방지)
    private bool BuyFortunePouch(int index, ShopOffer offer)
    {
        if (gm.fortuneDatabase == null)
        {
            Debug.LogWarning("[YutGameManager] FortuneUpgradeDatabase 미연결 — 복주머니를 구매할 수 없습니다.", gm);
            return false;
        }
        // 후보 전부를 표시·선택할 수 있는 슬롯 구성인지 — 상품 추첨 가능 여부와 같은 판정 공용
        int need = gm.shopConfig != null ? gm.shopConfig.fortuneCandidateCount : 3;
        if (!FortuneChoiceReady(need, true))
        {
            return false;
        }
        // 점괘 후보 사전 검증 — 차감 전에 유효 후보 3개를 확보할 수 있는지 확인
        if (!gm.fortuneManager.BeginChoice(need))
        {
            return false;   // 후보 부족 — 돈·상품·선택 상태 모두 보존 (사유는 BeginChoice가 로그)
        }
        if (!gm.economyManager.TrySpend(offer.price))
        {
            gm.fortuneManager.ClearChoice();   // 잔액 부족 — 추첨한 후보만 정리하고 대기 상태 해제
            Debug.Log($"상점 구매 실패: 복주머니 — 잔액 부족 ({gm.economyManager.money}냥 < {offer.price}냥)");
            return false;
        }
        soldOfferIndices.Add(index);
        Debug.Log($"상점 구매: 복주머니 — {offer.price}냥 (잔액 {gm.economyManager.money}냥) → 점괘 {gm.fortuneManager.pendingCandidates.Count}택1");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 보유 방울 판매 — 인스턴스 ID 기준 제거 + 판매가 지급을 한 번에 처리.
    // 목록 순서가 바뀌어도 ID로 대상을 찾으므로 다른 방울이 팔리지 않는다
    public bool SellBellInstance(int instanceId)
    {
        if (!isOpen || gm.fortuneManager.isChoosingFortune)
        {
            return false;   // 점괘 선택 대기 중 판매 차단 — 미완료 구매를 상태 변화로 덮지 않는다
        }
        OwnedBell bell = gm.bellManager.ownedBells.Find(b => b.instanceId == instanceId);
        if (bell == null)
        {
            return false;
        }
        int sellPrice = bell.sellPrice;
        if (!gm.bellManager.RemoveBell(instanceId))
        {
            return false;
        }
        gm.economyManager.AddMoney(sellPrice);
        Debug.Log($"상점 판매: {bell.bellName} — +{sellPrice}냥 (잔액 {gm.economyManager.money}냥)");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 보스 전환 — 상품 목록·판매 완료 표시·리프레시 비용을 초기화한다 (사용자 확정:
    // 같은 보스 구간에서는 누적, 다음 보스 소개 시 2냥부터 다시 시작)
    public void ResetForBoss()
    {
        isOpen = false;
        offersGenerated = false;
        offers.Clear();
        soldOfferIndices.Clear();
        refreshCount = 0;
    }

    // 런 초기화 — 상점 상태 정리
    public void ResetForNewRun() => ResetForBoss();
}
