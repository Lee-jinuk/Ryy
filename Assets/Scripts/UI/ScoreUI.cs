using UnityEngine;
using UnityEngine.UI;

// 점수·능력치 UI — 윷 기본점수·윷 배율·전체배율·이번 피해·누적 피해·
// 투척 횟수·능력치 정보판·배율 이유 표시. 표시 전용 (피해 계산은 ScoreManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.basicScoreText·multScoreText·totalMultText·totalScoreText·
//       remainText·throwDataText·throwScoreText·currentYutStatsText·
//       currentAttributeStatsText·multiplierReasonText
public class ScoreUI : MonoBehaviour
{
    public Text basicScoreText;            // 이번 투척 베이직 (숫자만 출력)
    public Text multScoreText;             // 이번 투척 윷 배율 — 성장 반영값 (숫자만 출력)
    public Text totalMultText;             // 이번 투척 전체배율 — 발동 효과별로 갱신 (숫자만 출력)
    public Text totalScoreText;            // 누적 총점 (숫자만 출력)
    public Text remainText;                // 남은 투척 기회 (숫자만 출력)
    public Text throwDataText;             // 전체 투척 기회 (숫자만 출력)
    public Text throwScoreText;            // 이번 투척 확정 점수 (숫자만 출력)
    public Text currentYutStatsText;       // 능력치 정보판 — 윷별 기본점수×배율 (미연결 시 생략)
    public Text currentAttributeStatsText; // 능력치 정보판 — 속성 효과 상태 (미연결 시 생략)
    public Text multiplierReasonText;      // 배율 변화 이유 표시 — "추혼부 ×4" 등 (미연결 시 무시)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    public void Refresh(YutGameManager gm)
    {
        if (gm.basicScoreText != null)
        {
            gm.basicScoreText.text = gm.basicScore.ToString();
        }
        if (gm.multScoreText != null)
        {
            gm.multScoreText.text = gm.multiplier.ToString();
        }
        // 전체배율 — 기본 전체배율(×1)에서 발동 효과별로 갱신. 미연결 시 표시 생략 (계산에는 영향 없음)
        if (gm.totalMultText != null)
        {
            gm.totalMultText.text = gm.totalMult.ToString();
        }
        // 총점 표시는 연출 단계에 맞춰 displayTotal 사용 — '토탈 합산' 단계에서만 증가
        if (gm.totalScoreText != null)
        {
            gm.totalScoreText.text = gm.displayTotal.ToString();
        }
        if (gm.remainText != null)
        {
            gm.remainText.text = (gm.maxThrows - gm.currentThrows).ToString();
        }
        if (gm.throwDataText != null)
        {
            gm.throwDataText.text = gm.maxThrows.ToString();
        }
        if (gm.throwScoreText != null)
        {
            gm.throwScoreText.text = gm.throwScore.ToString();
        }

        // 능력치 정보판 — 현재 런타임 윷 기본점수·배율 + 속성 착지 효과 상태 표시 (조회 전용)
        if (gm.currentYutStatsText != null)
        {
            gm.currentYutStatsText.text =
                $"도 {StatText(gm, YutResult.Do)}\n" +
                $"개 {StatText(gm, YutResult.Gae)}\n" +
                $"걸 {StatText(gm, YutResult.Geol)}\n" +
                $"윷 {StatText(gm, YutResult.Yut)}\n" +
                $"모 {StatText(gm, YutResult.Mo)}";
        }
        if (gm.currentAttributeStatsText != null)
        {
            // 속성 효과 정보판 — 불 배율(기본+화령 성장+복주머니) / 물 연속 횟수·배율 / 현재 풀 도트 (보스전 단위 상태)
            float fireMult = gm.tileEffects.CurrentAttrMult(TileAttribute.Fire);
            int combo = gm.tileEffects.waterComboCount;
            float waterMult = WaterComboDisplay(gm, combo + 1);
            gm.currentAttributeStatsText.text =
                $"불 ×{FormatStat(fireMult)}\n" +
                $"물 {combo}연속 → ×{FormatStat(waterMult)}\n" +
                $"풀 도트 {gm.tileEffects.forestDot}";
        }
    }

    // 배율 변화 이유 표시 — 이 Text의 유일한 작성자는 이 컴포넌트다
    public void SetMultiplierReason(YutGameManager gm, string reason)
    {
        if (gm.multiplierReasonText != null)
        {
            gm.multiplierReasonText.text = reason;
        }
    }

    // 능력치 정보판 헬퍼 — 정수 배율은 소수점 없이 표시 (×4 / ×4.5)
    private static string FormatStat(float v) =>
        v % 1f == 0f ? ((int)v).ToString() : v.ToString("0.##");

    // 윷 정보판 표시 — "기본점수 ×배율" (예: 도 20 ×6 / 윷 50 ×6.5)
    private static string StatText(YutGameManager gm, YutResult result)
    {
        YutStatEntry stat = gm.rollManager.GetStat(result);
        return $"{stat.baseScore} ×{FormatStat(stat.mult)}";
    }

    // 물 연속 정보판 표시용 배율 — 설정 목록 클램프 + 복주머니 강화, TileEffectManager와 동일 기준
    private static float WaterComboDisplay(YutGameManager gm, int count)
    {
        var mults = gm.balanceConfig != null ? gm.balanceConfig.waterComboMults : null;
        float baseMult = mults != null && mults.Count > 0
            ? mults[Mathf.Clamp(count - 1, 0, mults.Count - 1)]
            : 1f;
        return baseMult + gm.fortuneManager.GetWaterComboBonus();
    }
}
