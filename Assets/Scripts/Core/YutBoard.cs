using System.Collections.Generic;
using UnityEngine;

// 말판 칸 데이터 — 로직과 UI를 분리해 관리 (UI 참조 없음)
[System.Serializable]
public class BoardTile
{
    public int index;               // 고유 인덱스 0~17
    public int baseScore;           // 10~180 중복 없는 기본 점수 (보스전 시작 시 무작위 배치)
    public int bonusScore;          // 칸 강화 등 추가 보너스 (향후 아이템용 슬롯)
    public bool hasBellEvent;       // 방울 이벤트 칸 — 점수 칸에 부여되는 추가 속성
    public bool bellUsedThisRound;  // 판당 1회 발동 제한 플래그
    public int Score => baseScore + bonusScore;
}

// 방울 데이터 — 현재는 이름/설명만 (실제 발동 효과는 미구현)
[System.Serializable]
public class BellData
{
    public string bellName = "방울";
    [TextArea] public string description = "";
}

// 18칸 원형 말판 상태 — 점수 배치·말 위치·완주 횟수를 관리.
// 보스전 시작 시 재구축되며 같은 보스전의 3판 동안 유지된다
[System.Serializable]
public class YutBoard
{
    public const int TileCount = 18;

    public List<BoardTile> tiles = new List<BoardTile>();
    public int totalMoved = 0;    // 시작점(BoardTile_01) 기준 누적 이동 칸 수
    public int pieceIndex = 0;    // 현재 말 위치 (칸 인덱스, 시작 = 0 = BoardTile_01)
    public int lapCount = 0;      // 완주 횟수 — BoardTile_18을 지나 BoardTile_01로 넘어갈 때 증가

    // steps칸 시계방향 이동. 경유 칸 인덱스를 path에 기록하고 도착 칸 인덱스를 반환
    public int MovePiece(int steps, List<int> path)
    {
        path?.Clear();
        for (int i = 1; i <= steps; i++)
        {
            int p = pieceIndex + i;
            if (p >= TileCount)
            {
                p -= TileCount;
            }
            path?.Add(p);
        }
        totalMoved += steps;
        // 18칸을 넘어 순환할 때마다 완주 (BoardTile_18 → BoardTile_01 경계 통과)
        while (totalMoved >= TileCount * (lapCount + 1))
        {
            lapCount++;
        }
        pieceIndex = totalMoved % TileCount;
        return pieceIndex;
    }
}
