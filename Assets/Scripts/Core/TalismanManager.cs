// ──────────────────────────────────────────────
// TalismanManager.cs — 소모형 부적 시스템
//
// 담당:
//   - 보스전 동안 유지되는 뽑기 덱(drawPile)과 손패(hand) — 매 보스전 시작 시 재구성
//   - 후보 선택 창 상태 (시작 7택5 / 땅 3택1 공용)
//   - 사용 가능 구간 검증, 효과 발동, 카드 소모
//   - '이번 투척' 효과의 세션별 보관·해제
//
// 덱 규칙 (사용자 확정):
//   - 덱 = includeInDeck 활성 카드 각 1장, **매 보스전 시작 시** 섞어서 새로 구성 (2026-10-22 변경)
//   - 사용한 카드는 그 보스전에서 소멸 — 다음 보스전의 새 덱에는 다시 포함
//   - 시작 선택은 매 보스전 시작 시 후보 7장 중 5장
//   - 땅 착지 = 부적 후보 3장 중 1장 (기존 방울 대체)
//
// 미확정 임시값 (사용자 확정 대기 — 변경 쉽게 한 곳에 모음):
//   - 미선택 후보는 덱으로 복귀
//   - 한 투척에 여러 장 사용 가능 (피해 배율 곱셈 중첩)
//   - 봉쇄부 중복 사용 시 중첩 없음, 보스 전환 시 미소비 봉쇄 해제
//   - 인연부: 덱이 획득 장수보다 적으면 카드 미소모·미발동 (덱 1장 경계 미확정)
//   - 포위부 중복 사용 시 더 넓은 범위만 유지 (범위 합산 없음)
// ──────────────────────────────────────────────
using System.Collections.Generic;
using UnityEngine;

public class TalismanManager
{
    private YutGameManager gm;
    private System.Random rng = new System.Random();

    public const int HandLimit = 10;   // 손패 최대 보유 수 — 초과 획득분은 덱 복귀 없이 해당 보스전에서 소멸 (다음 보스전 새 덱에는 복귀)

    // 초과 소멸 안내 — 마지막 획득 처리에서 소멸한 카드 수. UI가 읽어 안내 문구 표시
    public int pendingOverflowNotice;

    // 보스전 단위 덱 — 카드 원본(TalismanData) 참조 목록, 보스전 시작마다 재구성.
    // 사용·획득 시 목록이 변하며 원본 데이터 자체는 불변.
    public readonly List<TalismanData> drawPile = new List<TalismanData>();
    public readonly List<TalismanData> hand = new List<TalismanData>();
    private bool deckInitialized = false;

    // 선택 창 상태 — 시작 7택5·땅 3택1이 같은 구조를 공유
    public bool isChoosing = false;

    // 환위부 ±1 선택창 상태 — 후보 선택과 별도 창. 열려 있는 동안 투척·재투척·확정 입력 차단.
    // 선택 확정 전에는 카드를 소모하지 않으며 취소로 닫을 수 있다
    public bool isChoosingMoveAdjust = false;
    private int pendingMoveAdjustHandIndex = -1;   // 선택 대기 중인 카드의 손패 인덱스
    public readonly List<TalismanData> candidates = new List<TalismanData>();
    public readonly List<int> selectedIndices = new List<int>();
    public int requiredPicks = 0;

    // '이번 투척' 피해 효과 — 세션 종료 시 ClearSessionEffects로 해제.
    // 비피해 효과(재기·인연·봉쇄)는 사용 즉시 각자 대상에 반영되고 여기 남지 않는다.
    public class ActiveTalismanEffect
    {
        public TalismanEffectKind kind;
        public TileAttribute attribute;
        public float mult;           // 효과 수치 — totalMultOp에 따라 전체배율 곱연산(×N) 또는 가산(+N)으로 해석
        public EffectOp totalMultOp = EffectOp.Multiply;   // 카드 정의의 연산 방식 복사본
        public string displayName;
        public string sourceId;      // 정산 기록 식별자 — 카드 고유 ID
        public int useId;            // 사용 부적 표시 기록과 연결하는 식별자 (0 = 기록 없음)
    }
    public readonly List<ActiveTalismanEffect> sessionEffects = new List<ActiveTalismanEffect>();

    // 이번 투척에 사용 성공한 카드의 표시 기록 — 사용 부적 패널이 읽는다.
    // 실제 효과(세션 효과·드로우·특별 기회 등)와 별개의 표시 전용 데이터 —
    // 기록을 지워도 게임 상태는 변하지 않는다. 새 세션 시작·보스전·런 초기화 시 정리
    public readonly List<UsedTalismanRecord> usedThisThrow = new List<UsedTalismanRecord>();
    private int nextUseId = 1;

    // 다음 차례 최초 투척 확률 보정 예약 — 도걸부·모윷부.
    // 사용 차례가 아닌 다음 PrepareSession에서 1회 소비되고 보스전을 넘기지 않는다
    public struct PendingProbBoost
    {
        public YutResult result1;    // 첫 번째 대상 결과
        public YutResult result2;    // 두 번째 대상 결과
        public float mult;           // 대상 가중치 곱 (도걸부 ×1.5 / 모윷부 ×1.2 — 적용 후 100% 정규화)
        public string displayName;
    }
    public readonly List<PendingProbBoost> pendingProbBoosts = new List<PendingProbBoost>();

    // 차례 지속 효과 — 족쇄부(보스 이동 감소)·추적부(근접 착지 윷 기본배율).
    // 사용 차례를 1턴으로 세고 PrepareTurn(OnTurnStart)마다 1턴씩 차감한다
    public struct TurnEffect
    {
        public int turnsLeft;
        public float value;          // 족쇄부 = 이동 감소 칸 / 추적부 = 윷 배율 곱
        public int range;            // 추적부의 보스 기준 거리 범위 (족쇄부 미사용)
        public int useId;            // 사용 부적 기록 연결 — 같은 차례 정산 단계에 연결
        public string displayName;
    }
    public readonly List<TurnEffect> shackleEffects = new List<TurnEffect>();   // 족쇄부 — 보스 이동 감소 (속박령과 합산)
    public readonly List<TurnEffect> chaseEffects = new List<TurnEffect>();     // 추적부 — 근접 착지 윷 배율

    // 족쇄부 보스 이동 감소 합계 — BossManager.ApplyMoveReduction이 속박령과 합산해 조회한다
    public int BossMoveReduction
    {
        get
        {
            int sum = 0;
            foreach (TurnEffect e in shackleEffects)
            {
                sum += Mathf.RoundToInt(e.value);
            }
            return sum;
        }
    }

    // 다음 차례 확률 보정 예약을 꺼내면서 목록을 비운다 — YutRollManager.PrepareSession에서
    // 새 세션의 최초 투척 가중치에 반영할 때 1회만 소비한다 (없으면 null)
    public List<PendingProbBoost> ConsumeProbBoosts()
    {
        if (pendingProbBoosts.Count == 0)
        {
            return null;
        }
        var boosts = new List<PendingProbBoost>(pendingProbBoosts);
        pendingProbBoosts.Clear();
        return boosts;
    }

    // 새 차례 준비 — 차례 지속 효과의 남은 턴을 1 차감하고 만료분을 제거한다.
    // 사용 차례를 1턴으로 세므로 PrepareTurn에서만 감소한다 (정산·UI 갱신 미호출)
    public void OnTurnStart()
    {
        TickTurnEffects(shackleEffects);
        TickTurnEffects(chaseEffects);
    }

    private static void TickTurnEffects(List<TurnEffect> effects)
    {
        for (int i = effects.Count - 1; i >= 0; i--)
        {
            TurnEffect e = effects[i];
            e.turnsLeft--;
            if (e.turnsLeft <= 0)
            {
                effects.RemoveAt(i);
            }
            else
            {
                effects[i] = e;
            }
        }
    }

    // 이동 연출과 함께 사유를 표시하는 카드 효과 — 정산 안내 단계에서는 제외하고
    // 이동 경로 재생 시 사용 부적 표시를 제거한다 (진격부·후퇴부·환영부·정지부)
    public static bool IsMoveDisplayEffect(TalismanEffectKind kind) =>
        kind == TalismanEffectKind.MoveDelta || kind == TalismanEffectKind.ResultMoveDelta
        || kind == TalismanEffectKind.StopMove;

    public TalismanManager(YutGameManager gameManager) { gm = gameManager; }

    // ── 덱 초기화 ──
    // 새 런 초기화 — 보스전 초기화와 같은 내용 (덱·손패 모두 보스전 단위)
    public void ResetForNewRun() => ResetForBoss();

    // 보스전 초기화 — 이전 손패·후보 선택·사용 기록·투척 한정 효과를 정리하고
    // 덱을 다음 OpenPick에서 새로 구성하게 한다 (사용·소멸 카드도 새 덱에 복귀).
    // 실제 전투 진입(BeginBossFight)에서만 호출 — 소개·상점·건너뛰기에서는 호출되지 않는다 (2026-10-22 확정)
    public void ResetForBoss()
    {
        drawPile.Clear();
        hand.Clear();
        candidates.Clear();
        selectedIndices.Clear();
        sessionEffects.Clear();
        usedThisThrow.Clear();
        nextUseId = 1;
        isChoosing = false;
        isChoosingMoveAdjust = false;
        pendingMoveAdjustHandIndex = -1;
        requiredPicks = 0;
        pendingOverflowNotice = 0;
        pendingProbBoosts.Clear();   // 확률 부적 예약은 보스전을 넘기지 않는다 (사용자 확정)
        shackleEffects.Clear();
        chaseEffects.Clear();
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
        // includeInDeck = false인 카드 정의는 덱에서 제외 — 정의 자체와 enum은 유지한다
        foreach (TalismanData card in gm.talismanPool)
        {
            if (card == null || !card.includeInDeck) continue;
            drawPile.Add(card);   // 에셋 원본은 참조만 — 런타임 덱에서 카드 정의를 변경하지 않는다
        }
        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
        Debug.Log($"부적 덱 구성 — {drawPile.Count}장");
    }

    // 선택 창 규격 — 시작 7택5·땅 3택1 (2026-10-27 확정 — 기존 시작 5택3에서 변경)
    public const int StartCandidateCount = 7;
    public const int StartPickCount = 5;
    public const int LandCandidateCount = 3;
    public const int LandPickCount = 1;
    public const int OptionSlotMax = StartCandidateCount;   // 공용 선택창이 수용하는 최대 후보 수

    // 화면에 실제 연결된 후보 슬롯 수 — 이름·설명·레어도 배열 중 가장 작은 길이.
    // 슬롯이 후보 수보다 적으면 보이지 않는 카드를 선택할 수 없어 창에 갇힐 수 있으므로
    // 필요 선택 수를 슬롯 수 이내로 제한한다 (후보 자체는 규격대로 뽑는다)
    private int AvailableOptionSlots()
    {
        int nameLen = gm.talismanOptionNameTexts != null ? gm.talismanOptionNameTexts.Length : 0;
        int descLen = gm.talismanOptionDescTexts != null ? gm.talismanOptionDescTexts.Length : 0;
        int rareLen = gm.talismanOptionRarityTexts != null ? gm.talismanOptionRarityTexts.Length : 0;
        return Mathf.Min(nameLen, descLen, rareLen);
    }

    // ── 후보 선택 (시작 7택5 / 땅 3택1 공용) ──
    private void OpenPick(int candidateCount, int picks, string tag)
    {
        EnsureDeck();
        int slots = AvailableOptionSlots();
        if (slots <= 0)
        {
            Debug.LogError($"{tag} — 부적 후보 UI 슬롯이 연결되지 않아 선택을 생략한다 (덱 소모 없음). " +
                "talismanOptionNameTexts/DescTexts/RarityTexts 배열 연결 확인 필요");
            return;
        }
        int n = Mathf.Min(candidateCount, drawPile.Count);
        if (n <= 0)
        {
            Debug.Log($"{tag} — 덱에 남은 부적이 없어 선택을 생략한다.");
            return;
        }
        if (slots < n)
        {
            Debug.LogWarning($"{tag} — 후보 {n}장 중 {n - slots}장은 표시 슬롯({slots}칸)이 없어 선택할 수 없다. " +
                $"옵션 배열을 {OptionSlotMax}칸으로 확장하고 SelectTalismanOption 버튼을 추가하면 전체 선택 가능");
        }
        // 후보를 덱에서 꺼낸다 — 미선택분은 확정 시 덱 복귀
        candidates.Clear();
        candidates.AddRange(drawPile.GetRange(0, n));
        drawPile.RemoveRange(0, n);
        selectedIndices.Clear();
        requiredPicks = Mathf.Min(picks, n, slots);   // 슬롯 부족 시에도 확정 가능한 수로 제한
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

    // 매 보스전 시작 — 후보 7장 중 5장
    public void OpenStartPick() => OpenPick(StartCandidateCount, StartPickCount, "보스전 시작 부적 선택");

    // 땅 칸 착지 — 후보 3장 중 1장 (기존 방울 선택 대체)
    public void OpenLandPick() => OpenPick(LandCandidateCount, LandPickCount, "땅 칸 부적 선택");

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
    // 최초 던지기 전 준비 단계 — 세션은 차례 준비에 생성되고 최초 투척(rolled)부터 사용 불가.
    // 연출·선택창·상점·비전투 상태에서는 불가.
    public bool CanUseNow()
    {
        if (gm.currentState != GameState.Playing) return false;
        if (isChoosing || isChoosingMoveAdjust || gm.isPresenting) return false;
        var session = gm.rollManager.session;
        return session != null && !session.rolled && !session.confirmed;
    }

    // 최초 투척 후~확정 전 사용 구간 — 환위부(백색 부적) 전용.
    // 재투척 전·사이·소진 후에도 확정 전이면 사용 가능 (사용자 확정).
    // 낙 세션에서는 불가 — 이동 보정으로 낙을 우회할 수 없다
    public bool CanUseAfterRoll()
    {
        if (gm.currentState != GameState.Playing) return false;
        if (isChoosing || isChoosingMoveAdjust || gm.isPresenting) return false;
        var session = gm.rollManager.session;
        return session != null && session.rolled && !session.confirmed && !session.isNak;
    }

    // 카드별 사용 판정 — 카드 데이터의 사용 시점에 따라 구간 검사를 나눈다.
    // 손패 버튼 활성화와 실제 실행이 이 한 경로를 공유한다 (public — UI가 카드별 조회)
    public bool CanUseCard(TalismanData card)
    {
        if (card == null) return false;
        if (card.useTiming == TalismanUseTiming.AfterRollBeforeConfirm)
        {
            return CanUseAfterRoll();
        }
        return CanUseNow() && card.useTiming == TalismanUseTiming.BeforeFirstThrow;
    }

    // 사용 성공한 카드의 표시 기록 생성 — 사용 시점 데이터를 복사해 보관.
    // useId로 정산 단계(ScoreEffectStep.useId)와 패널 항목을 연결한다
    private UsedTalismanRecord RecordUse(TalismanData card, string resultNote)
    {
        var rec = new UsedTalismanRecord
        {
            useId = nextUseId++,
            cardId = card.cardId,
            talismanName = card.talismanName,
            description = card.description,
            effect = card.effect,
            value = card.value,
            resultNote = resultNote
        };
        usedThisThrow.Add(rec);
        return rec;
    }

    // 카드 사용 — 실패한 시도는 카드를 소모하지 않고 표시 기록도 남기지 않는다.
    // 같은 인덱스 연타도 첫 성공 1회만 반영된다 (소모 후 인덱스가 밀린다).
    public bool UseTalisman(int handIndex)
    {
        if (handIndex < 0 || handIndex >= hand.Count) return false;
        TalismanData card = hand[handIndex];
        if (!CanUseCard(card)) return false;
        // 인연부 — 사용 카드를 먼저 제거한 뒤 카드의 획득 장수(value)만큼 덱 순서대로 뽑는다
        // (손패 10장 → 9장 → 획득 → 10장). 덱이 획득 장수보다 적으면 사용 실패·미소모
        // (덱 1장 남은 경우의 처리는 미확정 — 임시로 '장수 부족 시 발동 불가' 적용)
        if (card.effect == TalismanEffectKind.DrawCard)
        {
            EnsureDeck();
            int drawCount = Mathf.Max(1, Mathf.RoundToInt(card.value));
            if (drawPile.Count < drawCount) return false;
            hand.RemoveAt(handIndex);
            int gained = 0, lost = 0;
            for (int i = 0; i < drawCount; i++)
            {
                TalismanData drawn = drawPile[0];
                drawPile.RemoveAt(0);
                if (AcquireCard(drawn)) gained++; else lost++;   // 손패 한도 검사 — 새로 뽑은 카드만 소멸 대상
                Debug.Log($"부적 사용: {card.talismanName} → {drawn.talismanName} 획득");
            }
            RecordUse(card, lost > 0
                ? $"{card.talismanName} — {gained}장 획득·{lost}장 소멸"
                : $"{card.talismanName} — {gained}장 획득");
            gm.uiManager.UpdateUI();
            return true;
        }
        // 환위부 — ±1 선택창을 먼저 연다. 선택 확정 전에는 카드 소모·효과 적용 없음 (취소 가능)
        if (card.effect == TalismanEffectKind.MoveAdjust)
        {
            isChoosingMoveAdjust = true;
            pendingMoveAdjustHandIndex = handIndex;
            gm.uiManager.UpdateUI();
            return true;
        }
        if (!ApplyEffect(card, nextUseId, out string resultNote)) return false;
        RecordUse(card, resultNote);
        hand.RemoveAt(handIndex); // 사용 즉시 소멸 — 런에서 제거
        Debug.Log($"부적 사용: {card.talismanName} ({card.cardId})");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 환위부 ±1 선택 확정 — 여기서만 이동 보정 적용·카드 소모·기록 (delta = +1 또는 −1).
    // 재검증은 '사용 개시' 판정(CanUseAfterRoll)과 분리한다 — 열려 있는 선택창
    // 자체(isChoosingMoveAdjust)는 완료 거부 사유가 아니다 (2026-10-22 오후 버그 수정)
    public bool ChooseMoveAdjust(int delta)
    {
        if (!isChoosingMoveAdjust) return false;
        if (delta != 1 && delta != -1) return false;   // ±1 외 보정 거부
        var session = gm.rollManager.session;
        if (pendingMoveAdjustHandIndex < 0 || pendingMoveAdjustHandIndex >= hand.Count || session == null)
        {
            CancelMoveAdjust();   // 대기 카드·세션이 사라진 비정상 상태 — 소모 없이 닫기
            return false;
        }
        TalismanData card = hand[pendingMoveAdjustHandIndex];
        // 나머지 상태 재검증 — Playing·투척 완료·미확정·연출 중 아님·땅 선택창 없음
        if (card.effect != TalismanEffectKind.MoveAdjust ||
            gm.currentState != GameState.Playing || isChoosing || gm.isPresenting ||
            !session.rolled || session.confirmed)
        {
            return false;   // 창은 열린 채 유지 — 취소로 닫을 수 있다
        }
        session.moveDelta += delta;   // 기존 진격부·후퇴부 보정과 같은 합산 경로 (최소 이동 0칸 처리 유지)
        RecordUse(card, $"{card.talismanName} — 이동 {(delta > 0 ? "+" : "")}{delta}칸 보정");
        hand.RemoveAt(pendingMoveAdjustHandIndex);
        pendingMoveAdjustHandIndex = -1;
        isChoosingMoveAdjust = false;
        Debug.Log($"부적 사용: {card.talismanName} ({card.cardId}) — 이동 {delta:+0;-0}칸");
        gm.uiManager.UpdateUI();
        return true;
    }

    // 환위부 선택 취소 — 카드 소모·효과 적용 없이 창만 닫는다
    public void CancelMoveAdjust()
    {
        if (!isChoosingMoveAdjust) return;
        pendingMoveAdjustHandIndex = -1;
        isChoosingMoveAdjust = false;
        gm.uiManager.UpdateUI();
    }

    private bool ApplyEffect(TalismanData card, int useId, out string resultNote)
    {
        resultNote = null;
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
                // 승부부 — 이번 차례에 재투척을 한 번도 하지 않고 확정하면 ×value.
                // 사용 즉시 재투척 강제 포기는 폐기됐다 (2026-10-28) — 카드 사용 전후의
                // 재투척 실행 여부(rerollAttempted)를 정산이 검사한다
                sessionEffects.Add(new ActiveTalismanEffect
                {
                    kind = card.effect,
                    attribute = card.attribute,
                    mult = card.value,          // 피해 부적 = 전체배율 수치 / 추혼부 = 잡기 성공 시 추가 수치
                    totalMultOp = card.totalMultOp,   // 가산/곱연산 구분은 카드 데이터로 결정
                    displayName = card.talismanName,
                    sourceId = card.cardId,
                    useId = useId
                });
                return true;

            case TalismanEffectKind.ExtraReroll:
                // 재기부 특별 재투척 — 이미 재투척한 윷도 다시 던질 수 있는 별도 기회.
                // 기본 기회가 0이어도 사용 가능하지만 결단령 금지·낙·강제 포기 상태는 우회하지 않는다.
                // 기본 기회의 같은 윷 제한·사용 기록은 그대로 유지된다
                if (gm.bellManager.HasBell(BellEffectKind.RerollBan)
                    || session == null || session.rerollsBanned || session.rerollsForfeited || session.isNak)
                {
                    return false;
                }
                session.specialRerolls += Mathf.RoundToInt(card.value);
                resultNote = $"{card.talismanName} — 특별 재투척 +{Mathf.RoundToInt(card.value)}, 이미 처리됨";
                return true;

            case TalismanEffectKind.MoveDelta:
                // 진격부·후퇴부 — 이번 투척 이동 거리 보정 합산 (윷 결과·배율 미변경,
                // 정산에서 확정된 착지 칸 기준으로 모든 판정). 안내는 정산의 이동 연출과 함께 표시
                session.moveDelta += Mathf.RoundToInt(card.value);
                resultNote = $"{card.talismanName} — 이동 {(card.value > 0 ? "+" : "")}{Mathf.RoundToInt(card.value)}칸 보정";
                return true;

            case TalismanEffectKind.ResultBonus:
                // 일도부~모령부 — 바로 다음 성공한 재투척의 전체 결과를 세션이 평가한다.
                // 카드별 목표(targetResult)로 독립 대기 — 재투척 없이 확정하면 발동 없음
                session.ArmResultBonus(card.targetResult, card.value, card.talismanName, useId);
                return true;

            case TalismanEffectKind.BossMoveSeal:
                // 다음 최초 던지기의 보스 이동을 1회 봉쇄 — 이미 완료된 이번 이동은 되돌리지 않는다
                // (중복 사용 시 규칙 미확정 — 임시로 플래그만 세워 중첩 없음).
                // 표시 기록이 사라져도 봉쇄 상태(pendingMoveSeal)는 유지된다
                gm.bossManager.pendingMoveSeal = true;
                resultNote = $"{card.talismanName} — 다음 보스 이동 봉쇄 예약";
                return true;

            case TalismanEffectKind.CatchWiden:
                // 포위부 — 이번 투척 잡기 판정을 보스 기준 전후 range칸까지 확장.
                // 같은 카드 중복 사용 시 더 넓은 범위만 유지 (범위는 합산하지 않는다)
                session.catchWidenRange = Mathf.Max(session.catchWidenRange, Mathf.RoundToInt(card.range));
                resultNote = $"{card.talismanName} — 보스 전후 {Mathf.RoundToInt(card.range)}칸 잡기 범위 확장";
                return true;

            case TalismanEffectKind.WaterComboBonus:
                // 수룡부 — 이번 물 착지의 연계 횟수 추가. 연수령과 독립적으로 각 1회씩 반영
                session.waterComboBonusCount += Mathf.Max(1, Mathf.RoundToInt(card.value));
                resultNote = $"{card.talismanName} — 물 착지 시 연계 +{Mathf.Max(1, Mathf.RoundToInt(card.value))} 예약";
                return true;

            case TalismanEffectKind.ForestDotBoost:
                // 초목부 — 이번 풀 착지로 등록·증가되는 도트량만 배율 (저장 도트 전체·즉시 피해는 미변경)
                session.forestDotBoostMult *= card.value;
                resultNote = $"{card.talismanName} — 이번 풀 도트 등록량 ×{card.value}";
                return true;

            case TalismanEffectKind.ResultMoveDelta:
                // 환영부 — 최종 확정 결과에 따라 이동 보정 (도·개·걸 +2 / 윷·모 −3).
                // 실제 보정은 정산에서 확정 결과를 보고 계산한다
                session.resultMoveDelta = true;
                resultNote = $"{card.talismanName} — 도·개·걸 +2 / 윷·모 −3 이동 보정";
                return true;

            case TalismanEffectKind.StopMove:
                // 정지부 — 합산 보정과 무관하게 최종 이동 0, 현재 칸을 착지로 처리
                session.stopMove = true;
                resultNote = $"{card.talismanName} — 이번 투척 이동 0칸";
                return true;

            case TalismanEffectKind.NextTurnProbBoost:
                // 도걸부·모윷부 — 다음 차례 최초 투척 1회의 결과 분포에 대상 가중치 배율.
                // 이번 차례 세션에는 적용하지 않는다 — 다음 PrepareSession에서 1회 소비
                pendingProbBoosts.Add(new PendingProbBoost
                {
                    result1 = card.targetResult,
                    result2 = card.targetResult2,
                    mult = card.value,
                    displayName = card.talismanName,
                });
                resultNote = $"{card.talismanName} — 다음 차례 최초 투척 확률 보정 예약";
                return true;

            case TalismanEffectKind.BoardReshuffle:
                // 역행부 — 말·보스 위치와 물 연계·도트·완주·런 강화를 유지한 채 속성만 재추첨
                gm.boardManager.ReshuffleAttributes();
                resultNote = $"{card.talismanName} — 말판 속성 재배치";
                return true;

            case TalismanEffectKind.BossMoveReduce:
                // 족쇄부 — 사용 차례 포함 duration턴 보스 이동 −value (속박령과 합산).
                // 이동 예고는 PlannedEffectiveSteps 조회에 즉시 반영된다
                shackleEffects.Add(new TurnEffect
                {
                    turnsLeft = Mathf.Max(1, card.duration),
                    value = Mathf.Max(1, Mathf.RoundToInt(card.value)),
                    useId = useId,
                    displayName = card.talismanName,
                });
                resultNote = $"{card.talismanName} — 보스 이동 −{Mathf.Max(1, Mathf.RoundToInt(card.value))} ({Mathf.Max(1, card.duration)}턴)";
                return true;

            case TalismanEffectKind.ProximityBaseBoost:
                // 추적부 — 사용 차례 포함 duration턴, 보스 전후 range칸 이내 착지 공격의 윷 기본배율 ×value
                chaseEffects.Add(new TurnEffect
                {
                    turnsLeft = Mathf.Max(1, card.duration),
                    value = card.value,
                    range = card.range,
                    useId = useId,
                    displayName = card.talismanName,
                });
                resultNote = $"{card.talismanName} — 보스 전후 {card.range}칸 착지 윷 배율 ×{card.value} ({Mathf.Max(1, card.duration)}턴)";
                return true;
        }
        return false;
    }

    // 새 투척 세션/보스전/런 경계에서 '이번 투척' 효과 해제 — 사용 표시 기록도 함께 정리
    public void ClearSessionEffects()
    {
        sessionEffects.Clear();
        usedThisThrow.Clear();
    }

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
