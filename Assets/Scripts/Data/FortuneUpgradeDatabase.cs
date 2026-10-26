using System.Collections.Generic;
using UnityEngine;

// FortuneUpgradeDatabase.cs — 점괘 강화 정의 목록 에셋
// 16종 강화의 단일 기준. Scene에 목록을 중복 저장하지 않는다.
// 에셋 생성: Assets 우클릭 → Create → RYY → Fortune Upgrade Database
[CreateAssetMenu(fileName = "FortuneUpgradeDatabase", menuName = "RYY/Fortune Upgrade Database")]
public class FortuneUpgradeDatabase : ScriptableObject
{
    public List<FortuneUpgradeData> upgrades = new List<FortuneUpgradeData>();
}
