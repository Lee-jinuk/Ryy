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
    public readonly List<BellData> offers = new List<BellData>();   // 현재 상품 후보 — 보스 구간 동안 유지
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

    // '상점' 버튼 — 보스 소개 화면에서 진입. 상품 후보는 진입마다 다시 추첨
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
        if (offers.Count == 0)
        {
            RollOffers();   // 목록은 보스 구간 동안 유지 — 이미 있으면 닫기·재진입으로 무료 갱신되지 않는다 (사용자 확정)
        }
        isOpen = true;
        GameUIManager.SetPanel(gm.shopPanel, true);
        gm.uiManager.UpdateUI();
        Debug.Log($"--- 상점 진입 — 보유 {gm.economyManager.money}냥, 상품 {offers.Count}종 ---");
    }

    // '상점 닫기' 버튼 — 보스 소개 화면으로 복귀 (bossSelectPanel은 유지되어 있음)
    public void CloseShop()
    {
        if (!isOpen)
        {
            return;
        }
        isOpen = false;
        GameUIManager.SetPanel(gm.shopPanel, false);
        gm.uiManager.UpdateUI();
    }

    // 상품 후보 추첨 — 구매 가능한 정의(effectEnabled) 중 미보유 종류에서 무작위 shopOfferCount종.
    // 판매 완료 표시를 초기화하고 전체를 새로 구성한다 (리프레시·보스 구간 첫 추첨 공용)
    private void RollOffers()
    {
        offers.Clear();
        soldOfferIndices.Clear();
        List<BellData> pool = BuildCandidatePool();
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Min(gm.shopConfig != null ? gm.shopConfig.shopOfferCount : 4, pool.Count);
        offers.AddRange(pool.GetRange(0, count));
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

    // 리프레시 가능 — 상점 열림 + 후보 풀이 존재. 후보가 전혀 없으면 차감 없이 실패한다
    public bool CanRefresh => isOpen && BuildCandidatePool().Count > 0;

    // '새로고침' 버튼 — 비용 차감 후 상품 전체(구매 완료 칸 포함)를 새로 구성하고 비용을 증가시킨다.
    // 잔액 부족·상점 닫힘·후보 없음이면 돈·상품·비용을 바꾸지 않는다
    public bool RefreshShop()
    {
        if (!isOpen)
        {
            return false;
        }
        if (BuildCandidatePool().Count == 0)
        {
            Debug.Log("상점 리프레시 실패 — 구매 가능한 방울 후보가 없습니다 (전부 보유 중이거나 비활성).");
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

    // 상품 구매(int): index = 상품 목록 순번 — 잔액·한도·중복·판매 완료를 검사하고 한 번에 처리
    public bool BuyOffer(int index)
    {
        if (!isOpen || index < 0 || index >= offers.Count || soldOfferIndices.Contains(index))
        {
            return false;
        }
        BellData offer = offers[index];
        // 장착 한도·중복은 TryAddBell이 최종 검사 — 잔액 부족이면 차감 없이 실패
        if (!gm.economyManager.TrySpend(offer.buyPrice))
        {
            Debug.Log($"상점 구매 실패: {offer.bellName} — 잔액 부족 ({gm.economyManager.money}냥 < {offer.buyPrice}냥)");
            return false;
        }
        if (!gm.bellManager.TryAddBell(offer))
        {
            // 한도·중복 실패 — 차감된 돈을 되돌려 거래를 원자적으로 되돌린다
            gm.economyManager.AddMoney(offer.buyPrice);
            Debug.Log($"상점 구매 실패: {offer.bellName} — 장착 불가 (한도 {gm.bellManager.MaxBells} 또는 중복 종류)");
            gm.uiManager.UpdateUI();
            return false;
        }
        soldOfferIndices.Add(index);   // 같은 상품 재구매 차단 — 연타도 여기서 막힌다
        Debug.Log($"상점 구매: {offer.bellName} — {offer.buyPrice}냥 (잔액 {gm.economyManager.money}냥)");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 보유 방울 판매 — 인스턴스 ID 기준 제거 + 판매가 지급을 한 번에 처리.
    // 목록 순서가 바뀌어도 ID로 대상을 찾으므로 다른 방울이 팔리지 않는다
    public bool SellBellInstance(int instanceId)
    {
        if (!isOpen)
        {
            return false;
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
        offers.Clear();
        soldOfferIndices.Clear();
        refreshCount = 0;
    }

    // 런 초기화 — 상점 상태 정리
    public void ResetForNewRun() => ResetForBoss();
}
