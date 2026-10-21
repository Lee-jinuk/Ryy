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
    public readonly List<BellData> offers = new List<BellData>();   // 현재 상품 후보
    public readonly HashSet<int> soldOfferIndices = new HashSet<int>();   // 판매 완료된 후보 인덱스

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
        RollOffers();
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

    // 상품 후보 추첨 — 구매 가능한 정의(effectEnabled) 중 미보유 종류에서 무작위 shopOfferCount종
    private void RollOffers()
    {
        offers.Clear();
        soldOfferIndices.Clear();
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
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }
        int count = Mathf.Min(gm.shopConfig != null ? gm.shopConfig.shopOfferCount : 4, pool.Count);
        offers.AddRange(pool.GetRange(0, count));
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

    // 보유 방울 판매(int): 보유 목록 순번 → 인스턴스 ID 기준 제거 + 판매가 지급을 한 번에 처리
    public bool SellBell(int ownedIndex)
    {
        if (!isOpen || ownedIndex < 0 || ownedIndex >= gm.bellManager.ownedBells.Count)
        {
            return false;
        }
        OwnedBell bell = gm.bellManager.ownedBells[ownedIndex];
        int instanceId = bell.instanceId;
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

    // 런 초기화 — 상점 상태 정리
    public void ResetForNewRun()
    {
        isOpen = false;
        offers.Clear();
        soldOfferIndices.Clear();
    }
}
