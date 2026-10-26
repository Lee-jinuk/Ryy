// ──────────────────────────────────────────────
// BellData.cs — 방울 정의와 보유 인스턴스 자료형
//
// 방울 정의(BellData)는 BellDatabase 에셋에 보관되는 불변 설정.
// 실제 보유 상태(OwnedBell)는 런타임에 BellManager가 관리한다 —
// 남은 발동 횟수·소멸·판매는 인스턴스에만 적용되고 정의는 변하지 않는다.
// ──────────────────────────────────────────────
using UnityEngine;

// 방울 효과 종류 — 신규 항목은 끝에만 추가하고, 폐기한 번호는 재사용하지 않는다
// (기존 값의 숫자가 바뀌면 직렬화된 방울이 깨진다).
// 2026-10-26 재편: 1·2·4·6·7·10번(구 도걸·윷령·화령·도깨비·개화·걸음·모령·청개구리·재복·지령방울)은
// 정의가 폐기돼 번호만 비워 둔다 — 20종 체계의 신규 효과는 11번부터 사용.
// 20종 모두 지속형이며 발동으로 소멸하지 않는다.
public enum BellEffectKind
{
    None = 0,
    BossMoveReduce = 3,    // 속박령 — 보유 중 보스 이동 거리 −1칸 (최소 1칸, 봉쇄부·목박령 0칸 미해제)
    FreeReroll = 5,        // 윤회령 — 개별 재투척 시 확률(chance)로 기회 미소모 — 기본·특별 재투척 모두 적용
    WaterComboGuard = 8,   // 청류령 — 물 연계마다 끊김 방어 1회 (비물 착지 시 기록 보존·횟수 미증가·자체 효과 정상)
    DotDoubleChance = 9,   // 잔향령 — 정상 풀 도트 지급 시 chance 확률로 같은 피해 1회 추가 지급
    ExtraReroll = 11,      // 삼고령 — 세션 기본 재투척 기회 +bonusCount (같은 윷 1회 제한·특별 기회 별도 유지)
    MoveBonus = 12,        // 축지령 — 확정 윷 이동 거리 +bonusCount칸 (윷 기본점수·윷 배율 유지)
    FireChanceMult = 13,   // 화염령 — 불 공격 시 chance 확률로 말판 곱연산 ×damageMult (출처는 방울 기록)
    DotSealChance = 14,    // 목박령 — 풀 도트 지급 시 chance 확률로 다음 예정 보스 이동 1회 봉쇄
    WaterComboChance = 15, // 연수령 — 물 공격 시 chance 확률로 물 연계 횟수 +bonusCount (이번 공격부터)
    RerollBan = 16,        // 결단령 — 모든 재투척 금지 대신 전체배율 방울가산 +totalMultAdd (기타 재투척 효과·재기부 무력화)
    RerollFrontBias = 17,  // 양면령 — 기본·특별 재투척의 앞면 확률을 frontUpChance로 변경
    ChanceBoost = 18,      // 공령명 — chanceBoostable 확률 효과의 발동 확률 ×chanceBoostMult (원본 미변경)
    ProbGeolYut = 19,      // 걸윷령 — 최초 투척 걸·윷 결과 확률 각 +probBonusPercent%p (targetResult/2)
    ProbGae = 20,          // 쌍개령 — 최초 투척 개 결과 확률 +probBonusPercent%p (targetResult)
    ProbMoDo = 21,         // 극단령 — 최초 투척 모·도 결과 확률 각 +probBonusPercent%p (targetResult/2)
    LapTotalMult = 22,     // 윤환령 — 현재 완주 횟수 × perLapBonus를 기본 전체배율에 가산
    EarthNoPickMult = 23,  // 지맥령 — 땅 착지 카드 선택 제거 + 땅 말판 배율 ×tileMult
    EarthNoPickGold = 24,  // 금맥령 — 땅 착지 카드 선택 제거 + bonusMoney냥 지급
    ProximityMult = 25,    // 접전령 — 보스와 말판 최단 거리 proximityRange칸 이내 공격 시 방울 피해 배율 ×damageMult
    FireMultBonus = 26,    // 폭염령 — 불 공격 시 전체배율 말판가산 +tileMultAdd (말판 자체 배율 미변경)
}

// 방울 정의 — BellDatabase 에셋에 저장. 런타임에 수정하지 않는다
[System.Serializable]
public class BellData
{
    public string bellId;                          // 고유 ID — b_suryeong 등
    public string bellName = "방울";
    [TextArea] public string description = "";
    public BellEffectKind effect = BellEffectKind.None;
    public YutResult targetResult;                 // 결과 대상 효과용 (확률 방울의 증가 대상 1)
    public YutResult targetResult2;                // 결과 대상 효과의 두 번째 대상. BackDo(0) = 미사용
    public TileAttribute targetAttribute;          // 속성 대상 효과용 (화염·목박·연수·지맥·금맥·폭염)
    public float chance;                           // 확률 효과 발동률 0~1 (잔향 0.2 / 윤회 0.05 / 화염 0.2 / 목박 0.1 / 연수 0.1)
    public bool chanceBoostable;                   // 공령명 유효 확률 배율 적용 대상 — 확률 효과 5종만 true
    public int bonusCount;                         // 횟수·칸수 가산 — 삼고령 기본 재투척 +N / 축지령 이동 +N칸 / 연수령 연계 +N
    public float damageMult;                       // 방울 피해 배율(곱연산) — 화염령·결단령·접전령 조건 시 ×N (0 = 미적용)
    public float totalMultAdd;                     // 전체배율 가산 수치 — 가산 합계에 +N (0 = 미사용.
                                                   // 현재 20종 모두 0 — 가산 방울은 사용자 지정 시에만 설정)
    public float tileMultBonus;                    // (미사용) 구 말판 자체 배율 가산 — 폭염령 분류 변경(2026-10-27)으로 폐기, tileMultAdd로 대체
    public float tileMultAdd;                      // 전체배율 말판가산 수치 — 조건 충족 시 가산 합계의 말판 항에 +N
                                                   // (폭염령 불 공격 +1). 말판 자체 배율이 아닌 전체배율 가산 항이다
    public float tileMult;                         // 말판 배율 곱셈 — 지맥령 땅 ×N (0 = 미적용)
    public int bonusMoney;                         // 착지 수입 — 금맥령 땅 +N냥
    public float frontUpChance;                    // 양면령 — 재투척 앞면 확률 (0 = 미적용, 0.7)
    public int proximityRange;                     // 접전령 — 보스와의 판정 거리(칸)
    public float probBonusPercent;                 // 최초 투척 결과 확률 가산 %p — 걸윷령·쌍개령·극단령 (+10)
    public float chanceBoostMult;                  // 공령명 — 적용 대상 확률 배율 (2)
    public float perLapBonus;                      // 윤환령 — 완주 1회당 기본 전체배율 가산 (1)
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
    public float chance;
    public bool chanceBoostable;
    public int bonusCount;
    public float damageMult;
    public float totalMultAdd;
    public float tileMultBonus;
    public float tileMultAdd;
    public float tileMult;
    public int bonusMoney;
    public float frontUpChance;
    public int proximityRange;
    public float probBonusPercent;
    public float chanceBoostMult;
    public float perLapBonus;
    public int remainingUses;                      // 0 이하 + useLimit>0 처리는 BellManager — -1 = 무제한(지속형)
    public int sellPrice;
}
