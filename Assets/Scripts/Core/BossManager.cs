using UnityEngine;

// 보스 데이터 — 이름·HP·특수능력 수치를 데이터로 분리해 보스별 다른 능력 추가에 대비
[System.Serializable]
public class BossData
{
    public string bossName = "저승지기";
    public int maxHP = 1000;                    // 목표 점수 = 보스 최대 HP
    [TextArea] public string abilityDescription = "낙 확률 +2%p";
    public float nakBonusPercent = 2f;          // 현재 공통 특수능력 수치 (낙 확률 +N%p)
}

// 보스와 관문 담당 — 보스 데이터 접근, 보스 선택, 보스 소개 화면 정보, 다음 관문·최종 클리어 조건
// 보스 목록 원본은 YutGameManager.bosses (Inspector 편집 유지), 선택 인덱스는 gm.selectedBossIndex
// 일반 C# 클래스 (MonoBehaviour 아님) — Inspector 연결에 영향 없음
public class BossManager
{
    private readonly YutGameManager gm;

    // 보스 소개 화면에 표시 중인 보스 — '진입' 버튼(EnterBossBattle)이 이 보스전을 시작
    public int pendingBossIndex;

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

    // 보스 선택 버튼: index 0~4 — Start 상태에서만 동작 (보스전 진행 중 변경 방지)
    public void SelectBoss(int index)
    {
        if (gm.currentState != GameState.Start)
        {
            return;
        }
        if (gm.bosses == null || index < 0 || index >= gm.bosses.Count)
        {
            Debug.LogWarning($"SelectBoss: 잘못된 인덱스 {index} (0~{(gm.bosses?.Count ?? 0) - 1})");
            return;
        }
        gm.selectedBossIndex = index;
        gm.goalScore = gm.bosses[index].maxHP;   // 선택 화면에서 목표 점수 미리보기
        // 보스 소개 패널이 열려 있으면 표시 내용도 함께 갱신 (최초 선택과 다음 관문 소개가 같은 UI 공유)
        if (gm.bossSelectPanel != null && gm.bossSelectPanel.activeSelf)
        {
            ShowBossSelect(index);
        }
        Debug.Log($"--- 보스 선택: {gm.bosses[index].bossName} (HP {gm.bosses[index].maxHP}, {gm.bosses[index].abilityDescription}) ---");
        gm.uiManager.UpdateUI();
    }

    // 보스 소개/진입 화면 표시 — 최초 선택(Start)과 클리어 후 다음 관문(Cleared)에서 같은 UI 사용
    public void ShowBossSelect(int index)
    {
        if (gm.bosses == null || index < 0 || index >= gm.bosses.Count)
        {
            Debug.LogWarning($"ShowBossSelect: 잘못된 인덱스 {index} (0~{(gm.bosses?.Count ?? 0) - 1})");
            return;
        }
        pendingBossIndex = index;
        BossData boss = gm.bosses[index];
        if (gm.bossSelectPanel == null)
        {
            // 보스 소개 패널 미연결 — 소개를 건너뛰고 바로 전투 진입 (패널 생성·연결 후 자동으로 소개 화면 사용)
            Debug.LogWarning("[YutGameManager] bossSelectPanel 미연결 — 보스 소개를 건너뛰고 바로 전투에 진입합니다. 씬에 BossSelectPanel을 만들어 연결하세요.");
            gm.EnterBossBattle();
            return;
        }
        gm.uiManager.FillBossSelectInfo(boss);
        GameUIManager.ActivatePanel(gm.bossSelectPanel, nameof(gm.bossSelectPanel));
        Debug.Log($"--- 보스 소개: {boss.bossName} | 목표 {boss.maxHP} | {boss.abilityDescription} ---");
    }
}
