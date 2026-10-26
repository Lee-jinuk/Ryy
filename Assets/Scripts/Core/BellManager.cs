// ──────────────────────────────────────────────
// BellManager.cs — 방울 장착·효과 시스템
//
// 담당:
//   - 보유 방울 인스턴스(ownedBells) — 런 단위 장착 상태, 최대 shopConfig.maxBells(기본 4)개
//   - 성장형 방울 발동 처리 (윷 배율·속성 공격력 런타임 성장, 발동 한도 소진 시 인스턴스만 소멸)
//   - 속성별 런타임 공격력 성장값(attrAttackBonus) — 칸 기본값과 분리된 런 상태
//   - 지속형 방울 조회(족쇄·청개구리·재복·천운) — 실제 적용은 각 시스템이 호출해 수행
//
// 규칙 (사용자 확정):
//   - 같은 종류의 방울은 1개까지 보유 (중복 장착 불가)
//   - 소멸·판매해도 그 방울이 쌓은 런 성장은 런 종료까지 유지
//   - 방울 정의(BellData/BellDatabase 에셋)는 런타임에 변경하지 않는다
//   - 천운방울(BellEffectKind.FreeReroll)은 확률 미확정 — 데이터만 존재하고 효과 비활성
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

    // 속성별 런타임 공격력 성장값 — 칸 기본값(YutBoard.AttackScore)과 분리된 런 상태.
    // 방울이 소멸·판매돼도 이 성장은 런 종료까지 유지된다
    public readonly Dictionary<TileAttribute, int> attrAttackBonus = new Dictionary<TileAttribute, int>
    {
        { TileAttribute.Fire, 0 }, { TileAttribute.Water, 0 }, { TileAttribute.Forest, 0 },
    };

    // 속성별 런타임 착지 배율 성장값 — GameBalanceConfig.fireTileMult 등 원본과 분리된 런 상태.
    // 화령방울(AttrMultGrowth)이 쌓는 성장분 — 방울 소멸·판매 후에도 런 종료까지 유지 (사용자 확정)
    public readonly Dictionary<TileAttribute, float> attrMultBonus = new Dictionary<TileAttribute, float>
    {
        { TileAttribute.Fire, 0f }, { TileAttribute.Water, 0f }, { TileAttribute.Forest, 0f },
    };

    public BellManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    public int MaxBells => gm.shopConfig != null ? Mathf.Max(1, gm.shopConfig.maxBells) : 4;

    // 종류별 보유 여부 — 중복 장착 불가 규칙과 상점 표시에 사용
    public bool OwnsKind(string bellId) =>
        bellId != null && ownedBells.Exists(b => b.bellId == bellId);

    // 효과 종류 보유 여부 — 지속형 효과 적용 검사용
    public bool HasBell(BellEffectKind kind) =>
        ownedBells.Exists(b => b.effect == kind);

    // 속성의 런타임 성장 공격력 — 칸 기본값에 더해 정산·표시에 사용
    public int GetAttrAttackBonus(TileAttribute attr) =>
        attrAttackBonus.TryGetValue(attr, out int bonus) ? bonus : 0;

    // 속성의 런타임 성장 착지 배율 — 설정 기본 배율에 더해 정산·표시에 사용 (화령방울 성장분)
    public float GetAttrMultBonus(TileAttribute attr) =>
        attrMultBonus.TryGetValue(attr, out float bonus) ? bonus : 0f;

    // 방울 획득 — 상점 구매의 유일한 경로. 장착 한도·중복 종류는 여기서 차단 (호출 전 가격 검사 별도)
    public bool TryAddBell(BellData data)
    {
        if (data == null || ownedBells.Count >= MaxBells || OwnsKind(data.bellId))
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
            growthAmount = data.growthAmount,
            chance = data.chance,
            remainingUses = data.useLimit > 0 ? data.useLimit : -1,   // -1 = 지속형(한도 없음)
            sellPrice = data.sellPrice,
        });
        return true;
    }

    // 인스턴스 ID로 방울 제거 — 판매·한도 소진 소멸 공통 경로.
    // 인스턴스만 제거한다 — 누적 성장(attrAttackBonus·yutStats)은 런 종료까지 유지 (사용자 확정)
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

    // 성장형 방울 발동 — 확정 정산당 조건을 한 번씩 평가한다 (ScoreManager가 착지 확정 후 호출).
    // 성장분은 발동한 이번 공격부터 적용 (사용자 확정).
    // 반환: 이번 정산에서 발동한 방울의 표시용 결과 — ScoreManager가 단계 기록으로 변환
    public class GrowthTrigger
    {
        public string bellName;
        public string note;          // "도걸방울 — 도 배율 +0.5" / "화령방울 — 불 착지 배율 1.5 → 2"
        public bool isBasic;         // true = Basic(공격력) 변화 단계, false = Multiplier 변화 단계
        public bool isInfo;          // true = 안내 전용 — 점수판 수치를 바꾸지 않음 (속성 착지 배율 등 다른 단위의 성장)
        public float before;
        public float after;
    }

    public List<GrowthTrigger> ApplyGrowth(YutResult result, BoardTile tile)
    {
        var triggers = new List<GrowthTrigger>();
        var exhausted = new List<int>();   // 순회 중 제거 방지 — 한도 소진분을 순회 후 제거
        foreach (OwnedBell bell in ownedBells)
        {
            if (bell.remainingUses == 0)
            {
                continue;
            }
            switch (bell.effect)
            {
                case BellEffectKind.YutMultGrowth:
                    // 대상 결과(최대 2종 — 도걸방울은 도·걸, 윷령방울은 윷·모)를 최종 확정한
                    // 이번 공격부터 그 결과의 런타임 배율 +성장량. 두 결과는 합산 발동 횟수를 공유한다
                    if (bell.targetResult != result &&
                        (bell.targetResult2 == YutResult.BackDo || bell.targetResult2 != result))
                    {
                        continue;
                    }
                    {
                        YutStatEntry stat = gm.rollManager.GetStat(result);
                        float before = stat.mult;
                        stat.mult = Mathf.Max(0f, stat.mult + bell.growthAmount);
                        triggers.Add(new GrowthTrigger
                        {
                            bellName = bell.bellName,
                            note = $"{bell.bellName} — {YutGameManager.ToKorean(result)} 배율 +{bell.growthAmount}",
                            isBasic = false,
                            before = before,
                            after = stat.mult,
                        });
                    }
                    break;
                case BellEffectKind.AttrPowerGrowth:
                    // 최종 착지 칸의 속성 전체 런타임 공격력 +성장량 — 이번 공격부터 반영
                    if (bell.targetAttribute != tile.attribute ||
                        !attrAttackBonus.ContainsKey(tile.attribute))
                    {
                        continue;
                    }
                    {
                        int before = tile.AttackScore + attrAttackBonus[tile.attribute];
                        attrAttackBonus[tile.attribute] += Mathf.RoundToInt(bell.growthAmount);
                        triggers.Add(new GrowthTrigger
                        {
                            bellName = bell.bellName,
                            note = $"{bell.bellName} — {BoardManager.AttributeName(tile.attribute)} 공격력 +{Mathf.RoundToInt(bell.growthAmount)}",
                            isBasic = true,
                            before = before,
                            after = tile.AttackScore + attrAttackBonus[tile.attribute],
                        });
                    }
                    break;
                case BellEffectKind.AttrMultGrowth:
                    // 화령방울 — 최종 착지 칸의 속성 착지 배율 +성장량 (런타임 성장분, 설정 원본 미변경).
                    // 성장한 이번 공격부터 반영 — ApplyGrowth는 데미지 계산 전에 호출된다 (사용자 확정)
                    if (bell.targetAttribute != tile.attribute ||
                        !attrMultBonus.ContainsKey(tile.attribute))
                    {
                        continue;
                    }
                    {
                        // 속성 착지 배율의 전체값을 공통 조회 경로로 기록 — 설정 기본 + 화령 성장 + 복주머니 강화
                        // (복주머니로 2.0 상태면 안내도 2 → 2.5로 표시, 실제 계산과 같은 기준)
                        float before = gm.tileEffects.CurrentAttrMult(tile.attribute);
                        attrMultBonus[tile.attribute] += bell.growthAmount;
                        float after = gm.tileEffects.CurrentAttrMult(tile.attribute);
                        triggers.Add(new GrowthTrigger
                        {
                            bellName = bell.bellName,
                            note = $"{bell.bellName} — {BoardManager.AttributeName(tile.attribute)} 착지 배율 {before} → {after}",
                            isBasic = false,
                            isInfo = true,   // 성장분은 전체 실효 배율이 아닌 속성 배율 단위 — 점수판 유지
                            before = before,
                            after = after,
                        });
                    }
                    break;
                default:
                    continue;   // 지속형·비활성 방울은 성장 평가 대상이 아니다
            }
            if (bell.remainingUses > 0 && --bell.remainingUses == 0)
            {
                exhausted.Add(bell.instanceId);   // 한도 소진 — 방울만 소멸, 누적 성장은 유지
                Debug.Log($"--- {bell.bellName} 발동 한도 소진 — 방울 소멸 (성장은 유지) ---");
            }
        }
        foreach (int id in exhausted)
        {
            RemoveBell(id);
        }
        return triggers;
    }

    // 청개구리방울 유효 상성 — 원래 강점 속성 ×3, 나머지 공격 속성 ×0.5.
    // 상성 능력이 없는 보스는 '원래 강점'이 없으므로 적용하지 않는다 (2026-10-22 — 상점 후보 제외됨)
    public float FrogAffinityMult(TileAttribute attribute)
    {
        if (attribute != TileAttribute.Fire && attribute != TileAttribute.Water &&
            attribute != TileAttribute.Forest)
        {
            return 1f;
        }
        BossAffinity affinity = gm.bossManager.GetAffinity(gm.selectedBossIndex);
        if (affinity == null)
        {
            return 1f;
        }
        return attribute == affinity.strong ? 3f : 0.5f;
    }

    // 런 초기화 — 보유 방울·성장·인스턴스 카운터 전부 정리
    public void ResetForNewRun()
    {
        ownedBells.Clear();
        nextInstanceId = 1;
        attrAttackBonus[TileAttribute.Fire] = 0;
        attrAttackBonus[TileAttribute.Water] = 0;
        attrAttackBonus[TileAttribute.Forest] = 0;
        attrMultBonus[TileAttribute.Fire] = 0f;
        attrMultBonus[TileAttribute.Water] = 0f;
        attrMultBonus[TileAttribute.Forest] = 0f;
    }
}
