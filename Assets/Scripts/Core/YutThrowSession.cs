using UnityEngine;

// 윷 4개 투척 세션 — 차례 준비(세션 생성·부적 사용 가능) → 최초 투척
// → 윷별 재투척(기본: 서로 다른 윷 최대 1개, 같은 윷 1회) → 결과 확정
// 면 조합(앞면 개수)으로만 결과를 만든다: 0=모 1=도 2=개 3=걸 4=윷
// 백도는 이 경로에서 생성되지 않는다 (신규 발생 조건 미확정 — 임의 추가 금지)
// 낙은 면 조합과 무관한 별도 결과 — 최초 투척 접수 시 1% 판정(InitialRollNak)으로만
// 발생하고, 네 윷의 면 조합으로 억지 표현하지 않는다 (2026-10-27 확정)
// 세션 데이터는 런타임 전용 — Inspector 직렬화 없음 (일반 C# 클래스)
//
// 세션은 차례 준비 시점에 생성된다(rolled=false = 최초 투척 전 준비 상태).
// 준비 단계에서 사용한 부적 효과는 세션 필드에 보관되고 InitialRoll이 지우지 않는다 —
// 최초 투척은 면만 던지고 세션 초기화는 새 세션 객체 생성으로만 일어난다.
//
// 부적 연동 상태 (세션 한정 — 다음 세션으로 이월 없음):
//   specialRerolls — 재기부의 '특별 재투척' 기회. 기본 기회와 달리 이미 재투척한
//     윷도 다시 던질 수 있다. 사용 우선순위: 미재투척 윷은 기본 기회부터 소비하고
//     기본 기회가 없거나 이미 재투척한 윷이면 특별 기회를 소비한다
//   moveDelta — 진격부·후퇴부의 이동 거리 보정 합산 (윷 배율·결과는 미변경)
//   resultBonus — 일도부~모령부: 사용 후 바로 다음 성공한 재투척의 전체 결과가
//     목표면 성공 기록 → 확정 결과가 같은 종류일 때만 그 윷 배율 ×2
//   freeRerollChance — 윤회령: 개별 재투척 실행 시 이 확률로 기회 차감 생략
//     (면·결과·사용 기록은 정상 갱신, 같은 윷 반복 사용 차단도 유지)
//   maxRerolls — 삼고령: 기본 재투척 기회 가산 (세션 생성 시 설정, 기본 1)
//   rerollsBanned — 결단령: 모든 재투척 금지 (세션 생성 시 설정)
//   rerollFrontUpChance — 양면령 기본값 + 앞·뒷면 점괘 보정의 최종 앞면 확률
//     (세션 생성 시 계산, 20~80% 제한 — 기본·특별 재투척 모두 적용)
//   rerollAttempted — 이번 세션에서 재투척을 1회라도 실행했는지 (승부부 판정 —
//     윤회령 무료 재투척·특별 재투척도 사용으로 친다)
//   catchWidenRange — 포위부: 잡기 판정을 보스 기준 원형 최단 거리 ±N칸까지 확장
//   waterComboBonusCount — 수룡부: 이번 물 착지의 연계 추가 횟수
//   forestDotBoostMult — 초목부: 이번 풀 착지로 등록·증가되는 도트량 배율
//   resultMoveDelta — 환영부: 최종 결과에 따라 이동 보정 (도·개·걸 +2 / 윷·모 −3)
//   stopMove — 정지부: 최종 이동 거리 0, 현재 칸을 최종 착지로 처리
public class YutThrowSession
{
    public const int StickCount = 4;   // 윷 개수
    public const int BaseRerolls = 1;  // 기본 재투척 기회 기준값 (기본 기회 — 2026-10-21 확정: 2→1)

    public int maxRerolls = BaseRerolls; // 이번 세션의 기본 재투척 기회 — 삼고령이 PrepareSession에서 +bonusCount

    public readonly bool[] frontUp = new bool[StickCount];          // 각 윷의 면 (true = 앞)
    public readonly bool[] rerolled = new bool[StickCount];         // 각 윷의 재투척 사용 여부
    public readonly bool[] speciallyRerolled = new bool[StickCount];// 특별 기회로 재투척된 윷 — 같은 윷 반복 사용 차단
    public int rerollUsed;        // 기본 기회로 재투척한 윷 개수
    public int specialRerolls;    // 재기부로 추가된 '특별 재투척' 남은 횟수 — 어떤 윷도 대상 가능(윷당 1회)
    public bool rerollsForfeited; // 레거시 강제 포기 상태 (2026-10-28 승부부 규칙 변경으로 미설정 — 필드는 보존)
    public bool rerollsBanned;    // 결단령 — 모든 재투척 금지 (세션 생성 시 설정, 포기와는 별개 상태)
    public bool rerollAttempted;  // 이번 세션에서 재투척을 1회라도 실행했는지 — 승부부 '재투척 미사용' 판정
                                  // (기본·특별·윤회령 무료 재투척 모두 사용으로 친다)
    public bool isNak;            // 낙 — 최초 투척 접수 시 1% 판정으로만 설정 (InitialRollNak).
                                  // 모든 재투척 불가·확정만 가능, 면 조합과 무관한 고정 결과
    public bool confirmed;        // 결과 확정 여부 — 확정 후 세션은 읽기 전용

    public int moveDelta;         // 진격부(+1)·후퇴부(−1)·환위부(±1) 이동 보정 합산 — 정산에서 이동 거리에 적용
    public int catchWidenRange;   // 포위부 — 잡기 판정을 보스 기준 원형 최단 거리 ±N칸으로 확장 (0 = 같은 칸만)
    public int waterComboBonusCount; // 수룡부 — 이번 물 착지에 추가되는 연계 횟수 (이번 공격부터 적용)
    public float forestDotBoostMult = 1f; // 초목부 — 이번 풀 착지로 등록·증가되는 도트량 배율
    public bool resultMoveDelta;  // 환영부 — 확정 결과별 이동 보정 (도·개·걸 +2 / 윷·모 −3, 윷 배율·결과 미변경)
    public bool stopMove;         // 정지부 — 최종 이동 거리 0, 현재 칸을 최종 착지로 처리 (합산 보정 무시)
    public bool rolled;           // 최초 투척 완료 여부 — false = 준비 단계(부적 사용 가능, 재투척·확정 불가)
    public float freeRerollChance; // 윤회령 — 개별 재투척 시 이 확률로 기회 차감을 생략 (세션 생성 시 공령명 반영값으로 설정, 0 = 미보유·미적용)
    public float rerollFrontUpChance; // 재투척 앞면 확률 — 양면령 기본값 + 점괘 면 보정의 최종값 (세션 생성 시 20~80% 제한, 0 = 기본 0.5 사용)
    // 최초 투척 결과 가중치 — 걸윷령·쌍개령·극단령의 보정 분포 (세션 생성 시 계산).
    // null·빈 맵이면 기본 면 분포. 재투척에는 적용되지 않는다
    public System.Collections.Generic.Dictionary<YutResult, float> initialResultWeights;

    // 일도부~모령부 — 목표 결과별 대기(armed)·성공(succeeded) 상태. 카드별 독립 관리.
    // useId는 사용 부적 표시와 정산 단계를 연결하는 기록 식별자
    public readonly System.Collections.Generic.Dictionary<YutResult, (float mult, string name, int useId)> armedResultBonus
        = new System.Collections.Generic.Dictionary<YutResult, (float, string, int)>();
    public readonly System.Collections.Generic.Dictionary<YutResult, (float mult, string name, int useId)> succeededResultBonus
        = new System.Collections.Generic.Dictionary<YutResult, (float, string, int)>();

    // 세션 진행 중 = 확정 전 (세션 생존 여부 — 준비 단계 포함)
    public bool Active => !confirmed;
    // 투척 진행 중 = 최초 투척 완료 후 확정 전 (재투척·확정 입력 허용 조건)
    public bool RollInProgress => rolled && !confirmed;
    // 남은 재투척 수 = 기본 기회 잔여 + 특별 기회 — 표시는 합계 (승부부 포기·결단령 금지·낙 시 0)
    public int RemainingRerolls => rerollsForfeited || rerollsBanned || isNak ? 0 : (maxRerolls - rerollUsed) + specialRerolls;

    public int FrontCount
    {
        get
        {
            int count = 0;
            for (int i = 0; i < StickCount; i++)
            {
                if (frontUp[i])
                {
                    count++;
                }
            }
            return count;
        }
    }

    // 앞면 개수 → 결과 (0=모 1=도 2=개 3=걸 4=윷). 낙은 면 조합과 무관한 고정 결과
    public YutResult CurrentResult => isNak ? YutResult.Nak : FrontCount switch
    {
        0 => YutResult.Mo,
        1 => YutResult.Do,
        2 => YutResult.Gae,
        3 => YutResult.Geol,
        _ => YutResult.Yut,
    };

    // 최초 투척 — 4개 모두 던진다. 면과 재투척 기록만 초기화하고
    // 준비 단계에서 사용한 부적 효과(특별 재투척·이동 보정·결과별 부적 대기·포기)는 보존한다
    public void InitialRoll()
    {
        for (int i = 0; i < StickCount; i++)
        {
            frontUp[i] = false;
            rerolled[i] = false;
            speciallyRerolled[i] = false;
        }
        // 확률 방울 보정이 있으면 결과를 먼저 추첨하고 해당 결과에 맞는 면 조합을 생성한다 —
        // 보정 결과와 화면의 4개 면이 반드시 일치. 재투척 이후에는 실제 면 조합으로 판정한다
        if (initialResultWeights != null && initialResultWeights.Count > 0)
        {
            RollWeightedFaces();
        }
        else
        {
            for (int i = 0; i < StickCount; i++)
            {
                frontUp[i] = Random.value < 0.5f;
            }
        }
        rerollUsed = 0;
        rolled = true;
        confirmed = false;
        Debug.Log($"--- 윷 투척: {FaceString()} → {YutGameManager.ToKorean(CurrentResult)} (재투척 가능 {RemainingRerolls}) ---");
    }

    // 최초 투척 낙 — 면을 던지지 않고 결과를 낙으로 고정한다.
    // 준비 단계 부적 효과는 InitialRoll과 같이 보존하고, 면·재투척 기록만 초기화한다.
    // 낙 발생 시 재투척은 CanReroll이 전부 거부해 확정만 가능 — 면 조합으로 표현하지 않는다
    public void InitialRollNak()
    {
        for (int i = 0; i < StickCount; i++)
        {
            frontUp[i] = false;
            rerolled[i] = false;
            speciallyRerolled[i] = false;
        }
        rerollUsed = 0;
        rolled = true;
        confirmed = false;
        isNak = true;
        Debug.Log("--- 윷 투척: 낙 — 확정만 가능 (재투척 불가) ---");
    }

    // 확률 방울 보정 추첨 — 세션에 설정된 결과 가중치로 결과를 먼저 추첨하고,
    // 그 결과의 앞면 개수에 맞는 면 조합을 균등 추첨으로 배치한다 (이 호출에서만 1회 추첨)
    private void RollWeightedFaces()
    {
        float total = 0f;
        foreach (float w in initialResultWeights.Values)
        {
            total += Mathf.Max(0f, w);
        }
        if (total <= 0f)
        {
            for (int i = 0; i < StickCount; i++)   // 가중치 오류 — 기본 면 분포로 복귀
            {
                frontUp[i] = Random.value < 0.5f;
            }
            return;
        }
        float roll = Random.value * total;
        float acc = 0f;
        YutResult result = YutResult.Mo;
        foreach (YutResult r in YutGameManager.NormalResults)   // enum 순서 순회 — 결정적 추첨 순서
        {
            acc += initialResultWeights.TryGetValue(r, out float w) ? Mathf.Max(0f, w) : 0f;
            if (roll < acc)
            {
                result = r;
                break;
            }
        }
        int fronts = FrontCountFor(result);
        int placed = 0;
        while (placed < fronts)   // 앞면 k개의 위치를 균등 추첨 — C(4,k) 조합이 같은 확률
        {
            int idx = Random.Range(0, StickCount);
            if (!frontUp[idx])
            {
                frontUp[idx] = true;
                placed++;
            }
        }
    }

    // 결과 → 앞면 개수 — CurrentResult의 역변환 (0=모 1=도 2=개 3=걸 4=윷)
    private static int FrontCountFor(YutResult result) => result switch
    {
        YutResult.Mo => 0,
        YutResult.Do => 1,
        YutResult.Gae => 2,
        YutResult.Geol => 3,
        _ => 4,
    };

    // 지정한 윷의 재투척 가능 여부 — 확정 전·미포기 상태에서:
    //   미재투척 윷은 기본 기회가 남아 있으면 가능(기본 기회 우선 소비)
    //   그 외(이미 재투척한 윷, 기본 소진)는 특별 기회가 남아 있고 그 윷에 특별 기회를
    //   아직 쓰지 않았으면 가능 — 거부된 입력은 기회를 소모하지 않는다
    public bool CanReroll(int index)
    {
        if (!rolled || confirmed || rerollsForfeited || rerollsBanned || isNak || index < 0 || index >= StickCount)
        {
            return false;
        }
        if (!rerolled[index] && rerollUsed < maxRerolls)
        {
            return true;   // 기본 기회 — 서로 다른 윷 최대 maxRerolls개·같은 윷 1회 (삼고령으로 기회 증가 가능)
        }
        return specialRerolls > 0 && !speciallyRerolled[index];   // 재기부 특별 기회
    }

    // 지정한 윷 하나만 다시 던져 결과를 다시 판정한다.
    // 재투척 성공 시 armed 결과별 부적을 평가한다 — 전체 결과가 목표면 성공 기록,
    // 다르면 그 카드는 실패로 끝 (이후 재투척으로 재도전 불가)
    public bool Reroll(int index)
    {
        if (!CanReroll(index))
        {
            return false;
        }
        bool usedSpecial = rerolled[index] || rerollUsed >= maxRerolls;
        // 윤회령 — 이번 재투척이 무료면 해당 기회의 차감만 생략한다.
        // 사용 기록(rerolled·speciallyRerolled)은 그대로 남겨 같은 윷 반복 사용 차단을 유지한다
        bool freeReroll = freeRerollChance > 0f && Random.value < freeRerollChance;
        // 양면령 — 재투척 앞면 확률은 세션에 설정된 값, 미설정이면 기본 0.5 (최초 투척에는 미적용)
        frontUp[index] = Random.value < (rerollFrontUpChance > 0f ? rerollFrontUpChance : 0.5f);
        if (usedSpecial)
        {
            if (!freeReroll)
            {
                specialRerolls--;
            }
            speciallyRerolled[index] = true;   // 특별 기회는 같은 윷에 반복 사용 불가
        }
        else
        {
            if (!freeReroll)
            {
                rerollUsed++;
            }
        }
        rerolled[index] = true;
        rerollAttempted = true;   // 승부부 판정용 — 기본·특별·윤회령 무료 재투척 모두 사용으로 친다
        EvaluateArmedBonuses();
        Debug.Log($"윷 {index + 1}번 재투척{(usedSpecial ? "(특별)" : "")}{(freeReroll ? " — 윤회령으로 기회 미소모" : "")} → {FaceString()} = {YutGameManager.ToKorean(CurrentResult)} (남은 재투척 {RemainingRerolls})");
        return true;
    }

    // armed 카드 평가 — 바로 다음 성공한 재투척 1회만 검사하고 전부 해제한다
    private void EvaluateArmedBonuses()
    {
        if (armedResultBonus.Count == 0)
        {
            return;
        }
        foreach (System.Collections.Generic.KeyValuePair<YutResult, (float mult, string name, int useId)> kv in armedResultBonus)
        {
            if (CurrentResult == kv.Key)
            {
                succeededResultBonus[kv.Key] = kv.Value;
                Debug.Log($"결과별 부적 {kv.Value.name} — 재투척 결과 일치 ({YutGameManager.ToKorean(kv.Key)}) → 성공 대기");
            }
            else
            {
                Debug.Log($"결과별 부적 {kv.Value.name} — 재투척 결과 불일치 ({YutGameManager.ToKorean(CurrentResult)}) → 발동 실패");
            }
        }
        armedResultBonus.Clear();
    }

    // 일도부~모령부 사용 — 목표 결과로 대기 상태를 세운다 (바로 다음 재투척에서 평가)
    public void ArmResultBonus(YutResult target, float mult, string displayName, int useId)
    {
        armedResultBonus[target] = (mult, displayName, useId);
        Debug.Log($"결과별 부적 {displayName} — 다음 재투척 목표 {YutGameManager.ToKorean(target)} 대기");
    }

    // 결과 확정 — 이후 재투척·변경 불가. 이동·공격 정산은 호출자가 처리
    public void Confirm()
    {
        confirmed = true;
        Debug.Log($"--- 결과 확정: {YutGameManager.ToKorean(CurrentResult)} ---");
    }

    public string FaceString()
    {
        string faces = "";
        for (int i = 0; i < StickCount; i++)
        {
            faces += frontUp[i] ? "앞" : "뒤";
        }
        return faces;
    }
}
