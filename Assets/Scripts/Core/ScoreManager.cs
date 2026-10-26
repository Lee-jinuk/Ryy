using System.Collections.Generic;
using UnityEngine;

public enum EffectSourceType { YutResult, Talisman, Bell, Spell, Boss, TileEffect }
public enum EffectTarget { Basic, YutMult, TotalMult }   // YutMult = 윷 배율(성장 반영), TotalMult = 전체배율 — 두 단위를 섞지 않는다
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
    public int useId;                // 연결된 사용 부적 기록 (0 = 카드 단계 아님 — 상성·잡기 등은 카드 표시 제거 없음)
    public string reasonText;        // 배율 이유 표시 텍스트 — "추혼부 ×4", "잡기 상성 ×2" 등. 비어 있으면 displayName 사용
    public float preApplyDelay = -1f;    // 이유 표시 → 배율 갱신 전 추가 대기(초). <0 = 바로 갱신 — 탐욕부 전용 타이밍
    public float postApplyDelay = -1f;   // 배율 갱신 → 카드 제거 대기(초). <0 = effectStepDelay 사용
}

// 한 번의 투척에 대한 확정 정산 — 점수는 여기서 계산되고, 연출은 이 데이터만 재생한다
[System.Serializable]
public class ThrowSettlement
{
    public YutResult result;
    public int baseBasic;            // 효과 적용 전 초기 베이직 = 확정 윷의 기본점수 (점수제 전환 — 칸 공격점수 폐지)
    public float baseMult;           // 효과 적용 전 초기 멀티플 (방울 성장 전)
    public int finalBasic;           // 모든 효과 적용 후 베이직
    public float finalMult;          // 성장 반영 윷 배율 — 방울·복주머니 성장(+0.5) 포함 소수 가능. 전체배율이 아니다
    public float baseTotalMult = 1f; // 런타임 기본 전체배율 설정값 — 가산 합계의 첫 항목 (윤환령 가산 미포함)
    public float lapBonus;           // 윤환령 가산 — 런 누적 완주 × perLapBonus (미장착 0, 판매 시 가산만 제거)
    public float talismanAddMult;    // 부적 가산 합계 — totalMultOp == Add 조건 충족 부적 수치의 합 (없으면 0)
    public float catchAddMult;       // 잡기 가산 합계 — 잡기 가산 효과의 합 (현재 적용 효과 없음, 구조 예약)
    public float bellAddMult;        // 방울 가산 합계 — totalMultAdd > 0 방울의 합 (결단령 +2 등, 없으면 0)
    public float tileAddMult;        // 말판 가산 합계 — tileMultAdd 조건 충족분의 합 (폭염령 불 공격 +1 등, 없으면 0)
    public float affinityAddMult;    // 보스 상성 가산 합계 — 상성 가산 효과의 합 (현재 적용 효과 없음, 구조 예약)
    // 전체배율 가산 항 합계 — 기본 + 윤환령 + 부적 + 잡기 + 방울 + 말판 + 상성 가산. 곱연산 적용 전 값
    public float AdditiveTotal => baseTotalMult + lapBonus + talismanAddMult + catchAddMult
        + bellAddMult + tileAddMult + affinityAddMult;
    public float totalMult = 1f;     // 최종 전체배율 = 가산 합계 × 곱연산 부적 × 잡기 × 곱연산 방울 × 말판 × 상성
    public float bellDamageMult = 1f;// 별도 방울 피해 배율 — 해당 효과가 없으면 ×1 (향후 방울 피해 배율 효과용)
    public int throwScore;           // 기초 피해(기본점수×윷 배율) × 전체배율의 최종 피해 — TotalScore에 1회만 합산
    public int landingTileIndex = -1;    // 말판 도착 칸 (-1 = 이동 없음 — 저주)
    public bool attackLanded;            // 공격 칸(불·물·풀·땅) 착지 — START·쇠는 공격 0. 땅은 추가 배율 ×1 공격 허용 (2026-10-22 확정, 구 tileScore>0 대체)
    public float affinityMult = 1f;      // 이번 정산의 유효 상성 배율 — 상성 능력이 있는 보스만 적용, 없으면 ×1
    public float actualAffinityMult = 1f;// 실제 착지 속성의 보스 상성 배율 (약점/강점/기본, 능력 없으면 1) — 기록·디버깅용
    public float resultBonusMult = 1f;   // 결과별 부적(일도부~모령부) 보너스 — 성공+최종 결과 일치 시에만 적용
    public bool caught;                  // 잡기 성공 — 최종 도착 칸 = 보스 말 위치 (통과는 미발동)
    public float catchMult = 1f;         // 잡기 배율 (실패 ×1 / 성공 GameBalanceConfig.catchMultiplier — 단일 배율, 추혼부는 추가 배율로 별도)
    public float talismanMult = 1f;      // 이번 투척 부적 피해 효과의 누적 배율 (미적용 ×1, 조건 만족분의 곱)
    public float tileEffectMult = 1f;    // 속성 착지 효과 배율 — 불 ×2 / 물 연계 / 풀·비공격 ×1 (표시 일치용)
    public string tileEffectNote;        // 착지 효과 설명 — "3연속 ×4"·"도트 등록 +60" 등 (착지 정보 표시용)
    public bool talismanCancelled;       // 탐욕부 충돌 등으로 최종 피해가 0으로 취소됨 — 연출·스킵 후에도 기록 유지
    public bool landedOnBellTile;        // 땅 칸에 정확히 착지했는지 — 부적 뽑기 화면 발동 조건 (방울 대체)
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

    // 런타임 기본 전체배율 — 설정 기본값의 런 복사본. 보스 전환에 유지되고 새 런에서 설정값으로 초기화.
    // 개별 효과 배율과 별개 — 이번 공격의 전체배율은 매번 정산에서 새로 계산한다
    public float baseTotalMult = 1f;

    // 새 런 초기화 — 런타임 기본 전체배율을 설정값으로 되돌린다 (보스 전환에서는 호출하지 않음)
    internal void ResetForNewRun()
    {
        baseTotalMult = gm.balanceConfig != null ? gm.balanceConfig.baseTotalMult : 1f;
    }

    public ScoreManager(YutGameManager gm)
    {
        this.gm = gm;
        baseTotalMult = gm.balanceConfig != null ? gm.balanceConfig.baseTotalMult : 1f;   // 첫 실행도 설정값과 같은 기준 — ResetForNewRun과 동일 경로
    }

    // 확정 결과(도~모) 투척 정산: 말 이동(+이동 보정) → 잡기 판정 → 가산 항목 수집
    //   → 곱연산 부적 → 잡기 → 곱연산 방울 → 말판 효과 → 보스 상성 → 총점 합산
    // 공식 (2026-10-27 확정): 기초 피해 = 윷 기본점수 × 성장 반영 윷 배율
    //   전체배율 = (기본 전체배율 + 윤환령 누적 완주 + 부적 가산 + 방울 가산)
    //     × 곱연산 부적(결과별 부적 × 피해 부적 곱) × 잡기 × 곱연산 방울 × 말판 × 보스 상성
    //   최종 피해 = 기초 피해 × 전체배율 → 곱셈 후 최종 정수화. START·쇠는 공격 0 (사용자 확정).
    //   가산 효과 없으면 합계 0이 아닌 기본값 1만 남고, 곱연산 효과 없으면 해당 배율 ×1.
    //   모든 결과는 투척 1회를 소모
    public void SettleNormalThrow(ThrowSettlement settlement, YutResult selected)
    {
        // 1) 말 이동 → 도착 칸 확정 — 진격부·후퇴부의 세션 이동 보정과 축지령의 방울 보정을 합산
        //    (윷 결과·배율 미변경). 보정 후 0칸이면 이동 없이 현재 칸으로 정산
        YutThrowSession session = gm.rollManager.session;
        int moveDelta = session != null ? session.moveDelta : 0;
        OwnedBell moveBonusBell = gm.bellManager.GetBell(BellEffectKind.MoveBonus);
        int bellMoveBonus = moveBonusBell != null ? moveBonusBell.bonusCount : 0;
        int steps = Mathf.Max(0, BoardManager.MoveSteps(selected) + moveDelta + bellMoveBonus);
        int landing = gm.board.MovePiece(steps, settlement.movePath);
        BoardTile tile = gm.board.tiles[landing];
        settlement.landingTileIndex = landing;
        // 지맥령·금맥령 보유 시 땅 착지 카드 선택은 제거된다 — 착지 자체는 공격 칸으로 유지
        bool earthNoPick = gm.bellManager.HasBell(BellEffectKind.EarthNoPickMult)
            || gm.bellManager.HasBell(BellEffectKind.EarthNoPickGold);
        settlement.landedOnBellTile = tile.attribute == TileAttribute.Earth && !earthNoPick;
        settlement.attackLanded = tile.attribute == TileAttribute.Fire
            || tile.attribute == TileAttribute.Water || tile.attribute == TileAttribute.Forest
            || tile.attribute == TileAttribute.Earth;   // 땅 공격 허용 — 추가 배율 ×1 (2026-10-22 확정)

        // baseBasic은 확정 윷의 기본점수
        YutStatEntry stat = gm.rollManager.GetStat(selected);
        settlement.baseBasic = stat.baseScore;
        settlement.baseMult = stat.mult;
        settlement.baseTotalMult = baseTotalMult;   // 런타임 기본 전체배율 설정값 — 가산 합계의 출발점
        // 윤환령 — 런 누적 완주 횟수를 전체배율 가산 항에 더한다 (perLapBonus당 +1).
        // 이번 이동으로 완료한 완주는 위 MovePiece에서 이미 반영 — 최종 착지 시점의 횟수를 읽는다.
        // 장착 중에만 가산 — 판매하면 가산만 제거되고 누적 완주 횟수는 유지 (읽기 전용)
        OwnedBell lapBell = gm.bellManager.GetBell(BellEffectKind.LapTotalMult);
        settlement.lapBonus = lapBell != null ? gm.board.lapCount * lapBell.perLapBonus : 0f;
        settlement.finalBasic = settlement.baseBasic;
        settlement.finalMult = stat.mult;

        gm.currentThrows++;   // 확정 투척은 기본 투척 기회 정확히 1회 소모 (재투척은 별도 소모 없음)

        // 결과별 부적(일도부~모령부) — 재투척 평가 성공 + 최종 결과가 같은 종류일 때만
        // 해당 윷 배율에 별도 ×배율 적용. 다른 종류이면 보너스 없음. 세션 한정, 영구 스탯 미변경
        settlement.resultBonusMult = 1f;
        string resultBonusName = null;
        int resultBonusUseId = 0;
        if (session != null &&
            session.succeededResultBonus.TryGetValue(selected, out (float mult, string name, int useId) bonus))
        {
            settlement.resultBonusMult = bonus.mult;
            resultBonusName = bonus.name;
            resultBonusUseId = bonus.useId;
        }

        // 2) 보스 말 잡기 판정 — 최종 도착 칸 = 보스 말 현재 칸이면 성공 (통과만으론 미발동)
        //    성공 시 잡기 배율(catchMultiplier ×4 단일) — 상성과 무관 (2026-10-26 단일화 — 구 별도 잡기 보너스 폐기).
        //    상성은 능력이 있는 보스에만 적용 — 비상성 보스의 모든 공격 상성은 ×1
        settlement.caught = landing == gm.bossManager.bossPieceIndex;
        BossAffinity affinity = gm.bossManager.GetAffinity(gm.selectedBossIndex);
        settlement.actualAffinityMult = gm.bossManager.AffinityMultiplier(tile.attribute);
        settlement.affinityMult = settlement.actualAffinityMult;
        settlement.catchMult = settlement.caught
            ? (gm.balanceConfig != null ? gm.balanceConfig.catchMultiplier : 4f)
            : 1f;

        // 3) 전체배율 가산 항목 수집 — 기본 + 윤환령 완주 + 부적 가산 + 방울 가산.
        //    가산 효과를 먼저 모두 합산하고 이후 곱연산 항목을 곱한다 (2026-10-27 확정 공식)
        if (bellMoveBonus != 0)
        {
            AddInfoStep(settlement.steps, 0, $"축지령 — 확정 이동 +{bellMoveBonus}칸", settlement.baseTotalMult);
        }
        float additiveSum = settlement.baseTotalMult;
        if (settlement.lapBonus != 0f)
        {
            AddAddStep(settlement.steps, EffectSourceType.Bell, $"bell_{lapBell.bellId}",
                $"윤환령 — 누적 완주 +{settlement.lapBonus}", settlement.lapBonus, additiveSum);
            additiveSum += settlement.lapBonus;
        }

        // 부적 피해 효과 평가 — 조건 충족 여부를 정산 시점에 1회만 판정한다.
        // 조건은 확정된 도착 속성·보스 약점 속성·잡기 여부로만 검사 — 통과 칸에는 미적용.
        // 추혼부는 잡기 성공 조건의 추가 배율로 평가된다
        var steppedUseIds = new HashSet<int>();
        if (resultBonusUseId > 0)
        {
            steppedUseIds.Add(resultBonusUseId);
        }
        bool greedCancelled = false;
        TalismanManager.ActiveTalismanEffect greedFx = null;
        var fxApplies = new List<KeyValuePair<TalismanManager.ActiveTalismanEffect, bool>>();
        foreach (TalismanManager.ActiveTalismanEffect fx in gm.talismanManager.sessionEffects)
        {
            if (fx.kind == TalismanEffectKind.GreedStrike)
            {
                greedCancelled = settlement.caught;   // 충돌(=잡기, 미확정 임시 해석) 시 공격 0 — 다른 증가 효과보다 우선
                greedFx = fx;
            }
            fxApplies.Add(new KeyValuePair<TalismanManager.ActiveTalismanEffect, bool>(
                fx, TalismanDamageApplies(fx, tile, settlement, affinity)));
        }

        // 가산 부적 — totalMultOp == Add 카드의 수치를 가산 합계에 더한다.
        // 조건 불충족 카드도 안내 단계로 기록 — 표시된 카드를 순차 제거하는 재생 단계
        settlement.talismanAddMult = 0f;
        foreach (KeyValuePair<TalismanManager.ActiveTalismanEffect, bool> e in fxApplies)
        {
            TalismanManager.ActiveTalismanEffect fx = e.Key;
            if (fx.totalMultOp != EffectOp.Add)
            {
                continue;
            }
            if (!e.Value)
            {
                Debug.Log($"부적 {fx.displayName} — 조건 불충족으로 미적용");
                AddTalismanFailStep(settlement.steps, fx, additiveSum);
                steppedUseIds.Add(fx.useId);
                continue;
            }
            settlement.talismanAddMult += fx.mult;
            AddAddStep(settlement.steps, EffectSourceType.Talisman, fx.sourceId,
                $"{fx.displayName} +{fx.mult}", fx.mult, additiveSum, fx.useId);
            additiveSum += fx.mult;
            steppedUseIds.Add(fx.useId);
        }

        // 가산 방울·가산 말판 — 공격 칸에서만 평가. totalMultAdd > 0 방울은 방울가산에,
        // tileMultAdd > 0 방울은 말판가산에 더한다 (잡기가산·상성가산은 현재 적용 효과 없음)
        settlement.bellAddMult = 0f;
        settlement.tileAddMult = 0f;
        if (settlement.attackLanded)
        {
            foreach (OwnedBell bell in gm.bellManager.ownedBells)
            {
                float add = BellAddFor(bell, tile, settlement);
                if (add != 0f)
                {
                    settlement.bellAddMult += add;
                    AddAddStep(settlement.steps, EffectSourceType.Bell, $"bell_{bell.bellId}",
                        $"{bell.bellName} +{add}", add, additiveSum);
                    additiveSum += add;
                }
                float tileAdd = BellTileAddFor(bell, tile, settlement);
                if (tileAdd != 0f)
                {
                    settlement.tileAddMult += tileAdd;
                    AddAddStep(settlement.steps, EffectSourceType.Bell, $"bell_{bell.bellId}",
                        $"{bell.bellName} +{tileAdd}", tileAdd, additiveSum);
                    additiveSum += tileAdd;
                }
            }
        }

        // 4) 곱연산 부적 — 결과별 부적 → 조건 충족 피해 부적 순으로 전체배율에 곱한다.
        //    조건 불충족 카드도 안내 단계로 기록한다 — 표시된 카드를 순차 제거하는 재생 단계.
        //    runningMult는 전체배율 단위 — 윷 배율은 기초 피해 쪽이고 여기에 곱하지 않는다
        float runningMult = additiveSum;
        if (settlement.attackLanded && settlement.resultBonusMult != 1f)
        {
            AddResultBonusStep(settlement.steps, resultBonusName, selected, runningMult, settlement.resultBonusMult, resultBonusUseId);
        }
        runningMult *= settlement.resultBonusMult;
        foreach (KeyValuePair<TalismanManager.ActiveTalismanEffect, bool> e in fxApplies)
        {
            TalismanManager.ActiveTalismanEffect fx = e.Key;
            if (fx.totalMultOp == EffectOp.Add)
            {
                continue;
            }
            if (!e.Value)
            {
                Debug.Log($"부적 {fx.displayName} — 조건 불충족으로 미적용");
                AddTalismanFailStep(settlement.steps, fx, runningMult);
                steppedUseIds.Add(fx.useId);
                continue;
            }
            float before = runningMult;
            runningMult *= fx.mult;
            settlement.talismanMult *= fx.mult;
            // 탐욕부가 잡기 취소될 예정이면 카드 표시 제거는 '피해 0' 취소 단계가 담당한다
            int stepUseId = (fx.kind == TalismanEffectKind.GreedStrike && greedCancelled) ? 0 : fx.useId;
            AddTalismanStep(settlement.steps, fx, before, runningMult, stepUseId);
            // 탐욕부는 별도 연출 타이밍 — 이유 표시 후 잠시 뒤 갱신, 갱신 후 잠시 유지하고 카드 제거.
            // 취소된 탐욕부(피해 0)는 배율 상승 연출을 하지 않으므로 일반 타이밍 유지
            if (fx.kind == TalismanEffectKind.GreedStrike && !greedCancelled)
            {
                ScoreEffectStep last = settlement.steps[settlement.steps.Count - 1];
                last.preApplyDelay = gm.presentationConfig != null ? gm.presentationConfig.greedStepPreDelay : 0.5f;
                last.postApplyDelay = gm.presentationConfig != null ? gm.presentationConfig.greedStepPostDelay : 0.5f;
            }
            steppedUseIds.Add(fx.useId);
        }
        settlement.talismanCancelled = greedCancelled;
        if (greedCancelled)
        {
            AddTalismanCancelStep(settlement.steps, runningMult, greedFx != null ? greedFx.useId : 0);
            if (greedFx != null)
            {
                steppedUseIds.Add(greedFx.useId);
            }
            runningMult = 0f;   // 취소 확정 — 이후 잡기·방울·말판·상성 단계는 유효 전체배율 0으로 표시
        }

        // 비피해·미발동 카드의 정산 안내 단계 — 배율 변경 없이 사용 내역을 재생하고 카드 표시를 제거한다
        // (즉시 효과는 사용 시점에 이미 실행됨 — 여기서 다시 적용하지 않는다)
        // 진격부·후퇴부는 이동 연출과 함께 표시하므로 여기서는 제외
        foreach (UsedTalismanRecord rec in gm.talismanManager.usedThisThrow)
        {
            if (rec.effect == TalismanEffectKind.MoveDelta || steppedUseIds.Contains(rec.useId))
            {
                continue;
            }
            string note = rec.effect == TalismanEffectKind.ResultBonus
                ? $"{rec.talismanName} — 발동 조건 불충족"
                : rec.resultNote;
            if (string.IsNullOrEmpty(note))
            {
                note = $"{rec.talismanName} — 적용됨";
            }
            AddInfoStep(settlement.steps, rec.useId, note, runningMult);
        }

        // 잡기 단계 — 곱연산 부적 다음·곱연산 방울 이전. 단일 잡기 배율(×4) 한 단계로 표시.
        // 취소 확정 시 runningMult는 0이라 전후 0으로 기록된다
        if (settlement.caught)
        {
            AddCatchStep(settlement.steps, runningMult, settlement.catchMult);
        }
        runningMult *= settlement.catchMult;

        // 방울 피해 배율(곱연산) — 조건 만족 방울의 배율을 순차 곱해 전체배율에 반영한다.
        // 공격 칸에서만 평가 — 비공격 칸(START·쇠)은 어차피 피해 0이라 단계도 남기지 않는다
        settlement.bellDamageMult = 1f;
        if (settlement.attackLanded)
        {
            ApplyBellDamageMults(settlement, tile, runningMult);
        }
        runningMult *= settlement.bellDamageMult;

        // 5) 기초 피해 × 전체배율 확정 → 속성 착지 효과 적용 → 총점 1회 합산.
        //    기초 피해 = 윷 기본점수 × 성장 반영 윷 배율.
        //    전체배율 = 가산 합계 × 곱연산 부적 × 잡기 × 곱연산 방울 × 말판 × 보스 상성.
        //    말판 배율·물 연속·도트 기록은 TileEffectManager가 공통 누적값(runningMult)으로 처리한다.
        //    피해는 전체 곱셈 후 정수화 — 표시용 throwScore는 '투척 점수 표시' 단계에서만 보인다
        float preEffectDamage = settlement.talismanCancelled
            ? 0f
            : settlement.finalBasic * settlement.finalMult * runningMult * settlement.affinityMult;
        settlement.throwScore = gm.tileEffects.ResolveLandingEffect(tile, preEffectDamage, settlement, runningMult);

        // 보스 상성 — 전체배율의 마지막 항목. 상성 단계는 공격 속성(불·물·풀) 착지에서만 —
        // 땅은 상성 대상이 아니다. 비상성 보스는 ×1이라 단계 없음
        if (settlement.attackLanded && affinity != null && tile.attribute != TileAttribute.Earth)
        {
            AddAffinityStep(settlement.steps, tile, affinity,
                runningMult * settlement.tileEffectMult, settlement.affinityMult);
        }
        // 최종 전체배율 — 가산 합계부터 상성까지 포함. 탐욕부 취소 확정 시 유효 전체배율은 0으로 유지
        settlement.totalMult = settlement.talismanCancelled
            ? 0f
            : settlement.AdditiveTotal * settlement.resultBonusMult * settlement.talismanMult
                * settlement.catchMult * settlement.bellDamageMult * settlement.tileEffectMult
                * settlement.affinityMult;
        gm.totalScore += settlement.throwScore;

        Debug.Log($"Yut Result: {YutGameManager.ToKorean(selected)} | {settlement.finalBasic} x 윷{settlement.finalMult} | 가산{settlement.AdditiveTotal}{(settlement.lapBonus != 0f ? $"(완주+{settlement.lapBonus})" : "")}{(settlement.resultBonusMult != 1f ? $" x 결과부적{settlement.resultBonusMult}" : "")}{(settlement.talismanMult != 1f ? $" x 부적{settlement.talismanMult}" : "")}{(settlement.caught ? $" x 잡기{settlement.catchMult}" : "")}{(settlement.bellDamageMult != 1f ? $" x 방울{settlement.bellDamageMult}" : "")} x 착지{settlement.tileEffectMult}{(settlement.affinityMult != 1f ? $" x 상성{settlement.affinityMult}" : "")}{(settlement.talismanCancelled ? " → 취소" : "")} = {settlement.throwScore} | 전체배율 {settlement.totalMult} | Throws: {gm.currentThrows}/{gm.maxThrows} | Total: {gm.totalScore}");
    }

    // 방울 피해 배율 적용 — 조건을 만족한 방울의 damageMult를 순차 곱하고 단계를 기록한다.
    // 새 피해 배율 방울은 BellDamageMultFor에 조건을 추가한다
    private void ApplyBellDamageMults(ThrowSettlement settlement, BoardTile tile, float runningMult)
    {
        foreach (OwnedBell bell in gm.bellManager.ownedBells)
        {
            float mult = BellDamageMultFor(bell, tile, settlement, runningMult);
            if (mult <= 0f)
            {
                continue;
            }
            float before = runningMult * settlement.bellDamageMult;
            settlement.bellDamageMult *= mult;
            AddBellDamageStep(settlement.steps, bell, mult, before, runningMult * settlement.bellDamageMult);
        }
    }

    // 부적 피해 효과의 조건 충족 판정 — 정산 시점에 1회 평가해 가산·곱연산 경로가 공용한다.
    // 순수 조회 — 추첨·상태 변경 없음
    private static bool TalismanDamageApplies(TalismanManager.ActiveTalismanEffect fx,
        BoardTile tile, ThrowSettlement settlement, BossAffinity affinity)
    {
        switch (fx.kind)
        {
            case TalismanEffectKind.FireBoost:
            case TalismanEffectKind.WaterBoost:
            case TalismanEffectKind.ForestBoost:
                return tile.attribute == fx.attribute;
            case TalismanEffectKind.CatchPursuit:
                return settlement.caught;   // 잡기 성공 시에만 최종 피해 추가 배율로 적용
            case TalismanEffectKind.WeaknessStrike:
                // 상성 능력이 없는 보스는 약점이 없다 — 천운부는 조건을 충족하지 않는다 (2026-10-22 확정)
                return affinity != null && tile.attribute == affinity.weak;
            case TalismanEffectKind.GreedStrike:
            case TalismanEffectKind.AllOrNothing:
                return true;
            default:
                return false;
        }
    }

    // 방울별 전체배율 방울가산 수치 — 공격 칸에서만 평가. 0이면 미적용.
    // 현재는 평면 가산만: totalMultAdd > 0이면 보유 중 무조건 가산한다 (결단령 +2).
    // 조건형 방울가산(예: 특정 착지 시 +N)은 사용자 지정 시 여기에 조건을 추가한다
    private float BellAddFor(OwnedBell bell, BoardTile tile, ThrowSettlement settlement)
    {
        return bell.totalMultAdd;
    }

    // 방울별 전체배율 말판가산 수치 — 공격 칸에서만 평가. 0이면 미적용.
    // 폭염령 — 대상 속성(targetAttribute) 공격 시 tileMultAdd를 말판가산 항에 더한다.
    // 말판 자체 배율이 아닌 가산 항목 — 곱연산 말판 배율과 별개로 합산된다
    private float BellTileAddFor(OwnedBell bell, BoardTile tile, ThrowSettlement settlement)
    {
        switch (bell.effect)
        {
            case BellEffectKind.FireMultBonus:
                return tile.attribute == bell.targetAttribute ? bell.tileMultAdd : 0f;
            default:
                return 0f;
        }
    }

    // 방울별 피해 배율 조건 판정 — 0이면 미적용. 확률 방울은 이 호출에서 1회만 추첨하며
    // 실패도 안내 단계로 남긴다 (UI 갱신·표시 경로에서는 호출되지 않는다)
    private float BellDamageMultFor(OwnedBell bell, BoardTile tile, ThrowSettlement settlement,
        float runningMult)
    {
        switch (bell.effect)
        {
            // 결단령은 방울가산(+2)·화염령은 말판 곱연산(×2, TileEffectManager)으로 분류 이동 — 여기서 처리하지 않는다
            case BellEffectKind.ProximityMult:
            {
                // 접전령 — 최종 착지와 보스 실제 위치의 원형 말판 최단 거리가 범위 이내.
                // 거리 0(잡기 칸)도 충족 — 잡기 배율과 중첩된다
                int dist = BoardDistance(settlement.landingTileIndex, gm.bossManager.bossPieceIndex);
                if (dist <= bell.proximityRange)
                {
                    return bell.damageMult;
                }
                AddBellInfoStep(settlement, bell, $"{bell.bellName} — 거리 {dist}칸 (범위 {bell.proximityRange})", runningMult * settlement.bellDamageMult);
                return 0f;
            }
            default:
                return 0f;
        }
    }

    // 원형 말판 최단 거리 — 양방향 이동 차의 작은 쪽 (화면 좌표·경유 칸 미사용)
    private static int BoardDistance(int a, int b)
    {
        int d = Mathf.Abs(a - b);
        return Mathf.Min(d, YutBoard.TileCount - d);
    }

    // 방울 안내 단계 — 배율 미적용(미발동·조건 불충족)도 발동 근거로 남긴다.
    // internal — 화염령처럼 말판 분류 효과도 TileEffectManager에서 방울 출처 단계로 기록한다.
    // effectiveValue는 이 시점의 유효 전체배율 표시값 — 호출자가 공통 누적값을 그대로 넘긴다
    internal void AddBellInfoStep(ThrowSettlement settlement, OwnedBell bell, string note,
        float effectiveValue)
    {
        settlement.steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Bell,
            sourceId = $"bell_{bell.bellId}",
            displayName = note,
            reasonText = note,
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValueF = 1f,
            beforeValueF = effectiveValue,
            afterValueF = effectiveValue,
            order = settlement.steps.Count,
        });
    }

    // 방울 배율 단계 기록 — 배율 곱셈의 근거와 전후 전체배율을 남긴다.
    // internal — 말판 분류로 이동한 화염령도 출처는 방울로 기록한다
    internal void AddBellDamageStep(List<ScoreEffectStep> steps, OwnedBell bell, float mult,
        float before, float after)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Bell,
            sourceId = $"bell_{bell.bellId}",
            displayName = $"{bell.bellName} ×{mult}",
            reasonText = $"{bell.bellName} ×{mult}",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValueF = mult,
            beforeValueF = before,
            afterValueF = after,
            order = steps.Count,
        });
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

    // 보스 상성 단계 기록 — 상성 능력이 있는 보스에서만 호출된다.
    // 비상성 보스는 상성 ×1로 단계를 남기지 않는다
    private void AddAffinityStep(List<ScoreEffectStep> steps, BoardTile tile, BossAffinity affinity,
        float before, float affinityMult)
    {
        string attrName = BoardManager.AttributeName(tile.attribute);
        string role = gm.bossManager.AffinityRoleName(tile.attribute);
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Boss,
            sourceId = $"boss{gm.selectedBossIndex}_affinity",
            displayName = $"보스 상성 — {attrName} {role}",
            reasonText = role == "약점" || role == "강점" ? $"{role} 상성 ×{affinityMult}" : $"일반 상성 ×{affinityMult}",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,                    // 소수 배율은 opValueF에 기록
            opValueF = affinityMult,
            beforeValue = 0,
            beforeValueF = before,                // 기본 전체배율 × 결과별 부적까지 반영된 전체배율
            afterValue = 0,
            afterValueF = before * affinityMult,   // 상성 반영 전체배율
            delta = 0,
            order = steps.Count,
        });
    }

    // 결과별 부적(일도부~모령부) 단계 기록 — 성공 상태 + 최종 결과 일치 시에만 호출
    private void AddResultBonusStep(List<ScoreEffectStep> steps, string cardName, YutResult result,
        float before, float bonusMult, int useId)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Talisman,
            sourceId = $"talisman_result_{result}",
            displayName = $"{cardName} — {YutGameManager.ToKorean(result)} ×{bonusMult}",
            reasonText = $"{cardName} ×{bonusMult}",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = bonusMult,
            beforeValue = 0,
            beforeValueF = before,                 // 전체배율 (결과별 부적 적용 전 = 기본 전체배율)
            afterValue = 0,
            afterValueF = before * bonusMult,      // 보너스 반영 전체배율
            delta = 0,
            order = steps.Count,
            useId = useId,
        });
    }

    // 보스 말 잡기 단계 기록 — 성공 시에만 기록 (실패는 ×1이라 단계 없음).
    // 단일 잡기 배율 "보스 잡기 ×4" 한 단계로 표시한다 (2026-10-26: 별도 잡기 보너스 단계 폐기)
    private void AddCatchStep(List<ScoreEffectStep> steps, float effectiveMultBefore, float catchMult,
        string label = "보스 잡기")
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Boss,
            sourceId = $"boss{gm.selectedBossIndex}_catch",
            displayName = label,
            reasonText = $"{label} ×{catchMult}",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = (int)catchMult,
            opValueF = catchMult,
            beforeValue = 0,
            beforeValueF = effectiveMultBefore,                  // 상성까지 반영된 전체배율
            afterValue = 0,
            afterValueF = effectiveMultBefore * catchMult,       // 잡기 배율 반영 전체배율
            delta = 0,
            order = steps.Count,
        });
    }

    // 부적 피해 효과 단계 기록 — 조건 통과한 카드별로 적용 배율과 전후 실효 멀티플을 남긴다
    private void AddTalismanStep(List<ScoreEffectStep> steps, TalismanManager.ActiveTalismanEffect fx,
        float before, float after, int useId)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Talisman,
            sourceId = fx.sourceId,
            displayName = $"{fx.displayName} ×{fx.mult}",
            reasonText = $"{fx.displayName} ×{fx.mult}",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = fx.mult,
            beforeValue = 0,
            beforeValueF = before,
            afterValue = 0,
            afterValueF = after,
            delta = 0,
            order = steps.Count,
            useId = useId,
        });
    }

    // 부적 조건 불충족 단계 기록 — 배율 변경 없이 안내만 재생하고 해당 카드 표시를 제거한다
    private void AddTalismanFailStep(List<ScoreEffectStep> steps, TalismanManager.ActiveTalismanEffect fx,
        float runningMult)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Talisman,
            sourceId = fx.sourceId,
            displayName = $"{fx.displayName} — 조건 불충족",
            reasonText = $"{fx.displayName} — 조건 불충족",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = 1f,                    // ×1 — Mult 표시 유지
            beforeValue = 0,
            beforeValueF = runningMult,
            afterValue = 0,
            afterValueF = runningMult,        // 전후 동일 — 배율 변화 없음
            delta = 0,
            order = steps.Count,
            useId = fx.useId,
        });
    }

    // 전체배율 가산 단계 기록 — 윤환령·가산 부적·가산 방울의 누적 합계를 +N으로 표시한다.
    // before는 직전까지의 가산 합계, after는 이번 항목까지 반영한 합계
    private void AddAddStep(List<ScoreEffectStep> steps, EffectSourceType source, string sourceId,
        string note, float add, float before, int useId = 0)
    {
        steps.Add(new ScoreEffectStep
        {
            source = source,
            sourceId = sourceId,
            displayName = note,
            reasonText = note,
            target = EffectTarget.TotalMult,
            op = EffectOp.Add,
            opValue = 0,
            opValueF = add,
            beforeValue = 0,
            beforeValueF = before,
            afterValue = 0,
            afterValueF = before + add,
            delta = 0,
            order = steps.Count,
            useId = useId,
        });
    }

    // 비피해·미발동 카드의 사용 내역 안내 단계 — 배율 변경 없이 표시만 재생한다
    private void AddInfoStep(List<ScoreEffectStep> steps, int useId, string note, float runningMult)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Talisman,
            sourceId = $"used_{useId}",
            displayName = note,
            reasonText = note,
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = 1f,
            beforeValue = 0,
            beforeValueF = runningMult,
            afterValue = 0,
            afterValueF = runningMult,
            delta = 0,
            order = steps.Count,
            useId = useId,
        });
    }

    // 탐욕부 충돌 — 공격 0 단계 기록 (우선순위: 공격 0은 모든 증가 효과를 덮어쓴다)
    private void AddTalismanCancelStep(List<ScoreEffectStep> steps, float runningMult, int useId)
    {
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Talisman,
            sourceId = "talisman_greed_cancel",
            displayName = "탐욕부 충돌 — 공격 0",
            reasonText = "탐욕부 — 잡기 성공으로 피해 0",
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = 0f,
            beforeValue = 0,
            beforeValueF = runningMult,
            afterValue = 0,
            afterValueF = 0f,
            delta = 0,
            order = steps.Count,
            useId = useId,
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
