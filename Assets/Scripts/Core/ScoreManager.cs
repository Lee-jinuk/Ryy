using System.Collections.Generic;
using UnityEngine;

public enum EffectSourceType { YutResult, Talisman, Bell, Spell }
public enum EffectTarget { Basic, Multiplier }
public enum EffectOp { Add, Multiply }

// 효과 적용 1단계 기록 — 향후 부적/방울/주술/피버 발동 연출은 이 목록을 순서대로 재생한다
[System.Serializable]
public class ScoreEffectStep
{
    public EffectSourceType source;  // 효과 발생 원인 (윷 결과/부적/방울/주술/피버)
    public string sourceId;          // 효과를 발생시킨 아이템·능력 식별자
    public string displayName;       // 화면에 표시할 효과 이름
    public EffectTarget target;      // 적용 대상: 베이직 or 멀티플
    public EffectOp op;              // 연산 종류: +고정값 or ×배율 (×2와 +2는 서로 다른 연산)
    public int opValue;              // 연산값 (×2이면 2, +3이면 3)
    public int beforeValue;          // 적용 전 수치
    public int afterValue;           // 적용 후 수치
    public int delta;                // 실제 증가량 (after - before)
    public int order;                // 연출 재생 순서
}

// 한 번의 투척에 대한 확정 정산 — 점수는 여기서 계산되고, 연출은 이 데이터만 재생한다
[System.Serializable]
public class ThrowSettlement
{
    public YutResult result;
    public int baseBasic;            // 효과 적용 전 초기 베이직 (윷 스탯 기준)
    public int baseMult;             // 효과 적용 전 초기 멀티플
    public int finalBasic;           // 모든 효과 적용 후 베이직
    public int finalMult;            // 모든 효과 적용 후 멀티플
    public int throwScore;           // finalBasic × finalMult — TotalScore에 1회만 합산
    public int landingTileIndex = -1;    // 말판 도착 칸 (-1 = 이동 없음 — 저주)
    public int tileScore;                // 도착 칸의 점수 (칸 강화 포함)
    public int lapBonus;                 // 이 투척에 적용된 완주 보너스
    public bool landedOnBellTile;        // 방울 이벤트 칸에 정확히 착지했는지
    public List<int> movePath = new List<int>();  // 경유 칸 인덱스 — 이동 연출 재생용
    public List<ScoreEffectStep> steps = new List<ScoreEffectStep>();
}

// 점수와 피버 담당 — 투척별 정산 계산, 피버 콤보·보너스, 저주 결과 처리, 효과 단계 기록
// 점수 계산의 유일한 경로 (다른 Manager가 독립적으로 점수를 복제·누적하지 않는다)
// 표시용 필드(basicScore 등)와 누적 필드(roundScore/totalScore)의 저장소는 Inspector 노출을
// 위해 YutGameManager에 유지하고, 쓰기는 이 클래스만 수행한다.
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class ScoreManager
{
    private readonly YutGameManager gm;

    public int lastBackDoLoss;   // 최근 백도 차감량 (디버그/연출용)

    public ScoreManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 일반 결과(도~모) 투척 정산: 말 이동 → 초기 베이직·멀티플 → 효과 기록 → 점수 확정 → 총점 합산
    // 모든 결과(일반·저주)는 투척 1회를 정확히 소모한다
    public void SettleNormalThrow(ThrowSettlement settlement, YutResult selected)
    {
        // 1) 말 이동 → 도착 칸 점수 + 완주 보너스 + 윷 강화치 = 이번 투척 베이직
        int steps = BoardManager.MoveSteps(selected);
        int landing = gm.board.MovePiece(steps, settlement.movePath);
        BoardTile tile = gm.board.tiles[landing];
        settlement.landingTileIndex = landing;
        settlement.tileScore = tile.Score;
        settlement.lapBonus = gm.board.lapCount * 10;   // 완주 보너스 = 완주 횟수 × 10 (도착 칸에 적용)
        settlement.landedOnBellTile = tile.hasBellEvent;

        YutStatEntry stat = gm.rollManager.GetStat(selected);
        settlement.baseBasic = tile.Score + settlement.lapBonus + stat.basicBonus;
        settlement.baseMult = stat.mult;
        settlement.finalBasic = settlement.baseBasic;
        settlement.finalMult = stat.mult;

        gm.currentThrows++;   // 일반 결과도 투척 1회 소모

        // 2) 향후 부적·방울·주술 효과는 이 지점에서 settlement.steps에 순차 추가된다

        // 3) 이번 투척 점수 확정 + 이번 판 점수·보스전 총점 1회 합산
        //    표시용 throwScore 필드는 여기서 갱신하지 않는다 — '투척 점수 표시' 단계에서만 보인다
        settlement.throwScore = settlement.finalBasic * settlement.finalMult;
        gm.roundScore += settlement.throwScore;
        gm.totalScore += settlement.throwScore;

        Debug.Log($"Yut Result: {YutGameManager.ToKorean(selected)} | {settlement.finalBasic} x {settlement.finalMult} = {settlement.throwScore} | Throws: {gm.currentThrows}/{gm.maxThrows} | Round: {gm.roundScore} | Total: {gm.totalScore}");
    }

    // 저주 계열 결과 처리: 백도와 낙은 별개 결과이며 둘 다 투척 1회 소모
    public void HandleCurseResult(YutResult result)
    {
        gm.currentThrows++;

        if (result == YutResult.BackDo)
        {
            // 보스 번호 = 선택 순번 + 1 (1~5: 이번 판 점수 차감 / 6~10: 이번 판 점수 초기화)
            int bossNumber = gm.selectedBossIndex + 1;
            if (bossNumber <= 5)
            {
                // 이번 판 누적 점수에서만 차감 — 이전 판 확정분은 불침범
                lastBackDoLoss = Mathf.Min(gm.roundScore, gm.backDoPenalty);
                gm.roundScore -= lastBackDoLoss;
                gm.totalScore -= lastBackDoLoss;
            }
            else
            {
                lastBackDoLoss = gm.roundScore;
                gm.totalScore -= gm.roundScore;
                gm.roundScore = 0;
            }
            gm.totalScore = Mathf.Max(0, gm.totalScore);
        }
        // 낙: 획득 점수 0 + 누적 점수 유지 — 추가 처리 없음 (투척 소모·콤보 단절은 위 공통 처리)
    }

    private static void AddStep(List<ScoreEffectStep> steps, EffectSourceType source, string sourceId,
        string displayName, EffectTarget target, EffectOp op, int opValue, int before, int after)
    {
        steps.Add(new ScoreEffectStep
        {
            source = source,
            sourceId = sourceId,
            displayName = displayName,
            target = target,
            op = op,
            opValue = opValue,
            beforeValue = before,
            afterValue = after,
            delta = after - before,
            order = steps.Count,
        });
    }
}
