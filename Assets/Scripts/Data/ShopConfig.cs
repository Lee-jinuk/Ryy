// ──────────────────────────────────────────────
// ShopConfig.cs — 상점·경제 설정 에셋
// 시작 자금·장착 한도·상점 상품 수의 단일 기준.
// 에셋 생성: Assets 우클릭 → Create → RYY → Shop Config
// ──────────────────────────────────────────────
using UnityEngine;

[CreateAssetMenu(fileName = "ShopConfig", menuName = "RYY/Shop Config")]
public class ShopConfig : ScriptableObject
{
    public int startingMoney = 50;      // 새 런 시작 자금(냥) — 런당 1회 지급, 재진입·보스 전환으로 재지급 없음
    public int maxBells = 4;            // 방울 최대 보유·장착 수
    public int shopOfferCount = 4;      // 상점 무작위 상품 후보 수 (고정 슬롯 수와 맞춘다)
    public int refreshBaseCost = 2;     // 상점 리프레시 시작 비용(냥)
    public int refreshCostMultiplier = 2;   // 리프레시 성공마다 다음 비용에 곱하는 배수 (2→4→8→…)
    public int bossClearBaseReward = 10;    // 보스 클리어 기본 보상(냥)
    public int bossClearPerThrowReward = 5; // 남은 기본 투척 횟수당 추가 보상(냥)
}
