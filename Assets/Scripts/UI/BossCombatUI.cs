using UnityEngine;
using UnityEngine.UI;

// 전투 중 보스 정보 UI — 이름·현재/최대 HP·능력·상성.
// 표시 전용 (보스 데이터·능력 판정은 BossManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.bossNameText·bossAbilityText·bossCurrentHPText·
//       bossAffinityText·goalScoreText(목표 점수 = 보스 최대 HP)
public class BossCombatUI : MonoBehaviour
{
    public Text bossNameText;        // 보스 이름
    public Text bossAbilityText;     // 보스 특수능력 설명
    public Text bossCurrentHPText;   // 보스 현재 체력 = Max(0, 목표 − 누적 피해) (숫자만 출력)
    public Text bossAffinityText;    // 보스 상성 정보 — "기본: 불 100%\n약점: 물 200%\n강점: 풀 50%" (미연결 시 무시)
    public Text goalScoreText;       // 목표 점수 = 보스 최대 HP (숫자만 출력)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    public void Refresh(YutGameManager gm, bool boardActive)
    {
        if (gm.goalScoreText != null)
        {
            gm.goalScoreText.text = gm.goalScore.ToString();
        }
        // 보스 현재 체력 = Max(0, 최대 체력 - 누적 피해). 연출 중에는 표시값 displayTotal을 사용해
        // '토탈 합산' 단계에서 totalScoreText와 동시에 갱신되고, 종료·스킵 후 실제 체력과 일치한다
        if (gm.bossCurrentHPText != null)
        {
            gm.bossCurrentHPText.text = Mathf.Max(0, gm.goalScore - gm.displayTotal).ToString();
        }

        // 보스 정보 — 이름·특수능력만 표시 (HP 바 없음: 보스는 목표 점수 관문)
        BossData boss = gm.bossManager.CurrentBoss;
        if (boss != null)
        {
            if (gm.bossNameText != null)
            {
                gm.bossNameText.text = boss.bossName;
            }
            if (gm.bossAbilityText != null)
            {
                gm.bossAbilityText.text = boss.abilityDescription;
            }
        }

        // 보스 상성 정보창 — 전투 중인 보스 기준. 상성 능력이 없는 보스는 비워서 숨긴다
        if (gm.bossAffinityText != null)
        {
            string affinityText = boardActive && boss != null
                ? gm.bossManager.BuildAffinityText(gm.selectedBossIndex)
                : "";
            gm.bossAffinityText.text = affinityText;
            gm.bossAffinityText.gameObject.SetActive(!string.IsNullOrEmpty(affinityText));
        }
    }
}
