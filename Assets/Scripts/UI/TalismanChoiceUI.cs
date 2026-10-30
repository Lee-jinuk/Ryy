using UnityEngine;
using UnityEngine.UI;

// 부적 선택창 UI — 시작 7택5·땅/START 3택1 공용 팝업.
// 표시 전용 (후보 생성·선택 판정은 TalismanManager 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.talismanChoicePanel·talismanOptionNameTexts·
//       talismanOptionDescTexts·talismanOptionRarityTexts·talismanChoiceInfoText·talismanConfirmButton
public class TalismanChoiceUI : MonoBehaviour
{
    public GameObject talismanChoicePanel;                     // 부적 후보 선택창
    public Text[] talismanOptionNameTexts = new Text[7];       // 후보 카드 이름 (7칸 — 3택1은 앞 3칸 사용)
    public Text[] talismanOptionDescTexts = new Text[7];       // 후보 카드 설명
    public Text[] talismanOptionRarityTexts = new Text[7];     // 후보 카드 레어도
    public Text talismanChoiceInfoText;                        // 선택 진행 안내 — "2 / 3 장 선택" (미연결 시 무시)
    public Button talismanConfirmButton;                       // '선택 확정' 버튼 — 정원 도달 시만 interactable

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 후보 이름·설명·레어도 표시 + 선택 진행 안내 (선택 중이 아니면 빈칸)
    // 시작 7택5·땅 3택1이 같은 슬롯을 공유 — 후보 수만큼만 채우고 나머지는 빈칸.
    // 슬롯 상한 OptionSlotMax(7)까지 순회 — Inspector 배열이 더 짧으면 각 요소 접근이 길이로 제한돼 뒷칸은 빈칸 유지
    public void Refresh(YutGameManager gm)
    {
        bool talChoosing = gm.talismanManager.isChoosing;
        for (int i = 0; i < TalismanManager.OptionSlotMax; i++)
        {
            bool hasOption = talChoosing && i < gm.talismanManager.candidates.Count;
            TalismanData option = hasOption ? gm.talismanManager.candidates[i] : null;
            if (i < gm.talismanOptionNameTexts.Length && gm.talismanOptionNameTexts[i] != null)
            {
                gm.talismanOptionNameTexts[i].text = hasOption ? option.talismanName : "";
            }
            if (i < gm.talismanOptionDescTexts.Length && gm.talismanOptionDescTexts[i] != null)
            {
                gm.talismanOptionDescTexts[i].text = hasOption ? option.description : "";
            }
            if (i < gm.talismanOptionRarityTexts.Length && gm.talismanOptionRarityTexts[i] != null)
            {
                gm.talismanOptionRarityTexts[i].text = hasOption ? gm.talismanManager.RarityName(option.rarity) : "";
            }
        }
        if (gm.talismanChoiceInfoText != null)
        {
            gm.talismanChoiceInfoText.text = talChoosing
                ? $"{gm.talismanManager.selectedIndices.Count} / {gm.talismanManager.requiredPicks} 장 선택"
                : "";
        }
        if (gm.talismanConfirmButton != null)
        {
            gm.talismanConfirmButton.interactable =
                talChoosing && gm.talismanManager.selectedIndices.Count == gm.talismanManager.requiredPicks;
        }
        // 부적 선택창 패널 표시 — 선택 상태와 패널 활성을 동기화
        GameUIManager.SetPanel(gm.talismanChoicePanel, talChoosing);
    }
}
