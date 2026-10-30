using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 사용 부적 목록 UI — 사용 카드 표시 영역·항목 부모·항목 Prefab + 항목 동적 생성/제거.
// 표시 전용 (사용 기록·효과 소비 판정은 TalismanManager.usedThisThrow 소유)
// 참조 연결의 기준은 이 컴포넌트. 읽기는 gm 접근자(컴포넌트 우선·레거시 폴백)로 통일한다.
// 이전: YutGameManager.usedTalismanPanel·usedTalismanContent·usedTalismanItemPrefab
public class UsedTalismanUI : MonoBehaviour
{
    public GameObject usedTalismanPanel;       // 사용 부적 패널 — 이번 투척 사용 카드 표시 영역 (미연결 시 생략)
    public Transform usedTalismanContent;      // 사용 부적 목록 부모 — 항목 Prefab이 자식으로 생성 (미연결 시 생략)
    public GameObject usedTalismanItemPrefab;  // 사용 부적 항목 Prefab — 자식 Text 첫 번째=이름, 두 번째=설명(선택)

    // useId → 생성된 항목 오브젝트 (손패 슬롯과 무관한 표시 전용 런타임 상태)
    private readonly Dictionary<int, GameObject> usedItems = new Dictionary<int, GameObject>();
    private bool usedPanelWarned;

    // 사용 부적 패널 동기화 — 사용 성공 기록을 동적 항목으로 표시한다.
    // 표시 전용: 항목 생성·제거가 실제 효과·덱·손패에 영향을 주지 않는다.
    // 미연결 시 표시만 생략하고 게임 계산은 계속 진행한다
    public void Sync(YutGameManager gm)
    {
        var records = gm.talismanManager.usedThisThrow;
        if (gm.usedTalismanContent == null || gm.usedTalismanItemPrefab == null)
        {
            if (!usedPanelWarned && records.Count > 0)
            {
                usedPanelWarned = true;
                Debug.LogWarning("[YutGameManager] usedTalismanContent/usedTalismanItemPrefab 미연결 — 사용 부적 패널 표시가 생략됩니다.", gm);
            }
            return;
        }
        // 기록에서 지워졌거나 정산 재생으로 소비된 항목 정리
        var stale = new List<int>();
        foreach (KeyValuePair<int, GameObject> kv in usedItems)
        {
            UsedTalismanRecord rec = records.Find(r => r.useId == kv.Key);
            if (rec == null || rec.displayConsumed)
            {
                stale.Add(kv.Key);
            }
        }
        foreach (int id in stale)
        {
            if (usedItems[id] != null) Destroy(usedItems[id]);
            usedItems.Remove(id);
        }
        // 새 사용 기록의 항목 생성 — 첫 번째 자식 Text = 이름, 두 번째 = 설명(선택)
        foreach (UsedTalismanRecord rec in records)
        {
            if (rec.displayConsumed || usedItems.ContainsKey(rec.useId)) continue;
            GameObject item = Instantiate(gm.usedTalismanItemPrefab, gm.usedTalismanContent);
            Text[] texts = item.GetComponentsInChildren<Text>(true);
            if (texts.Length > 0 && texts[0] != null) texts[0].text = rec.talismanName;
            if (texts.Length > 1 && texts[1] != null) texts[1].text = rec.description;
            usedItems[rec.useId] = item;
        }
        GameUIManager.SetPanel(gm.usedTalismanPanel, records.Exists(r => !r.displayConsumed));
    }

    // 정산 단계와 연결된 사용 카드 표시 1개 제거 — 기록만 소비 처리, 게임 상태는 건드리지 않는다
    public void RemoveUsedItem(YutGameManager gm, int useId)
    {
        UsedTalismanRecord rec = gm.talismanManager.usedThisThrow.Find(r => r.useId == useId);
        if (rec != null) rec.displayConsumed = true;
        if (usedItems.TryGetValue(useId, out GameObject item))
        {
            usedItems.Remove(useId);
            if (item != null) Destroy(item);
        }
        Sync(gm);
    }

    // 정산 종료·스킵 시 표시 정리 — 이유 텍스트를 비우고 남은 사용 카드 항목을 전부 제거한다.
    // 다음 투척에 이전 효과 표시가 남지 않게 하는 정리 경로
    public void ClearFeedback(YutGameManager gm)
    {
        if (gm.scoreUI != null) gm.scoreUI.SetMultiplierReason(gm, "");
        foreach (UsedTalismanRecord rec in gm.talismanManager.usedThisThrow)
        {
            rec.displayConsumed = true;
        }
        foreach (KeyValuePair<int, GameObject> kv in usedItems)
        {
            if (kv.Value != null) Destroy(kv.Value);
        }
        usedItems.Clear();
        Sync(gm);
    }
}
