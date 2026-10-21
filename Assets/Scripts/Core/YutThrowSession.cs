using UnityEngine;

// 윷 4개 투척 세션 — 차례 준비(세션 생성·부적 사용 가능) → 최초 투척
// → 윷별 재투척(기본: 서로 다른 윷 최대 1개, 같은 윷 1회) → 결과 확정
// 면 조합(앞면 개수)으로만 결과를 만든다: 0=모 1=도 2=개 3=걸 4=윷
// 백도·낙은 이 경로에서 생성되지 않는다 (신규 발생 조건 미확정 — 임의 추가 금지)
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
public class YutThrowSession
{
    public const int StickCount = 4;   // 윷 개수
    public const int MaxRerolls = 1;   // 세션당 재투척 가능한 서로 다른 윷 수 (기본 기회 — 2026-10-21 확정: 2→1)

    public readonly bool[] frontUp = new bool[StickCount];          // 각 윷의 면 (true = 앞)
    public readonly bool[] rerolled = new bool[StickCount];         // 각 윷의 재투척 사용 여부
    public readonly bool[] speciallyRerolled = new bool[StickCount];// 특별 기회로 재투척된 윷 — 같은 윷 반복 사용 차단
    public int rerollUsed;        // 기본 기회로 재투척한 윷 개수
    public int specialRerolls;    // 재기부로 추가된 '특별 재투척' 남은 횟수 — 어떤 윷도 대상 가능(윷당 1회)
    public bool rerollsForfeited; // 승부부 — 남은 기본·특별 기회를 전부 포기한 상태 (세션 내 번복 불가)
    public bool confirmed;        // 결과 확정 여부 — 확정 후 세션은 읽기 전용

    public int moveDelta;         // 진격부(+1)·후퇴부(−1) 이동 보정 합산 — 정산에서 이동 거리에 적용
    public bool rolled;           // 최초 투척 완료 여부 — false = 준비 단계(부적 사용 가능, 재투척·확정 불가)

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
    // 남은 재투척 수 = 기본 기회 잔여 + 특별 기회 — 표시는 합계 (승부부 포기 시 0)
    public int RemainingRerolls => rerollsForfeited ? 0 : (MaxRerolls - rerollUsed) + specialRerolls;

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

    // 앞면 개수 → 결과 (0=모 1=도 2=개 3=걸 4=윷)
    public YutResult CurrentResult => FrontCount switch
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
            frontUp[i] = Random.value < 0.5f;
            rerolled[i] = false;
            speciallyRerolled[i] = false;
        }
        rerollUsed = 0;
        rolled = true;
        confirmed = false;
        Debug.Log($"--- 윷 투척: {FaceString()} → {YutGameManager.ToKorean(CurrentResult)} (재투척 가능 {RemainingRerolls}) ---");
    }

    // 지정한 윷의 재투척 가능 여부 — 확정 전·미포기 상태에서:
    //   미재투척 윷은 기본 기회가 남아 있으면 가능(기본 기회 우선 소비)
    //   그 외(이미 재투척한 윷, 기본 소진)는 특별 기회가 남아 있고 그 윷에 특별 기회를
    //   아직 쓰지 않았으면 가능 — 거부된 입력은 기회를 소모하지 않는다
    public bool CanReroll(int index)
    {
        if (!rolled || confirmed || rerollsForfeited || index < 0 || index >= StickCount)
        {
            return false;
        }
        if (!rerolled[index] && rerollUsed < MaxRerolls)
        {
            return true;   // 기본 기회 — 서로 다른 윷 최대 1개·같은 윷 1회
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
        bool usedSpecial = rerolled[index] || rerollUsed >= MaxRerolls;
        frontUp[index] = Random.value < 0.5f;
        if (usedSpecial)
        {
            specialRerolls--;
            speciallyRerolled[index] = true;   // 특별 기회는 같은 윷에 반복 사용 불가
        }
        else
        {
            rerollUsed++;
        }
        rerolled[index] = true;
        EvaluateArmedBonuses();
        Debug.Log($"윷 {index + 1}번 재투척{(usedSpecial ? "(특별)" : "")} → {FaceString()} = {YutGameManager.ToKorean(CurrentResult)} (남은 재투척 {RemainingRerolls})");
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
