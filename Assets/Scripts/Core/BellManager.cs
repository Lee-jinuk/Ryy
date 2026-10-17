using System.Collections.Generic;
using UnityEngine;

// 방울 시스템 담당 — 방울 이벤트 칸 발동, 후보 3개 제시·1개 선택, 보유 방울 상태, 판/런 초기화
// 방울 후보 풀(bellPool)과 보유 목록(ownedBells)의 저장소는 Inspector 노출을 위해 YutGameManager에 유지
// 방울 실제 효과는 미구현 — 임의로 추가하지 않는다
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class BellManager
{
    private readonly YutGameManager gm;

    public bool isChoosingBell;       // 방울 선택창 표시 중 — 투척 입력 차단
    public int selectedBellOption = -1;
    public readonly List<BellData> currentBellOptions = new List<BellData>();

    public BellManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 방울 이벤트 칸 착지 → 정산 연출 종료 후 선택창 표시 (판이 계속될 때만)
    public void OpenBellChoice()
    {
        if (gm.bellPool == null || gm.bellPool.Count < 3)
        {
            Debug.LogWarning("[YutGameManager] bellPool이 3개 미만입니다 — 방울 이벤트를 건너뜁니다.", gm);
            return;
        }
        // 후보 3개 무작위 추출 (중복 없음)
        List<int> pick = new List<int>();
        for (int i = 0; i < gm.bellPool.Count; i++)
        {
            pick.Add(i);
        }
        for (int i = pick.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (pick[i], pick[j]) = (pick[j], pick[i]);
        }
        currentBellOptions.Clear();
        for (int i = 0; i < 3; i++)
        {
            currentBellOptions.Add(gm.bellPool[pick[i]]);
        }
        selectedBellOption = -1;
        isChoosingBell = true;
        GameUIManager.ActivatePanel(gm.bellChoicePanel, nameof(gm.bellChoicePanel));
        gm.uiManager.UpdateUI();
        Debug.Log("--- 방울 이벤트: 3개 중 1개를 선택하세요 ---");
    }

    // 방울 후보 선택 버튼(int): index 0~2 — 선택만 하고 확정은 ConfirmBellChoice()
    public void SelectBellOption(int index)
    {
        if (!isChoosingBell || index < 0 || index >= currentBellOptions.Count)
        {
            return;
        }
        selectedBellOption = index;
        Debug.Log($"방울 후보 선택: {currentBellOptions[index].bellName} (확정 버튼으로 결정)");
        gm.uiManager.UpdateUI();
    }

    // 방울 선택 확정 버튼 — 선택한 방울을 이번 판 임시 보유에 추가하고 창 닫기
    public void ConfirmBellChoice()
    {
        if (!isChoosingBell || selectedBellOption < 0)
        {
            return;
        }
        BellData chosen = currentBellOptions[selectedBellOption];
        gm.ownedBells.Add(chosen);
        isChoosingBell = false;
        GameUIManager.SetPanel(gm.bellChoicePanel, false);
        Debug.Log($"--- 방울 획득: {chosen.bellName} (이번 판 한정) ---");
        gm.uiManager.UpdateUI();
    }

    // 원클릭 버전 — 옵션 버튼에 바로 연결하면 선택과 확정을 한 번에 처리
    public void SelectBellAndConfirm(int index)
    {
        SelectBellOption(index);
        ConfirmBellChoice();
    }

    // 다음 판 진행 — 임시 방울 소멸 + 방울 칸 판당 발동 제한 리셋 (말판 배치·말 위치·완주는 유지)
    public void ResetForNextRound()
    {
        gm.ownedBells.Clear();
        foreach (BoardTile tile in gm.board.tiles)
        {
            tile.bellUsedThisRound = false;
        }
    }

    // 런 종료 시 방울 이벤트 상태 초기화
    public void ResetForNewRun()
    {
        isChoosingBell = false;
        selectedBellOption = -1;
        currentBellOptions.Clear();
        gm.ownedBells.Clear();
    }
}
