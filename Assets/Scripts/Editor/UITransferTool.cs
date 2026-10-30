#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// YutGameManager의 레거시 UI 연결(legacy*)을 화면별 UI 컴포넌트로 복사하는 이전 도구.
/// - 컴포넌트가 없으면 해당 패널 오브젝트(없으면 매니저 오브젝트)에 자동 부착한다.
/// - 새 필드에 이미 연결이 있으면 절대 덮어쓰지 않고 건너뛴다.
/// - 여러 번 실행해도 컴포넌트·연결이 중복되지 않는다(멱등).
/// - 씬 오브젝트를 삭제·재생성하지 않고 Undo를 지원한다.
/// 사용: 메뉴 'YUTROLL/UI 참조 이전 — 미리보기'로 결과 확인 후 '... — 실행' 실행, Ctrl+S로 씬 저장.
/// </summary>
public static class UITransferTool
{
    // (매니저의 컴포넌트 필드, 부착할 호스트를 결정하는 매니저 패널 프로퍼티)
    static readonly string[,] compHost =
    {
        { "startScreenUI",      "startPanel" },
        { "bossSelectScreenUI", "bossSelectPanel" },
        { "gameScreenUI",       "gamePanel" },
        { "clearScreenUI",      "clearPanel" },
        { "shopScreenUI",       "shopPanel" },
        { "gameOverScreenUI",   "gameOverPanel" },
        { "yutThrowUI",         "gamePanel" },
        { "scoreUI",            "gamePanel" },
        { "bossCombatUI",       "gamePanel" },
        { "boardUI",            "boardPanel" },
        { "talismanHandUI",     "gamePanel" },
        { "equippedBellUI",     "gamePanel" },
        { "talismanChoiceUI",   "talismanChoicePanel" },
        { "moveAdjustUI",       "moveAdjustPanel" },
        { "fortuneChoiceUI",    "fortuneChoicePanel" },
        { "usedTalismanUI",     "usedTalismanPanel" },
    };

    [MenuItem("YUTROLL/UI 참조 이전 (열린 씬) — 미리보기")]
    public static void Preview() { Migrate(true); }

    [MenuItem("YUTROLL/UI 참조 이전 (열린 씬) — 실행")]
    public static void Run() { Migrate(false); }

    static void Migrate(bool preview)
    {
        YutGameManager gm = Object.FindAnyObjectByType<YutGameManager>();
        if (gm == null)
        {
            Debug.LogError("[UITransfer] 열린 씬에 YutGameManager가 없습니다.");
            return;
        }

        int attached = 0, copied = 0, skippedFilled = 0, skippedEmpty = 0;
        List<string> log = new List<string>();

        for (int i = 0; i < compHost.GetLength(0); i++)
        {
            string compFieldName = compHost[i, 0];
            string hostPropName = compHost[i, 1];

            FieldInfo compField = typeof(YutGameManager).GetField(compFieldName, BindingFlags.Public | BindingFlags.Instance);
            PropertyInfo hostProp = typeof(YutGameManager).GetProperty(hostPropName, BindingFlags.Public | BindingFlags.Instance);
            if (compField == null || hostProp == null) { Debug.LogError($"[UITransfer] 매핑 오류: {compFieldName}/{hostPropName}"); continue; }

            // 1) 컴포넌트 확보 — 매니저 필드 → 패널 검색 → 부착
            Component comp = compField.GetValue(gm) as Component;
            if (comp == null)
            {
                GameObject host = hostProp.GetValue(gm) as GameObject;
                if (host == null) host = gm.gameObject;   // 패널 미연결이면 매니저 오브젝트에 부착
                comp = host.GetComponent(compField.FieldType) as Component;
                if (comp == null)
                {
                    if (preview) { log.Add($"[부착 예정] {compField.FieldType.Name} → {host.name}"); attached++; }
                    else
                    {
                        comp = Undo.AddComponent(host, compField.FieldType);
                        attached++;
                        log.Add($"[부착] {compField.FieldType.Name} → {host.name}");
                    }
                }
                if (!preview && comp != null)
                {
                    Undo.RecordObject(gm, "UI 참조 이전");
                    compField.SetValue(gm, comp);
                }
            }
            if (comp == null) continue;   // 미리보기에서 실제 부착 없이는 후속 복사 불가

            // 2) 컴포넌트의 public 직렬화 필드 → gm.legacyXxx에서 복사 (비어 있을 때만)
            foreach (FieldInfo f in compField.FieldType.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!typeof(Object).IsAssignableFrom(f.FieldType) &&
                    !(f.FieldType.IsArray && typeof(Object).IsAssignableFrom(f.FieldType.GetElementType())))
                    continue;

                string legacyName = "legacy" + char.ToUpper(f.Name[0]) + f.Name.Substring(1);
                FieldInfo legacyField = typeof(YutGameManager).GetField(legacyName, BindingFlags.Public | BindingFlags.Instance);
                if (legacyField == null) continue;

                object cur = f.GetValue(comp);
                object src = legacyField.GetValue(gm);
                if (!IsEmpty(cur)) { skippedFilled++; continue; }
                if (IsEmpty(src)) { skippedEmpty++; continue; }

                if (preview) { log.Add($"[복사 예정] {compField.FieldType.Name}.{f.Name} ← {legacyName}"); copied++; }
                else
                {
                    Undo.RecordObject(comp, "UI 참조 이전");
                    f.SetValue(comp, CloneValue(src, f.FieldType));
                    copied++;
                }
            }
        }

        string mode = preview ? "미리보기" : "실행";
        Debug.Log($"[UITransfer:{mode}] 부착 {attached} / 복사 {copied} / 이미 연결돼 건너뜀 {skippedFilled} / 원본 비어 건너뜀 {skippedEmpty}\n" +
                  (log.Count > 0 ? string.Join("\n", log) : "변경 없음") +
                  (preview ? "" : "\nCtrl+S로 씬을 저장하세요."));
        if (!preview) EditorUtility.SetDirty(gm);
    }

    static bool IsEmpty(object v)
    {
        if (v == null) return true;
        if (v is Object o) return o == null;
        if (v is System.Array a)
        {
            foreach (object e in a) if (e is Object eo && eo != null) return false;
            return true;
        }
        return false;
    }

    static object CloneValue(object src, System.Type t)
    {
        if (t.IsArray && src is System.Array a)
        {
            var copy = System.Array.CreateInstance(t.GetElementType(), a.Length);
            a.CopyTo(copy, 0);
            return copy;
        }
        return src;
    }
}
#endif
