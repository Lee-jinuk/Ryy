// ──────────────────────────────────────────────
// BellManager.cs — 방울 장착·보유 상태 관리
//
// 담당:
//   - 보유 방울 인스턴스(ownedBells) — 런 단위 장착 상태, 열린 슬롯(OpenSlots)까지 보유
//   - 열린 슬롯 수(openBellSlots) — 기본 shopConfig.baseBellSlots(4)·최대 maxBellSlots(8).
//     보유 수와 별도 관리. 확장 수단(아이템 종류·수치·지속)은 미확정 — ExpandBellSlots가 공용 경로
//   - 지속형 방울 조회 — 실제 효과 적용은 각 담당 시스템이 수행한다:
//       청류령(WaterComboGuard) → TileEffectManager 물 연계 끊김 방어
//       잔향령(DotDoubleChance) → YutGameManager.ApplyTurnStartDot 추가 지급 판정
//       속박령(BossMoveReduce)  → BossManager.ApplyMoveReduction 이동 거리 보정
//       윤회령(FreeReroll)      → YutThrowSession.Reroll 기회 차감 생략 판정
//   - EffectiveChance — 공령명(ChanceBoost) 보유 시 적용 대상(chanceBoostable)의
//     유효 발동 확률만 배율 적용해 반환한다 (원본 chance는 미변경)
//
// 규칙 (사용자 확정):
//   - 같은 종류의 방울은 1개까지 보유 (중복 장착 불가)
//   - 현재 4종 모두 지속형 — 효과 발동만으로 소멸하지 않는다
//   - 방울 정의(BellData/BellDatabase 에셋)는 런타임에 변경하지 않는다
//
// 과거 방울 선택 이벤트(땅 칸 3택1)·bellSlots·임시 보유 구조는 땅 칸 부적 선택과
// 장착 시스템으로 대체되어 제거됐다 — 관련 필드·함수는 남기지 않는다.
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class BellManager
{
    private readonly YutGameManager gm;

    // 보유·장착 방울 — 런 단위 상태. 보스 전환에 유지, 새 런에서 정리
    public readonly List<OwnedBell> ownedBells = new List<OwnedBell>();
    private int nextInstanceId = 1;

    // 현재 열린 슬롯 — 런 단위 상태. 보유 수와 별도 관리한다 (빈 열린 칸 존재 가능).
    // <=0이면 아직 초기화 전 → 기본 슬롯 수로 간주
    private int openBellSlots;

    public BellManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    public int BaseBellSlots => gm.shopConfig != null ? Mathf.Max(1, gm.shopConfig.baseBellSlots) : 4;
    public int MaxBellSlots => gm.shopConfig != null ? Mathf.Max(BaseBellSlots, gm.shopConfig.maxBellSlots) : 8;

    // 현재 열린 슬롯 수 — 획득·구매 가능 검사와 슬롯 표시 기준 (보유 수 아님)
    public int OpenSlots =>
        Mathf.Clamp(openBellSlots <= 0 ? BaseBellSlots : openBellSlots, BaseBellSlots, MaxBellSlots);

    // 방울 슬롯 확장 공용 경로 — 확장 아이템의 종류·수치·지속 범위는 미확정.
    // 확정 전까지 이 메서드만 연결 지점으로 둔다 (테스트: YutGameManager.ExpandBellSlots)
    public void ExpandBellSlots(int amount)
    {
        int next = Mathf.Min(MaxBellSlots, OpenSlots + Mathf.Max(0, amount));
        if (next == OpenSlots) return;
        openBellSlots = next;
        Debug.Log($"방울 슬롯 확장: {OpenSlots}칸");
        gm.uiManager.UpdateUI();
    }

    // 종류별 보유 여부 — 중복 장착 불가 규칙과 상점 표시에 사용
    public bool OwnsKind(string bellId) =>
        bellId != null && ownedBells.Exists(b => b.bellId == bellId);

    // 효과 종류 보유 여부 — 지속형 효과 적용 검사용
    public bool HasBell(BellEffectKind kind) =>
        ownedBells.Exists(b => b.effect == kind);

    // 효과 종류의 보유 인스턴스 — 확률(chance) 등 데이터 필드 참조용. 미보유면 null
    public OwnedBell GetBell(BellEffectKind kind) =>
        ownedBells.Find(b => b.effect == kind);

    // 유효 발동 확률 — 공령명 보유 시 chanceBoostable 방울의 확률만 chanceBoostMult 배.
    // 원본 chance는 변경하지 않고 조회 시점에만 배율을 적용한다. 미보유·비대상이면 원본 그대로
    public float EffectiveChance(OwnedBell bell)
    {
        if (bell == null || bell.chance <= 0f || !bell.chanceBoostable)
        {
            return bell != null ? bell.chance : 0f;
        }
        OwnedBell booster = GetBell(BellEffectKind.ChanceBoost);
        return booster != null ? bell.chance * booster.chanceBoostMult : bell.chance;
    }

    // 효과 종류의 유효 발동 확률 — 미보유면 0
    public float EffectiveChance(BellEffectKind kind) =>
        EffectiveChance(GetBell(kind));

    // 방울 획득 — 상점 구매의 유일한 경로. 장착 한도·중복 종류는 여기서 차단 (호출 전 가격 검사 별도)
    public bool TryAddBell(BellData data)
    {
        if (data == null || ownedBells.Count >= OpenSlots || OwnsKind(data.bellId))
        {
            return false;
        }
        ownedBells.Add(new OwnedBell
        {
            instanceId = nextInstanceId++,
            bellId = data.bellId,
            bellName = data.bellName,
            description = data.description,
            effect = data.effect,
            targetResult = data.targetResult,
            targetResult2 = data.targetResult2,
            targetAttribute = data.targetAttribute,
            chance = data.chance,
            chanceBoostable = data.chanceBoostable,
            bonusCount = data.bonusCount,
            damageMult = data.damageMult,
            totalMultAdd = data.totalMultAdd,
            tileMultBonus = data.tileMultBonus,
            tileMultAdd = data.tileMultAdd,
            tileMult = data.tileMult,
            bonusMoney = data.bonusMoney,
            frontUpChance = data.frontUpChance,
            proximityRange = data.proximityRange,
            probBonusPercent = data.probBonusPercent,
            chanceBoostMult = data.chanceBoostMult,
            perLapBonus = data.perLapBonus,
            remainingUses = data.useLimit > 0 ? data.useLimit : -1,   // -1 = 지속형(한도 없음)
            sellPrice = data.sellPrice,
        });
        return true;
    }

    // 인스턴스 ID로 방울 제거 — 판매의 공통 경로. 인스턴스만 제거한다
    public bool RemoveBell(int instanceId)
    {
        int i = ownedBells.FindIndex(b => b.instanceId == instanceId);
        if (i < 0)
        {
            return false;
        }
        Debug.Log($"--- 방울 제거: {ownedBells[i].bellName} ---");
        ownedBells.RemoveAt(i);
        return true;
    }

    // 런 초기화 — 보유 방울·인스턴스 카운터·열린 슬롯을 기본값으로 정리
    public void ResetForNewRun()
    {
        ownedBells.Clear();
        nextInstanceId = 1;
        openBellSlots = BaseBellSlots;
    }
}
