using UnityEngine;

// 환위부 이동 보정 선택창 UI — +1/−1/취소 버튼은 패널 안의 기존 OnClick
// (ChooseMoveAdjustPlus/Minus·CancelMoveAdjust)을 그대로 사용.
// 표시 전용 (보정 적용은 YutThrowSession·TalismanManager 소유)
// 이전: YutGameManager.moveAdjustPanel
public class MoveAdjustUI : MonoBehaviour
{
    public GameObject moveAdjustPanel;   // 환위부 ±1 이동 보정 선택창

    // ── 표시 갱신 — 선택 대기 중에만 표시 (확정·취소로 닫힘) ──
    public void Refresh(YutGameManager gm)
    {
        GameUIManager.SetPanel(gm.moveAdjustPanel, gm.talismanManager.isChoosingMoveAdjust);
    }
}
