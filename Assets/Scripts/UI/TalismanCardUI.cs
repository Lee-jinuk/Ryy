using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 부적 손패 카드 1장 — 배경 Image + 제목 Text 표시만 담당한다.
// 클릭 = 공용 설명창 표시, 위로 드래그 후 사용 영역에 놓기 = 사용 시도.
// 클릭과 드래그는 EventSystem이 분리한다 — 드래그가 발생하면 PointerClick이 오지 않는다.
// 실제 사용 판정·소모·효과는 TalismanManager 경유 (handUI → gm.UseTalisman) — 이 컴포넌트는
// 카드 이름·색상으로 사용 가능 여부를 판단하지 않는다.
public class TalismanCardUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image background;          // 카드 배경 — appearance에 따라 부적 스프라이트로 교체
    public Text nameText;             // 카드 제목 (Raycast Target은 꺼둔다)
    public Button button;             // 강조·전환 표시용 — OnClick에 사용 함수를 연결하지 않는다
    public int slotIndex = -1;        // 손패 슬롯 인덱스 — Refresh가 매번 갱신
    [HideInInspector] public TalismanData boundCard; // 현재 표시 카드 인스턴스 — 드롭 시 손패 재검사용
    [HideInInspector] public TalismanHandUI handUI;
    [HideInInspector] public float homeY;            // 정렬 기준 Y — 모든 카드가 같은 값을 쓴다
    [HideInInspector] public int homeSibling = -1;   // 정렬 기준 형제 순서 — 드래그 후 복구용

    private RectTransform rt;
    private Vector2 basePos;
    private bool dragging;

    void Awake()
    {
        rt = transform as RectTransform;
        if (handUI == null) handUI = GetComponentInParent<TalismanHandUI>();
        if (background == null) background = GetComponent<Image>();
        if (button == null) button = GetComponent<Button>();
        if (nameText == null) nameText = GetComponentInChildren<Text>();
    }

    // 첫 배치 전의 원래 Y·형제 순서를 기준값으로 한 번만 기록한다
    public void CaptureHome()
    {
        if (homeSibling >= 0 || rt == null) return;
        homeY = rt.anchoredPosition.y;
        homeSibling = transform.GetSiblingIndex();
    }

    public void SetBasePos(Vector2 pos)
    {
        basePos = pos;
        if (!dragging && rt != null) rt.anchoredPosition = pos;
    }

    public void Restore()
    {
        if (rt != null) rt.anchoredPosition = basePos;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        handUI?.OnCardClicked(this);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragging = true;
        transform.SetAsLastSibling(); // 드래그 중 다른 카드보다 앞에 표시
        handUI?.OnCardDragBegin(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rt == null) return;
        float s = handUI != null && handUI.uiCanvas != null ? handUI.uiCanvas.scaleFactor : 1f;
        rt.anchoredPosition += eventData.delta / Mathf.Max(0.01f, s);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragging = false;
        handUI?.OnCardDragEnd(this, eventData);
    }
}
