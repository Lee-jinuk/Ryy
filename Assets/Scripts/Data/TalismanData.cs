// ──────────────────────────────────────────────
// TalismanData.cs — 소모형 부적 카드 정의
// 카드 원본 정의 자료형. 런타임 보유·사용 상태는
// TalismanManager가 관리하며, 원본 데이터는 효과
// 실행 과정에서 변경하지 않는다.
// 식별은 cardId + effect 종류로 한다 — 이름·설명
// 문자열 비교로 효과를 실행하지 않는다.
// ──────────────────────────────────────────────

// 카드 레어도 4종 — 카드별 배정과 등급별 추첨
// 확률은 아직 미확정이므로 신규 카드는 '미정'으로 둔다.
public enum TalismanRarity
{
    범품,
    진품,
    영물,
    신물,
    미정        // 레어도 배정 미확정 상태
}

// 효과 종류 — 이름 문자열이 아닌 이 enum으로 판별한다.
// 신규 항목은 끝에만 추가한다 — 기존 값의 숫자가 바뀌면 직렬화된 카드가 깨진다.
public enum TalismanEffectKind
{
    FireBoost,       // 화염부 — 불 칸 착지 시 최종 피해 ×value
    WaterBoost,      // 수룡부 — 물 칸 착지 시 최종 피해 ×value
    ForestBoost,     // 초목부 — 풀 칸 착지 시 최종 피해 ×value
    CatchPursuit,    // 추혼부 — 잡기 성공 시 최종 피해에 추가로 ×value 곱셈 (기본 잡기 배율 유지)
    GreedStrike,     // 탐욕부 — 최종 피해 ×value, 잡기 성공 시 공격 0
    ExtraReroll,     // 재기부 — 이번 세션 특별 재투척 기회 +value (이미 재투척한 윷도 가능)
    DrawCard,        // 인연부 — 즉시 덱에서 value장 획득
    BossMoveSeal,    // 봉쇄부 — 다음 최초 던지기의 보스 이동 1회 금지
    WeaknessStrike,  // 천운부 — 약점 속성 칸 착지 시 최종 피해 ×value (약점 상성과 별도)
    AllOrNothing,    // 승부부 — 남은 재투척 전부 포기 → 최종 피해 ×value
    MoveDelta,       // 진격부·후퇴부 — 이번 투척 이동 거리 ±value (윷 배율·결과 미변경)
    ResultBonus,     // 일도부~모령부 — 다음 재투척의 전체 결과 = targetResult이고 최종 결과도 같으면 그 윷 배율 ×value
    WaterComboKeep,  // 수룡부 — 이번 턴 물 외 착지에도 물 연속 기록 유지 + 현재 물 연계 배율 적용 (사용자 확정)
    ForestDotBoost,  // 초목부 — 이번 풀 착지로 등록·증가되는 도트량 ×value (사용자 확정)
    CatchWiden,      // 포위부 — 이번 턴 보스와 같은 칸 또는 전후 range칸 착지도 잡기 성공
    MoveAdjust,      // 환위부 — 투척 후 확정 전 이동 거리 +1/−1 선택 보정 (백색 부적)
    NextTurnProbBoost, // 도걸부·모윷부 — 다음 차례 최초 투척의 면 조합 추첨에서 targetResult·targetResult2 결과 조합에 가중치 ×value
    BoardReshuffle,  // 역행부 — 말판 속성 배치를 새로 섞기 (말 위치·상태 유지)
    BossMoveReduce,  // 족쇄부 — 사용 차례 포함 duration턴 보스 이동 거리 −value (속박령과 합산)
    ProximityBaseBoost, // 추적부 — 사용 차례 포함 duration턴 보스와 전후 range칸 이내 착지 시 윷 기본점수 ×value
    NoRerollBaseBoost,  // 고집부 — 사용 차례 포함 duration턴 재투척 미사용 차례 공격의 윷 기본점수 ×value
    TriAttributeCombo,  // 삼재부 — 사용 차례 포함 duration턴 불·물·풀 수집 완성 시 세 번째 착지 최종 피해 ×value
    PrevResultMatch,    // 윤회부 — 이번 투척 최종 결과 = 직전 차례 최종 결과이면 최종 피해 ×value
    ResultGamble        // 도박부 — 선택한 윷 결과와 최종 확정 결과 일치 시 이번 보스전 윷 배율 ×value
}

// 카드 사용 가능 시점 — 코드가 아닌 카드 데이터로 판정한다.
// 신규 시점은 끝에만 추가한다 — 기존 값의 숫자가 바뀌면 직렬화된 카드가 깨진다.
public enum TalismanUseTiming
{
    BeforeFirstThrow,       // 최초 던지기 전 준비 단계에서만 사용 가능
    AfterRollBeforeConfirm, // 최초 투척 후~확정 전 — 재투척 전·사이·소진 후에도 가능 (환위부, 사용자 확정)
}

// 카드 외형 분류 — 노란 부적·하얀 부적. 표시 구분용 데이터이며
// 사용 가능 시점·효과와 무관하다. 어느 카드가 어느 색인지는 미확정 —
// 사용자 확정 전까지 전부 '미정'으로 둔다.
public enum TalismanAppearance
{
    미정,
    노란부적,
    하얀부적,
}

[System.Serializable]
public class TalismanData
{
    public string cardId;            // 고유 ID — 효과 판별·디버깅용
    public string talismanName;
    [UnityEngine.TextArea] public string description;
    public TalismanRarity rarity = TalismanRarity.미정;
    public TalismanEffectKind effect;

    // 효과 수치 — 의미는 effect 종류에 따라 다르다 (피해 증가율과 혼동 금지):
    //   피해 부적(화염/수룡/초목/탐욕/천운/승부) = 최종 피해에 곱할 배율 (4/2)
    //   추혼부 = 잡기 성공 시 최종 피해에 추가로 곱할 배율 (4 — 잡기 배율 변경 아님)
    //   재기부 = 특별 재투척 횟수 (1) / 인연부 = 획득 장수 (2)
    //   봉쇄부 = 미사용 (다음 보스 이동 1회 금지 — 수치 없음)
    //   진격부·후퇴부 = 이동 거리 보정 (+1 / −1)
    //   일도부~모령부 = 목표 결과 윷 배율에 곱할 배율 (2)
    public float value;

    // 전체배율 연산 방식 — 피해 효과 카드의 value 해석. Multiply = 가산 합계 확정 후 ×value,
    // Add = 전체배율 가산 합계에 +value. 기존 카드는 전부 Multiply 유지 — 가산 부적은
    // 사용자 지정 카드에만 설정한다 (2026-10-27 확정 구조)
    public EffectOp totalMultOp = EffectOp.Multiply;

    // 속성 조건 카드가 검사할 칸 속성 (Fire/Water/Forest)
    // 비조건 카드에서는 무시된다.
    public TileAttribute attribute;

    // 결과별 부적(일도부~모령부)의 목표 윷 결과 — ResultBonus 종류에서만 사용.
    // 도걸부·모윷부(NextTurnProbBoost)의 첫 번째 목표 결과로도 사용
    public YutResult targetResult;

    // 두 번째 목표 윷 결과 — 도걸부·모윷부(NextTurnProbBoost) 전용
    // (도걸부 = 도+걸, 모윷부 = 모+윷). 다른 종류에서는 무시
    public YutResult targetResult2;

    // 지속 턴 수 — 족쇄부(3)·추적부(2)·고집부(3)·삼재부(3)의 유효 기간.
    // 사용 차례를 1턴으로 센다 (사용자 확정). 0이면 지속 효과 없음
    public int duration;

    // 거리 조건 — 포위부(1)·추적부(2)의 보스 기준 전후 칸 범위. 다른 종류에서는 무시
    public int range;

    // 덱 포함 여부 — false이면 카드 정의는 유지하되 새 런의 덱·선택 후보·획득 경로에서 제외된다
    public bool includeInDeck = true;

    // 사용 가능 시점 — 현재 전 카드 BeforeFirstThrow(최초 던지기 전 준비 단계).
    // 코드가 색상·이름·설명 문자열로 사용 가능 여부를 판단하지 않고 이 값만 본다
    public TalismanUseTiming useTiming = TalismanUseTiming.BeforeFirstThrow;

    // 외형 분류 — 표시 전용 데이터. 사용 시점·효과 판정에 사용하지 않는다
    public TalismanAppearance appearance = TalismanAppearance.미정;
}
