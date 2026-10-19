// ──────────────────────────────────────────────
// BossDatabase.cs — 보스 목록 에셋
// 보스 순서·이름·최대 HP·능력 데이터의 단일 기준.
// 런타임 상성 등 보스별 무작위 상태는 여기 저장하지 않는다.
// 에셋 생성: Assets 우클릭 → Create → RYY → Boss Database
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BossDatabase", menuName = "RYY/Boss Database")]
public class BossDatabase : ScriptableObject
{
    public List<BossData> bosses = new List<BossData>();
}
