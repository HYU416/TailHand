// FindComponentsWindow.cs
// Unity EditorWindow: ヒエラルキーから特定スクリプトやインターフェイスを持つオブジェクトを検索
// Menu: Tools/Find Scripts in Hierarchy

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FindComponentsWindow : EditorWindow
{
    [MenuItem("Tools/Find Scripts in Hierarchy")]
    public static void Open()
    {
        var w = GetWindow<FindComponentsWindow>("Find in Hierarchy");
        w.minSize = new Vector2(520, 360);
        w.Show();
    }

    // UI state
    private string _typeName = "";
    private bool _includeInactive = true;
    private Vector2 _scroll;

    // results
    private List<ResultRow> _results = new List<ResultRow>();

    private class ResultRow
    {
        public GameObject go;
        public Component componentSample; // 見つけたコンポーネントの代表1つ
        public string sceneName;
        public string pathInHierarchy;
        public bool isActiveInHierarchy;
    }

    private static readonly GUIContent GC_TypeName = new GUIContent("Type / Interface Name",
        "例: PlayerAttack / MyNamespace.PlayerAttack / IDamageable など（完全修飾も可）");

    private static readonly GUIContent GC_IncludeInactive = new GUIContent("Include Inactive",
        "非アクティブのGameObjectも検索対象に含める");

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        _typeName = EditorGUILayout.TextField(GC_TypeName, _typeName);
        if (GUILayout.Button("SelectCS", GUILayout.Width(90)))
        {
            // Assets/Resources/Scripts 以下のスクリプトの名前をすべてリストにする
            var scripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Resources/Scripts" })
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Select(path => System.IO.Path.GetFileNameWithoutExtension(path))
                .Distinct()
                .OrderBy(name => name)
                .ToArray();
            GenericMenu menu = new GenericMenu();
            foreach (var s in scripts)
            {
                menu.AddItem(new GUIContent(s), s == _typeName, () =>
                {
                    _typeName = s;
                    Repaint();
                });
            }
            menu.ShowAsContext();
            // テキストを選択した後画面に表示
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Search", GUILayout.Width(90)))
        {
            SearchNow();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _includeInactive = EditorGUILayout.Toggle(GC_IncludeInactive, _includeInactive);
        if (GUILayout.Button("Select All", GUILayout.Width(100)))
        {
            Selection.objects = _results.Select(r => (UnityEngine.Object)r.go).ToArray();
        }
        if (GUILayout.Button("Copy Paths", GUILayout.Width(100)))
        {
            var text = string.Join("\n", _results.Select(r =>
                $"{r.sceneName}:{r.pathInHierarchy} [{(r.isActiveInHierarchy ? "Active" : "Inactive")}]"));
            EditorGUIUtility.systemCopyBuffer = text;
            ShowNotification(new GUIContent("Copied paths to clipboard"));
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Results: {_results.Count}", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        using (var scroll = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scroll.scrollPosition;

            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox("ヒットなし。Type/Interface名を入力して Search を押してください。", MessageType.Info);
            }
            else
            {
                foreach (var r in _results)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        // 左：オブジェクトとパス情報
                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.ObjectField(r.go, typeof(GameObject), true);
                        EditorGUILayout.LabelField($"{r.sceneName}  |  {(r.isActiveInHierarchy ? "Active" : "Inactive")}",
                            EditorStyles.miniLabel);
                        EditorGUILayout.LabelField(r.pathInHierarchy, EditorStyles.wordWrappedLabel);
                        if (r.componentSample != null)
                        {
                            EditorGUILayout.LabelField($"Component: {r.componentSample.GetType().FullName}",
                                EditorStyles.miniLabel);
                        }
                        EditorGUILayout.EndVertical();

                        // 右：操作ボタン
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(90)))
                        {
                            if (GUILayout.Button("Ping"))
                            {
                                EditorGUIUtility.PingObject(r.go);
                            }
                            if (GUILayout.Button("Select"))
                            {
                                Selection.activeObject = r.go;
                                EditorGUIUtility.PingObject(r.go);
                            }
                        }
                    }
                }
            }
        }
    }

    private void SearchNow()
    {
        _results.Clear();

        var trimmed = _typeName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            ShowNotification(new GUIContent("Type名/Interface名を入力してください"));
            return;
        }

        // 1) Type解決（クラス名 or 完全修飾名 or インターフェイス名）
        var targetType = ResolveTypeByName(trimmed);

        // もし解決できなくても「インターフェイス/基底型っぽい名前かも」なので、後で名前一致で判定も試す
        bool hasResolvedType = targetType != null;

        // 2) シーン内の全 GameObject を列挙
        var allSceneGOs = EnumerateAllSceneGameObjects(_includeInactive);

        // 3) コンポーネント一致チェック
        foreach (var go in allSceneGOs)
        {
            if (go == null) continue;

            Component hitComponent = null;

            // a) Typeが解決できた場合：その型を持つか（派生/実装含む）
            if (hasResolvedType)
            {
                hitComponent = go.GetComponents<Component>()
                                 .FirstOrDefault(c => c != null && targetType.IsAssignableFrom(c.GetType()));
            }
            else
            {
                // b) 名前一致モード：型が解決できない時は、型名/インターフェイス名の文字一致で判定
                hitComponent = go.GetComponents<Component>()
                                 .FirstOrDefault(c => c != null && TypeNameMatches(c.GetType(), trimmed));
            }

            if (hitComponent != null)
            {
                _results.Add(new ResultRow
                {
                    go = go,
                    componentSample = hitComponent,
                    sceneName = go.scene.IsValid() ? go.scene.name : "(no scene)",
                    pathInHierarchy = BuildHierarchyPath(go),
                    isActiveInHierarchy = go.activeInHierarchy
                });
            }
        }

        // 種類・パスで整列
        _results = _results
            .OrderBy(r => r.sceneName)
            .ThenBy(r => r.pathInHierarchy, StringComparer.Ordinal)
            .ToList();

        Repaint();

        if (_results.Count == 0)
        {
            ShowNotification(new GUIContent("該当オブジェクトは見つかりませんでした"));
        }
    }

    // ==========================
    // Helpers
    // ==========================

    private static IEnumerable<GameObject> EnumerateAllSceneGameObjects(bool includeInactive)
    {
        // シーンに属し、かつアセットでないもの（Prefabアセットを除外）を拾う
        // Unity 2023+ は FindObjectsByType が速い。古い版向けに Resources もフォールバック。
#if UNITY_2023_1_OR_NEWER
        var all = GameObject.FindObjectsByType<GameObject>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        return all.Where(IsSceneObject);
#else
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        if (!includeInactive)
            all = all.Where(g => g.activeInHierarchy).ToArray();
        return all.Where(IsSceneObject);
#endif
    }

    private static bool IsSceneObject(GameObject go)
    {
        // シーンに属していて、アセット（Prefabファイル内）ではない
        if (!go.scene.IsValid()) return false;
        if (EditorUtility.IsPersistent(go)) return false; // Project内のアセットは除外
        return true;
    }

    private static string BuildHierarchyPath(GameObject go)
    {
        var stack = new Stack<string>();
        var t = go.transform;
        while (t != null)
        {
            stack.Push(t.name);
            t = t.parent;
        }
        return string.Join("/", stack);
    }

    private static bool TypeNameMatches(Type t, string query)
    {
        if (t == null) return false;
        // 完全修飾名 or 単純名で大小無視一致
        if (string.Equals(t.FullName, query, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(t.Name, query, StringComparison.OrdinalIgnoreCase)) return true;

        // 実装インターフェイス名とも照合
        foreach (var itf in t.GetInterfaces())
        {
            if (string.Equals(itf.FullName, query, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(itf.Name, query, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static Type ResolveTypeByName(string name)
    {
        // まず完全一致を各アセンブリから探す
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t1 = asm.GetType(name, throwOnError: false, ignoreCase: true);
                if (t1 != null) return t1;
            }
            catch { /* 通らないアセンブリもあるので握りつぶす */ }
        }

        // 単純名一致（名前衝突の可能性があるので最初に見つかったもの）
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type match = null;
            try
            {
                match = asm.GetTypes().FirstOrDefault(t =>
                    string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
            }
            catch { }
            if (match != null) return match;
        }

        // 見つからない場合は null（名前一致モードにフォールバック）
        return null;
    }
}
#endif