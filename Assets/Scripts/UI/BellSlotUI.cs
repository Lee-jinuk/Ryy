// ──────────────────────────────────────────────
// BellSlotUI.cs — 방울 고정 슬롯 표시 컴포넌트
//
// Scene에 사용자가 직접 배치한 고정 슬롯 1개에 붙인다.
// 이름·설명·상태/가격 Text와 행동 Button을 명시적으로 참조한다 —
// 자식 Text 탐색 순서로 내용을 구분하지 않는다.
// 표시와 클릭 전달만 담당한다 — 돈 계산·방울 효과·장착 판정은
// EconomyManager·BellManager·ShopManager가 처리한다.
// 사용 그룹: 게임 화면 장착 4칸 / 상점 구매 4칸 / 상점 장착·판매 4칸
// ──────────────────────────────────────────────
using UnityEngine;
using UnityEngine.UI;

public class BellSlotUI : MonoBehaviour
{
    public Text nameText;        // 방울 이름
    public Text descText;        // 방울 설명
    public Text statusText;      // 남은 발동 횟수·가격 등 상황별 한 줄
    public Button actionButton;  // 없으면 표시 전용 슬롯 (게임 화면 장착 슬롯)
    public Text actionLabel;     // 버튼 라벨 — 미연결 시 버튼의 자식 Text를 찾는다

    // 슬롯 채우기 — buttonLabel이 비어 있으면 버튼을 숨긴다.
    // 리스너는 매번 교체해 갱신이 반복돼도 누적되지 않는다
    public void Bind(string bellName, string desc, string status, string buttonLabel, bool interactable, UnityEngine.Events.UnityAction onClick)
    {
        if (nameText != null)
        {
            nameText.text = bellName ?? "";
        }
        if (descText != null)
        {
            descText.text = desc ?? "";
        }
        if (statusText != null)
        {
            statusText.text = status ?? "";
        }
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();   // 이전 슬롯의 클릭 동작 잔류 방지
            bool showButton = !string.IsNullOrEmpty(buttonLabel);
            actionButton.gameObject.SetActive(showButton);
            if (showButton)
            {
                if (onClick != null)
                {
                    actionButton.onClick.AddListener(onClick);
                }
                actionButton.interactable = interactable;
            }
        }
        SetLabel(string.IsNullOrEmpty(buttonLabel) ? "" : buttonLabel);
    }

    // 빈 슬롯 — 이전 상품 정보와 클릭 동작이 남지 않게 정리한다
    public void Clear() => Bind("", "", "", "", false, null);

    private void SetLabel(string label)
    {
        Text labelText = actionLabel != null
            ? actionLabel
            : actionButton != null ? actionButton.GetComponentInChildren<Text>(true) : null;
        if (labelText != null)
        {
            labelText.text = label;
        }
    }
}
