using UnityEngine;

// 게임 화면 장착 방울 UI — 열린 슬롯만 표시하는 자동 배치 슬롯 (기본 4칸·최대 8칸).
// (방울 보유·효과 판정·열린 슬롯 수는 BellManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.equipBellSlots
//
// 배치 규칙 — 열린 슬롯 수 기준 (보유 수 아님 — 판매해도 배치 안 바뀜):
//   열린 1~4칸 = 큰 크기 한 줄 가운데 / 5~8칸 = 작은 크기 두 줄
//   (5→3+2, 6→3+3, 7→4+3, 8→4+4, 각 줄 가운데 정렬)
// 열린 빈 슬롯은 표시, 잠긴 슬롯(인덱스 ≥ 열린 수)은 숨긴다.
public class EquippedBellUI : MonoBehaviour
{
    public BellSlotUI[] equipBellSlots = new BellSlotUI[8];   // 장착 방울 슬롯 — 최대 8칸, 버튼 없음
    public RectTransform slotContainer;                       // 배치 기준 영역 — 미연결 시 첫 슬롯의 부모
    public Vector2 bigSlotSize = new Vector2(120f, 120f);     // 열린 4칸 이하 — 한 줄 배치 크기
    public Vector2 smallSlotSize = new Vector2(80f, 80f);     // 열린 5칸 이상 — 두 줄 배치 크기
    public float slotGap = 10f;                               // 같은 줄 슬롯 간격
    public float rowGap = 10f;                                // 두 줄일 때 줄 간격

    private bool bellPanelWarned;
    private bool slotsPrepared;

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 장착 방울 표시 동기화 — 열린 슬롯을 배치하고 보유 수만큼 앞에서부터 채운다.
    // 표시 전용 — 장착 상태에 영향 없음
    public void Refresh(YutGameManager gm)
    {
        bool anySlot = false;
        foreach (BellSlotUI slot in gm.equipBellSlots)
        {
            if (slot != null) anySlot = true;
        }
        if (!anySlot)
        {
            if (!bellPanelWarned && gm.bellManager.ownedBells.Count > 0)
            {
                bellPanelWarned = true;
                Debug.LogWarning("[YutGameManager] equipBellSlots 미연결 — 장착 방울 표시가 생략됩니다. Scene에 슬롯(BellSlotUI)을 만들어 연결하세요.", gm);
            }
            return;
        }
        if (!slotsPrepared) PrepareSlots();

        int open = gm.bellManager.OpenSlots;
        LayoutSlots(gm.equipBellSlots, open);

        for (int i = 0; i < gm.equipBellSlots.Length; i++)
        {
            BellSlotUI slot = gm.equipBellSlots[i];
            if (slot == null) continue;
            if (i < gm.bellManager.ownedBells.Count)
            {
                OwnedBell bell = gm.bellManager.ownedBells[i];
                slot.Bind(bell.bellName, bell.description, BellSlotUI.UsesText(bell), "", false, null);
            }
            else
            {
                slot.Clear();
            }
        }
    }

    void PrepareSlots()
    {
        slotsPrepared = true;
        if (slotContainer == null)
        {
            foreach (BellSlotUI slot in equipBellSlots)
            {
                if (slot != null) { slotContainer = slot.transform.parent as RectTransform; break; }
            }
        }
    }

    // 열린 칸만 배치·활성화. 슬롯 인덱스 순서대로 위 줄부터 채운다.
    // 앵커는 컨테이너 중앙(0.5,0.5)으로 정규화해 anchoredPosition = 중앙 기준 오프셋으로 통일
    void LayoutSlots(BellSlotUI[] slots, int open)
    {
        if (slotContainer == null) return;
        bool twoRows = open > 4;
        Vector2 size = twoRows ? smallSlotSize : bigSlotSize;
        int row1 = twoRows ? (open + 1) / 2 : open;
        int placed = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            BellSlotUI slot = slots[i];
            if (slot == null) continue;
            if (i >= open) { slot.gameObject.SetActive(false); continue; }
            slot.gameObject.SetActive(true);
            RectTransform rt = slot.transform as RectTransform;
            if (rt == null) { placed++; continue; }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            int row = placed < row1 ? 0 : 1;
            int col = row == 0 ? placed : placed - row1;
            int count = row == 0 ? row1 : open - row1;
            float x = (col - (count - 1) * 0.5f) * (size.x + slotGap);
            float y = row == 0 ? (twoRows ? (size.y + rowGap) * 0.5f : 0f) : -(size.y + rowGap) * 0.5f;
            rt.anchoredPosition = new Vector2(x, y);
            placed++;
        }
    }
}
