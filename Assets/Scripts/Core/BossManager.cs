using System.Collections.Generic;
using UnityEngine;

// 보스 데이터 — 이름·HP·특수능력 수치를 데이터로 분리해 보스별 다른 능력 추가에 대비
// 참고: nakBonusPercent(낙 +N%p)는 신규 윷 4개 면 조합 방식에 적용 불가 — 새 작동 방식 미확정
[System.Serializable]
public class BossData
{
    public string bossName = "저승지기";
    public int maxHP = 1000;                    // 목표 점수 = 보스 최대 HP
    [TextArea] public string abilityDescription = "낙 확률 +2%p";
    public float nakBonusPercent = 2f;          // 보스 능력 수치 (신규 투척 방식 미적용 — 미확정)
}

// 런타임 보스 상성 — 불·물·풀 3속성이 기본/약점/강점 역할을 중복 없이 하나씩 맡는다
// 보스별로 런당 1회 생성해 유지한다. 원본 BossData에는 저장하지 않는다 (런 종료 시 소멸)
public class BossAffinity
{
    public TileAttribute normal;   // 기본 100%
    public TileAttribute weak;     // 약점 200%
    public TileAttribute strong;   // 강점 50%
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

    // 상성 대상이 되는 공격 속성 — START·땅·쇠는 상성 없이 공격점수 0 유지
    private static readonly TileAttribute[] CombatAttributes =
        { TileAttribute.Fire, TileAttribute.Water, TileAttribute.Forest };

    // 보스별 런타임 상성 — 처음 소개/사용 시점에 1회 생성 후 런 동안 유지 (UI 갱신·전투 진입·건너뛰기로 재추첨 없음)
    private readonly Dictionary<int, BossAffinity> runAffinities = new Dictionary<int, BossAffinity>();

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
        EnsureAffinity(index);   // 처음 소개되는 시점에 상성 1회 생성 — 이후 전투·소개에서 같은 값 사용
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

    // ============ 보스 상성 (런타임 데이터 — BossData 원본 미변경) ============

    // 보스 상성 조회 — 없으면 이 시점에 1회 생성해 저장한다. 조회는 추첨을 반복하지 않는다
    public BossAffinity EnsureAffinity(int bossIndex)
    {
        if (!runAffinities.TryGetValue(bossIndex, out BossAffinity affinity))
        {
            // 불·물·풀을 무작위로 섞어 기본/약점/강점에 하나씩 배정 — 역할 중복 없음
            List<TileAttribute> shuffled = new List<TileAttribute>(CombatAttributes);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }
            affinity = new BossAffinity { normal = shuffled[0], weak = shuffled[1], strong = shuffled[2] };
            runAffinities[bossIndex] = affinity;
            string bossName = gm.bosses != null && bossIndex < gm.bosses.Count ? gm.bosses[bossIndex].bossName : $"보스 {bossIndex + 1}";
            Debug.Log($"--- {bossName} 상성 배정 (런 고정): 기본 {BoardManager.AttributeName(affinity.normal)} / 약점 {BoardManager.AttributeName(affinity.weak)} / 강점 {BoardManager.AttributeName(affinity.strong)} ---");
        }
        return affinity;
    }

    // 칸 속성 → 현재 전투 보스의 상성 배율 (약점 ×2 / 강점 ×0.5 / 기본 ×1)
    // 공격 속성이 아닌 칸(START·땅·쇠)은 상성과 무관하게 ×1 반환 — 어차피 공격점수 0이라 데미지 0
    public float AffinityMultiplier(TileAttribute attribute)
    {
        if (attribute != TileAttribute.Fire && attribute != TileAttribute.Water && attribute != TileAttribute.Forest)
        {
            return 1f;
        }
        BossAffinity affinity = EnsureAffinity(gm.selectedBossIndex);
        if (attribute == affinity.weak)
        {
            return 2f;
        }
        if (attribute == affinity.strong)
        {
            return 0.5f;
        }
        return 1f;
    }

    // 상성 역할 이름 — 정산 기록 표시용
    public string AffinityRoleName(TileAttribute attribute)
    {
        BossAffinity affinity = EnsureAffinity(gm.selectedBossIndex);
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

    // GamePanel 상성 정보창 텍스트 — "기본: 불 100% / 약점: 물 200% / 강점: 풀 50%"
    public string BuildAffinityText(int bossIndex)
    {
        BossAffinity affinity = EnsureAffinity(bossIndex);
        return $"기본: {BoardManager.AttributeName(affinity.normal)} 100%\n" +
               $"약점: {BoardManager.AttributeName(affinity.weak)} 200%\n" +
               $"강점: {BoardManager.AttributeName(affinity.strong)} 50%";
    }

    // 런 종료 시 상성 전부 폐기 — 새 런에서 첫 소개/사용 시 새로 생성한다
    public void ResetAffinities() => runAffinities.Clear();
}
