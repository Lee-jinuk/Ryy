using UnityEngine;
using UnityEngine.UI;

// 윷 투척 UI — 면 표시(Image/Text)·던지기·재투척·확정 버튼·상태 표시.
// 실제 세션(frontUp)만 읽어 표시 — 표시용 재추첨·결과 판정 없음
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.mainResultText·stickFaceImages·
//       stickFrontSprite·stickBackSprite·rerollButtons·throwStatusText·
//       rerollRemainText·rollButton·confirmThrowButton
public class YutThrowUI : MonoBehaviour
{
    public Text mainResultText;                      // 중앙 결과: 윷 결과 이름만 표시 (전투 중에만 보임)
    public Image[] stickFaceImages = new Image[4];   // 각 윷의 앞/뒤 PNG — 미투척·낙에는 숨김 (미연결 시 Text만 표시)
    public Sprite stickFrontSprite;                  // 윷 앞면 스프라이트
    public Sprite stickBackSprite;                   // 윷 뒷면 스프라이트
    public Button[] rerollButtons = new Button[4];   // 윷별 재투척 버튼 — 재투척 가능할 때만 interactable
    public Text throwStatusText;                     // 투척 상태창 — "현재 결과: 개 / 재투척 가능: 2" 또는 확정 결과
    public Text rerollRemainText;                    // 남은 재투척 수 (N회 형식, 미연결 시 무시)
    public Image[] flameImages = new Image[7];       // 남은 던지기 횟수 불꽃 7칸 — 왼쪽부터 표시, 나머지 숨김 (표시 용량 7, 규칙 무관)
    public Button rollButton;                        // '윷 던지기' 버튼 — 미투척 세션에서만 interactable
    public Button confirmThrowButton;                // '결과 확정' 버튼 — 세션 진행 중에만 interactable

    // ── 표시 갱신 — GameUIManager.UpdateUI가 매 갱신마다 호출 ──

    // 중앙 결과 + 윷 4개 투척 세션 표시 — 면 표시, 재투척 버튼 활성, 상태창, 남은 재투척, 확정 버튼
    // 준비 단계(rolled=false)에는 면·재투척·확정을 표시/허용하지 않는다 — 부적 사용만 가능
    public void Refresh(YutGameManager gm)
    {
        YutThrowSession session = gm.rollManager.session;

        if (gm.mainResultText != null)
        {
            // 윷 결과 이름만 표시 — Start에서는 오브젝트 자체를 숨긴다.
            // 세션의 실시간 판정값 사용: 최초 투척·개별 재투척 직후 현재 조합, 확정 후 확정 결과, 미투척 세션에서는 비움
            bool showResult = gm.currentState == GameState.Playing
                || gm.currentState == GameState.Cleared
                || gm.currentState == GameState.GameOver;
            gm.mainResultText.gameObject.SetActive(showResult);
            gm.mainResultText.text = showResult && session != null && session.rolled
                ? YutGameManager.ToKorean(session.CurrentResult) : "";
        }

        bool sessionActive = session != null && session.RollInProgress;
        bool throwInputOpen = sessionActive && !gm.isPresenting
            && !gm.talismanManager.isChoosing && !gm.talismanManager.isChoosingMoveAdjust;
        for (int i = 0; i < YutThrowSession.StickCount; i++)
        {
            if (i < gm.stickFaceImages.Length && gm.stickFaceImages[i] != null)
            {
                // 면 PNG — 세션의 실제 면(frontUp)만 반영하고 다시 추첨하지 않는다.
                // 미투척(준비 단계)·낙에는 면이 없어 이미지를 숨긴다 (enabled 토글 — 레이아웃 유지)
                bool showFace = session != null && session.rolled && !session.isNak;
                gm.stickFaceImages[i].enabled = showFace;
                if (showFace)
                {
                    gm.stickFaceImages[i].sprite = session.frontUp[i]
                        ? gm.stickFrontSprite : gm.stickBackSprite;
                }
            }
            if (i < gm.rerollButtons.Length && gm.rerollButtons[i] != null)
            {
                gm.rerollButtons[i].interactable = throwInputOpen && session.CanReroll(i);
            }
        }
        if (gm.throwStatusText != null)
        {
            if (session == null)
            {
                gm.throwStatusText.text = "";
            }
            else if (!session.rolled)
            {
                gm.throwStatusText.text = "준비 — 윷을 던지세요 (부적 사용 가능)";
            }
            else if (session.Active)
            {
                gm.throwStatusText.text = session.isNak
                    ? "낙 — 확정만 가능"
                    : $"현재 결과: {YutGameManager.ToKorean(session.CurrentResult)} / 재투척 가능: {session.RemainingRerolls}";
            }
            else
            {
                gm.throwStatusText.text = $"확정 결과: {YutGameManager.ToKorean(session.CurrentResult)}";
            }
        }
        if (gm.rerollRemainText != null)
        {
            gm.rerollRemainText.text = session != null ? session.RemainingRerolls + "회" : "";
        }
        // 남은 던지기 횟수 불꽃 — maxThrows - currentThrows만큼 왼쪽부터 표시 (enabled 토글 — 레이아웃 유지)
        int remainingThrows = gm.maxThrows - gm.currentThrows;
        for (int i = 0; i < gm.flameImages.Length; i++)
        {
            if (gm.flameImages[i] != null)
            {
                gm.flameImages[i].enabled = i < remainingThrows;
            }
        }
        if (gm.confirmThrowButton != null)
        {
            gm.confirmThrowButton.interactable = throwInputOpen;
        }
        // '윷 던지기' 버튼 — 미투척 세션 + 입력 개방 상태에서만 활성.
        // RollYut의 실제 차단 조건과 같은 판정 — 표시상 활성과 입력 차단의 어긋남 방지
        if (gm.rollButton != null)
        {
            gm.rollButton.interactable = gm.currentState == GameState.Playing
                && session != null && !session.rolled
                && !gm.isPresenting && !gm.talismanManager.isChoosing
                && !gm.talismanManager.isChoosingMoveAdjust
                && gm.currentThrows < gm.maxThrows;
        }
    }
}
