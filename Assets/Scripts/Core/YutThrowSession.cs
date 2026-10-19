using UnityEngine;

// 윷 4개 투척 세션 — 최초 투척 → 윷별 재투척(서로 다른 윷 최대 2개, 같은 윷 1회) → 결과 확정
// 면 조합(앞면 개수)으로만 결과를 만든다: 0=모 1=도 2=개 3=걸 4=윷
// 백도·낙은 이 경로에서 생성되지 않는다 (신규 발생 조건 미확정 — 임의 추가 금지)
// 세션 데이터는 런타임 전용 — Inspector 직렬화 없음 (일반 C# 클래스)
public class YutThrowSession
{
    public const int StickCount = 4;   // 윷 개수
    public const int MaxRerolls = 2;   // 세션당 재투척 가능한 서로 다른 윷 수

    public readonly bool[] frontUp = new bool[StickCount];   // 각 윷의 면 (true = 앞)
    public readonly bool[] rerolled = new bool[StickCount];  // 각 윷의 재투척 사용 여부
    public int rerollUsed;        // 이번 세션에서 재투척한 윷 개수
    public int bonusRerolls;      // 재기부 등 이번 세션에만 추가된 재투척 한도 (다음 세션으로 이월 없음)
    public bool rerollsForfeited; // 승부부 — 남은 재투척을 전부 포기한 상태 (세션 내 번복 불가)
    public bool confirmed;        // 결과 확정 여부 — 확정 후 세션은 읽기 전용

    // 세션 진행 중 = 확정 전 (재투척·확정 버튼 활성 조건, 새 최초 투척 덮어쓰기 차단 조건)
    public bool Active => !confirmed;
    public int MaxRerollCount => MaxRerolls + bonusRerolls;
    public int RemainingRerolls => rerollsForfeited ? 0 : MaxRerollCount - rerollUsed;

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

    // 최초 투척 — 4개 모두 던진다. 재투척 기록과 함께 세션 전체를 초기화한다
    public void InitialRoll()
    {
        for (int i = 0; i < StickCount; i++)
        {
            frontUp[i] = Random.value < 0.5f;
            rerolled[i] = false;
        }
        rerollUsed = 0;
        confirmed = false;
        Debug.Log($"--- 윷 투척: {FaceString()} → {YutGameManager.ToKorean(CurrentResult)} (재투척 가능 {RemainingRerolls}) ---");
    }

    // 지정한 윷의 재투척 가능 여부 — 확정 전, 세션 한도(기본+부적 보너스) 미만,
    // 승부부로 재투척을 포기하지 않았고 아직 재투척하지 않은 윷
    public bool CanReroll(int index) =>
        !confirmed && !rerollsForfeited && rerollUsed < MaxRerollCount
        && index >= 0 && index < StickCount && !rerolled[index];

    // 지정한 윷 하나만 다시 던져 결과를 다시 판정한다 (같은 윷 1회 한정)
    public bool Reroll(int index)
    {
        if (!CanReroll(index))
        {
            return false;
        }
        frontUp[index] = Random.value < 0.5f;
        rerolled[index] = true;
        rerollUsed++;
        Debug.Log($"윷 {index + 1}번 재투척 → {FaceString()} = {YutGameManager.ToKorean(CurrentResult)} (남은 재투척 {RemainingRerolls})");
        return true;
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
