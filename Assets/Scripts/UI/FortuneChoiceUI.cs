using UnityEngine;
using UnityEngine.UI;

// 복주머니 점괘 선택창 UI — 후보 3칸·안내 텍스트.
// 표시 전용 (후보 생성·효과 적용은 FortuneUpgradeManager·ShopManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.fortuneChoicePanel·fortuneOptionSlots·fortuneChoiceText
public class FortuneChoiceUI : MonoBehaviour
{
    public GameObject fortuneChoicePanel;                      // 점괘 선택 패널 — 미연결 시 복주머니 구매 거부 (돈만 차감 방지)
    public BellSlotUI[] fortuneOptionSlots = new BellSlotUI[3]; // 점괘 후보 슬롯 고정 3칸 — BellSlotUI 재사용
    public Text fortuneChoiceText;                             // 선택 안내 텍스트 (미연결 시 무시)

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 점괘 선택 표시 동기화 — pendingCandidates는 FortuneUpgradeManager가 보관, UI는 표시만.
    // 재표시로 후보를 재추첨하지 않고, 선택 버튼 리스너는 Bind가 매번 교체해 누적되지 않는다
    public void Refresh(YutGameManager gm)
    {
        bool choosing = gm.fortuneManager.isChoosingFortune;
        GameUIManager.SetPanel(gm.fortuneChoicePanel, choosing);
        if (!choosing)
        {
            return;
        }
        if (gm.fortuneChoiceText != null)
        {
            gm.fortuneChoiceText.text = "복주머니 — 점괘 1개를 선택하세요";
        }
        if (gm.fortuneOptionSlots == null)
        {
            return;
        }
        for (int i = 0; i < gm.fortuneOptionSlots.Length; i++)
        {
            BellSlotUI slot = gm.fortuneOptionSlots[i];
            if (slot == null) continue;
            if (i < gm.fortuneManager.pendingCandidates.Count)
            {
                FortuneUpgradeData candidate = gm.fortuneManager.pendingCandidates[i];
                int index = i;   // 클로저용 — 람다가 인덱스 변수를 공유하지 않게 고정
                slot.Bind(candidate.upgradeName, candidate.description, "", "선택", true,
                    () => gm.fortuneManager.ChooseUpgrade(index));
            }
            else
            {
                slot.Clear();
            }
        }
    }
}
