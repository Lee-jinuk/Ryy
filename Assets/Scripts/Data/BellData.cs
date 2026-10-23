// ──────────────────────────────────────────────
// BellData.cs — 방울 정의와 보유 인스턴스 자료형
//
// 방울 정의(BellData)는 BellDatabase 에셋에 보관되는 불변 설정.
// 실제 보유 상태(OwnedBell)는 런타임에 BellManager가 관리한다 —
// 남은 발동 횟수·소멸·판매는 인스턴스에만 적용되고 정의는 변하지 않는다.
// ──────────────────────────────────────────────
using UnityEngine;

// 방울 효과 종류 — effect 종류와 정의는 유지하면서 효과 코드를 지우지 않는다.
// 신규 항목은 끝에만 추가한다 — 기존 값의 숫자가 바뀌면 직렬화된 방울이 깨진다.
public enum BellEffectKind
{
    None = 0,
    YutMultGrowth = 1,     // 대상 윷 결과(최대 2종 — targetResult·targetResult2) 확정마다 그 결과의 런타임 배율 +성장량, 합산 한도 소진 시 소멸
    AttrPowerGrowth = 2,   // 대상 속성 칸 최종 착지마다 그 속성 전체 런타임 공격력 +성장량 (구 공격력 성장 — 현재 비활성 정의만 남음)
    BossMoveReduce = 3,    // 보유 중 최초 투척의 보스 이동 거리 −1칸 (최소 1칸, 봉쇄부 0칸은 미해제)
    FrogAffinity = 4,      // 보스 원래 강점 속성 상성 ×3, 나머지 공격 속성 ×0.5 — 잡기 강제 상성보다 우선
    FreeReroll = 5,        // 개별 재투척 시 확률(chance)로 기회 미소모 — 기본·특별 재투척 모두 적용
    GoldOnWeakness = 6,    // 보스 원래 약점 속성 칸 공격 판정당 1냥 (정산당 1회)
    AttrMultGrowth = 7,    // 대상 속성 칸 최종 착지마다 그 속성 착지 배율 +성장량 (화령방울 — 불 ×1.5 기본에 런타임 성장분 합산)
    WaterComboGuard = 8,   // 수령방울 — 물 연계마다 끊김 방어 1회 (물 외 착지 시 기록 보존·횟수 미증가·자체 효과 정상)
    DotDoubleChance = 9,   // 목령방울 — 정상 풀 도트 지급 시 chance 확률로 도트 1회 추가 지급
    GoldOnEarth = 10,      // 지령방울 — 땅 칸 최종 착지 정산마다 +growthAmount냥 (정산당 1회)
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
    public YutResult targetResult2;                // YutMultGrowth 두 번째 대상 결과 (도걸방울=걸·윷령방울=모). BackDo(0) = 미사용
    public TileAttribute targetAttribute;          // AttrPowerGrowth·AttrMultGrowth 대상 속성
    public float growthAmount;                     // 1회 성장량 (배율 +0.5 / 공격력 +5 / 수입 +3냥)
    public float chance;                           // 확률 효과 발동률 0~1 (목령방울 0.2 / 천운방울 0.07) — 다른 종류에서는 무시
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
    public YutResult targetResult2;
    public TileAttribute targetAttribute;
    public float growthAmount;
    public float chance;
    public int remainingUses;                      // 0 이하 + useLimit>0 처리는 BellManager — -1 = 무제한(지속형)
    public int sellPrice;
}
