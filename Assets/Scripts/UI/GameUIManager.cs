using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// UI 표시와 연출 담당 — 텍스트 갱신, 패널 표시 토글, 정산 연출(단계별 딜레이 재생)
// 게임 규칙 계산은 하지 않는다. 연출이 쓰는 표시용 임시값은 YutGameManager 필드를 통해 접근한다.
// 일반 C# 클래스 (MonoBehaviour 아님) — 코루틴은 YutGameManager가 대신 실행한다
public class GameUIManager
{
    private readonly YutGameManager gm;

    public GameUIManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    // 패널 단순 토글
    public static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    // 켜야 할 패널 전용: 미연결이면 경고, 켰는데도 안 보이면 부모 비활성 경고
    public static void ActivatePanel(GameObject panel, string fieldName)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[YutGameManager] {fieldName}이(가) Inspector에 연결되지 않았습니다.");
            return;
        }
        panel.SetActive(true);
        if (!panel.activeInHierarchy)
        {
            Debug.LogWarning($"[YutGameManager] {fieldName}은(는) 켜졌지만 부모가 비활성 상태라 화면에 보이지 않습니다. Hierarchy를 확인하세요.", panel);
        }
    }

    // 보스 소개 화면 텍스트 채우기 (표시용 — 패널 토글은 호출자가 처리)
    public void FillBossSelectInfo(BossData boss)
    {
        if (gm.bossSelectNameText != null)
        {
            gm.bossSelectNameText.text = boss.bossName;
        }
        if (gm.bossSelectGoalText != null)
        {
            gm.bossSelectGoalText.text = boss.maxHP.ToString();
        }
        if (gm.bossSelectAbilityText != null)
        {
            gm.bossSelectAbilityText.text = boss.abilityDescription;
        }
        // 소개 중인 보스(pendingBossIndex) 상성 — 이미 생성된 런 고정 데이터 재사용, 건너뛰기 시 다음 보스 값으로 갱신
        if (gm.bossSelectAffinityText != null)
        {
            gm.bossSelectAffinityText.text = gm.bossManager.BuildAffinityText(gm.bossManager.pendingBossIndex);
        }
    }

    public void UpdateUI()
    {
        if (gm.mainResultText != null)
        {
            // 윷 결과 이름만 표시 — Start에서는 오브젝트 자체를 숨긴다
            bool showResult = gm.currentState == GameState.Playing
                || gm.currentState == GameState.Cleared
                || gm.currentState == GameState.GameOver;
            gm.mainResultText.gameObject.SetActive(showResult);
            gm.mainResultText.text = showResult && gm.hasRolled ? YutGameManager.ToKorean(gm.lastResult) : "";
        }
        if (gm.basicScoreText != null)
        {
            gm.basicScoreText.text = gm.basicScore.ToString();
        }
        if (gm.multScoreText != null)
        {
            gm.multScoreText.text = gm.multiplier.ToString();
        }
        // 총점 표시는 연출 단계에 맞춰 displayTotal 사용 — '토탈 합산' 단계에서만 증가
        if (gm.totalScoreText != null)
        {
            gm.totalScoreText.text = gm.displayTotal.ToString();
        }
        if (gm.goalScoreText != null)
        {
            gm.goalScoreText.text = gm.goalScore.ToString();
        }
        // 보스 현재 체력 = Max(0, 최대 체력 - 누적 피해). 연출 중에는 표시값 displayTotal을 사용해
        // '토탈 합산' 단계에서 totalScoreText와 동시에 갱신되고, 종료·스킵 후 실제 체력과 일치한다
        if (gm.bossCurrentHPText != null)
        {
            gm.bossCurrentHPText.text = Mathf.Max(0, gm.goalScore - gm.displayTotal).ToString();
        }
        if (gm.remainText != null)
        {
            gm.remainText.text = (gm.maxThrows - gm.currentThrows).ToString();
        }
        if (gm.throwDataText != null)
        {
            gm.throwDataText.text = gm.maxThrows.ToString();
        }
        if (gm.throwScoreText != null)
        {
            gm.throwScoreText.text = gm.throwScore.ToString();
        }

        // 윷 4개 투척 세션 UI — 면 표시, 재투척 버튼 활성, 상태창, 남은 재투척, 확정 버튼
        YutThrowSession session = gm.rollManager.session;
        bool sessionActive = session != null && session.Active;
        bool throwInputOpen = sessionActive && !gm.isPresenting && !gm.bellManager.isChoosingBell && !gm.talismanManager.isChoosing;
        for (int i = 0; i < YutThrowSession.StickCount; i++)
        {
            if (i < gm.stickFaceTexts.Length && gm.stickFaceTexts[i] != null)
            {
                gm.stickFaceTexts[i].text = session != null
                    ? (session.frontUp[i] ? "앞" : "뒤")
                    : "";
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
            else if (session.Active)
            {
                gm.throwStatusText.text = $"현재 결과: {YutGameManager.ToKorean(session.CurrentResult)} / 재투척 가능: {session.RemainingRerolls}";
            }
            else
            {
                gm.throwStatusText.text = $"확정 결과: {YutGameManager.ToKorean(session.CurrentResult)}";
            }
        }
        if (gm.rerollRemainText != null)
        {
            gm.rerollRemainText.text = session != null ? session.RemainingRerolls.ToString() : "";
        }
        if (gm.confirmThrowButton != null)
        {
            gm.confirmThrowButton.interactable = throwInputOpen;
        }
        // 판 진행도 표시는 라운드 구조 폐지와 함께 제거됨

        // 말판 UI — 칸 점수·방울 아이콘·완주 횟수 (Start에서는 갱신 생략)
        bool boardActive = gm.currentState == GameState.Playing
            || gm.currentState == GameState.Cleared
            || gm.currentState == GameState.GameOver;
        if (boardActive)
        {
            gm.boardManager.UpdateBoardUI();
        }

        // 방울 선택창 — 후보 이름·설명 표시 (선택 중이 아니면 빈칸)
        for (int i = 0; i < 3; i++)
        {
            bool hasOption = gm.bellManager.isChoosingBell && i < gm.bellManager.currentBellOptions.Count;
            if (i < gm.bellOptionNameTexts.Length && gm.bellOptionNameTexts[i] != null)
            {
                gm.bellOptionNameTexts[i].text = hasOption ? gm.bellManager.currentBellOptions[i].bellName : "";
            }
            if (i < gm.bellOptionDescTexts.Length && gm.bellOptionDescTexts[i] != null)
            {
                gm.bellOptionDescTexts[i].text = hasOption ? gm.bellManager.currentBellOptions[i].description : "";
            }
        }

        // 부적 선택창 — 후보 이름·설명·레어도 표시 + 선택 진행 안내 (선택 중이 아니면 빈칸)
        // 시작 5택3·땅 3택1이 같은 슬롯을 공유 — 후보 수만큼만 채우고 나머지는 빈칸
        bool talChoosing = gm.talismanManager.isChoosing;
        for (int i = 0; i < 5; i++)
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
        SetPanel(gm.talismanChoicePanel, talChoosing);

        // 손패 — 이름 표시 + 사용 버튼 활성 (최초 투척 후~확정 전 구간에서만 사용 가능)
        bool canUseTalisman = gm.talismanManager.CanUseNow();
        for (int i = 0; i < TalismanManager.HandLimit; i++)
        {
            bool hasCard = i < gm.talismanManager.hand.Count;
            if (i < gm.talismanHandNameTexts.Length && gm.talismanHandNameTexts[i] != null)
            {
                gm.talismanHandNameTexts[i].text = hasCard ? gm.talismanManager.hand[i].talismanName : "";
            }
            if (i < gm.talismanHandDescTexts.Length && gm.talismanHandDescTexts[i] != null)
            {
                gm.talismanHandDescTexts[i].text = hasCard ? gm.talismanManager.hand[i].description : "";
            }
            if (i < gm.talismanUseButtons.Length && gm.talismanUseButtons[i] != null)
            {
                gm.talismanUseButtons[i].interactable = canUseTalisman && hasCard;
            }
        }
        // 초과 획득 소멸 안내 — 다음 선택창이 열리면 지워진다
        if (gm.talismanNoticeText != null)
        {
            gm.talismanNoticeText.text = gm.talismanManager.pendingOverflowNotice > 0
                ? $"손패가 가득 차 부적 {gm.talismanManager.pendingOverflowNotice}장이 소멸했습니다."
                : "";
        }

        // 보유 임시 방울 — bellSlots 앞에서부터 활성화 + 자식 Text에 이름 표시
        if (gm.bellSlots != null)
        {
            for (int i = 0; i < gm.bellSlots.Length; i++)
            {
                if (gm.bellSlots[i] == null)
                {
                    continue;
                }
                bool owned = i < gm.ownedBells.Count;
                gm.bellSlots[i].SetActive(owned);
                if (owned)
                {
                    Text slotText = gm.bellSlots[i].GetComponentInChildren<Text>(true);
                    if (slotText != null)
                    {
                        slotText.text = gm.ownedBells[i].bellName;
                    }
                }
            }
        }

        // 착지 칸 정보 — "불 - 50점" (상성·윷 배율 적용 전 칸 기본점수). 새 보스전·새 런에서 비워진다
        if (gm.landingTileText != null)
        {
            gm.landingTileText.text = gm.landingTileInfo;
        }

        // 보스 정보 — 이름·특수능력만 표시 (HP 바 없음: 보스는 목표 점수 관문)
        BossData boss = gm.bossManager.CurrentBoss;
        if (boss != null)
        {
            if (gm.bossNameText != null)
            {
                gm.bossNameText.text = boss.bossName;
            }
            if (gm.bossAbilityText != null)
            {
                gm.bossAbilityText.text = boss.abilityDescription;
            }

            // 클리어·게임오버 결과 텍스트 (패널이 꺼져 있어도 값은 항상 최신)
            if (gm.clearBossNameText != null)
            {
                gm.clearBossNameText.text = boss.bossName;
            }
            if (gm.clearScoreText != null)
            {
                gm.clearScoreText.text = gm.totalScore.ToString();
            }
            if (gm.gameOverBossNameText != null)
            {
                gm.gameOverBossNameText.text = boss.bossName;
            }
            if (gm.gameOverScoreText != null)
            {
                gm.gameOverScoreText.text = gm.totalScore.ToString();
            }
            if (gm.gameOverGoalText != null)
            {
                gm.gameOverGoalText.text = gm.goalScore.ToString();
            }
        }

        // 보스 상성 정보창 — 전투 중인 보스 기준 (첫 소개 시점에 생성된 런 고정값 표시)
        if (gm.bossAffinityText != null)
        {
            gm.bossAffinityText.text = boardActive && boss != null
                ? gm.bossManager.BuildAffinityText(gm.selectedBossIndex)
                : "";
        }

        // 건너뛰기 버튼은 소개 화면이 열려 있고 다음 보스가 남아 있을 때만 표시 (마지막 보스 소개 시 숨김)
        SetPanel(gm.skipBossButton,
            gm.bossSelectPanel != null && gm.bossSelectPanel.activeSelf && gm.bossManager.CanSkipPending);
    }

    // 정산 데이터를 화면에 순서대로 재생 — 점수 계산과 분리되어 스킵해도 같은 결과
    // YutGameManager가 StartCoroutine으로 실행한다
    public IEnumerator PresentSettlement(ThrowSettlement settlement)
    {
        gm.isPresenting = true;

        // 1) 윷 결과 표시 (이전 투척의 베이직·멀티플·투척점수는 0으로 초기화된 상태)
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);

        // 2) 말 이동 연출 — 경유 칸을 한 칸씩 따라 이동 (저주 결과는 이동 없음)
        foreach (int tileIndex in settlement.movePath)
        {
            gm.boardManager.MovePieceVisual(tileIndex);
            yield return new WaitForSeconds(gm.moveStepDelay);
        }
        ShowLandingInfo(settlement);   // 최종 착지 후 칸 정보 표시 ("불 - 50점")

        // 3) 도착 칸 베이직 → 윷 멀티플 순서로 표시
        gm.basicScore = settlement.baseBasic;
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.multiplier = settlement.baseMult;
        UpdateUI();

        // 4) 발동 효과를 기록 순서대로 재생 (소수 배율 단계는 float 필드 사용 — 보스 상성 ×0.5 등)
        foreach (ScoreEffectStep step in settlement.steps)
        {
            yield return new WaitForSeconds(gm.effectStepDelay);
            bool hasFloat = step.opValueF != 0f || step.afterValueF != 0f;
            if (step.target == EffectTarget.Basic)
            {
                gm.basicScore = step.afterValue;
            }
            else
            {
                gm.multiplier = hasFloat ? step.afterValueF : step.afterValue;
            }
            float opShown = hasFloat ? step.opValueF : step.opValue;
            float afterShown = hasFloat ? step.afterValueF : step.afterValue;
            Debug.Log($"[효과 {step.order + 1}] {step.displayName} — {step.target} {step.beforeValue} → {afterShown} ({step.op} {opShown})");
            UpdateUI();
        }

        // 5) 이번 투척 점수 표시
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.throwScore = settlement.throwScore;
        UpdateUI();

        // 6) 토탈 합산 표시 + 클리어/게임오버 판정은 게임 흐름이므로 YutGameManager에 위임
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.FinishPresentation(settlement);
    }

    // 착지 칸 정보 문자열 설정 — "불 - 50점" (칸 기본점수, 상성·윷 배율 미적용)
    // 연출의 이동 완료 시점과 스킵 경로(FinishPresentation) 양쪽에서 호출 — 같은 값이라 중복 호출 안전
    public void ShowLandingInfo(ThrowSettlement settlement)
    {
        gm.landingTileInfo = settlement.landingTileIndex >= 0
            ? $"{BoardManager.AttributeName(gm.board.tiles[settlement.landingTileIndex].attribute)} - {settlement.tileScore}점"
            : "";
        UpdateUI();
    }
}
