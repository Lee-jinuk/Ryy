using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 말판과 말 이동 담당 — 18칸 점수 무작위 배치, 말 이동, 완주 판정 보조, 말판 UI 갱신
// 말판 데이터 원본은 YutGameManager.board (Inspector 노출 유지) — 이 클래스가 유일한 쓰기 경로
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class BoardManager
{
    private readonly YutGameManager gm;

    public BoardManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 윷 결과 → 이동 칸 수 (도1 개2 걸3 윷4 모5)
    public static int MoveSteps(YutResult result) => result switch
    {
        YutResult.Do => 1,
        YutResult.Gae => 2,
        YutResult.Geol => 3,
        YutResult.Yut => 4,
        YutResult.Mo => 5,
        _ => 0,
    };

    // 보스전 시작 시 말판 재구축: 10~180 중복 없는 무작위 배치 + 방울 칸 무작위 지정 + 말 위치·완주 초기화
    public void SetupBoard()
    {
        List<int> scorePool = new List<int>();
        for (int s = 10; s <= 180; s += 10)
        {
            scorePool.Add(s);
        }
        for (int i = scorePool.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (scorePool[i], scorePool[j]) = (scorePool[j], scorePool[i]);
        }

        gm.board.tiles.Clear();
        for (int i = 0; i < YutBoard.TileCount; i++)
        {
            gm.board.tiles.Add(new BoardTile { index = i, baseScore = scorePool[i] });
        }
        gm.board.pieceIndex = 0;   // 시작점 = BoardTile_01 (인덱스 0)
        gm.board.totalMoved = 0;
        gm.board.lapCount = 0;

        // 방울 이벤트 칸 무작위 지정 (개수는 Inspector 설정값)
        List<int> indices = new List<int>();
        for (int i = 0; i < YutBoard.TileCount; i++)
        {
            indices.Add(i);
        }
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        int bellCount = Mathf.Clamp(gm.bellEventTileCount, 0, YutBoard.TileCount);
        for (int i = 0; i < bellCount; i++)
        {
            gm.board.tiles[indices[i]].hasBellEvent = true;
        }

        UpdateBoardUI();
        Debug.Log($"--- 말판 배치 완료 | 방울 칸: {bellCount}개 ---");
    }

    // 말판 UI 갱신 — 칸의 자식 Text에 점수 표시, 'BellIcon' 이름의 자식으로 방울 칸 표시
    public void UpdateBoardUI()
    {
        for (int i = 0; i < gm.boardTiles.Length && i < gm.board.tiles.Count; i++)
        {
            GameObject tileObj = gm.boardTiles[i];
            if (tileObj == null)
            {
                continue;
            }
            Text tileText = tileObj.GetComponentInChildren<Text>(true);
            if (tileText != null)
            {
                tileText.text = gm.board.tiles[i].Score.ToString();
            }
            Transform bellIcon = tileObj.transform.Find("BellIcon");
            if (bellIcon != null)
            {
                bellIcon.gameObject.SetActive(gm.board.tiles[i].hasBellEvent && !gm.board.tiles[i].bellUsedThisRound);
            }
        }
        if (gm.lapCountText != null)
        {
            gm.lapCountText.text = gm.board.lapCount.ToString();
        }
    }

    // 말 UI를 해당 칸 위치로 이동 (정산 데이터의 경유 칸을 순서대로 재생)
    public void MovePieceVisual(int tileIndex)
    {
        if (gm.playerPiece == null || tileIndex < 0 || tileIndex >= gm.boardTiles.Length)
        {
            return;
        }
        GameObject tileObj = gm.boardTiles[tileIndex];
        if (tileObj != null)
        {
            gm.playerPiece.position = tileObj.transform.position;
        }
    }

    // 런 종료 시 말판·말·완주 초기화 — 다음 보스전에서 SetupBoard로 재배치된다
    public void ResetForNewRun()
    {
        foreach (BoardTile tile in gm.board.tiles)
        {
            tile.bonusScore = 0;
            tile.hasBellEvent = false;
            tile.bellUsedThisRound = false;
        }
        gm.board.pieceIndex = 0;
        gm.board.totalMoved = 0;
        gm.board.lapCount = 0;
        MovePieceVisual(0);
    }
}
