// ──────────────────────────────────────────────
// EconomyManager.cs — 냥(화폐) 관리
//
// 모든 돈 증감은 이 클래스의 AddMoney/TrySpend 경로를 통과한다.
// 시작 자금은 ResetForNewRun에서 런당 1회 지급 — 상점 재진입·보스 전환·건너뛰기로 재지급 없음.
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
// ──────────────────────────────────────────────
using UnityEngine;

public class EconomyManager
{
    private readonly YutGameManager gm;

    public int money;   // 현재 보유 냥 — 런 단위 상태. 보스 전환에 유지, 새 런에서 시작 자금으로 리셋

    // 보스전당 클리어 보상 중복 지급 방지 — 보스전 시작에서 리셋하고 지급 시 세운다
    private bool clearRewardPaid;

    public EconomyManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 새 런 — 시작 자금 지급 (런당 1회). Awake·ResetRunState 경로에서만 호출
    public void ResetForNewRun()
    {
        clearRewardPaid = false;
        money = Mathf.Max(0, gm.shopConfig != null ? gm.shopConfig.startingMoney : 50);
        Debug.Log($"--- 런 시작 자금: {money}냥 ---");
    }

    // 보스전 시작 — 이번 보스의 클리어 보상 지급 여부를 초기화한다
    public void ResetClearReward() => clearRewardPaid = false;

    // 보스 클리어 보상 — 기본 10냥 + 남은 기본 투척 횟수 × 5냥 (ShopConfig 조정 가능).
    // 보스전당 1회만 지급 — 연출 스킵·패널 재표시·상점 재진입으로 중복 지급되지 않는다.
    // 게임오버·건너뛰기는 호출되지 않으므로 미지급. 지급 총액을 반환한다 (미지급이면 0)
    public int GrantBossClearReward()
    {
        if (clearRewardPaid)
        {
            return 0;
        }
        clearRewardPaid = true;
        int baseReward = gm.shopConfig != null ? gm.shopConfig.bossClearBaseReward : 10;
        int perThrow = gm.shopConfig != null ? gm.shopConfig.bossClearPerThrowReward : 5;
        int remainingThrows = Mathf.Max(0, gm.maxThrows - gm.currentThrows);
        int total = baseReward + remainingThrows * perThrow;
        AddMoney(total);
        Debug.Log($"보스 클리어 보상: 기본 {baseReward}냥 + 남은 투척 {remainingThrows}회 × {perThrow}냥 = {total}냥 (잔액 {money}냥)");
        return total;
    }

    // 돈 지급 — 양수만 허용
    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        money += amount;
    }

    // 돈 지출 — 잔액 부족 시 변경 없이 false. 성공 시에만 차감 (부분 차감 없음)
    public bool TrySpend(int amount)
    {
        if (amount < 0 || money < amount)
        {
            return false;
        }
        money -= amount;
        return true;
    }
}
