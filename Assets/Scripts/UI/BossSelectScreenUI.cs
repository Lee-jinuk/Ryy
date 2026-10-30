using UnityEngine;
using UnityEngine.UI;

// 보스 소개/진입 화면 UI 참조 — 표시 전용 (게임 규칙 계산 없음)
// 이전: YutGameManager.bossSelectPanel·bossSelectNameText·bossSelectGoalText·
//       bossSelectAbilityText·bossSelectAffinityText·skipBossButton
public class BossSelectScreenUI : MonoBehaviour
{
    public GameObject bossSelectPanel;        // 보스 소개/진입 패널 — 최초 선택과 클리어 후 다음 보스 소개에 공용
    public Text bossSelectNameText;           // 진입 예정 보스 이름
    public Text bossSelectGoalText;           // 진입 예정 보스 목표 점수 (= 보스 HP)
    public Text bossSelectAbilityText;        // 진입 예정 보스 특수능력 설명
    public Text bossSelectAffinityText;       // 소개 중인 보스 상성 정보 (미연결 시 무시)
    public GameObject skipBossButton;         // '건너뛰기' 버튼 — 마지막 보스 소개 시 자동 숨김 (미연결 시 무시)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 보스 소개 화면 텍스트 채우기 (표시용 — 패널 토글은 호출자가 처리)
    public void FillInfo(YutGameManager gm, BossData boss)
    {
        if (gm.bossSelectNameText != null)
        {
            gm.bossSelectNameText.text = boss.bossName;
        }
        if (gm.bossSelectGoalText != null)
        {
            gm.bossSelectGoalText.text = boss.maxHP.ToString();
        }
        if (gm.bossSelectAbilityText != null)
        {
            gm.bossSelectAbilityText.text = boss.abilityDescription;
        }
        // 소개 중인 보스(pendingBossIndex) 상성 — 상성 능력이 있는 보스만 표시, 없으면 비움(숨김)
        if (gm.bossSelectAffinityText != null)
        {
            string affinityText = gm.bossManager.BuildAffinityText(gm.bossManager.pendingBossIndex);
            gm.bossSelectAffinityText.text = affinityText;
            gm.bossSelectAffinityText.gameObject.SetActive(!string.IsNullOrEmpty(affinityText));
        }
    }

    // 건너뛰기 버튼은 소개 화면이 열려 있고 다음 보스가 남아 있을 때만 표시 (마지막 보스 소개 시 숨김)
    public void Refresh(YutGameManager gm)
    {
        GameUIManager.SetPanel(gm.skipBossButton,
            gm.bossSelectPanel != null && gm.bossSelectPanel.activeSelf && gm.bossManager.CanSkipPending);
    }
}
