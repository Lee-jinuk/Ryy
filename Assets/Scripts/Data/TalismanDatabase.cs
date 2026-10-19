// ──────────────────────────────────────────────
// TalismanDatabase.cs — 부적 카드 정의 에셋
// 카드 정의의 단일 기준 데이터. Scene에 카드 목록을
// 저장하지 않는다 — 이 에셋 하나만 편집하면 된다.
// 런타임 덱·손패는 이 목록을 참조만 하며 변경하지 않는다.
// 에셋 생성: Assets 우클릭 → Create → RYY → Talisman Database
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TalismanDatabase", menuName = "RYY/Talisman Database")]
public class TalismanDatabase : ScriptableObject
{
    public List<TalismanData> cards = new List<TalismanData>();
}
