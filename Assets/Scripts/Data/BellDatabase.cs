// ──────────────────────────────────────────────
// BellDatabase.cs — 방울 정의 목록 에셋
// 방울 12종의 단일 기준. Scene에 목록을 중복 저장하지 않는다.
// 에셋 생성: Assets 우클릭 → Create → RYY → Bell Database
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BellDatabase", menuName = "RYY/Bell Database")]
public class BellDatabase : ScriptableObject
{
    public List<BellData> bells = new List<BellData>();
}
