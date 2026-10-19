using System.Collections.Generic;
using UnityEngine;

public enum EffectSourceType { YutResult, Talisman, Bell, Spell, Boss }
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
    // 소수 배율 단계(보스 상성 ×0.5 등)용 float 필드 — 0이 아니면 int 필드 대신 사용
    public float opValueF;           // 실제 적용 배율 (0.5/2 등) — int로 표현 불가한 소수 배율 기록용
    public float beforeValueF;       // 적용 전 소수 수치 — 상성 반영 멀티플 등
    public float afterValueF;        // 적용 후 소수 수치 — 상성 반영 멀티플 등
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
    public int tileScore;                // 도착 칸의 기본 공격점수 (불·물·풀 50 / START·땅·쇠 0)
    public float affinityMult = 1f;      // 도착 칸 속성에 대한 보스 상성 배율 (약점 2 / 강점 0.5 / 기본 1)
    public bool caught;                  // 잡기 성공 — 최종 도착 칸 = 보스 말 위치 (통과는 미발동)
    public float catchMult = 1f;         // 잡기 배율 (성공 ×2 / 실패 ×1)
    public bool landedOnBellTile;        // 땅 칸에 정확히 착지했는지 — 방울 뽑기 화면 발동 조건
    public List<int> movePath = new List<int>();  // 경유 칸 인덱스 — 이동 연출 재생용
    public List<ScoreEffectStep> steps = new List<ScoreEffectStep>();
}

// 점수 정산 담당 — 투척별 정산 계산, 저주 결과 처리, 효과 단계 기록
// 점수 계산의 유일한 경로 (다른 Manager가 독립적으로 점수를 복제·누적하지 않는다)
// 표시용 필드(basicScore 등)와 누적 필드(totalScore)의 저장소는 Inspector 노출을
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

    // 확정 결과(도~모) 투척 정산: 말 이동 → 도착 칸 공격점수 × 보스 상성 × 윷 배율 → 점수 확정 → 총점 합산
    // 공식: 최종 데미지 = 도착 칸 기본 공격점수 × 상성 배율 × 윷 결과 배율 (완주·윷 기본점수 가산 없음)
    // START·땅·쇠는 상성과 무관하게 공격점수 0 → 데미지 0. 모든 결과는 투척 1회를 정확히 소모한다
    public void SettleNormalThrow(ThrowSettlement settlement, YutResult selected)
    {
        // 1) 말 이동 → 도착 칸의 속성과 기본 공격점수 확정
        int steps = BoardManager.MoveSteps(selected);
        int landing = gm.board.MovePiece(steps, settlement.movePath);
        BoardTile tile = gm.board.tiles[landing];
        settlement.landingTileIndex = landing;
        settlement.tileScore = tile.AttackScore;
        settlement.landedOnBellTile = tile.attribute == TileAttribute.Earth;

        YutStatEntry stat = gm.rollManager.GetStat(selected);
        settlement.affinityMult = gm.bossManager.AffinityMultiplier(tile.attribute);
        settlement.baseBasic = tile.AttackScore;
        settlement.baseMult = stat.mult;
        settlement.finalBasic = settlement.baseBasic;
        settlement.finalMult = stat.mult;

        gm.currentThrows++;   // 확정 투척은 기본 투척 기회 정확히 1회 소모 (재투척은 별도 소모 없음)

        // 2) 보스 상성 적용 기록 — 공격 속성 칸(불·물·풀) 착지 시에만. 소수 배율은 float 필드에 기록
        if (tile.AttackScore > 0)
        {
            AddAffinityStep(settlement.steps, tile, stat.mult, settlement.affinityMult);
        }

        // 3) 보스 말 잡기 판정 — 최종 도착 칸 = 보스 말 현재 칸이면 성공 (통과만으론 미발동, 칸 인덱스 비교)
        //    성공 시 최종 피해 ×2 — 칸 점수·윷 배율·상성을 모두 계산한 뒤 적용. 0점 칸은 잡아도 0
        settlement.caught = landing == gm.bossManager.bossPieceIndex;
        settlement.catchMult = settlement.caught ? 2f : 1f;
        if (settlement.caught)
        {
            AddCatchStep(settlement.steps, stat.mult * settlement.affinityMult, settlement.catchMult);
        }
        // 향후 부적·방울·주술 효과는 이 지점에서 settlement.steps에 순차 추가된다

        // 4) 이번 투척 점수 확정 + 보스전 총점 1회 합산 — 곱셈(상성·잡기 소수 포함)을 모두 마친 뒤 정수화
        //    표시용 throwScore 필드는 여기서 갱신하지 않는다 — '투척 점수 표시' 단계에서만 보인다
        settlement.throwScore = Mathf.RoundToInt(settlement.finalBasic * settlement.affinityMult * settlement.finalMult * settlement.catchMult);
        gm.totalScore += settlement.throwScore;

        Debug.Log($"Yut Result: {YutGameManager.ToKorean(selected)} | {settlement.finalBasic} x 상성{settlement.affinityMult} x {settlement.finalMult}{(settlement.caught ? " x 잡기2" : "")} = {settlement.throwScore} | Throws: {gm.currentThrows}/{gm.maxThrows} | Total: {gm.totalScore}");
    }

    // 저주 계열 결과 처리: 백도와 낙은 별개 결과이며 둘 다 투척 1회 소모
    public void HandleCurseResult(YutResult result)
    {
        gm.currentThrows++;

        if (result == YutResult.BackDo)
        {
            // 보스 번호 = 선택 순번 + 1 (1~5: 누적 점수 차감 / 6~10: 누적 점수 초기화)
            // 라운드 폐지 후 차감 기준은 보스전 누적 총점(totalScore) — 구 판별 누적 roundScore와 동치였다
            int bossNumber = gm.selectedBossIndex + 1;
            if (bossNumber <= 5)
            {
                lastBackDoLoss = Mathf.Min(gm.totalScore, gm.backDoPenalty);
                gm.totalScore -= lastBackDoLoss;
            }
            else
            {
                lastBackDoLoss = gm.totalScore;
                gm.totalScore = 0;
            }
            gm.totalScore = Mathf.Max(0, gm.totalScore);
        }
        // 낙: 획득 점수 0 + 누적 점수 유지 — 추가 처리 없음 (투척 소모·콤보 단절은 위 공통 처리)
    }

    // 보스 상성 단계 기록 — 배율 적용 과정을 정산 기록에 남겨 연출·디버깅에 사용한다
    private void AddAffinityStep(List<ScoreEffectStep> steps, BoardTile tile, int yutMult, float affinityMult)
    {
        string attrName = BoardManager.AttributeName(tile.attribute);
        string role = gm.bossManager.AffinityRoleName(tile.attribute);
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Boss,
            sourceId = $"boss{gm.selectedBossIndex}_affinity",
            displayName = $"보스 상성 — {attrName} {role}",
            target = EffectTarget.Multiplier,
            op = EffectOp.Multiply,
            opValue = 0,                    // 소수 배율은 opValueF에 기록
            opValueF = affinityMult,
            beforeValue = yutMult,
            beforeValueF = yutMult,
            afterValue = 0,
            afterValueF = yutMult * affinityMult,   // 상성 반영 실효 멀티플 (예: 3×0.5=1.5)
            delta = 0,
            order = steps.Count,
        });
    }

    // 보스 말 잡기 단계 기록 — 성공 시에만 기록 (실패는 ×1이라 단계 없음)
    private void AddCatchStep(List<ScoreEffectStep> steps, float effectiveMultBefore, float catchMult)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Boss,
            sourceId = $"boss{gm.selectedBossIndex}_catch",
            displayName = "보스 말 잡기",
            target = EffectTarget.Multiplier,
            op = EffectOp.Multiply,
            opValue = (int)catchMult,
            opValueF = catchMult,
            beforeValue = 0,
            beforeValueF = effectiveMultBefore,                  // 윷 배율 × 상성까지 반영된 멀티플
            afterValue = 0,
            afterValueF = effectiveMultBefore * catchMult,       // 잡기 ×2 반영 최종 실효 멀티플
            delta = 0,
            order = steps.Count,
        });
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
