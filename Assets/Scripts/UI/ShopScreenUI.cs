using UnityEngine;
using UnityEngine.UI;

// 상점 화면 UI — 구매 4칸·장착/판매 최대 8칸·돈·새로고침 표시.
// 표시 전용 (가격 계산·구매 판정·돈 차감은 ShopManager·EconomyManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.shopPanel·shopMoneyText·shopOfferSlots·shopBellSlots·refreshButton·refreshCostText
public class ShopScreenUI : MonoBehaviour
{
    public GameObject shopPanel;                            // 상점 패널 — 보스 소개 화면에서 진입 (ClearPanel 자식으로 두지 말 것)
    public Text shopMoneyText;                              // 상점 내 보유 냥 표시 (미연결 시 무시)
    public BellSlotUI[] shopOfferSlots = new BellSlotUI[4]; // 상점 구매 상품 슬롯 — 고정 4칸, 구매 버튼 포함
    public BellSlotUI[] shopBellSlots = new BellSlotUI[8];  // 상점 장착/판매 슬롯 — 최대 8칸(보유 수만큼 채움), 판매 버튼 포함
    public Button refreshButton;                            // 상점 리프레시 버튼 — OnClick = RefreshShop (미연결 시 무시)
    public Text refreshCostText;                            // 다음 리프레시 비용 표시 — "새로고침 N냥" (미연결 시 무시)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 상점 표시 동기화 — 고정 슬롯(shopOfferSlots·shopBellSlots) + 잔액 + 리프레시 비용.
    // 슬롯의 버튼 리스너는 Bind가 매번 교체해 누적되지 않는다
    public void Refresh(YutGameManager gm)
    {
        bool open = gm.shopManager.isOpen;
        bool choosing = gm.fortuneManager.isChoosingFortune;   // 점괘 선택 대기 중 — 구매·판매·리프레시 버튼 비활성
        if (gm.shopMoneyText != null)
        {
            gm.shopMoneyText.text = open ? $"{gm.economyManager.money}냥" : "";
        }
        if (gm.refreshCostText != null)
        {
            gm.refreshCostText.text = open ? $"새로고침 {gm.shopManager.CurrentRefreshCost}냥" : "";
        }
        if (gm.refreshButton != null)
        {
            gm.refreshButton.interactable = open && gm.shopManager.CanRefresh;
        }
        // 구매 상품 슬롯 — 상품 수만큼 채우고 나머지는 빈 칸. 구매 완료는 리프레시 전까지 표시 유지
        for (int i = 0; i < gm.shopOfferSlots.Length; i++)
        {
            BellSlotUI slot = gm.shopOfferSlots[i];
            if (slot == null) continue;
            if (open && i < gm.shopManager.offers.Count)
            {
                ShopOffer offer = gm.shopManager.offers[i];
                int index = i;   // 클로저용 — 람다가 인덱스 변수를 공유하지 않게 고정
                bool sold = gm.shopManager.soldOfferIndices.Contains(index);
                string name = offer.kind == ShopOfferKind.FortunePouch
                    ? (gm.shopConfig != null ? gm.shopConfig.fortunePouchName : "복주머니")
                    : offer.bell.bellName;
                string desc = offer.kind == ShopOfferKind.FortunePouch
                    ? (gm.shopConfig != null ? gm.shopConfig.fortunePouchDescription : "")
                    : offer.bell.description;
                int price = gm.shopManager.PriceFor(offer);   // 할인 적용 구매가 — 표시·판정·차감 공용 조회
                if (sold)
                {
                    slot.Bind(name, desc, $"{price}냥", "구매 완료", false, null);
                }
                else if (offer.kind == ShopOfferKind.FortunePouch)
                {
                    // 복주머니 — 장착 슬롯 미점유: 방울 한도와 무관하게 잔액 + 실제 구매 가능 여부 검사
                    // (유효 점괘 후보 부족이면 '구매 가능'으로 표시하지 않는다)
                    slot.Bind(name, desc, $"{price}냥", "구매",
                        !choosing && gm.economyManager.money >= price && gm.shopManager.FortunePouchAvailable,
                        () => gm.shopManager.BuyOffer(index));
                }
                else
                {
                    slot.Bind(name, desc, $"{price}냥", "구매",
                        !choosing && gm.economyManager.money >= price && gm.bellManager.ownedBells.Count < gm.bellManager.OpenSlots,
                        () => gm.shopManager.BuyOffer(index));
                }
            }
            else
            {
                slot.Clear();
            }
        }
        // 장착/판매 슬롯 — 보유 수만큼 채움. 인스턴스 ID로 바인딩해 목록 순서 변경으로 다른 방울이 팔리지 않게 한다
        for (int i = 0; i < gm.shopBellSlots.Length; i++)
        {
            BellSlotUI slot = gm.shopBellSlots[i];
            if (slot == null) continue;
            if (open && i < gm.bellManager.ownedBells.Count)
            {
                OwnedBell bell = gm.bellManager.ownedBells[i];
                int instanceId = bell.instanceId;
                slot.Bind(bell.bellName, bell.description,
                    $"{bell.sellPrice}냥 · {BellSlotUI.UsesText(bell)}", "판매", !choosing,
                    () => gm.shopManager.SellBellInstance(instanceId));
            }
            else
            {
                slot.Clear();
            }
        }
    }
}
