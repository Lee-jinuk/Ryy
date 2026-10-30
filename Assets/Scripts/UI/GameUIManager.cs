using System.Collections;
using UnityEngine;

// UI 갱신 조정자 — 개별 Text·Image·버튼 갱신은 화면별 UI 컴포넌트에 위임한다.
// (YutThrowUI·ScoreUI·BossCombatUI·BoardUI·TalismanHandUI·EquippedBellUI·
//  StartScreenUI·BossSelectScreenUI·GameScreenUI·ClearScreenUI·ShopScreenUI·GameOverScreenUI·
//  TalismanChoiceUI·MoveAdjustUI·FortuneChoiceUI·UsedTalismanUI)
// 이 클래스는 패널 공용 헬퍼와 정산 연출(ThrowSettlement 단계 재생)만 담당한다.
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

    // 보스 소개 화면 텍스트 채우기 — 실제 작성은 BossSelectScreenUI 소유
    public void FillBossSelectInfo(BossData boss)
    {
        if (gm.bossSelectScreenUI != null)
        {
            gm.bossSelectScreenUI.FillInfo(gm, boss);
        }
    }

    // 전체 표시 갱신 — 각 화면·팝업 UI 컴포넌트가 자기 소유 필드를 갱신한다.
    // 컴포넌트가 씬에 없으면 해당 화면 표시만 생략된다 (EnsureUIComponents가 런타임에 자동 부착)
    public void UpdateUI()
    {
        bool boardActive = gm.currentState == GameState.Playing
            || gm.currentState == GameState.Cleared
            || gm.currentState == GameState.GameOver;

        if (gm.yutThrowUI != null) gm.yutThrowUI.Refresh(gm);
        if (gm.scoreUI != null) gm.scoreUI.Refresh(gm);
        if (gm.bossCombatUI != null) gm.bossCombatUI.Refresh(gm, boardActive);
        if (gm.boardUI != null) gm.boardUI.Refresh(gm, boardActive);
        if (gm.talismanChoiceUI != null) gm.talismanChoiceUI.Refresh(gm);
        if (gm.moveAdjustUI != null) gm.moveAdjustUI.Refresh(gm);
        if (gm.talismanHandUI != null) gm.talismanHandUI.Refresh(gm);
        if (gm.usedTalismanUI != null) gm.usedTalismanUI.Sync(gm);
        if (gm.gameScreenUI != null) gm.gameScreenUI.Refresh(gm);
        if (gm.equippedBellUI != null) gm.equippedBellUI.Refresh(gm);
        if (gm.shopScreenUI != null) gm.shopScreenUI.Refresh(gm);
        if (gm.fortuneChoiceUI != null) gm.fortuneChoiceUI.Refresh(gm);

        // 클리어·게임오버 결과 텍스트 (패널이 꺼져 있어도 값은 항상 최신)
        BossData boss = gm.bossManager.CurrentBoss;
        if (boss != null)
        {
            if (gm.clearScreenUI != null) gm.clearScreenUI.Refresh(gm, boss);
            if (gm.gameOverScreenUI != null) gm.gameOverScreenUI.Refresh(gm, boss);
        }
        if (gm.bossSelectScreenUI != null) gm.bossSelectScreenUI.Refresh(gm);
    }

    // ── 사용 부적 패널 + 배율 이유 표시 (실제 작성은 UsedTalismanUI·ScoreUI 소유) ──

    // 정산 단계와 연결된 사용 카드 표시 1개 제거
    public void RemoveUsedItem(int useId)
    {
        if (gm.usedTalismanUI != null) gm.usedTalismanUI.RemoveUsedItem(gm, useId);
    }

    // 배율 변화 이유 표시 — ScoreUI 소유 필드
    public void SetMultiplierReason(string reason)
    {
        if (gm.scoreUI != null) gm.scoreUI.SetMultiplierReason(gm, reason);
    }

    // 정산 종료·스킵 시 표시 정리 — 이유 텍스트 비우고 남은 사용 카드 표시 제거
    public void ClearUsedTalismanFeedback()
    {
        if (gm.usedTalismanUI != null) gm.usedTalismanUI.ClearFeedback(gm);
    }

    // ── 정산 연출 ──

    // 정산 데이터를 화면에 순서대로 재생 — 점수 계산과 분리되어 스킵해도 같은 결과
    // YutGameManager가 StartCoroutine으로 실행한다
    public IEnumerator PresentSettlement(ThrowSettlement settlement)
    {
        gm.isPresenting = true;

        // 1) 윷 결과 표시 (이전 투척의 베이직·멀티플·투척점수는 0으로 초기화된 상태)
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);

        // 2) 말 이동 연출 — 경유 칸을 한 칸씩 따라 이동 (저주 결과는 이동 없음).
        //    진격부·후퇴부·환영부·정지부는 이동 보정 이유를 이동 연출과 함께 표시하고 항목을 제거한다
        var moveRecords = gm.talismanManager.usedThisThrow.FindAll(
            r => TalismanManager.IsMoveDisplayEffect(r.effect) && !r.displayConsumed);
        if (moveRecords.Count > 0)
        {
            SetMultiplierReason(string.Join(" / ", moveRecords.ConvertAll(
                r => string.IsNullOrEmpty(r.resultNote) ? $"{r.talismanName} — 이동 보정" : r.resultNote)));
        }
        foreach (int tileIndex in settlement.movePath)
        {
            gm.boardManager.MovePieceVisual(tileIndex);
            yield return new WaitForSeconds(gm.moveStepDelay);
        }
        foreach (UsedTalismanRecord r in moveRecords)
        {
            RemoveUsedItem(r.useId);
        }
        ShowLandingInfo(settlement);   // 최종 착지 후 칸 정보 표시 ("불 - 50점")

        // 3) 도착 칸 베이직 → 윷 배율 순서로 표시 ("개 기본 배율 ×3" 이유 표시).
        //    전체배율은 기본 전체배율(×1)에서 시작 — 이후 효과 단계가 TotalMult로 갱신한다
        gm.basicScore = settlement.baseBasic;
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);
        SetMultiplierReason($"{YutGameManager.ToKorean(settlement.result)} 기본 배율 ×{settlement.baseMult}");
        gm.multiplier = settlement.baseMult;
        gm.totalMult = settlement.baseTotalMult;
        UpdateUI();

        // 4) 발동 효과를 기록 순서대로 재생 — 이유 표시 → Mult 갱신 → 간격 → 카드 표시 제거
        //    상성·잡기처럼 카드가 아닌 단계(useId=0)는 카드 목록을 건드리지 않는다
        foreach (ScoreEffectStep step in settlement.steps)
        {
            SetMultiplierReason(string.IsNullOrEmpty(step.reasonText) ? step.displayName : step.reasonText);
            if (step.preApplyDelay > 0f)   // 탐욕부 — 이유 표시 후 잠시 뒤 배율 갱신
            {
                yield return new WaitForSeconds(step.preApplyDelay);
            }
            bool hasFloat = step.opValueF != 0f || step.afterValueF != 0f;
            if (step.target == EffectTarget.Basic)
            {
                gm.basicScore = step.afterValue;
            }
            else if (step.target == EffectTarget.YutMult)
            {
                gm.multiplier = hasFloat ? step.afterValueF : step.afterValue;   // 윷 배율 단위 — 방울·복주머니 성장 단계
            }
            else
            {
                gm.totalMult = hasFloat ? step.afterValueF : step.afterValue;    // 전체배율 단위 — 상성·잡기·부적·착지 단계
            }
            float opShown = hasFloat ? step.opValueF : step.opValue;
            float afterShown = hasFloat ? step.afterValueF : step.afterValue;
            Debug.Log($"[효과 {step.order + 1}] {step.displayName} — {step.target} {step.beforeValue} → {afterShown} ({step.op} {opShown})");
            UpdateUI();
            yield return new WaitForSeconds(
                step.postApplyDelay > 0f ? step.postApplyDelay : gm.effectStepDelay);
            if (step.useId > 0)
            {
                RemoveUsedItem(step.useId);
            }
        }

        // 5) 이번 투척 점수 표시
        gm.throwScore = settlement.throwScore;
        UpdateUI();

        // 6) 토탈 합산 표시 + 클리어/게임오버 판정은 게임 흐름이므로 YutGameManager에 위임
        yield return new WaitForSeconds(gm.effectStepDelay);
        gm.FinishPresentation(settlement);
    }

    // 착지 칸 정보 문자열 설정 — 속성 착지 효과 설명 ("물 - 3연속 ×4", "풀 - 도트 등록 +60")
    // 연출의 이동 완료 시점과 스킵 경로(FinishPresentation) 양쪽에서 호출 — 같은 값이라 중복 호출 안전
    public void ShowLandingInfo(ThrowSettlement settlement)
    {
        gm.landingTileInfo = settlement.landingTileIndex >= 0
            ? $"{BoardManager.AttributeName(gm.board.tiles[settlement.landingTileIndex].attribute)} - {settlement.tileEffectNote}"
            : "";
        UpdateUI();
    }
}
