// ──────────────────────────────────────────────
// BellData.cs — 방울 정의와 보유 인스턴스 자료형
//
// 방울 정의(BellData)는 BellDatabase 에셋에 보관되는 불변 설정.
// 실제 보유 상태(OwnedBell)는 런타임에 BellManager가 관리한다 —
// 남은 발동 횟수·소멸·판매는 인스턴스에만 적용되고 정의는 변하지 않는다.
// ──────────────────────────────────────────────
using UnityEngine;

// 방울 효과 종류 — effect 종류와 정의는 유지하면서 효과 코드를 지우지 않는다
public enum BellEffectKind
{
    None = 0,
    YutMultGrowth = 1,     // 대상 윷 결과 확정마다 해당 윷 런타임 배율 +성장량 (한도 소진 시 소멸)
    AttrPowerGrowth = 2,   // 대상 속성 칸 최종 착지마다 그 속성 전체 런타임 공격력 +성장량
    BossMoveReduce = 3,    // 보유 중 최초 투척의 보스 이동 거리 −1칸 (최소 1칸, 봉쇄부 0칸은 미해제)
    FrogAffinity = 4,      // 보스 원래 강점 속성 상성 ×3, 나머지 공격 속성 ×0.5 — 잡기 강제 상성보다 우선
    FreeReroll = 5,        // 개별 재투척 시 확률로 기회 미소모 — 확률 미확정: effectEnabled=false로 비활성
    GoldOnWeakness = 6,    // 보스 원래 약점 속성 칸 공격 판정당 1냥 (정산당 1회)
}

// 방울 정의 — BellDatabase 에셋에 저장. 런타임에 수정하지 않는다
[System.Serializable]
public class BellData
{
    public string bellId;                          // 고유 ID — b_dokkaebi 등
    public string bellName = "방울";
    [TextArea] public string description = "";
    public BellEffectKind effect = BellEffectKind.None;
    public YutResult targetResult;                 // YutMultGrowth 대상 결과
    public TileAttribute targetAttribute;          // AttrPowerGrowth 대상 속성
    public float growthAmount;                     // 1회 성장량 (배율 +0.5 / 공격력 +5 / 수입 +1냥)
    public int useLimit;                           // 발동 한도 — 0이면 보유 중 무제한(지속형)
    public int buyPrice = 20;                      // 상점 구매가(냥)
    public int sellPrice = 10;                     // 상점 판매가(냥)
    public bool effectEnabled = true;              // false = 설정 미완료 — 상점 상품 풀에서도 제외
}

// 실제 보유·장착 방울 인스턴스 — 구매/획득 시점의 정의 데이터를 복사해 보관.
// 인스턴스 제거(소멸·판매)가 방울 정의·다른 인스턴스에 영향을 주지 않는다
[System.Serializable]
public class OwnedBell
{
    public int instanceId;                         // 장착 인스턴스 고유 식별자 — 판매·표시 연결용
    public string bellId;
    public string bellName;
    public string description;
    public BellEffectKind effect;
    public YutResult targetResult;
    public TileAttribute targetAttribute;
    public float growthAmount;
    public int remainingUses;                      // 0 이하 + useLimit>0 처리는 BellManager — -1 = 무제한(지속형)
    public int sellPrice;
}
