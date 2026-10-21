using System.Collections.Generic;
using UnityEngine;

// 말판 칸 속성 — 인덱스 0은 START 고정, 나머지 칸은 독립 추첨으로 속성을 정한다
// 땅 = 방울 뽑기 화면 / 쇠 = 공격점수 0 / 불·물·풀(Forest 표시명 '풀') = 기본 공격점수 50
public enum TileAttribute { Start, Fire, Water, Forest, Earth, Metal }

// 말판 칸 데이터 — 로직과 UI를 분리해 관리 (UI 참조 없음)
[System.Serializable]
public class BoardTile
{
    public int index;                    // 고유 인덱스 0~29
    public TileAttribute attribute;

    // 기본 공격점수 — 불·물·풀만 50, START·땅·쇠는 0
    public int AttackScore =>
        attribute == TileAttribute.Fire || attribute == TileAttribute.Water || attribute == TileAttribute.Forest
            ? 50
            : 0;
}

// 30칸 원형 말판 상태 — 속성 배치·말 위치·완주 횟수를 관리.
// 배치는 런당 1회 생성해 게임오버까지 유지 (보스 전환·건너뛰기에서 재추첨 없음)
[System.Serializable]
public class YutBoard
{
    public const int TileCount = 30;

    public List<BoardTile> tiles = new List<BoardTile>();
    public bool layoutGenerated;   // 이번 런의 배치 생성 완료 여부 — ResetForNewRun에서 리셋
    public int totalMoved = 0;    // START(인덱스 0) 기준 누적 이동 칸 수
    public int pieceIndex = 0;    // 현재 말 위치 (칸 인덱스, 시작 = 0 = START)
    public int lapCount = 0;      // 완주 횟수 — 인덱스 29를 지나 0(START)으로 넘어갈 때 증가 (기록용, 점수 보너스 없음)

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
        // 30칸을 넘어 순환할 때마다 완주 (인덱스 29 → 0 경계 통과)
        while (totalMoved >= TileCount * (lapCount + 1))
        {
            lapCount++;
        }
        pieceIndex = totalMoved % TileCount;
        return pieceIndex;
    }
}
