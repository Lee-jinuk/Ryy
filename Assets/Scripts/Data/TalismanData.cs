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
public enum TalismanEffectKind
{
    FireBoost,       // 화염부 — 불 칸 착지 시 피해 +value%
    WaterBoost,      // 수룡부 — 물 칸 착지 시 피해 +value%
    ForestBoost,     // 초목부 — 풀 칸 착지 시 피해 +value%
    CatchPursuit,    // 추혼부 — 보스와 같은 칸(잡기) 시 추가피해 +value%
    GreedStrike,     // 탐욕부 — 피해 +value%, 보스와 같은 칸이면 공격 0
    ExtraReroll,     // 재기부 — 이번 세션 재투척 한도 +value
    DrawCard,        // 인연부 — 즉시 덱에서 1장 뽑기
    BossMoveSeal,    // 봉쇄부 — 이번 투척 후 보스 이동거리 -value칸
    WeaknessStrike,  // 천운부 — 약점 속성 칸 착지 시 추가피해 +value%
    AllOrNothing     // 승부부 — 남은 재투척 전부 포기 → 이번 착지 피해 +value%
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
    //   피해 부적(화염/수룡/초목/탐욕/천운/승부) = 최종 피해에 곱할 배율 (2.5/1.5/3)
    //   추혼부 = 잡기 배율 대체값 (4 — ×2를 ×4로 교체, 추가 곱셈 아님)
    //   재기부 = 추가 재투척 횟수 (1) / 인연부 = 획득 장수 (2)
    //   봉쇄부 = 미사용 (다음 보스 이동 1회 금지 — 수치 없음)
    public float value;

    // 속성 조건 카드가 검사할 칸 속성 (Fire/Water/Forest)
    // 비조건 카드에서는 무시된다.
    public TileAttribute attribute;
}
