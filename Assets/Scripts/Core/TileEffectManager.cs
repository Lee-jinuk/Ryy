// ──────────────────────────────────────────────
// TileEffectManager.cs — 속성 착지 효과 (불·물·풀)
//
// 말판 기본공격점수 제거 후 착지 효과로 전환된 시스템 (2026-10-22 확정):
//   불 — 부적·방울 반영 후 피해 D에 ×fireTileMult (기본 1.5)
//   물 — 최종 착지 연속 횟수별 연계 배율 (1→2→4→8→16, 보스전 단위 기록)
//   풀 — D를 즉시 적용 + 차례 시작 도트 등록 (최초 20% / 이후 +5%, 보스전 단위)
//
// 상태 소유권: 물 연속 횟수와 풀 도트는 보스전 단위 상태로 여기만 관리한다.
// 갱신은 확정 정산(ResolveLandingEffect)에서만 — 도트 피해 지급은 정상 접수된
// 다음 최초 던지기(RollYut → ApplyTurnStartDot)에서 차례당 1회 (2026-10-22 오후 변경).
// 재투척·확정·선택창 닫기·UI 갱신·연출 스킵으로 중복 지급·변경되지 않는다.
// 도트 틱이 부적·잡기·방울 성장·추가 도트를 발동시키는 순환은 구현하지 않는다 (사용자 확정).
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
// ──────────────────────────────────────────────
using UnityEngine;

public class TileEffectManager
{
    private readonly YutGameManager gm;

    // 물 연속 착지 횟수 — 보스전 단위. 물 외 속성·START 착지 시 초기화.
    // 최종 착지만 검사하고 경유 칸은 보지 않는다. 보스 전환·런 종료에서 리셋 (사용자 확정)
    public int waterComboCount;

    // 풀 도트 — 보스전 단위 등록 상태. 등록 시점에 반올림해 정수로 누적한다 (사용자 확정).
    // 보스 전환·런 종료에서 제거. 부적·방울·보스 능력 변경으로 기존 도트를 재계산하지 않는다
    public int forestDot;
    public bool forestDotActive;   // 도트 등록 여부 — 피해 0 풀 공격은 등록으로 취급하지 않는다 (사용자 확정)

    public TileEffectManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 보스전 시작·런 초기화 — 물 연속·풀 도트를 모두 정리한다
    public void ResetForBoss()
    {
        waterComboCount = 0;
        forestDot = 0;
        forestDotActive = false;
    }

    // 착지 효과 해석 — 확정된 최종 착지 칸에 대해 효과 적용 전 피해 D를 받아 최종 피해를 반환한다.
    // 공격 칸(불·물·풀): 배율 적용 + 연속·도트 갱신 + 정산 단계 기록.
    // 비공격 칸(START·땅·쇠): 피해 0 + 물 연속 초기화 (단계 기록 없음)
    public int ResolveLandingEffect(BoardTile tile, float preEffectDamage, ThrowSettlement settlement)
    {
        switch (tile.attribute)
        {
            case TileAttribute.Fire:
            {
                waterComboCount = 0;
                // 불 착지 배율 = 설정 기본 + 화령방울 성장 + 복주머니 강화 — 공통 조회 경로 사용
                float mult = CurrentAttrMult(TileAttribute.Fire);
                settlement.tileEffectMult = mult;
                settlement.tileEffectNote = $"피해 ×{mult}";
                AddTileStep(settlement, $"불 착지 효과 ×{mult}", mult);
                return Mathf.RoundToInt(preEffectDamage * mult);
            }
            case TileAttribute.Water:
            {
                waterComboCount++;   // 확정 정산당 1회만 증가 — 재투척·UI 갱신·스킵으로 중복 증가 없음
                float mult = WaterComboMult(waterComboCount);
                settlement.tileEffectMult = mult;
                settlement.tileEffectNote = $"{waterComboCount}연속 ×{mult}";
                AddTileStep(settlement, $"물 착지 효과 ×{mult} ({waterComboCount}연속)", mult);
                return Mathf.RoundToInt(preEffectDamage * mult);
            }
            case TileAttribute.Forest:
            {
                waterComboCount = 0;
                int damage = Mathf.RoundToInt(preEffectDamage);
                // 도트 등록·추가 — 풀 착지 공격에서만 발생, 피해 0이면 등록으로 취급하지 않는다.
                // 등록한 차례에는 추가 틱을 발생시키지 않고 다음 차례 시작부터 적용된다
                if (preEffectDamage > 0f)
                {
                    if (!forestDotActive)
                    {
                        int added = Mathf.RoundToInt(preEffectDamage * (DotInitialPercent / 100f));
                        forestDot = added;
                        forestDotActive = true;
                        settlement.tileEffectNote = $"도트 등록 +{added}";
                        AddInfoTileStep(settlement, $"풀 착지 — 도트 등록 +{added} (차례당 {forestDot})");
                    }
                    else
                    {
                        int added = Mathf.RoundToInt(preEffectDamage * (DotAddPercent / 100f));
                        forestDot += added;
                        settlement.tileEffectNote = $"도트 +{added} (총 {forestDot})";
                        AddInfoTileStep(settlement, $"풀 착지 — 도트 +{added} (총 {forestDot})");
                    }
                }
                else
                {
                    settlement.tileEffectNote = "피해 0 — 도트 미등록";
                    AddInfoTileStep(settlement, "풀 착지 — 피해 0으로 도트 미등록");
                }
                return damage;
            }
            case TileAttribute.Earth:
            {
                waterComboCount = 0;   // 물 외 착지 — 연속 기록 초기화
                // 땅 공격 허용 (2026-10-22 확정) — 별도 기본공격점수 없이 윷 기반 D를 ×1로 적용.
                // 풀 도트는 땅 공격으로 등록·증가하지 않으며, 땅 부적 선택 기능은 그대로 유지된다
                settlement.tileEffectMult = 1f;
                settlement.tileEffectNote = "공격 ×1 — 부적 선택";
                AddInfoTileStep(settlement, "땅 착지 — 공격 ×1 (부적 선택)");
                return Mathf.RoundToInt(preEffectDamage);
            }
            default:
            {
                waterComboCount = 0;   // 다른 속성·START 착지 시 연속 기록 초기화
                settlement.tileEffectMult = 1f;
                settlement.tileEffectNote = "공격 없음";
                return 0;
            }
        }
    }

    // 속성 착지 배율 현재값 — 설정 기본 + 화령방울 성장 + 복주머니 강화의 유일한 조회 경로.
    // 불만 설정 기본값(fireTileMult)이 있고 나머지 공격 속성은 기본 ×1.
    // 실제 데미지 계산·정보판 표시·방울 성장 안내가 모두 이 경로를 사용한다
    public float CurrentAttrMult(TileAttribute attr)
    {
        float baseMult = attr == TileAttribute.Fire
            ? (gm.balanceConfig != null ? gm.balanceConfig.fireTileMult : 1.5f)
            : 1f;
        return baseMult
            + gm.bellManager.GetAttrMultBonus(attr)
            + gm.fortuneManager.GetAttrMultBonus(attr);
    }

    // 차례 시작 도트 틱 피해 — 등록된 도트는 차례 시작마다 적용되고 소모되지 않는다
    public int TurnStartDotDamage => forestDotActive ? forestDot : 0;

    // 물 연속 배율 — 설정 목록 기준, 목록을 넘는 횟수는 마지막 배율로 고정 (6회 이상 수치 미확정)
    private float WaterComboMult(int count)
    {
        var mults = gm.balanceConfig != null ? gm.balanceConfig.waterComboMults : null;
        if (mults == null || mults.Count == 0)
        {
            return 1f;
        }
        // 복주머니 물 연계 강화 — 각 단계 배율에 같은 양을 합산 (설정 원본 미변경)
        return mults[Mathf.Clamp(count - 1, 0, mults.Count - 1)]
            + gm.fortuneManager.GetWaterComboBonus();
    }

    // 풀 도트 등록 비율 = 설정 기본 + 복주머니 강화(%p) — 이후 등록분부터 적용, 기존 저장분 재계산 없음
    private float DotInitialPercent =>
        (gm.balanceConfig != null ? gm.balanceConfig.forestDotInitialPercent : 20f)
            + gm.fortuneManager.GetForestDotBonus();

    private float DotAddPercent =>
        (gm.balanceConfig != null ? gm.balanceConfig.forestDotAddPercent : 5f)
            + gm.fortuneManager.GetForestDotBonus();

    // 속성 착지 배율 단계 기록 — 배율이 실제로 변한 효과 (불 ×1.5 / 물 연계).
    // 탐욕부 취소 확정 후에는 유효 전체배율 0을 유지 — 말판 단계가 양수로 되돌리지 않는다
    private void AddTileStep(ThrowSettlement settlement, string note, float mult)
    {
        float before = settlement.talismanCancelled
            ? 0f
            : settlement.baseTotalMult * settlement.resultBonusMult * settlement.affinityMult
                * settlement.catchMult * settlement.catchBonusMult * settlement.talismanMult;
        settlement.steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.TileEffect,
            sourceId = $"tile_{settlement.landingTileIndex}",
            displayName = note,
            reasonText = note,
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = mult,   // 실제 적용된 말판 배율 기록 — 취소 시에도 전후 0은 유지되나 효과 자체의 배율은 정확히 남긴다
            beforeValue = 0,
            beforeValueF = before,
            afterValue = 0,
            afterValueF = before * mult,
            delta = 0,
            order = settlement.steps.Count,
        });
    }

    // 속성 착지 안내 단계 기록 — 배율 ×1 (풀 도트 등록·미등록 안내)
    private void AddInfoTileStep(ThrowSettlement settlement, string note)
    {
        float running = settlement.talismanCancelled
            ? 0f
            : settlement.baseTotalMult * settlement.resultBonusMult * settlement.affinityMult
                * settlement.catchMult * settlement.catchBonusMult * settlement.talismanMult;
        settlement.steps.Add(new ScoreEffectStep
        {
            source = EffectSourceType.TileEffect,
            sourceId = $"tile_{settlement.landingTileIndex}",
            displayName = note,
            reasonText = note,
            target = EffectTarget.TotalMult,
            op = EffectOp.Multiply,
            opValue = 0,
            opValueF = 1f,
            beforeValue = 0,
            beforeValueF = running,
            afterValue = 0,
            afterValueF = running,
            delta = 0,
            order = settlement.steps.Count,
        });
    }
}
