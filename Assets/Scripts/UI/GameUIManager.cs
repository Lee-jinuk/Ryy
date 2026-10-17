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
    }

    private string FeverLabel()
    {
        if (gm.feverCombo <= 0)
        {
            return "";
        }
        if (gm.feverCombo == 1)
        {
            return "Hit";
        }
        if (gm.feverCombo == 2)
        {
            return "피버";
        }
        return $"피버{gm.feverCombo}";
    }

    public void UpdateUI()
    {
        if (gm.mainResultText != null)
        {
            // 윷 결과 이름만 표시 — Start/TargetSelection에서는 오브젝트 자체를 숨긴다
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
        if (gm.feverText != null)
        {
            gm.feverText.text = FeverLabel();
        }
        if (gm.targetYutText != null)
        {
            bool targetConfirmed = gm.currentState != GameState.Start && gm.currentState != GameState.TargetSelection;
            gm.targetYutText.text = targetConfirmed ? YutGameManager.ToKorean(gm.selectedTarget) : "";
        }
        // 판 진행도 — Start 화면에서는 빈칸, 보스전 진행 중에는 실제 판 번호 표시
        if (gm.roundProgressText != null)
        {
            gm.roundProgressText.text = gm.currentState == GameState.Start ? "" : $"{gm.roundIndex} / {gm.roundsPerBoss}";
        }

        // 말판 UI — 칸 점수·방울 아이콘·완주 횟수 (Start/TargetSelection에서는 갱신 생략)
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

        // 던지기 버튼은 Playing 상태에서만, 그리고 연출·방울 선택 중에는 숨긴다
        SetPanel(gm.rollButton, gm.currentState == GameState.Playing && !gm.isPresenting && !gm.bellManager.isChoosingBell);
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

        // 3) 도착 칸 베이직 → 윷 멀티플 순서로 표시
        gm.basicScore = settlement.baseBasic;
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.multiplier = settlement.baseMult;
        UpdateUI();

        // 4) 발동 효과를 기록 순서대로 재생
        foreach (ScoreEffectStep step in settlement.steps)
        {
            yield return new WaitForSeconds(gm.effectStepDelay);
            if (step.target == EffectTarget.Basic)
            {
                gm.basicScore = step.afterValue;
            }
            else
            {
                gm.multiplier = step.afterValue;
            }
            Debug.Log($"[효과 {step.order + 1}] {step.displayName} — {step.target} {step.beforeValue} → {step.afterValue} ({step.op} {step.opValue}, 증가량 {(step.delta >= 0 ? "+" : "")}{step.delta})");
            UpdateUI();
        }

        // 5) 이번 투척 점수 표시
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.throwScore = settlement.throwScore;
        UpdateUI();

        // 6) 토탈 합산 표시 + 판 진행/클리어/게임오버 판정은 게임 흐름이므로 YutGameManager에 위임
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.FinishPresentation(settlement);
    }
}
