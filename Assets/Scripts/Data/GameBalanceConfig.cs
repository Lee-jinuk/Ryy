// ──────────────────────────────────────────────
// GameBalanceConfig.cs — 게임 밸런스 설정 에셋
// 기본 투척 횟수·윷 기본점수/배율·속성 착지 효과·보스 이동 단계 범위의 단일 기준.
// 런 중 윷 강화는 복사된 런타임 스탯에만 적용된다 — 원본 미변경.
// 에셋 생성: Assets 우클릭 → Create → RYY → Game Balance Config
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "RYY/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    public int maxThrows = 6;                 // 보스전 기본 투척 횟수 (2026-10-26: 5→6. 개별 재투척 1회 규칙은 별개로 유지)

    // 윷별 스탯 — baseScore가 공격 출발점(도20/개30/걸30/윷50/모60), mult가 배율(도6/개3/걸4/윷6/모5).
    // basicBonus는 예약 필드 — 의미 변경 없이 보존 (현재 미적용)
    public List<YutStatEntry> yutStats = new List<YutStatEntry>
    {
        new YutStatEntry { result = YutResult.Do,   basicBonus = 0, baseScore = 20, mult = 6 },
        new YutStatEntry { result = YutResult.Gae,  basicBonus = 0, baseScore = 30, mult = 3 },
        new YutStatEntry { result = YutResult.Geol, basicBonus = 0, baseScore = 30, mult = 4 },
        new YutStatEntry { result = YutResult.Yut,  basicBonus = 0, baseScore = 50, mult = 6 },
        new YutStatEntry { result = YutResult.Mo,   basicBonus = 0, baseScore = 60, mult = 5 },
    };

    // 속성 착지 효과 — 말판 기본공격점수 제거 후 착지 효과로 전환 (2026-10-22 확정)
    public float fireTileMult = 2f;           // 불 착지 — 부적·방울 반영 후 피해에 ×2 (2026-10-26: 1.5→2)
    // 물 착지 — 최종 착지 연속 횟수별 배율 (1회 ×1 / 2회 ×2 / 3회 ×4 / 4회 ×8 / 5회 ×16).
    // 목록을 넘는 횟수는 마지막 배율로 고정 — 6회 이상 수치는 미확정이라 확장하지 않는다
    public List<float> waterComboMults = new List<float> { 1f, 2f, 4f, 8f, 16f };
    public float forestDotInitialPercent = 20f; // 풀 최초 도트 등록 — 이번 공격 피해의 20% (등록 시 반올림)
    public float forestDotAddPercent = 5f;      // 풀 도트 추가 — 도트 보유 시 이번 공격 피해의 5%를 기존 도트에 합산

    // 보스 말 이동 — 기본은 고정 범위 4~7칸 (2026-10-26 확정 — 피해 비율 단계 미사용).
    // useBossMoveDamageStages를 켜면 아래 단계 배열로 누적 피해 비율 단계별 범위를 적용한다 — 구조는 보존, 기본은 비활성
    public bool useBossMoveDamageStages = false;
    public int bossMoveMin = 4;               // 고정 이동 최소 칸 (단계 미사용 시)
    public int bossMoveMax = 7;               // 고정 이동 최대 칸 (단계 미사용 시, 상한 포함)

    // 누적 피해 비율 단계별 보스 이동 범위 — useBossMoveDamageStages 활성 시에만 사용.
    // 단계 i는 피해 ≥ 최대 HP의 i/N (3단계: <1/3 → 4~7 / ≥1/3 → 3~6 / ≥2/3 → 2~5). 두 배열 길이는 같아야 함
    public int[] bossMoveStageMin = { 4, 3, 2 };
    public int[] bossMoveStageMax = { 7, 6, 5 };

    // 보스 말 잡기 (사용자 확정): 잡기 성공 시 잡기 배율 ×4 단일 — 상성 능력이 없는 보스에게도 동일하게 적용된다.
    // 2026-10-26: 별도 잡기 보너스(catchBonusMult ×2) 폐기, 잡기 배율 ×4로 단일화 (구 ×3 + 보너스 ×2 = ×6 대체).
    // 추혼부는 잡기 배율을 대체하지 않고 추가 ×4 — 부적 배율 곱셈에 포함
    public float catchMultiplier = 4f;      // 잡기 성공 시 잡기 배율 (단일 — 별도 보너스 없음)

    // 기본 전체배율 — 모든 공격의 전체배율 출발점(×1). 설정값과 런타임 값을 분리해 추후 강화 가능
    // (런타임 복사본은 ScoreManager.baseTotalMult — 보스 전환 유지, 새 런에서 이 값으로 초기화)
    public float baseTotalMult = 1f;
}
