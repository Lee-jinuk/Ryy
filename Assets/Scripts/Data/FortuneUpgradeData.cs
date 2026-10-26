using UnityEngine;

// 점괘 강화 효과 종류 — 24종 전부 활성 (2026-10-27 확률 강화 정책 확정).
// 신규 항목은 끝에만 추가한다 — 기존 값의 숫자가 바뀌면 직렬화된 정의가 깨진다.
public enum FortuneUpgradeKind
{
    YutMult = 0,         // 대상 윷 결과의 런타임 기본 배율 +amount
    AttrMult = 1,        // 대상 속성 칸 착지 배율 +amount (별도 누적 저장소)
    WaterComboMult = 2,  // 물 연계 각 단계 배율 +amount — [1,2,4,8,16] → 1회 강화 시 [1.5,2.5,4.5,8.5,16.5]
    ForestDotRate = 3,   // 풀 도트 최초·추가 등록 비율 각각 +amount%p — 이후 등록분부터 적용
    YutProb = 4,         // 대상 윷 결과의 최초 투척 발생 확률 +amount%p — 비대상 기존 비율로 차감
    AttrProb = 5,        // 대상 속성 칸 발생 확률 +amount%p — 이후 말판 생성부터 적용
    YutBaseScore = 6,    // 대상 윷 결과의 런타임 기본점수 +amount — 배율 강화와 별도 값
    RerollFrontProb = 7, // 재투척 앞면 확률 +amount%p — 양면령 기본값에 보정 후 20~80% 제한
    RerollBackProb = 8,  // 재투척 뒷면 확률 +amount%p — 앞면 강화와 상쇄되는 반대 방향 보정
    ShopDiscount = 9,    // 상점 구매 할인율 +amount%p — 다음 보스 처치 후 상점부터 적용, 최대 50%
}

// 점괘 강화 정의 — FortuneUpgradeDatabase 에셋의 불변 데이터.
// 획득 횟수·누적 강화·선택 대기 등 런타임 상태는 FortuneUpgradeManager가 소유한다.
[System.Serializable]
public class FortuneUpgradeData
{
    public string upgradeId;
    public string upgradeName;
    [TextArea] public string description;
    public FortuneUpgradeKind kind = FortuneUpgradeKind.YutMult;
    public YutResult targetResult;          // YutMult·YutProb·YutBaseScore 대상 결과
    public TileAttribute targetAttribute;   // AttrMult·AttrProb 대상 속성
    public float amount;                    // 선택 1회당 강화량 (배율 +0.5 / 기본점수 +5 / 확률·비율 %p)
    public bool effectEnabled = true;       // 점괘 후보 포함 여부 — 24종 전부 활성 (실제 효과 유무는 후보 추첨에서 별도 판정)
}
