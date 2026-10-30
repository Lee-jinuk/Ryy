using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 부적 손패 UI — 이미지 카드 방식.
// 표시 전용 (덱·손패 상태와 사용 판정은 TalismanManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.talismanNoticeText
//
// 카드 동작:
//   - 보유 카드만 표시, 빈 슬롯 숨김. 이름·appearance 스프라이트만 갱신 (설명은 카드 안에 두지 않음)
//   - X 간격을 카드 수·컨테이너 너비에 맞춰 조절 (겹침 허용, 크기·회전·Y 고정)
//   - 클릭 = 공용 설명창 표시 / 위로 드래그→사용 영역 드롭 = gm.UseTalisman 경유 사용 시도
//   - 카드 선택과 사용 가능 여부는 분리 — 사용 불가 카드도 설명 표시
public class TalismanHandUI : MonoBehaviour
{
    public Text talismanNoticeText;                       // 부적 안내 — "손패가 가득 차 부적 N장이 소멸했습니다." (미연결 시 무시)

    [Header("카드")]
    public TalismanCardUI[] cardSlots = new TalismanCardUI[10]; // 카드 슬롯 — 손패 한도 10과 일치
    public Sprite yellowSprite;             // appearance=노란부적 카드 배경
    public Sprite whiteSprite;              // appearance=하얀부적 카드 배경
    public RectTransform cardContainer;     // 카드 배치 기준 영역 — 미연결 시 첫 카드의 부모
    public RectTransform descriptionPanel;  // 공용 설명창 루트 — 미연결 시 클릭 설명 없음
    public Text descriptionNameText;        // 설명창 이름
    public Text descriptionText;            // 설명창 설명
    public RectTransform useDropArea;       // 위로 드래그해 사용하는 영역 — 미연결 시 드래그 사용 없음
    public Text handCountText;              // 손패 장수 "N/10" (미연결 시 무시)
    public float cardGap = 10f;             // 카드가 다 들어갈 때의 최대 간격 — 넘치면 음수로 겹침
    public float descriptionOffset = 8f;    // 설명창과 카드 상단 사이 여백
    public float dragUseMinPixels = 80f;    // 위로 드래그 사용 인정 최소 픽셀 (스크린 좌표)

    [HideInInspector] public Canvas uiCanvas;

    private bool cardsPrepared;
    private TalismanData selectedCard;      // 설명창이 표시 중인 카드 인스턴스
    private YutGameManager cachedGm;

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    public void Refresh(YutGameManager gm)
    {
        cachedGm = gm;
        if (uiCanvas == null) uiCanvas = GetComponentInParent<Canvas>();
        if (!cardsPrepared) PrepareCards();
        RefreshCards(gm);

        if (handCountText != null)
            handCountText.text = $"{gm.talismanManager.hand.Count}/{TalismanManager.HandLimit}";

        // 표시 중이던 카드가 손패에서 사라지면 설명창 닫기 (사용·소멸·보스전 리셋)
        if (selectedCard != null && !gm.talismanManager.hand.Contains(selectedCard))
            HideDescription();

        // 초과 획득 소멸 안내 — 다음 선택창이 열리면 지워진다
        if (gm.talismanNoticeText != null)
        {
            gm.talismanNoticeText.text = gm.talismanManager.pendingOverflowNotice > 0
                ? $"손패가 가득 차 부적 {gm.talismanManager.pendingOverflowNotice}장이 소멸했습니다."
                : "";
        }
    }

    // 카드 배치 기준 — 부모 미연결 시 첫 카드의 부모로 대체하고 각 카드의 원래 Y·형제 순서 기록
    void PrepareCards()
    {
        cardsPrepared = true;
        foreach (var c in cardSlots)
        {
            if (c == null) continue;
            if (c.handUI == null) c.handUI = this;
            c.CaptureHome();
        }
        if (cardContainer == null)
        {
            foreach (var c in cardSlots)
            {
                if (c != null) { cardContainer = c.transform.parent as RectTransform; break; }
            }
        }
    }

    void RefreshCards(YutGameManager gm)
    {
        var hand = gm.talismanManager.hand;
        int shown = 0;
        for (int i = 0; i < cardSlots.Length && i < TalismanManager.HandLimit; i++)
        {
            var c = cardSlots[i];
            if (c == null) continue;
            bool hasCard = i < hand.Count;
            c.gameObject.SetActive(hasCard);
            c.slotIndex = i;
            c.boundCard = hasCard ? hand[i] : null;
            if (!hasCard) continue;
            shown++;
            if (c.nameText != null) c.nameText.text = hand[i].talismanName;
            if (c.background != null)
            {
                var s = SpriteFor(hand[i].appearance); // 미정·미연결이면 배치 스프라이트 유지
                if (s != null) c.background.sprite = s;
            }
            if (c.button != null) c.button.interactable = true; // 선택과 사용 가능 여부 분리
        }
        LayoutCards(shown);
    }

    Sprite SpriteFor(TalismanAppearance ap)
    {
        if (ap == TalismanAppearance.노란부적) return yellowSprite;
        if (ap == TalismanAppearance.하얀부적) return whiteSprite;
        return null;
    }

    // 일자 배치 — 모든 카드 같은 Y·회전·크기, X 간격만 카드 수와 컨테이너 너비로 조절.
    // 컨테이너보다 카드가 넓으면 간격이 음수가 돼 균등 겹침. 카드 순서와 형제 순서도 복구한다.
    void LayoutCards(int shown)
    {
        if (shown <= 0 || cardContainer == null) return;
        float cw = 0f;
        foreach (var c in cardSlots)
        {
            if (c != null && c.gameObject.activeSelf)
            {
                cw = (c.transform as RectTransform).rect.width;
                break;
            }
        }
        if (cw <= 0f) return;
        float w = cardContainer.rect.width;
        float spacing = shown <= 1 ? 0f : Mathf.Min(cw + cardGap, (w - cw) / (shown - 1));
        int j = 0;
        for (int i = 0; i < cardSlots.Length; i++)
        {
            var c = cardSlots[i];
            if (c == null || !c.gameObject.activeSelf) continue;
            c.SetBasePos(new Vector2(-(shown - 1) * spacing * 0.5f + j * spacing, c.homeY));
            if (c.homeSibling >= 0)
                c.transform.SetSiblingIndex(Mathf.Min(c.homeSibling, c.transform.parent.childCount - 1));
            j++;
        }
    }

    int CountActiveCards()
    {
        int n = 0;
        foreach (var c in cardSlots) if (c != null && c.gameObject.activeSelf) n++;
        return n;
    }

    // ── 카드 입력 — TalismanCardUI에서 호출 ──

    // 클릭 = 설명 선택. 같은 카드 재클릭은 닫기. 사용 불가 카드도 설명은 표시한다.
    public void OnCardClicked(TalismanCardUI card)
    {
        if (card == null || card.boundCard == null || descriptionPanel == null) return;
        if (selectedCard == card.boundCard && descriptionPanel.gameObject.activeSelf)
        {
            HideDescription();
            return;
        }
        selectedCard = card.boundCard;
        if (descriptionNameText != null) descriptionNameText.text = selectedCard.talismanName;
        if (descriptionText != null) descriptionText.text = selectedCard.description;
        descriptionPanel.gameObject.SetActive(true);
        PositionDescription(card);
    }

    // 카드 상단 위에 설명창을 띄우고, 캔버스 경계를 벗어나면 안쪽으로 밀어 넣는다
    void PositionDescription(TalismanCardUI card)
    {
        var cardRt = card.transform as RectTransform;
        var parent = descriptionPanel.parent as RectTransform;
        if (cardRt == null || parent == null) return;
        Camera cam = uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? uiCanvas.worldCamera : null;
        Vector3[] corners = new Vector3[4];
        cardRt.GetWorldCorners(corners); // 0=좌하 1=좌상 2=우상 3=우하
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, (corners[1] + corners[2]) * 0.5f);
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, cam, out Vector2 local))
            descriptionPanel.anchoredPosition = local + new Vector2(0f, descriptionOffset);
        if (uiCanvas == null) return;
        var canvasRt = uiCanvas.transform as RectTransform;
        Vector3[] pc = new Vector3[4], cc = new Vector3[4];
        descriptionPanel.GetWorldCorners(pc);
        canvasRt.GetWorldCorners(cc);
        float dx = pc[0].x < cc[0].x ? cc[0].x - pc[0].x : (pc[2].x > cc[2].x ? cc[2].x - pc[2].x : 0f);
        float dy = pc[0].y < cc[0].y ? cc[0].y - pc[0].y : (pc[1].y > cc[1].y ? cc[1].y - pc[1].y : 0f);
        descriptionPanel.position += new Vector3(dx, dy, 0f);
    }

    void HideDescription()
    {
        selectedCard = null;
        if (descriptionPanel != null) descriptionPanel.gameObject.SetActive(false);
    }

    public void OnCardDragBegin(TalismanCardUI card)
    {
        HideDescription();
    }

    // 위로 dragUseMinPixels 이상 끌어 사용 영역 안에 놓았을 때만 사용 시도.
    // 손패가 드래그 중 바뀌었을 수 있으니 인덱스가 아니라 카드 인스턴스로 다시 찾는다.
    // 실패·영역 밖·거리 부족이면 재정렬이 카드를 원위치로 되돌린다 (소모 없음)
    public void OnCardDragEnd(TalismanCardUI card, PointerEventData e)
    {
        bool upward = e.position.y - e.pressPosition.y >= dragUseMinPixels;
        bool inArea = useDropArea != null && RectTransformUtility.RectangleContainsScreenPoint(
            useDropArea, e.position, e.pressEventCamera);
        if (upward && inArea && cachedGm != null && card.boundCard != null)
        {
            int idx = cachedGm.talismanManager.hand.IndexOf(card.boundCard);
            if (idx >= 0) cachedGm.UseTalisman(idx); // 성공 시 UpdateUI → Refresh가 재정렬
        }
        if (cachedGm != null) LayoutCards(CountActiveCards());
    }
}
