using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI 표시와 연출 담당 — 텍스트 갱신, 패널 표시 토글, 정산 연출(단계별 딜레이 재생)
// 게임 규칙 계산은 하지 않는다. 연출이 쓰는 표시용 임시값은 YutGameManager 필드를 통해 접근한다.
// 일반 C# 클래스 (MonoBehaviour 아님) — 코루틴은 YutGameManager가 대신 실행한다
public class GameUIManager
{
    private readonly YutGameManager gm;

    // 사용 부적 패널 항목 — useId → 생성된 항목 오브젝트 (손패 슬롯과 무관한 표시 전용)
    private readonly Dictionary<int, GameObject> usedItems = new Dictionary<int, GameObject>();
    private bool usedPanelWarned;

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
        // 준비 단계(rolled=false)에는 면·재투척·확정을 표시/허용하지 않는다 — 부적 사용만 가능
        YutThrowSession session = gm.rollManager.session;
        bool sessionActive = session != null && session.RollInProgress;
        bool throwInputOpen = sessionActive && !gm.isPresenting && !gm.talismanManager.isChoosing;
        for (int i = 0; i < YutThrowSession.StickCount; i++)
        {
            if (i < gm.stickFaceTexts.Length && gm.stickFaceTexts[i] != null)
            {
                gm.stickFaceTexts[i].text = session != null && session.rolled
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
            else if (!session.rolled)
            {
                gm.throwStatusText.text = "준비 — 윷을 던지세요 (부적 사용 가능)";
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

        // 보스 이동 예고 표식 — 준비 단계(미투척 세션)에 예정 도착 칸을 표시하고
        // 최초 투척 접수·정산·비전투 상태에서는 숨긴다. 표식은 표시 전용 — 잡기 판정은 실제 보스 위치 사용
        if (gm.bossMovePreview != null)
        {
            bool previewVisible = gm.currentState == GameState.Playing
                && session != null && !session.rolled
                && gm.bossManager.movePlanReady;
            gm.bossMovePreview.gameObject.SetActive(previewVisible);
            if (previewVisible)
            {
                gm.boardManager.MoveBossPreviewVisual(gm.bossManager.PlannedDestination);
            }
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

        // 손패 — 이름 표시 + 사용 버튼 활성 (최초 던지기 전 준비 구간에서만 사용 가능)
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
        SyncUsedTalismanPanel();

        // 보유 냥 — 전투 화면 표시 (상점은 shopMoneyText가 따로 표시)
        if (gm.moneyText != null)
        {
            gm.moneyText.text = $"{gm.economyManager.money}냥";
        }

        // 장착 방울 목록 — 전투 화면 표시 (BellItem Prefab 동적 항목)
        SyncBellPanel();
        SyncShopPanel();   // 상점 열림 시 상품·보유 목록·잔액 표시

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

    // ── 사용 부적 패널 + 배율 이유 표시 ──

    // 사용 부적 패널 동기화 — 사용 성공 기록을 동적 항목으로 표시한다.
    // 표시 전용: 항목 생성·제거가 실제 효과·덱·손패에 영향을 주지 않는다.
    // 미연결 시 표시만 생략하고 게임 계산은 계속 진행한다
    private void SyncUsedTalismanPanel()
    {
        var records = gm.talismanManager.usedThisThrow;
        if (gm.usedTalismanContent == null || gm.usedTalismanItemPrefab == null)
        {
            if (!usedPanelWarned && records.Count > 0)
            {
                usedPanelWarned = true;
                Debug.LogWarning("[YutGameManager] usedTalismanContent/usedTalismanItemPrefab 미연결 — 사용 부적 패널 표시가 생략됩니다.", gm);
            }
            return;
        }
        // 기록에서 지워졌거나 정산 재생으로 소비된 항목 정리
        var stale = new List<int>();
        foreach (KeyValuePair<int, GameObject> kv in usedItems)
        {
            UsedTalismanRecord rec = records.Find(r => r.useId == kv.Key);
            if (rec == null || rec.displayConsumed)
            {
                stale.Add(kv.Key);
            }
        }
        foreach (int id in stale)
        {
            if (usedItems[id] != null) Object.Destroy(usedItems[id]);
            usedItems.Remove(id);
        }
        // 새 사용 기록의 항목 생성 — 첫 번째 자식 Text = 이름, 두 번째 = 설명(선택)
        foreach (UsedTalismanRecord rec in records)
        {
            if (rec.displayConsumed || usedItems.ContainsKey(rec.useId)) continue;
            GameObject item = Object.Instantiate(gm.usedTalismanItemPrefab, gm.usedTalismanContent);
            Text[] texts = item.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0 && texts[0] != null) texts[0].text = rec.talismanName;
            if (texts.Length > 1 && texts[1] != null) texts[1].text = rec.description;
            usedItems[rec.useId] = item;
        }
        SetPanel(gm.usedTalismanPanel, records.Exists(r => !r.displayConsumed));
    }

    // 정산 단계와 연결된 사용 카드 표시 1개 제거 — 기록만 소비 처리, 게임 상태는 건드리지 않는다
    public void RemoveUsedItem(int useId)
    {
        UsedTalismanRecord rec = gm.talismanManager.usedThisThrow.Find(r => r.useId == useId);
        if (rec != null) rec.displayConsumed = true;
        if (usedItems.TryGetValue(useId, out GameObject item))
        {
            usedItems.Remove(useId);
            if (item != null) Object.Destroy(item);
        }
        SyncUsedTalismanPanel();
    }

    // 배율 변화 이유 표시 — Mult 위에 사용자가 배치한 별도 Text ("추혼부 ×4" 등)
    public void SetMultiplierReason(string reason)
    {
        if (gm.multiplierReasonText != null)
        {
            gm.multiplierReasonText.text = reason;
        }
    }

    // 정산 종료·스킵 시 표시 정리 — 이유 텍스트를 비우고 남은 사용 카드 항목을 전부 제거한다.
    // 다음 투척에 이전 효과 표시가 남지 않게 하는 정리 경로
    public void ClearUsedTalismanFeedback()
    {
        SetMultiplierReason("");
        foreach (UsedTalismanRecord rec in gm.talismanManager.usedThisThrow)
        {
            rec.displayConsumed = true;
        }
        foreach (KeyValuePair<int, GameObject> kv in usedItems)
        {
            if (kv.Value != null) Object.Destroy(kv.Value);
        }
        usedItems.Clear();
        SyncUsedTalismanPanel();
    }

    // ── 방울 장착 패널 + 상점 표시 ──

    private string lastBellSig = null;                          // 장착 목록 스냅샷 — 변할 때만 재구성
    private readonly List<GameObject> bellItems = new List<GameObject>();
    private string lastShopSig = null;
    private readonly List<GameObject> shopOfferItems = new List<GameObject>();
    private readonly List<GameObject> shopBellItems = new List<GameObject>();
    private bool bellPanelWarned;

    // 장착 방울 목록 동기화 — 전투 화면(bellContent). 상점의 보유 목록은 SyncShopPanel이 담당.
    // 표시 전용: 항목 생성·제거가 실제 장착 상태에 영향을 주지 않는다
    private void SyncBellPanel()
    {
        if (gm.bellContent == null || gm.bellItemPrefab == null)
        {
            if (!bellPanelWarned && gm.bellManager.ownedBells.Count > 0)
            {
                bellPanelWarned = true;
                Debug.LogWarning("[YutGameManager] bellContent/bellItemPrefab 미연결 — 장착 방울 표시가 생략됩니다.", gm);
            }
            return;
        }
        string sig = "";
        foreach (OwnedBell b in gm.bellManager.ownedBells)
        {
            sig += $"{b.instanceId}:{b.remainingUses};";
        }
        if (sig == lastBellSig)
        {
            return;   // 목록·남은 횟수가 같으면 재구성 생략
        }
        lastBellSig = sig;
        foreach (GameObject item in bellItems)
        {
            if (item != null) Object.Destroy(item);
        }
        bellItems.Clear();
        foreach (OwnedBell bell in gm.bellManager.ownedBells)
        {
            GameObject item = Object.Instantiate(gm.bellItemPrefab, gm.bellContent);
            FillBellItem(item, bell);
            bellItems.Add(item);
        }
    }

    // BellItem 항목 채우기 — 자식 Text 순서: 1=이름, 2=설명, 3=남은 발동 횟수(선택)
    private static void FillBellItem(GameObject item, OwnedBell bell)
    {
        Text[] texts = item.GetComponentsInChildren<Text>(true);
        if (texts.Length > 0 && texts[0] != null)
        {
            texts[0].text = bell.bellName;
        }
        if (texts.Length > 1 && texts[1] != null)
        {
            texts[1].text = bell.description;
        }
        if (texts.Length > 2 && texts[2] != null)
        {
            texts[2].text = bell.remainingUses >= 0 ? $"남은 발동 {bell.remainingUses}회" : "지속";
        }
    }

    // 상점 표시 동기화 — 상점이 열려 있을 때만. 상품 목록·보유 방울 목록·잔액을 표시한다.
    // 상품 버튼은 코드로 onClick 연결 — Inspector OnClick 연결이 필요 없다
    private void SyncShopPanel()
    {
        if (gm.shopMoneyText != null)
        {
            gm.shopMoneyText.text = gm.shopManager.isOpen ? $"{gm.economyManager.money}냥" : "";
        }
        if (!gm.shopManager.isOpen || gm.shopItemPrefab == null)
        {
            if (lastShopSig != null)   // 닫힐 때 항목을 비워 다음 진입이 오래된 상품을 보여주지 않게 한다
            {
                lastShopSig = null;
                RebuildList(shopOfferItems);
                RebuildList(shopBellItems);
            }
            return;
        }
        // 스냅샷 비교 — 돈·장착 수·판매 완료 수가 같으면 재구성 생략
        string sig = $"{gm.economyManager.money}|{gm.bellManager.ownedBells.Count}|{gm.shopManager.soldOfferIndices.Count}";
        if (sig == lastShopSig)
        {
            return;
        }
        lastShopSig = sig;
        RebuildList(shopOfferItems);
        RebuildList(shopBellItems);
        if (gm.shopOfferContent != null)
        {
            for (int i = 0; i < gm.shopManager.offers.Count; i++)
            {
                int index = i;   // 클로저용 — 람다가 인덱스 변수를 공유하지 않게 고정
                BellData offer = gm.shopManager.offers[i];
                GameObject item = Object.Instantiate(gm.shopItemPrefab, gm.shopOfferContent);
                FillShopItem(item, offer.bellName, offer.description, $"{offer.buyPrice}냥", "구매");
                Button btn = item.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    bool sold = gm.shopManager.soldOfferIndices.Contains(index);
                    btn.interactable = !sold &&
                        gm.economyManager.money >= offer.buyPrice &&
                        gm.bellManager.ownedBells.Count < gm.bellManager.MaxBells;
                    if (!sold)
                    {
                        btn.onClick.AddListener(() => gm.shopManager.BuyOffer(index));
                    }
                    else
                    {
                        SetButtonLabel(btn, "판매 완료");
                    }
                }
                shopOfferItems.Add(item);
            }
        }
        if (gm.shopBellContent != null)
        {
            for (int i = 0; i < gm.bellManager.ownedBells.Count; i++)
            {
                int index = i;
                OwnedBell bell = gm.bellManager.ownedBells[i];
                GameObject item = Object.Instantiate(gm.shopItemPrefab, gm.shopBellContent);
                FillShopItem(item, bell.bellName, bell.description,
                    $"{bell.sellPrice}냥 · {(bell.remainingUses >= 0 ? $"남은 발동 {bell.remainingUses}회" : "지속")}", "판매");
                Button btn = item.GetComponentInChildren<Button>(true);
                if (btn != null)
                {
                    btn.onClick.AddListener(() => gm.shopManager.SellBell(index));
                }
                shopBellItems.Add(item);
            }
        }
    }

    private static void RebuildList(List<GameObject> items)
    {
        foreach (GameObject item in items)
        {
            if (item != null) Object.Destroy(item);
        }
        items.Clear();
    }

    // ShopItem 항목 채우기 — 자식 Text 순서: 1=이름, 2=설명, 3=가격/상태. 버튼 라벨은 자식 Text
    private static void FillShopItem(GameObject item, string name, string desc, string price, string actionLabel)
    {
        Text[] texts = item.GetComponentsInChildren<Text>(true);
        if (texts.Length > 0 && texts[0] != null)
        {
            texts[0].text = name;
        }
        if (texts.Length > 1 && texts[1] != null)
        {
            texts[1].text = desc;
        }
        if (texts.Length > 2 && texts[2] != null)
        {
            texts[2].text = price;
        }
        Button btn = item.GetComponentInChildren<Button>(true);
        if (btn != null)
        {
            SetButtonLabel(btn, actionLabel);
        }
    }

    private static void SetButtonLabel(Button btn, string label)
    {
        Text labelText = btn.GetComponentInChildren<Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    // 정산 데이터를 화면에 순서대로 재생 — 점수 계산과 분리되어 스킵해도 같은 결과
    // YutGameManager가 StartCoroutine으로 실행한다
    public IEnumerator PresentSettlement(ThrowSettlement settlement)
    {
        gm.isPresenting = true;

        // 1) 윷 결과 표시 (이전 투척의 베이직·멀티플·투척점수는 0으로 초기화된 상태)
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);

        // 2) 말 이동 연출 — 경유 칸을 한 칸씩 따라 이동 (저주 결과는 이동 없음).
        //    진격부·후퇴부는 이동 보정 이유를 이동 연출과 함께 표시하고 항목을 제거한다
        var moveRecords = gm.talismanManager.usedThisThrow.FindAll(
            r => r.effect == TalismanEffectKind.MoveDelta && !r.displayConsumed);
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

        // 3) 도착 칸 베이직 → 윷 멀티플 순서로 표시 ("개 기본 배율 ×3" 이유 표시)
        gm.basicScore = settlement.baseBasic;
        UpdateUI();
        yield return new WaitForSeconds(gm.effectStepDelay);
        SetMultiplierReason($"{YutGameManager.ToKorean(settlement.result)} 기본 배율 ×{settlement.baseMult}");
        gm.multiplier = settlement.baseMult;
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
            else
            {
                gm.multiplier = hasFloat ? step.afterValueF : step.afterValue;
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
