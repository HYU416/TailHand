

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FindPrefabWindow : EditorWindow
{
    [MenuItem("Tools/Find Prefab in Hierarchy")]
    public static void Open()
    {
        var w = GetWindow<FindPrefabWindow>("Find in Hierarchy");
        w.minSize = new Vector2(520, 360);
        w.Show();
    }

    // UI state
    private string _typeName = "";
    private bool _includeInactive = true;
    private bool _searchAllScenes = true;
    private Vector2 _scroll;

    // results
    private List<ResultRow> _results = new List<ResultRow>();

    private class ResultRow
    {
        public GameObject go;
        public Component componentSample; // �������R���|�[�l���g�̑�\1��
        public string sceneName;
        public string pathInHierarchy;
        public bool isActiveInHierarchy;
    }

    private static readonly GUIContent GC_TypeName = new GUIContent("Type / Interface Name",
        "��: PlayerAttack / MyNamespace.PlayerAttack / IDamageable �Ȃǁi���S�C�����j");

    private static readonly GUIContent GC_IncludeInactive = new GUIContent("Include Inactive",
        "��A�N�e�B�u��GameObject�������ΏۂɊ܂߂�");

    private static readonly GUIContent GC_SearchAllScenes = new GUIContent("Search All Scenes",
        "Project���̑SScene���J���Ă���Prefab�C���X�^���X������");

    private void OnGUI()
    {
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        _typeName = EditorGUILayout.TextField(GC_TypeName, _typeName);
        if (GUILayout.Button("SelectPrefab", GUILayout.Width(90)))
        {
            // Assets/Resources/Scripts �ȉ��̃X�N���v�g�̖��O�����ׂă��X�g�ɂ���
            var scripts = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/prefabs" })
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
            // �e�L�X�g��I���������ʂɕ\��
            GUI.FocusControl(null);
        }
        if (GUILayout.Button("Search", GUILayout.Width(90)))
        {
            SearchNow();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        _includeInactive = EditorGUILayout.Toggle(GC_IncludeInactive, _includeInactive);
        _searchAllScenes = EditorGUILayout.Toggle(GC_SearchAllScenes, _searchAllScenes);
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
                EditorGUILayout.HelpBox("�q�b�g�Ȃ��BType/Interface������͂��� Search �������Ă��������B", MessageType.Info);
            }
            else
            {
                foreach (var r in _results)
                {
                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        // ���F�I�u�W�F�N�g�ƃp�X���
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

                        // �E�F����{�^��
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

        var searchName = _typeName.Trim();
        if (string.IsNullOrEmpty(searchName))
        {
            ShowNotification(new GUIContent("Prefab������͂��Ă�������"));
            return;
        }

        string[] prefabGuids =
            AssetDatabase.FindAssets($"t:Prefab {searchName}");

        var targetPrefabPaths = prefabGuids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (targetPrefabPaths.Count == 0)
        {
            Repaint();
            ShowNotification(new GUIContent("�Y���Prefab������܂���"));
            return;
        }

        if (!_searchAllScenes)
        {
            foreach (var path in targetPrefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                _results.Add(new ResultRow
                {
                    go = prefab,
                    sceneName = "Prefab",
                    pathInHierarchy = path,
                    isActiveInHierarchy = true
                });
            }
            Repaint();
            return;
        }

        var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (var scenePath in scenePaths)
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                if (!scene.IsValid()) continue;

                foreach (var go in EnumerateAllSceneGameObjects(_includeInactive))
                {
                    if (go == null || go.scene.path != scene.path) continue;

                    var instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(go);
                    if (instanceRoot == null) continue;

                    var source = PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot);
                    var sourcePath = source != null ? AssetDatabase.GetAssetPath(source) : string.Empty;
                    if (string.IsNullOrEmpty(sourcePath) || !targetPrefabPaths.Contains(sourcePath)) continue;

                    var uniqueKey = $"{scene.path}:{instanceRoot.GetInstanceID()}";
                    if (!seen.Add(uniqueKey)) continue;

                    _results.Add(new ResultRow
                    {
                        go = instanceRoot,
                        sceneName = scene.name,
                        pathInHierarchy = BuildHierarchyPath(instanceRoot),
                        isActiveInHierarchy = instanceRoot.activeInHierarchy
                    });
                }
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        _results = _results
            .OrderBy(r => r.sceneName)
            .ThenBy(r => r.pathInHierarchy, StringComparer.Ordinal)
            .ToList();

        Repaint();
    }

    // ==========================
    // Helpers
    // ==========================

    private static IEnumerable<GameObject> EnumerateAllSceneGameObjects(bool includeInactive)
    {
        // �V�[���ɑ����A���A�Z�b�g�łȂ����́iPrefab�A�Z�b�g�����O�j���E��
        // Unity 2023+ �� FindObjectsByType �������B�Â��Ō����� Resources ���t�H�[���o�b�N�B
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
        // �V�[���ɑ����Ă��āA�A�Z�b�g�iPrefab�t�@�C�����j�ł͂Ȃ�
        if (!go.scene.IsValid()) return false;
        if (EditorUtility.IsPersistent(go)) return false; // Project���̃A�Z�b�g�͏��O
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
        // ���S�C���� or �P�����ő召������v
        if (string.Equals(t.FullName, query, StringComparison.OrdinalIgnoreCase)) return true;
        if (string.Equals(t.Name, query, StringComparison.OrdinalIgnoreCase)) return true;

        // �����C���^�[�t�F�C�X���Ƃ��ƍ�
        foreach (var itf in t.GetInterfaces())
        {
            if (string.Equals(itf.FullName, query, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(itf.Name, query, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static Type ResolveTypeByName(string name)
    {
        // �܂����S��v���e�A�Z���u������T��
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var t1 = asm.GetType(name, throwOnError: false, ignoreCase: true);
                if (t1 != null) return t1;
            }
            catch { /* �ʂ�Ȃ��A�Z���u��������̂ň���Ԃ� */ }
        }

        // �P������v�i���O�Փ˂̉\��������̂ōŏ��Ɍ����������́j
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

        // ������Ȃ��ꍇ�� null�i���O��v���[�h�Ƀt�H�[���o�b�N�j
        return null;
    }
}
#endif