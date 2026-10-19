// ──────────────────────────────────────────────
// GameBalanceConfig.cs — 게임 밸런스 설정 에셋
// 기본 투척 횟수·윷 배율·보스 이동 단계 범위의 단일 기준.
// 런 중 윷 강화는 복사된 런타임 스탯에만 적용된다 — 원본 미변경.
// 에셋 생성: Assets 우클릭 → Create → RYY → Game Balance Config
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "RYY/Game Balance Config")]
public class GameBalanceConfig : ScriptableObject
{
    public int maxThrows = 5;                 // 보스전 기본 투척 횟수

    // 윷별 스탯 — mult가 배율(도2/개3/걸4/윷5/모6).
    // basicBonus는 현재 공격 공식에 미적용(예약 필드)
    public List<YutStatEntry> yutStats = new List<YutStatEntry>
    {
        new YutStatEntry { result = YutResult.Do,   basicBonus = 0, mult = 2 },
        new YutStatEntry { result = YutResult.Gae,  basicBonus = 0, mult = 3 },
        new YutStatEntry { result = YutResult.Geol, basicBonus = 0, mult = 4 },
        new YutStatEntry { result = YutResult.Yut,  basicBonus = 0, mult = 5 },
        new YutStatEntry { result = YutResult.Mo,   basicBonus = 0, mult = 6 },
    };

    // 누적 피해 비율 단계별 보스 이동 범위 — 단계 i는 피해 ≥ 최대 HP의 i/N
    // (기본 3단계: <1/3 → 4~7 / ≥1/3 → 3~6 / ≥2/3 → 2~5). 두 배열 길이는 같아야 함
    public int[] bossMoveStageMin = { 4, 3, 2 };
    public int[] bossMoveStageMax = { 7, 6, 5 };
}
