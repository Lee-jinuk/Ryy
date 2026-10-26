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

    // 기본 공격점수 — 불·물·풀만 50, START·땅·쇠는 0. 계산 기준은 YutBoard.BaseAttackScore로 통일
    public int AttackScore => YutBoard.BaseAttackScore(attribute);
}

// 30칸 원형 말판 상태 — 속성 배치·말 위치·완주 횟수를 관리.
// 배치는 런당 1회 생성해 게임오버까지 유지 (보스 전환·건너뛰기에서 재추첨 없음)
[System.Serializable]
public class YutBoard
{
    public const int TileCount = 30;

    // 속성별 기본 공격점수 — 불·물·풀 50, START·땅·쇠 0.
    // 정산(ScoreManager의 칸 점수)과 표시(능력치 정보판)가 이 값을 공용으로 사용한다
    public static int BaseAttackScore(TileAttribute attribute) =>
        attribute == TileAttribute.Fire || attribute == TileAttribute.Water || attribute == TileAttribute.Forest
            ? 50
            : 0;

    public List<BoardTile> tiles = new List<BoardTile>();
    public bool layoutGenerated;   // 이번 런의 배치 생성 완료 여부 — ResetForNewRun에서 리셋
    public int totalMoved = 0;    // 보스전 이동 진행도 — START(인덱스 0) 기준 누적 칸 수.
                                  // 보스전 시작에서만 초기화하고 다음 보스전으로 부분 진행을 이월하지 않는다
    public int pieceIndex = 0;    // 현재 말 위치 (칸 인덱스, 시작 = 0 = START)
    public int lapCount = 0;      // 런 누적 완주 횟수 — 실제 이동으로 START 경계를 넘은 횟수.
                                  // 보스 전환·건너뛰기에서 유지하고 새 런 초기화에서만 0 (2026-10-27 확정)

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
        int prevTotal = totalMoved;
        totalMoved += steps;
        // 런 누적 완주 — 이번 보스전 이동 진행도가 START 경계(30칸 배수)를 넘은 횟수만큼 증가.
        // START에 정확히 도착하거나 통과하면 증가한다. START 배치 초기화·이동 0·UI 갱신은 제외
        lapCount += totalMoved / TileCount - prevTotal / TileCount;
        pieceIndex = totalMoved % TileCount;
        return pieceIndex;
    }
}
