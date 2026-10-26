// ──────────────────────────────────────────────
// ShopOffer.cs — 상점 상품 런타임 자료형
//
// 혼합 상점의 상품 1칸을 표현한다. 종류 판정은 kind enum으로 —
// 이름 문자열이나 임시 방울 데이터로 구분하지 않는다.
// 상품 목록 수명·구매 완료·리프레시는 ShopManager가 소유한다.
// ──────────────────────────────────────────────

public enum ShopOfferKind
{
    Bell = 0,          // 방울 상품 — bell 정의 참조, 구매 시 즉시 장착
    FortunePouch = 1,  // 복주머니 상품 — 구매 시 점괘 3택1 선택으로 이어짐, 장착 슬롯 미점유
}

// 상점 상품 1칸 — ShopManager.offers의 항목. ScriptableObject가 아닌 런타임 데이터
public class ShopOffer
{
    public ShopOfferKind kind;
    public BellData bell;    // kind == Bell일 때만 유효 — 방울 정의는 BellDatabase 에셋 참조
    public int price;        // 칸별 가격 — 방울은 bell.buyPrice, 복주머니는 ShopConfig.fortunePouchPrice

    public static ShopOffer Bell(BellData data) =>
        new ShopOffer { kind = ShopOfferKind.Bell, bell = data, price = data.buyPrice };

    public static ShopOffer FortunePouch(int price) =>
        new ShopOffer { kind = ShopOfferKind.FortunePouch, price = price };
}
