using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 말판과 말 이동 담당 — 30칸 속성 말판 생성, 말 이동, 완주 판정 보조, 말판 UI 갱신
// 말판 데이터 원본은 YutGameManager.board (Inspector 노출 유지) — 이 클래스가 유일한 쓰기 경로
// 배치(layoutGenerated)와 말 위치(pieceIndex/totalMoved/lapCount) 초기화는 분리한다
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

    // 속성별 독립 추첨 확률 (START 제외 29칸 각각에 적용 — 고정 칸 수 비율이 아니며 최소 개수 보장 없음)
    // 쇠 = 0% — 정의·표시는 유지하고 신규 배치 추첨에서만 제외 (2026-10-21 확정)
    private static readonly (TileAttribute attribute, float percent)[] AttributeWeights =
    {
        (TileAttribute.Fire, 30f),
        (TileAttribute.Water, 30f),
        (TileAttribute.Forest, 30f),
        (TileAttribute.Earth, 10f),
        (TileAttribute.Metal, 0f),
    };

    public static string AttributeName(TileAttribute attribute) => attribute switch
    {
        TileAttribute.Start => "START",
        TileAttribute.Fire => "불",
        TileAttribute.Water => "물",
        TileAttribute.Forest => "풀",
        TileAttribute.Earth => "땅",
        TileAttribute.Metal => "쇠",
        _ => attribute.ToString(),
    };

    // 속성 칸 1개 독립 추첨 — 불30 물30 풀30 땅10 쇠0(생성 제외) (%)
    private static TileAttribute DrawAttribute()
    {
        float roll = Random.Range(0f, 100f);
        float acc = 0f;
        foreach ((TileAttribute attribute, float percent) in AttributeWeights)
        {
            acc += percent;
            if (roll < acc)
            {
                return attribute;
            }
        }
        return TileAttribute.Earth;   // 예외 반환 — 쇠가 어떤 경로로도 생성되지 않게 한다
    }

    // 보스전 진입 시 호출 — 배치는 런당 1회만 생성하고 게임오버까지 유지한다
    // (보스 처치·다음 보스 진입·건너뛰기에서 재추첨하지 않음). 말 위치·완주는 매 보스전 초기화 (기존 동작 유지)
    public void SetupBoard()
    {
        if (!gm.board.layoutGenerated)
        {
            GenerateLayout();
        }
        gm.board.pieceIndex = 0;   // 시작점 = START (인덱스 0)
        gm.board.totalMoved = 0;
        gm.board.lapCount = 0;
        UpdateBoardUI();
    }

    // 30칸 배치 생성: 인덱스 0 = START 고정, 1~29 = 속성 독립 추첨
    private void GenerateLayout()
    {
        gm.board.tiles.Clear();
        gm.board.tiles.Add(new BoardTile { index = 0, attribute = TileAttribute.Start });
        for (int i = 1; i < YutBoard.TileCount; i++)
        {
            gm.board.tiles.Add(new BoardTile { index = i, attribute = DrawAttribute() });
        }
        gm.board.layoutGenerated = true;

        int[] counts = new int[6];
        foreach (BoardTile tile in gm.board.tiles)
        {
            counts[(int)tile.attribute]++;
        }
        Debug.Log($"--- 말판 배치 완료 (30칸, 런 고정) | 불 {counts[1]} 물 {counts[2]} 풀 {counts[3]} 땅 {counts[4]} 쇠 {counts[5]} ---");
    }

    // 말판 UI 갱신 — 칸의 자식 Text에 속성 이름 표시, 'BellIcon' 이름의 자식은 땅 칸(방울 뽑기) 표시
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
                tileText.text = AttributeName(gm.board.tiles[i].attribute);
            }
            Transform bellIcon = tileObj.transform.Find("BellIcon");
            if (bellIcon != null)
            {
                bellIcon.gameObject.SetActive(gm.board.tiles[i].attribute == TileAttribute.Earth);
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

    // 보스 말 UI를 해당 칸 위치로 즉시 이동 — 플레이어 말 상태와 완전히 분리 (이번 단계는 즉시 위치 변경, 이동 연출 없음)
    // 보스 말이 도착·통과한 칸의 공격·방울·상성 효과는 발동하지 않는다 (순수 표시)
    private bool bossPieceWarned;

    public void MoveBossPieceVisual(int tileIndex)
    {
        if (gm.bossPiece == null)
        {
            if (!bossPieceWarned)
            {
                bossPieceWarned = true;
                Debug.LogWarning("[YutGameManager] bossPiece 미연결 — 보스 말 위치는 갱신되지만 화면에 표시되지 않습니다. 말판에 보스 말 오브젝트를 만들어 연결하세요.", gm);
            }
            return;
        }
        if (tileIndex < 0 || tileIndex >= gm.boardTiles.Length)
        {
            return;
        }
        GameObject tileObj = gm.boardTiles[tileIndex];
        if (tileObj != null)
        {
            gm.bossPiece.position = tileObj.transform.position;
        }
    }

    // 보스 이동 예고 표식을 예정 도착 칸 위치에 배치 — 표식은 순수 표시이며
    // 잡기 판정·칸 효과와 무관. 본체(bossPiece)와 별개 오브젝트
    public void MoveBossPreviewVisual(int tileIndex)
    {
        if (gm.bossMovePreview == null || tileIndex < 0 || tileIndex >= gm.boardTiles.Length)
        {
            return;
        }
        GameObject tileObj = gm.boardTiles[tileIndex];
        if (tileObj != null)
        {
            gm.bossMovePreview.position = tileObj.transform.position;
        }
    }

    // 런 종료 시 말판 배치·말·완주 초기화 — 다음 런의 첫 보스전에서 새 배치를 생성한다
    public void ResetForNewRun()
    {
        gm.board.tiles.Clear();
        gm.board.layoutGenerated = false;
        gm.board.pieceIndex = 0;
        gm.board.totalMoved = 0;
        gm.board.lapCount = 0;
        MovePieceVisual(0);
    }
}
