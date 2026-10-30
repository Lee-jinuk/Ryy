using UnityEngine;
using UnityEngine.UI;

// 말판 UI — 칸 표시·플레이어/보스 말·이동 예고·완주·착지 정보.
// 표시 전용 (말판 규칙·이동은 BoardManager·BossManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.boardPanel·boardTiles·playerPiece·bossPiece·
//       bossMovePreview·lapCountText·landingTileText
public class BoardUI : MonoBehaviour
{
    public GameObject boardPanel;                       // 원형 말판 부모 (있으면 상태에 맞춰 표시)
    public GameObject[] boardTiles = new GameObject[30]; // 칸 오브젝트 30개 — START → 이동 방향 29칸 순서. 자식 Text = 속성명, 'BellIcon' 자식 = 땅 칸 표시
    public RectTransform playerPiece;                   // 플레이어 말 — 도착 칸 위치로 이동
    public RectTransform bossPiece;                     // 보스 말 — 플레이어와 별개, 최초 투척 성공 시마다 이동 (미연결 시 표시만 생략)
    public RectTransform bossMovePreview;               // 보스 다음 이동 예고 표식 — 준비 단계에 예정 도착 칸 표시, 최초 투척 접수 시 숨김
    public Text lapCountText;                           // 완주 횟수 (숫자만 출력, 미연결 시 무시)
    public Text landingTileText;                        // 착지 칸 정보 — "불 - 50점" 형식 (미연결 시 무시)

    private bool bossPieceWarned;

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    public void Refresh(YutGameManager gm, bool boardActive)
    {
        // 보스 이동 예고 표식 — 준비 단계(미투척 세션)에 예정 도착 칸을 표시하고
        // 최초 투척 접수·정산·비전투 상태에서는 숨긴다. 표식은 표시 전용 — 잡기 판정은 실제 보스 위치 사용
        if (gm.bossMovePreview != null)
        {
            YutThrowSession session = gm.rollManager.session;
            bool previewVisible = gm.currentState == GameState.Playing
                && session != null && !session.rolled
                && gm.bossManager.movePlanReady;
            gm.bossMovePreview.gameObject.SetActive(previewVisible);
            if (previewVisible)
            {
                MovePreview(gm, gm.bossManager.PlannedDestination);
            }
        }

        // 말판 UI — 칸 점수·방울 아이콘·완주 횟수 (Start에서는 갱신 생략)
        if (boardActive)
        {
            UpdateBoardDisplay(gm);
        }

        // 착지 칸 정보 — "불 - 50점" (상성·윷 배율 적용 전 칸 기본점수). 새 보스전·새 런에서 비워진다
        if (gm.landingTileText != null)
        {
            gm.landingTileText.text = gm.landingTileInfo;
        }
    }

    // 칸의 자식 Text에 속성 이름 표시, 'BellIcon' 이름의 자식은 땅 칸(방울 뽑기) 표시
    public void UpdateBoardDisplay(YutGameManager gm)
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
                tileText.text = BoardManager.AttributeName(gm.board.tiles[i].attribute);
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
    public void MovePiece(YutGameManager gm, int tileIndex)
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

    // 보스 말 UI를 해당 칸 위치로 즉시 이동 — 플레이어 말 상태와 완전히 분리 (이동 연출 없음)
    // 보스 말이 도착·통과한 칸의 공격·방울·상성 효과는 발동하지 않는다 (순수 표시)
    public void MoveBossPiece(YutGameManager gm, int tileIndex)
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
    public void MovePreview(YutGameManager gm, int tileIndex)
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
}
