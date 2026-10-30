using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 말판과 말 이동 담당 — 30칸 속성 말판 생성, 말 이동, 완주 판정 보조, 말판 UI 갱신
// 말판 데이터 원본은 YutGameManager.board (Inspector 노출 유지) — 이 클래스가 유일한 쓰기 경로
// 배치(layoutGenerated)·말 위치(pieceIndex/totalMoved)와 런 누적 완주(lapCount) 초기화는 분리한다
// — 완주는 새 런(ResetForNewRun)에서만 0, 보스 전환에서는 누적 유지 (2026-10-27 확정)
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

    // 속성 칸 1개 독립 추첨 — 불30 물30 풀30 땅10 쇠0(생성 제외) + 점괘 속성 확률 강화.
    // 강화는 이 배치 생성 시점의 누적값을 ApplyPercentBonuses 규칙으로 반영한다 —
    // 대상 +N%p, 비대상은 기존 비율로 차감(쇠 기본 0 → 생성 제외 유지).
    // START 고정·고정 칸 수 보장 없음·이미 생성된 배치는 재추첨하지 않는다
    private TileAttribute DrawAttribute()
    {
        var baseP = new float[AttributeWeights.Length];
        var bonusP = new float[AttributeWeights.Length];
        bool anyBonus = false;
        for (int i = 0; i < AttributeWeights.Length; i++)
        {
            baseP[i] = AttributeWeights[i].percent;
            bonusP[i] = gm.fortuneManager.GetAttrProbBonus(AttributeWeights[i].attribute);
            anyBonus |= bonusP[i] > 0f;
        }
        float[] pcts = anyBonus ? YutRollManager.ApplyPercentBonuses(baseP, bonusP) : baseP;
        float roll = Random.Range(0f, 100f);
        float acc = 0f;
        for (int i = 0; i < AttributeWeights.Length; i++)
        {
            acc += pcts[i];
            if (roll < acc)
            {
                return AttributeWeights[i].attribute;
            }
        }
        return TileAttribute.Earth;   // 예외 반환 — 쇠가 어떤 경로로도 생성되지 않게 한다
    }

    // 속성 확률 점괘의 실효 판정 — 대상 속성에 +pct를 가상 적용했을 때 다음 배치의
    // 발생 확률이 바뀌는지 비교한다. 순수 조회 — 배치·강화·추첨 상태를 바꾸지 않는다
    public bool WouldChangeAttrWeights(TileAttribute target, float pct)
    {
        if (pct <= 0f)
        {
            return false;
        }
        var baseP = new float[AttributeWeights.Length];
        var bonusP = new float[AttributeWeights.Length];
        for (int i = 0; i < AttributeWeights.Length; i++)
        {
            baseP[i] = AttributeWeights[i].percent;
            bonusP[i] = gm.fortuneManager.GetAttrProbBonus(AttributeWeights[i].attribute);
        }
        float[] current = YutRollManager.ApplyPercentBonuses(baseP, bonusP);
        for (int i = 0; i < AttributeWeights.Length; i++)
        {
            if (AttributeWeights[i].attribute == target)
            {
                bonusP[i] += pct;
            }
        }
        float[] next = YutRollManager.ApplyPercentBonuses(baseP, bonusP);
        for (int i = 0; i < current.Length; i++)
        {
            if (Mathf.Abs(current[i] - next[i]) > 0.0001f)
            {
                return true;
            }
        }
        return false;
    }

    // 보스전 진입 시 호출 — 매 실제 보스전 시작 시 누적 속성 확률로 배치를 새로 생성한다
    // (2026-10-23 사용자 확정 '매 보스전 말판 재배치' 4단계 구현 — 런당 1회 생성 규칙 폐기.
    // 소개 화면·상점·리프레시·UI 갱신·점괘 선택 직후·건너뛰기에서는 재배치하지 않는다).
    // 말 위치·보스전 이동 진행도는 초기화하고 런 누적 완주(lapCount)는 유지한다
    public void SetupBoard()
    {
        GenerateLayout();
        gm.board.pieceIndex = 0;   // 시작점 = START (인덱스 0) — 강제 배치는 완주 증가 아님
        gm.board.totalMoved = 0;
        UpdateBoardUI();
    }

    // 역행부 — 말·보스 위치와 부대 상태(물 연계·저장 도트·런 누적 완주·런 강화)를 유지한 채
    // START를 제외한 칸의 속성만 다시 추첨한다. 보스전 초기화(SetupBoard)와 달리
    // 말 위치·이동 진행도·완주를 건드리지 않는다 (2026-10-28 확정)
    public void ReshuffleAttributes()
    {
        for (int i = 1; i < gm.board.tiles.Count; i++)
        {
            gm.board.tiles[i].attribute = DrawAttribute();
        }
        UpdateBoardUI();
        Debug.Log("역행부 — 말판 속성 재배치 (말·보스 위치·연계·도트·완주 유지)");
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

    // ── 말판 표시는 BoardUI 소유 — 아래는 호출 경로 유지용 위임 래퍼 ──
    // (BoardUI가 없으면 표시만 생략 — EnsureUIComponents가 런타임에 자동 부착)

    // 말판 UI 갱신 — 칸의 자식 Text에 속성 이름 표시, 'BellIcon' 이름의 자식은 땅 칸(방울 뽑기) 표시
    public void UpdateBoardUI()
    {
        if (gm.boardUI != null) gm.boardUI.UpdateBoardDisplay(gm);
    }

    // 말 UI를 해당 칸 위치로 이동 (정산 데이터의 경유 칸을 순서대로 재생)
    public void MovePieceVisual(int tileIndex)
    {
        if (gm.boardUI != null) gm.boardUI.MovePiece(gm, tileIndex);
    }

    // 보스 말 UI를 해당 칸 위치로 즉시 이동 — 플레이어 말 상태와 완전히 분리 (이번 단계는 즉시 위치 변경, 이동 연출 없음)
    // 보스 말이 도착·통과한 칸의 공격·방울·상성 효과는 발동하지 않는다 (순수 표시)
    public void MoveBossPieceVisual(int tileIndex)
    {
        if (gm.boardUI != null) gm.boardUI.MoveBossPiece(gm, tileIndex);
    }

    // 보스 이동 예고 표식을 예정 도착 칸 위치에 배치 — 표식은 순수 표시이며
    // 잡기 판정·칸 효과와 무관. 본체(bossPiece)와 별개 오브젝트
    public void MoveBossPreviewVisual(int tileIndex)
    {
        if (gm.boardUI != null) gm.boardUI.MovePreview(gm, tileIndex);
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
