using UnityEngine;
using UnityEngine.UI;

// 클리어 화면 UI 참조 — 표시 전용 (게임 규칙 계산 없음)
// 이전: YutGameManager.clearPanel·clearBossNameText·clearScoreText·clearMessageText·nextBossButton
public class ClearScreenUI : MonoBehaviour
{
    public GameObject clearPanel;        // 보스 클리어 패널
    public Text clearBossNameText;       // 클리어한 보스 이름
    public Text clearScoreText;          // 클리어 시 최종 점수
    public Text clearMessageText;        // 클리어 메시지 — 마지막 보스 클리어 시 "최종 클리어" 표시 (선택 사항)
    public GameObject nextBossButton;    // '다음 관문' 버튼 — 마지막 보스 클리어 시 자동 숨김 (미연결 시 무시)

    // ── 표시 갱신 — 클리어 결과 텍스트 (패널이 꺼져 있어도 값은 항상 최신) ──
    public void Refresh(YutGameManager gm, BossData boss)
    {
        if (gm.clearBossNameText != null)
        {
            gm.clearBossNameText.text = boss.bossName;
        }
        if (gm.clearScoreText != null)
        {
            gm.clearScoreText.text = gm.totalScore.ToString();
        }
    }
}
