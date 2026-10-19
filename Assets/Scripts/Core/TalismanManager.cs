// ──────────────────────────────────────────────
// TalismanManager.cs — 소모형 부적 시스템
//
// 담당:
//   - 런 동안 유지되는 뽑기 덱(drawPile)과 손패(hand)
//   - 후보 선택 창 상태 (시작 5택3 / 땅 3택1 공용)
//   - 사용 가능 구간 검증, 효과 발동, 카드 소모
//   - '이번 투척' 효과의 세션별 보관·해제
//
// 덱 규칙 (사용자 확정):
//   - 덱 = 10종 각 1장, 런 시작 시 섞어서 구성
//   - 사용한 카드는 런에서 소멸 (버림 더미·재섞기 없음)
//   - 시작 선택은 매 보스전 시작 시 후보 5장 중 3장
//   - 땅 착지 = 부적 후보 3장 중 1장 (기존 방울 대체)
//
// 미확정 임시값 (사용자 확정 대기 — 변경 쉽게 한 곳에 모음):
//   - 미선택 후보는 덱으로 복귀
//   - 손패 상한 없음, 한 투척에 여러 장 사용 가능
//   - 탐욕부의 '충돌' = 기존 잡기 조건(같은 칸 착지)
//   - 봉쇄부는 다음 보스 이동 1회만 -2
//   - 승부부는 남은 재투척이 0이면 사용 불가
//   - 인연부: 덱이 비어 있으면 카드 미소모·미발동
//   - 보스 전환 시 손패 유지
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class TalismanManager
{
    private YutGameManager gm;
    private System.Random rng = new System.Random();

    public const int HandLimit = 10;   // 손패 최대 보유 수 — 초과 획득분은 덱 복귀 없이 런에서 소멸

    // 초과 소멸 안내 — 마지막 획득 처리에서 소멸한 카드 수. UI가 읽어 안내 문구 표시
    public int pendingOverflowNotice;

    // 런 단위 덱 — 카드 원본(TalismanData) 참조 목록.
    // 사용·획득 시 목록이 변하며 원본 데이터 자체는 불변.
    public readonly List<TalismanData> drawPile = new List<TalismanData>();
    public readonly List<TalismanData> hand = new List<TalismanData>();
    private bool deckInitialized = false;

    // 선택 창 상태 — 시작 5택3·땅 3택1이 같은 구조를 공유
    public bool isChoosing = false;
    public readonly List<TalismanData> candidates = new List<TalismanData>();
    public readonly List<int> selectedIndices = new List<int>();
    public int requiredPicks = 0;

    // '이번 투척' 피해 효과 — 세션 종료 시 ClearSessionEffects로 해제.
    // 비피해 효과(재기·인연·봉쇄)는 사용 즉시 각자 대상에 반영되고 여기 남지 않는다.
    public class ActiveTalismanEffect
    {
        public TalismanEffectKind kind;
        public TileAttribute attribute;
        public float mult;           // 최종 피해 배율 (×2.5/×1.5/×3). 추혼부는 잡기 배율 대체값(4)
        public string displayName;
        public string sourceId;      // 정산 기록 식별자 — 카드 고유 ID
    }
    public readonly List<ActiveTalismanEffect> sessionEffects = new List<ActiveTalismanEffect>();

    public TalismanManager(YutGameManager gameManager) { gm = gameManager; }

    // ── 덱 초기화 ──
    // 새 런에서 원본 풀을 1장씩 복사해 섞는다. 카드 원본은 불변.
    public void ResetForNewRun()
    {
        drawPile.Clear();
        hand.Clear();
        candidates.Clear();
        selectedIndices.Clear();
        sessionEffects.Clear();
        isChoosing = false;
        requiredPicks = 0;
        pendingOverflowNotice = 0;
        deckInitialized = false;
    }

    private void EnsureDeck()
    {
        if (deckInitialized) return;
        deckInitialized = true;
        drawPile.Clear();
        if (gm.talismanPool == null)
        {
            Debug.LogError("talismanDatabase 에셋 미연결 — 부적 덱을 구성할 수 없습니다.");
            return;
        }
        drawPile.AddRange(gm.talismanPool);   // 에셋 원본은 참조만 — 런타임 덱에서 카드 정의를 변경하지 않는다
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
        Debug.Log($"부적 덱 구성 — {drawPile.Count}장");
    }

    // ── 후보 선택 (시작 5택3 / 땅 3택1 공용) ──
    private void OpenPick(int candidateCount, int picks, string tag)
    {
        EnsureDeck();
        int n = Mathf.Min(candidateCount, drawPile.Count);
        if (n <= 0)
        {
            Debug.Log($"{tag} — 덱에 남은 부적이 없어 선택을 생략한다.");
            return;
        }
        // 후보를 덱에서 꺼낸다 — 미선택분은 확정 시 덱 복귀
        candidates.Clear();
        candidates.AddRange(drawPile.GetRange(0, n));
        drawPile.RemoveRange(0, n);
        selectedIndices.Clear();
        requiredPicks = Mathf.Min(picks, n);
        pendingOverflowNotice = 0;   // 새 선택창이 열리면 이전 소멸 안내는 지운다
        isChoosing = true;
        gm.uiManager.UpdateUI();
    }

    // 카드 획득 공통 경로 — 손패 여유가 있으면 보유, 가득 차면 런에서 소멸
    // 시작 선택·땅 선택·인연부 등 모든 획득은 이 함수를 통과한다
    private bool AcquireCard(TalismanData card)
    {
        if (hand.Count < HandLimit)
        {
            hand.Add(card);
            return true;
        }
        pendingOverflowNotice++;
        Debug.Log($"손패 한도({HandLimit}) 초과 — {card.talismanName} 소멸");
        return false;
    }

    // 매 보스전 시작 — 후보 5장 중 3장
    public void OpenStartPick() => OpenPick(5, 3, "보스전 시작 부적 선택");

    // 땅 칸 착지 — 후보 3장 중 1장 (기존 방울 선택 대체)
    public void OpenLandPick() => OpenPick(3, 1, "땅 칸 부적 선택");

    // 후보 토글 — 이미 선택된 것은 해제, 정원 초과 선택은 무시
    public void SelectCandidate(int index)
    {
        if (!isChoosing || index < 0 || index >= candidates.Count) return;
        if (selectedIndices.Contains(index)) selectedIndices.Remove(index);
        else if (selectedIndices.Count < requiredPicks) selectedIndices.Add(index);
        gm.uiManager.UpdateUI();
    }

    // 선택 확정 — 정원 미달 시 무시. 연타 중복 획득 방지는 isChoosing 해제 순서로 처리
    // 획득 순서 = 사용자가 마지막으로 선택한 순서 (selectedIndices의 추가 순서 —
    // 취소 후 재선택하면 뒤로 다시 붙는다). 초과분은 덱 복귀 없이 소멸
    public void ConfirmChoice()
    {
        if (!isChoosing || selectedIndices.Count != requiredPicks) return;
        isChoosing = false;
        foreach (int i in selectedIndices)
        {
            AcquireCard(candidates[i]);
        }
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            if (!selectedIndices.Contains(i))
            {
                drawPile.Insert(0, candidates[i]); // 미선택 카드는 덱 복귀 (미확정 임시 규칙)
            }
        }
        candidates.Clear();
        selectedIndices.Clear();
        requiredPicks = 0;
        gm.uiManager.UpdateUI();
    }

    // ── 사용 가능 구간 ──
    // 최초 투척 직후 ~ 결과 확정 전. 연출·선택창·비전투 상태에서는 불가.
    public bool CanUseNow()
    {
        if (gm.currentState != GameState.Playing) return false;
        if (isChoosing || gm.bellManager.isChoosingBell || gm.isPresenting) return false;
        var session = gm.rollManager.session;
        return session != null && !session.confirmed;
    }

    // 카드 사용 — 실패한 시도는 카드를 소모하지 않는다.
    // 같은 인덱스 연타도 첫 성공 1회만 반영된다 (소모 후 인덱스가 밀린다).
    public bool UseTalisman(int handIndex)
    {
        if (!CanUseNow() || handIndex < 0 || handIndex >= hand.Count) return false;
        TalismanData card = hand[handIndex];
        // 인연부 — 사용 카드를 먼저 제거한 뒤 카드의 획득 장수(value)만큼 덱 순서대로 뽑는다
        // (손패 10장 → 9장 → 획득 → 10장). 덱이 획득 장수보다 적으면 사용 실패·미소모
        // (덱 1장 남은 경우의 처리는 미확정 — 임시로 '장수 부족 시 발동 불가' 적용)
        if (card.effect == TalismanEffectKind.DrawCard)
        {
            EnsureDeck();
            int drawCount = Mathf.Max(1, Mathf.RoundToInt(card.value));
            if (drawPile.Count < drawCount) return false;
            hand.RemoveAt(handIndex);
            for (int i = 0; i < drawCount; i++)
            {
                TalismanData drawn = drawPile[0];
                drawPile.RemoveAt(0);
                AcquireCard(drawn);   // 손패 한도 검사 — 새로 뽑은 카드만 소멸 대상
                Debug.Log($"부적 사용: {card.talismanName} → {drawn.talismanName} 획득");
            }
            gm.uiManager.UpdateUI();
            return true;
        }
        if (!ApplyEffect(card)) return false;
        hand.RemoveAt(handIndex); // 사용 즉시 소멸 — 런에서 제거
        Debug.Log($"부적 사용: {card.talismanName} ({card.cardId})");
        gm.uiManager.UpdateUI();
        return true;
    }

    private bool ApplyEffect(TalismanData card)
    {
        var session = gm.rollManager.session;
        switch (card.effect)
        {
            // 피해 가산 계열 — 세션 효과로 보관, 정산에서 조건 검사
            case TalismanEffectKind.FireBoost:
            case TalismanEffectKind.WaterBoost:
            case TalismanEffectKind.ForestBoost:
            case TalismanEffectKind.CatchPursuit:
            case TalismanEffectKind.GreedStrike:
            case TalismanEffectKind.WeaknessStrike:
            case TalismanEffectKind.AllOrNothing:
                // 승부부 — 남은 재투척 전부 포기가 비용. 0이면 발동 불가(미소모)
                if (card.effect == TalismanEffectKind.AllOrNothing)
                {
                    if (session.RemainingRerolls <= 0) return false;
                    session.rerollsForfeited = true;
                }
                sessionEffects.Add(new ActiveTalismanEffect
                {
                    kind = card.effect,
                    attribute = card.attribute,
                    mult = card.value,          // 피해 부적 = 최종 피해 배율 / 추혼부 = 잡기 배율 대체값
                    displayName = card.talismanName,
                    sourceId = card.cardId
                });
                return true;

            case TalismanEffectKind.ExtraReroll:
                // 세션별 한도 증가 — 같은 윷 1회 제한·사용 기록은 그대로
                session.bonusRerolls += Mathf.RoundToInt(card.value);
                return true;

            case TalismanEffectKind.BossMoveSeal:
                // 다음 최초 던지기의 보스 이동을 1회 봉쇄 — 이미 완료된 이번 이동은 되돌리지 않는다
                // (중복 사용 시 규칙 미확정 — 임시로 플래그만 세워 중첩 없음)
                gm.bossManager.pendingMoveSeal = true;
                return true;
        }
        return false;
    }

    // 새 투척 세션/보스전/런 경계에서 '이번 투척' 효과 해제
    public void ClearSessionEffects() => sessionEffects.Clear();

    public string RarityName(TalismanRarity r)
    {
        switch (r)
        {
            case TalismanRarity.범품: return "범품";
            case TalismanRarity.진품: return "진품";
            case TalismanRarity.영물: return "영물";
            case TalismanRarity.신물: return "신물";
            default: return "미정";
        }
    }
}
