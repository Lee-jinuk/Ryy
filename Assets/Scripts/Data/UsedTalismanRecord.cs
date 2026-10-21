// ──────────────────────────────────────────────
// UsedTalismanRecord.cs — 이번 투척에 사용된 부적의 표시 기록
//
// 카드 사용 성공 시점의 데이터를 복사해 보관한다 — 손패 이동이나
// 원본 카드 정의 변경과 무관하게 정산 연출까지 안전하게 표시할 수 있다.
// 실제 효과(세션 효과·드로우·재투척 추가 등)는 TalismanManager/YutThrowSession이
// 관리하고, 이 기록은 표시 전용이다 — 기록을 지워도 게임 상태는 변하지 않는다.
// useId로 ScoreEffectStep과 사용 부적 패널 항목을 연결한다.
// ──────────────────────────────────────────────
[System.Serializable]
public class UsedTalismanRecord
{
    public int useId;                    // 이번 투척 내 사용 순번 (1부터) — 정산 단계·표시 항목 연결 식별자
    public string cardId;                // 원본 카드 ID
    public string talismanName;          // 사용 시점의 카드 이름
    public string description;           // 사용 시점의 카드 설명
    public TalismanEffectKind effect;    // 효과 종류 — 비피해 카드의 정산 안내 분기용
    public float value;                  // 사용 시점의 효과 수치
    public string resultNote;            // 사용 결과 안내 (드로우 결과·예약 상태 등 — 정산 재생 표시용)
    public bool displayConsumed;         // 정산 연출에서 표시 제거가 끝난 기록 — 재생성 방지
}
