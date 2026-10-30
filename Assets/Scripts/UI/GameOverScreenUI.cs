using UnityEngine;
using UnityEngine.UI;

// 게임오버 화면 UI 참조 — 표시 전용 (게임 규칙 계산 없음)
// 이전: YutGameManager.gameOverPanel·gameOverBossNameText·gameOverScoreText·gameOverGoalText
public class GameOverScreenUI : MonoBehaviour
{
    public GameObject gameOverPanel;         // 게임오버 패널
    public Text gameOverBossNameText;        // 도전한 보스 이름
    public Text gameOverScoreText;           // 최종 획득 점수
    public Text gameOverGoalText;            // 목표 점수

    // ── 표시 갱신 — 게임오버 결과 텍스트 (패널이 꺼져 있어도 값은 항상 최신) ──
    public void Refresh(YutGameManager gm, BossData boss)
    {
        if (gm.gameOverBossNameText != null)
        {
            gm.gameOverBossNameText.text = boss.bossName;
        }
        if (gm.gameOverScoreText != null)
        {
            gm.gameOverScoreText.text = gm.totalScore.ToString();
        }
        if (gm.gameOverGoalText != null)
        {
            gm.gameOverGoalText.text = gm.goalScore.ToString();
        }
    }
}
