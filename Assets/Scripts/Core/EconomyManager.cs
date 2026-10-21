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

    public EconomyManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 새 런 — 시작 자금 지급 (런당 1회). Awake·ResetRunState 경로에서만 호출
    public void ResetForNewRun()
    {
        money = Mathf.Max(0, gm.shopConfig != null ? gm.shopConfig.startingMoney : 50);
        Debug.Log($"--- 런 시작 자금: {money}냥 ---");
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
