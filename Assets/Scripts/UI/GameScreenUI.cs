using UnityEngine;
using UnityEngine.UI;

// 게임 화면 최상위 UI 참조 — 하위 컴포넌트(YutThrowUI·ScoreUI·BossCombatUI·
// BoardUI·TalismanHandUI·EquippedBellUI)의 소속 패널과 직접 소유 필드만 관리.
// 표시 전용 (게임 규칙 계산 없음)
// 이전: YutGameManager.gamePanel·moneyText
public class GameScreenUI : MonoBehaviour
{
    public GameObject gamePanel;   // 메인 게임 화면
    public Text moneyText;         // 보유 냥 표시 (전투 화면, 미연결 시 무시)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──
    // 보유 냥 — 전투 화면 표시 (상점은 shopMoneyText가 따로 표시)
    public void Refresh(YutGameManager gm)
    {
        if (gm.moneyText != null)
        {
            gm.moneyText.text = $"{gm.economyManager.money}냥";
        }
    }
}
