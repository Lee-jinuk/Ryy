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
    public float baseTotalMult = 1f; // 런타임 기본 전체배율 — GameBalanceConfig.baseTotalMult의 런 복사본 (보스 전환 유지, 새 런 초기화)
    public float totalMult = 1f;     // 최종 전체배율 = 기본 × 결과별 부적 × 부적 곱 × 상성 × 잡기 × 잡기 보너스 × 말판 × 별도 방울 피해 배율
    public float bellDamageMult = 1f;// 별도 방울 피해 배율 — 해당 효과가 없으면 ×1 (향후 방울 피해 배율 효과용)
    public int throwScore;           // 기초 피해(기본점수×윷 배율) × 전체배율의 최종 피해 — TotalScore에 1회만 합산
    public int landingTileIndex = -1;    // 말판 도착 칸 (-1 = 이동 없음 — 저주)
    public bool attackLanded;            // 공격 칸(불·물·풀·땅) 착지 — START·쇠는 공격 0. 땅은 추가 배율 ×1 공격 허용 (2026-10-22 확정, 구 tileScore>0 대체)
    public float affinityMult = 1f;      // 이번 정산의 유효 상성 배율 — 상성 능력이 있는 보스만 적용, 없으면 ×1
    public float actualAffinityMult = 1f;// 실제 착지 속성의 보스 상성 배율 (약점/강점/기본, 능력 없으면 1) — 기록·디버깅용
    public float resultBonusMult = 1f;   // 결과별 부적(일도부~모령부) 보너스 — 성공+최종 결과 일치 시에만 적용
    public bool caught;                  // 잡기 성공 — 최종 도착 칸 = 보스 말 위치 (통과는 미발동)
    public float catchMult = 1f;         // 잡기 배율 (실패 ×1 / 성공 GameBalanceConfig.catchMultiplier — 추혼부는 추가 배율로 별도)
    public float catchBonusMult = 1f;    // 별도 잡기 보너스 (실패 ×1 / 성공 GameBalanceConfig.catchBonusMult — 상성 무관, 2026-10-22 확정)
    public float talismanMult = 1f;      // 이번 투척 부적 피해 효과의 누적 배율 (미적용 ×1, 조건 만족분의 곱)
    public float tileEffectMult = 1f;    // 속성 착지 효과 배율 — 불 ×1.5 / 물 연계 / 풀·비공격 ×1 (표시 일치용)
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

    // 확정 결과(도~모) 투척 정산: 말 이동(+이동 보정) → 잡기 판정 → 상성(능력 보스만)
    //   → 결과별 부적 → 부적 배율 곱셈 → D 확정 → 속성 착지 효과(불×1.5/물 연계/풀 도트) → 총점 합산
    // 공식: 기초 피해 = 윷 기본점수 × 성장 반영 윷 배율
    //   전체배율 = 기본 전체배율(×1, 런타임) × 결과별 부적 × 부적 곱 × 보스 능력(상성) × 잡기 × 잡기 보너스
    //     × 별도 방울 피해 배율(현재 ×1) × 말판 배율(불×1.5/물 연계/풀·땅×1)
    //   최종 피해 = 기초 피해 × 전체배율 → 곱셈 후 최종 정수화. START·쇠는 공격 0 (사용자 확정).
    //   모든 결과는 투척 1회를 소모
    public void SettleNormalThrow(ThrowSettlement settlement, YutResult selected)
    {
        // 1) 말 이동 → 도착 칸 확정 — 진격부·후퇴부의 세션 이동 보정을 합산 (윷 결과·배율 미변경).
        //    보정 후 0칸이면 이동 없이 현재 칸으로 정산
        YutThrowSession session = gm.rollManager.session;
        int moveDelta = session != null ? session.moveDelta : 0;
        int steps = Mathf.Max(0, BoardManager.MoveSteps(selected) + moveDelta);
        int landing = gm.board.MovePiece(steps, settlement.movePath);
        BoardTile tile = gm.board.tiles[landing];
        settlement.landingTileIndex = landing;
        settlement.landedOnBellTile = tile.attribute == TileAttribute.Earth;
        settlement.attackLanded = tile.attribute == TileAttribute.Fire
            || tile.attribute == TileAttribute.Water || tile.attribute == TileAttribute.Forest
            || tile.attribute == TileAttribute.Earth;   // 땅 공격 허용 — 추가 배율 ×1 (2026-10-22 확정)

        // 방울 성장 — 최종 착지 칸·확정 결과 기준으로 발동한 이번 공격부터 반영 (사용자 확정).
        // baseBasic은 확정 윷의 기본점수 — 칸 공격점수·속성 공격력 성장 합산은 점수제 전환으로 제거
        YutStatEntry stat = gm.rollManager.GetStat(selected);
        settlement.baseBasic = stat.baseScore;
        settlement.baseMult = stat.mult;
        settlement.baseTotalMult = baseTotalMult;   // 런타임 기본 전체배율 — 이번 정산의 출발점
        foreach (BellManager.GrowthTrigger growth in gm.bellManager.ApplyGrowth(selected, tile))
        {
            AddBellStep(settlement.steps, growth, stat.mult);
        }
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
        //    성공 시 잡기 배율(catchMultiplier) + 별도 잡기 보너스(catchBonusMult) — 상성과 무관 (2026-10-22 확정).
        //    상성은 능력이 있는 보스에만 적용 — 비상성 보스의 모든 공격 상성은 ×1
        settlement.caught = landing == gm.bossManager.bossPieceIndex;
        BossAffinity affinity = gm.bossManager.GetAffinity(gm.selectedBossIndex);
        settlement.actualAffinityMult = gm.bossManager.AffinityMultiplier(tile.attribute);
        // 청개구리방울 — 상성 능력이 있는 보스에서만 의미 있음. 비상성 보스는 '원래 강점'이 없어 적용 불가
        bool frogActive = gm.bellManager.HasBell(BellEffectKind.FrogAffinity) && affinity != null;
        settlement.affinityMult = frogActive
            ? gm.bellManager.FrogAffinityMult(tile.attribute)
            : settlement.actualAffinityMult;
        settlement.catchMult = settlement.caught
            ? (gm.balanceConfig != null ? gm.balanceConfig.catchMultiplier : 3f)
            : 1f;
        settlement.catchBonusMult = settlement.caught
            ? (gm.balanceConfig != null ? gm.balanceConfig.catchBonusMult : 2f)
            : 1f;

        // 3) 상성·잡기 단계 기록 — 상성 능력이 있는 보스(또는 청개구리 적용)만 상성 단계를 남긴다.
        //    비상성 보스는 상성 ×1이라 단계 없음. 잡기는 공격 칸이 아니어도 판정·기록한다
        if (settlement.attackLanded)
        {
            if (settlement.resultBonusMult != 1f)
            {
                AddResultBonusStep(settlement.steps, resultBonusName, selected, settlement.baseTotalMult, settlement.resultBonusMult, resultBonusUseId);
            }
            // 상성 단계는 공격 속성(불·물·풀) 착지에서만 — 땅은 상성 대상이 아니다
            if ((affinity != null || frogActive) &&
                tile.attribute != TileAttribute.Earth)
            {
                AddAffinityStep(settlement.steps, tile, affinity, settlement.baseTotalMult * settlement.resultBonusMult,
                    settlement.affinityMult, frogActive);
            }
        }
        if (settlement.caught)
        {
            AddCatchStep(settlement.steps,
                settlement.baseTotalMult * settlement.resultBonusMult * settlement.affinityMult, settlement.catchMult);
            AddCatchStep(settlement.steps,
                settlement.baseTotalMult * settlement.resultBonusMult * settlement.affinityMult * settlement.catchMult,
                settlement.catchBonusMult, "잡기 보너스");
        }

        // 4) 부적 피해 효과 — 조건을 만족한 카드의 최종 피해 배율을 곱셈으로 중첩
        //    조건은 확정된 도착 속성·보스 약점 속성·잡기 여부로만 검사 — 통과 칸에는 미적용
        //    추혼부는 잡기 성공 조건의 추가 배율로 이 루프에서 1회 곱한다.
        //    조건 불충족 카드도 안내 단계로 기록한다 — 표시된 카드를 순차 제거하는 재생 단계.
        //    runningMult는 전체배율 단위 — 윷 배율은 기초 피해 쪽이고 여기에 곱하지 않는다
        float runningMult = settlement.baseTotalMult * settlement.resultBonusMult * settlement.affinityMult
            * settlement.catchMult * settlement.catchBonusMult;   // 잡기 보너스까지 포함 — 실제 전체배율 공식과 동일한 시작값
        var steppedUseIds = new HashSet<int>();
        if (resultBonusUseId > 0)
        {
            steppedUseIds.Add(resultBonusUseId);
        }
        bool greedCancelled = false;
        TalismanManager.ActiveTalismanEffect greedFx = null;
        foreach (TalismanManager.ActiveTalismanEffect fx in gm.talismanManager.sessionEffects)
        {
            bool applies = false;
            switch (fx.kind)
            {
                case TalismanEffectKind.FireBoost:
                case TalismanEffectKind.WaterBoost:
                case TalismanEffectKind.ForestBoost:
                    applies = tile.attribute == fx.attribute;
                    break;
                case TalismanEffectKind.CatchPursuit:
                    applies = settlement.caught;   // 잡기 성공 시에만 최종 피해 추가 배율로 적용
                    break;
                case TalismanEffectKind.WeaknessStrike:
                    // 상성 능력이 없는 보스는 약점이 없다 — 천운부는 조건을 충족하지 않는다 (2026-10-22 확정)
                    applies = affinity != null && tile.attribute == affinity.weak;
                    break;
                case TalismanEffectKind.GreedStrike:
                    applies = true;
                    greedCancelled = settlement.caught;   // 충돌(=잡기, 미확정 임시 해석) 시 공격 0 — 다른 증가 효과보다 우선
                    greedFx = fx;
                    break;
                case TalismanEffectKind.AllOrNothing:
                    applies = true;
                    break;
            }
            if (!applies)
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
        }

        // 재복방울 — 보스 약점 속성 칸으로 공격했으면 판정당 1냥 (정산당 1회 — 연출·스킵으로 중복 지급 없음)
        // 상성 능력이 없는 보스는 약점이 없으므로 수입을 지급하지 않는다 (2026-10-22 확정)
        if (settlement.attackLanded && affinity != null &&
            gm.bellManager.HasBell(BellEffectKind.GoldOnWeakness) &&
            tile.attribute == affinity.weak)
        {
            gm.economyManager.AddMoney(Mathf.Max(1, Mathf.RoundToInt(
                gm.bellManager.ownedBells.Find(b => b.effect == BellEffectKind.GoldOnWeakness)?.growthAmount ?? 1f)));
            settlement.steps.Add(new ScoreEffectStep
            {
                source = EffectSourceType.Bell,
                sourceId = "bell_jaebok",
                displayName = "재복방울 — 약점 공격 수입",
                reasonText = "재복방울 — 약점 공격 +1냥",
                target = EffectTarget.TotalMult,
                op = EffectOp.Multiply,
                opValueF = 1f,
                beforeValueF = runningMult,
                afterValueF = runningMult,   // 배율 변화 없음 — 수입 안내 단계
                order = settlement.steps.Count,
            });
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

        // 5) D 확정 → 속성 착지 효과 적용 → 총점 1회 합산.
        //    D = 윷 기본점수 × 윷 배율 × 결과별 부적 × 상성 × 잡기 × 잡기 보너스 × 부적 곱.
        //    불×1.5·물 연계·풀 도트는 TileEffectManager가 적용하고 물 연속·도트 기록을 갱신한다.
        //    표시용 throwScore 필드는 여기서 갱신하지 않는다 — '투척 점수 표시' 단계에서만 보인다
        // 5) 기초 피해 × 전체배율 확정 → 속성 착지 효과 적용 → 총점 1회 합산.
        //    기초 피해 = 윷 기본점수 × 성장 반영 윷 배율.
        //    전체배율 = 기본 전체배율 × 결과별 부적 × 상성 × 잡기 × 잡기 보너스 × 부적 곱 × 별도 방울 배율.
        //    불×1.5·물 연계·풀 도트는 TileEffectManager가 말판 배율로 적용하고 물 연속·도트 기록을 갱신한다.
        //    표시용 throwScore 필드는 여기서 갱신하지 않는다 — '투척 점수 표시' 단계에서만 보인다
        float preEffectDamage = settlement.talismanCancelled
            ? 0f
            : settlement.finalBasic * settlement.finalMult * runningMult * settlement.bellDamageMult;
        settlement.throwScore = gm.tileEffects.ResolveLandingEffect(tile, preEffectDamage, settlement);
        // 최종 전체배율 — 말판 배율까지 포함. 탐욕부 취소 확정 시 유효 전체배율은 0으로 유지
        // (이후 말판 안내 단계도 0으로 기록돼 양수로 복귀하지 않는다)
        settlement.totalMult = settlement.talismanCancelled
            ? 0f
            : runningMult * settlement.bellDamageMult * settlement.tileEffectMult;
        gm.totalScore += settlement.throwScore;

        Debug.Log($"Yut Result: {YutGameManager.ToKorean(selected)} | {settlement.finalBasic} x 윷{settlement.finalMult}{(settlement.resultBonusMult != 1f ? $" x 결과부적{settlement.resultBonusMult}" : "")}{(settlement.affinityMult != 1f ? $" x 상성{settlement.affinityMult}" : "")}{(settlement.caught ? $" x 잡기{settlement.catchMult} x 보너스{settlement.catchBonusMult}" : "")}{(settlement.talismanMult != 1f ? $" x 부적{settlement.talismanMult}" : "")}{(settlement.talismanCancelled ? " → 취소" : "")} x 착지{settlement.tileEffectMult} = {settlement.throwScore} | 전체배율 {settlement.totalMult} | Throws: {gm.currentThrows}/{gm.maxThrows} | Total: {gm.totalScore}");
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

    // 보스 상성 단계 기록 — 상성 능력이 있는 보스(또는 청개구리 적용)에서만 호출된다.
    // 비상성 보스는 상성 ×1로 단계를 남기지 않는다
    private void AddAffinityStep(List<ScoreEffectStep> steps, BoardTile tile, BossAffinity affinity,
        float before, float affinityMult, bool frogActive)
    {
        string attrName = BoardManager.AttributeName(tile.attribute);
        string role = gm.bossManager.AffinityRoleName(tile.attribute);
        steps.Add(new ScoreEffectStep
        {
            source = frogActive ? EffectSourceType.Bell : EffectSourceType.Boss,
            sourceId = frogActive ? "bell_frog" : $"boss{gm.selectedBossIndex}_affinity",
            displayName = frogActive ? $"청개구리방울 — {attrName} 상성 ×{affinityMult}"
                : $"보스 상성 — {attrName} {role}",
            // 이유 표시: 청개구리는 변경 상성, 실제 상성은 '약점/강점/일반 상성 ×N'
            reasonText = frogActive
                ? $"청개구리방울 — {(affinity != null && tile.attribute == affinity.strong ? "강점" : "")} 상성 ×{affinityMult}"
                : (role == "약점" || role == "강점" ? $"{role} 상성 ×{affinityMult}" : $"일반 상성 ×{affinityMult}"),
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

    // 방울 성장 단계 기록 — 윷 배율 성장(YutMult)·속성 공격력 성장(Basic)을 정산 기록에 남긴다.
    // 성장 처리는 BellManager.ApplyGrowth가 정산 계산 전에 수행 — 이 단계는 표시 재생 전용.
    // isInfo 단계(화령방울 등 다른 단위의 성장 안내)는 점수판 배율을 바꾸지 않는다 —
    // 성장분·속성 배율·윷 배율·전체배율을 같은 before/after 의미로 쓰지 않는다
    private void AddBellStep(List<ScoreEffectStep> steps, BellManager.GrowthTrigger growth, float displayedMult)
    {
        if (growth.isInfo)
        {
            steps.Add(new ScoreEffectStep
            {
                source = EffectSourceType.Bell,
                sourceId = $"bell_{growth.bellName}",
                displayName = growth.note,
                reasonText = growth.note,
                target = EffectTarget.YutMult,
                op = EffectOp.Multiply,
                opValueF = 1f,
                beforeValueF = displayedMult,
                afterValueF = displayedMult,    // 전후 동일 — 안내 전용, 윷 배율 표시 유지
                order = steps.Count,
            });
            return;
        }
        steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.Bell,
            sourceId = $"bell_{growth.bellName}",
            displayName = growth.note,
            reasonText = growth.note,
            target = growth.isBasic ? EffectTarget.Basic : EffectTarget.YutMult,
            op = EffectOp.Add,
            opValue = growth.isBasic ? Mathf.RoundToInt(growth.after - growth.before) : 0,
            opValueF = growth.isBasic ? 0f : growth.after - growth.before,
            beforeValue = growth.isBasic ? Mathf.RoundToInt(growth.before) : 0,
            afterValue = growth.isBasic ? Mathf.RoundToInt(growth.after) : 0,
            beforeValueF = growth.isBasic ? 0f : growth.before,
            afterValueF = growth.isBasic ? 0f : growth.after,
            delta = growth.isBasic ? Mathf.RoundToInt(growth.after - growth.before) : 0,
            order = steps.Count,
        });
    }

    // 보스 말 잡기 단계 기록 — 성공 시에만 기록 (실패는 ×1이라 단계 없음).
    // label로 잡기 배율(×3)과 별도 잡기 보너스(×2)를 구분해 표시한다
    private void AddCatchStep(List<ScoreEffectStep> steps, float effectiveMultBefore, float catchMult,
        string label = "보스 말 잡기")
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
