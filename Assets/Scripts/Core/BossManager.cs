using System.Collections.Generic;
using UnityEngine;

// 보스 데이터 — 이름·HP·특수능력 수치를 데이터로 분리해 보스별 다른 능력 추가에 대비
// 참고: nakBonusPercent(낙 +N%p)는 신규 윷 4개 면 조합 방식에 적용 불가 — 새 작동 방식 미확정
// 상성 = 보스별 선택 능력 (2026-10-22 확정) — useAffinity가 true인 보스만 약점·강점이 있고,
// 비활성 보스는 모든 공격 상성 ×1. 비활성 보스에게 자동 상성을 생성하지 않는다
[System.Serializable]
public class BossData
{
    public string bossName = "저승지기";
    public int maxHP = 1000;                    // 목표 점수 = 보스 최대 HP
    [TextArea] public string abilityDescription = "낙 확률 +2%p";
    public float nakBonusPercent = 2f;          // 보스 능력 수치 (신규 투척 방식 미적용 — 미확정)

    // 상성 능력 — 선택적 보스 능력. 어떤 보스에 적용할지는 미확정: 현재 전 보스 비활성
    public bool useAffinity;                    // 상성 능력 사용 여부 — false면 약점·강점 없이 상성 ×1
    public TileAttribute affinityWeak;          // 약점 속성 (불·물·풀 중 — 강점과 달라야 함)
    public TileAttribute affinityStrong;        // 강점 속성
    public float affinityWeakMult = 2f;         // 약점 상성 배율
    public float affinityStrongMult = 0.5f;     // 강점 상성 배율
}

// 보스 상성 조회 결과 — 활성 보스의 데이터로 구성된다 (런타임 무작위 생성 없음)
public class BossAffinity
{
    public TileAttribute normal;   // 기본 100% — 약점·강점이 아닌 나머지 공격 속성
    public TileAttribute weak;     // 약점
    public TileAttribute strong;   // 강점
}

// 보스와 관문 담당 — 보스 데이터 접근, 보스 소개 화면 정보(순차 소개·건너뛰기), 다음 관문·최종 클리어 조건
// 보스 목록 원본은 YutGameManager.bosses (Inspector 편집 유지).
// 인덱스 의미: pendingBossIndex = 소개 화면에 표시 중인 보스 / selectedBossIndex = 전투 중인 보스
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class BossManager
{
    private readonly YutGameManager gm;

    // 보스 소개 화면에 표시 중인 보스 — '다음' 버튼(EnterBossBattle)이 이 보스전을 시작
    public int pendingBossIndex;

    // 보스 말 위치 — 플레이어 말(board.pieceIndex)과 독립 관리. 같은 30칸 말판 사용
    // 시작 칸 = START(0)이며 모든 보스전 시작 시 START로 초기화 (소개·건너뛰기에서는 변경 없음)
    // 임시 동작 (미확정 — 확정 대기): 이동 방향 = 플레이어와 동일(인덱스 증가·순환)
    public int bossPieceIndex;
    public int lastBossMoveSteps;   // 직전 추첨 이동 거리 (디버그·검증용)

    // 봉쇄부 — 다음 최초 던지기의 보스 이동을 1회 금지 (미확정 임시 규칙: 중복 사용 중첩 없음,
    // 보스 전환 시 해제). 이미 완료된 이번 이동은 되돌리지 않고 다음 MoveBossPiece에서 소모된다
    public bool pendingMoveSeal;

    // 다음 이동 계획 — 차례 준비(PrepareBossMove)에 기본 거리를 1회 추첨해 보관하고
    // 최초 투척 접수(MoveBossPiece)에서 1회 소비한다. 재투척·확정·UI 갱신에서는 재추첨 없음
    public bool movePlanReady;
    public int plannedBaseSteps;    // 추첨된 기본 이동 거리 (족쇄·봉쇄 미반영)

    // 유효 이동 거리·목적지 — 족쇄방울·봉쇄부를 계산 시점에 반영 (봉쇄 플래그는 소모하지 않음).
    // 봉쇄부를 준비 단계에 쓰면 목적지가 현재 칸으로 즉시 바뀐다
    public int PlannedEffectiveSteps =>
        !movePlanReady ? 0 : pendingMoveSeal ? 0 : ApplyMoveReduction(plannedBaseSteps);
    public int PlannedDestination =>
        (bossPieceIndex + PlannedEffectiveSteps) % YutBoard.TileCount;

    // 상성 대상이 되는 공격 속성 — START·땅·쇠는 상성 없이 공격 0 유지
    private static readonly TileAttribute[] CombatAttributes =
        { TileAttribute.Fire, TileAttribute.Water, TileAttribute.Forest };

    public BossManager(YutGameManager gm)
    {
        this.gm = gm;
    }

    public BossData CurrentBoss =>
        gm.bosses != null && gm.bosses.Count > 0
            ? gm.bosses[Mathf.Clamp(gm.selectedBossIndex, 0, gm.bosses.Count - 1)]
            : null;

    // 다음 보스가 남아 있는지 — '다음 관문' 버튼 표시/진행 가능 조건
    public bool HasNextBoss =>
        gm.bosses != null && gm.selectedBossIndex + 1 < gm.bosses.Count;

    // 마지막 보스 클리어인지 — '다음 관문' 버튼 숨김 + '최종 클리어' 메시지 조건
    public bool IsFinalClear =>
        gm.bosses == null || gm.selectedBossIndex >= gm.bosses.Count - 1;

    // '건너뛰기' 가능 조건 — 소개 중인 보스의 다음 보스가 남아 있는지 (건너뛰기 버튼 표시·실행 조건)
    public bool CanSkipPending =>
        gm.bosses != null && pendingBossIndex + 1 < gm.bosses.Count;

    // 소개 화면의 '건너뛰기' 버튼 — 전투·보상·비용 없이 다음 보스 소개로 이동
    // 소개 화면이 열려 있고 Start/Cleared 상태일 때만 동작 (전투·정산·게임오버 중 차단)
    // 처치·클리어 기록과 무관 — pendingBossIndex만 전진하므로 연속 사용해도 되돌아가지 않는다
    public void SkipBoss()
    {
        bool introShowing = gm.bossSelectPanel != null && gm.bossSelectPanel.activeSelf;
        bool allowedState = gm.currentState == GameState.Start || gm.currentState == GameState.Cleared;
        if (!introShowing || !allowedState || !CanSkipPending)
        {
            return;
        }
        ShowBossSelect(pendingBossIndex + 1);
        Debug.Log($"--- 보스 건너뛰기 → 다음 보스 소개: {gm.bosses[pendingBossIndex].bossName} ---");
    }

    // 보스 소개/진입 화면 표시 — 게임 시작(Start)과 클리어 후 다음 관문(Cleared)에서 같은 UI 사용
    public void ShowBossSelect(int index)
    {
        if (gm.bosses == null || index < 0 || index >= gm.bosses.Count)
        {
            Debug.LogWarning($"ShowBossSelect: 잘못된 인덱스 {index} (0~{(gm.bosses?.Count ?? 0) - 1})");
            return;
        }
        pendingBossIndex = index;
        BossData boss = gm.bosses[index];
        gm.shopManager.ResetForBoss();   // 보스 전환 — 상품 목록·리프레시 비용 초기화 (사용자 확정: 보스 소개 시 리셋)
        if (gm.bossSelectPanel == null)
        {
            // 소개 화면은 필수 단계 — 패널 미연결 시 경고 후 대기 (자동 전투 진입 없음)
            Debug.LogWarning("[YutGameManager] bossSelectPanel 미연결 — 보스 소개 화면을 표시할 수 없어 대기합니다. 씬에 BossSelectPanel을 만들어 연결하세요.");
            return;
        }
        gm.uiManager.FillBossSelectInfo(boss);
        GameUIManager.ActivatePanel(gm.bossSelectPanel, nameof(gm.bossSelectPanel));
        gm.uiManager.UpdateUI();
        Debug.Log($"--- 보스 소개: {boss.bossName} | 목표 {boss.maxHP} | {boss.abilityDescription} ---");
    }

    // ============ 보스 상성 (선택적 능력 — BossData 데이터 주도, 무작위 생성 없음) ============

    // 보스의 상성 능력 보유 여부 — useAffinity + 약점·강점이 유효하게 설정된 경우만 true.
    // UI·부적·방울이 각자 다른 기준을 쓰지 않도록 활성 여부 판정은 이 경로로 통일한다
    public bool HasAffinityAbility(int bossIndex)
    {
        if (gm.bosses == null || bossIndex < 0 || bossIndex >= gm.bosses.Count)
        {
            return false;
        }
        BossData boss = gm.bosses[bossIndex];
        if (boss == null || !boss.useAffinity)
        {
            return false;
        }
        if (boss.affinityWeak == boss.affinityStrong)
        {
            Debug.LogWarning($"[YutGameManager] {boss.bossName}의 상성 설정 오류 — 약점과 강점이 같아 상성 능력을 적용하지 않습니다.", gm);
            return false;
        }
        return true;
    }

    // 보스 상성 조회 — 상성 능력이 있는 보스만 BossAffinity 반환, 없으면 null.
    // 가짜 약점·강점을 생성하지 않으므로 천운부·재복방울·청개구리방울은 null 검사로 조건 판정한다
    public BossAffinity GetAffinity(int bossIndex)
    {
        if (!HasAffinityAbility(bossIndex))
        {
            return null;
        }
        BossData boss = gm.bosses[bossIndex];
        TileAttribute normal = TileAttribute.Fire;
        foreach (TileAttribute attr in CombatAttributes)
        {
            if (attr != boss.affinityWeak && attr != boss.affinityStrong)
            {
                normal = attr;
                break;
            }
        }
        return new BossAffinity { normal = normal, weak = boss.affinityWeak, strong = boss.affinityStrong };
    }

    // 칸 속성 → 현재 전투 보스의 상성 배율 (데이터 지정 배율 / 비상성 보스 ×1)
    // 공격 속성이 아닌 칸(START·땅·쇠)은 상성과 무관하게 ×1 반환 — 공격 0이라 어차피 데미지 0
    public float AffinityMultiplier(TileAttribute attribute)
    {
        if (attribute != TileAttribute.Fire && attribute != TileAttribute.Water && attribute != TileAttribute.Forest)
        {
            return 1f;
        }
        if (!HasAffinityAbility(gm.selectedBossIndex))
        {
            return 1f;
        }
        BossData boss = gm.bosses[gm.selectedBossIndex];
        if (attribute == boss.affinityWeak)
        {
            return boss.affinityWeakMult;
        }
        if (attribute == boss.affinityStrong)
        {
            return boss.affinityStrongMult;
        }
        return 1f;
    }

    // 상성 역할 이름 — 정산 기록 표시용 (상성 능력 없으면 '기본')
    public string AffinityRoleName(TileAttribute attribute)
    {
        BossAffinity affinity = GetAffinity(gm.selectedBossIndex);
        if (affinity == null)
        {
            return "기본";
        }
        if (attribute == affinity.weak)
        {
            return "약점";
        }
        if (attribute == affinity.strong)
        {
            return "강점";
        }
        return "기본";
    }

    // GamePanel 상성 정보창 텍스트 — 상성 능력이 있는 보스만 실제 설정 정보를 표시, 없으면 빈 문자열
    public string BuildAffinityText(int bossIndex)
    {
        BossAffinity affinity = GetAffinity(bossIndex);
        if (affinity == null)
        {
            return "";
        }
        BossData boss = gm.bosses[bossIndex];
        return $"기본: {BoardManager.AttributeName(affinity.normal)} 100%\n" +
               $"약점: {BoardManager.AttributeName(affinity.weak)} {Mathf.RoundToInt(boss.affinityWeakMult * 100f)}%\n" +
               $"강점: {BoardManager.AttributeName(affinity.strong)} {Mathf.RoundToInt(boss.affinityStrongMult * 100f)}%";
    }

    // ============ 보스 말 (표시·이동 전용 — 칸 효과·공격·방울 발동 없음) ============

    // 현재 누적 피해 비율이 적용되는 이동 단계 — 실제 누적 피해 totalScore 기준 (연출용 displayTotal 아님)
    // 단계 i는 누적 피해 ≥ 최대 HP의 i/N 에서 적용 (N = 단계 수, 기본 3단계: <1/3 → 4~7, ≥1/3 → 3~6, ≥2/3 → 2~5)
    // 정수 곱셈 비교로 HP가 나누어떨어지지 않아도 경계 오류 없음. 변경분은 다음 최초 투척 추첨부터 적용
    public int BossMoveStage
    {
        get
        {
            int stages = Mathf.Min(gm.bossMoveStageMin?.Length ?? 0, gm.bossMoveStageMax?.Length ?? 0);
            if (stages <= 1 || gm.goalScore <= 0)
            {
                return 0;
            }
            int stage = 0;
            for (int i = 1; i < stages; i++)
            {
                if (gm.totalScore * stages >= i * gm.goalScore)
                {
                    stage = i;   // 경계값 도달 시 해당 단계 적용 — 두 경계를 한 번에 넘어도 최종 단계 선택
                }
            }
            return stage;
        }
    }

    // 보스 이동 거리 추첨 — 현재 피해 단계의 최소~최대 정수 균등 (상한 포함). 잘못된 설정은 보정 후 경고
    private int DrawBossMoveSteps()
    {
        int stage = BossMoveStage;
        int min = gm.bossMoveStageMin[stage];
        int max = gm.bossMoveStageMax[stage];
        if (min > max)
        {
            Debug.LogWarning($"[YutGameManager] 보스 이동 {stage}단계 최소값({min}) > 최대값({max}) — 순서를 바꿔 적용합니다.", gm);
            (min, max) = (max, min);
        }
        if (min < 1)
        {
            Debug.LogWarning($"[YutGameManager] 보스 이동 {stage}단계 최소값이 {min}칸 — 1칸 미만은 허용하지 않아 1로 보정합니다.", gm);
            min = 1;
        }
        return Random.Range(min, max + 1);   // Random.Range(int)는 상한 미포함 → max 포함을 위해 +1
    }

    // 족쇄방울 — 보스 이동 거리 −1칸, 최소 1칸 보장 (사용자 확정).
    // 봉쇄부의 이동 0칸은 PlannedEffectiveSteps·MoveBossPiece에서 먼저 처리되어 이 보정으로 되살아나지 않는다
    private int ApplyMoveReduction(int steps)
    {
        return gm.bellManager.HasBell(BellEffectKind.BossMoveReduce)
            ? Mathf.Max(1, steps - 1)
            : steps;
    }

    // 차례 준비 — 다음 이동의 기본 거리를 1회 추첨해 계획한다. 보스 말은 이동하지 않고
    // UI 갱신·재호출에서는 재추첨하지 않는다 (계획 소비는 MoveBossPiece에서만)
    public void PrepareBossMove()
    {
        plannedBaseSteps = DrawBossMoveSteps();
        movePlanReady = true;
    }

    // 보스 말 1회 이동 — 새 투척 세션의 최초 RollYut() 성공 시에만 호출 (재투척·확정·거부된 입력에서는 호출 금지)
    // 차례 준비에 추첨한 계획을 1회 소비해 실행한다 — 실행 시점에 재추첨하지 않는다
    // 플레이어 위치·누적 이동·완주와 무관하며, 도착·경유 칸의 효과를 발동시키지 않는다
    public void MoveBossPiece()
    {
        if (!movePlanReady)
        {
            PrepareBossMove();   // 안전망 — 정상 흐름에서는 차례 준비에 이미 계획돼 있다
        }
        movePlanReady = false;
        if (pendingMoveSeal)
        {
            // 봉쇄부 — 이번 보스 이동 생략하고 상태 소모. 실제 이동거리 0으로 기록
            pendingMoveSeal = false;
            lastBossMoveSteps = 0;
            Debug.Log($"봉쇄부 적용: 보스 이동 0칸 — 칸 {bossPieceIndex} 유지");
            return;
        }
        int steps = ApplyMoveReduction(plannedBaseSteps);
        lastBossMoveSteps = steps;
        bossPieceIndex = (bossPieceIndex + steps) % YutBoard.TileCount;   // 끝 칸 넘으면 START로 순환
        gm.boardManager.MoveBossPieceVisual(bossPieceIndex);
        Debug.Log($"--- 보스 말 이동: +{steps}칸 (계획 {plannedBaseSteps}) → 칸 {bossPieceIndex} (이동 단계 {BossMoveStage}) ---");
    }

    // 보스 말 초기화 — 논리 위치와 화면 위치를 함께 START(인덱스 0)로 되돌리고 이전 이동 계획을 폐기한다
    // 호출 시점: 모든 보스전 시작(BeginBossFight) + 런 초기화. 소개 화면·건너뛰기에서는 호출되지 않는다
    public void ResetBossPiece()
    {
        bossPieceIndex = 0;
        lastBossMoveSteps = 0;
        movePlanReady = false;
        gm.boardManager.MoveBossPieceVisual(0);
    }
}
