using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemSpawn))]
public class ItemSpawnEditor : Editor
{
    SerializedProperty rubbleSpawnRate;
    SerializedProperty obsidianSpawnRate;
    SerializedProperty flintSpawnRate;
    SerializedProperty noneSpawnRate;

    SerializedProperty rubblePrefab;
    SerializedProperty obsidianPrefab;
    SerializedProperty flintPrefab;

    private void OnEnable()
    {
        rubbleSpawnRate =
            serializedObject.FindProperty("rubbleSpawnRate");

        obsidianSpawnRate =
            serializedObject.FindProperty("obsidianSpawnRate");

        flintSpawnRate =
            serializedObject.FindProperty("flintSpawnRate");

        noneSpawnRate =
            serializedObject.FindProperty("noneSpawnRate");

        rubblePrefab =
            serializedObject.FindProperty("rubblePrefab");

        obsidianPrefab =
            serializedObject.FindProperty("obsidianPrefab");

        flintPrefab =
            serializedObject.FindProperty("flintPrefab");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 出現率
        EditorGUILayout.PropertyField(rubbleSpawnRate);

        EditorGUILayout.PropertyField(obsidianSpawnRate);

        EditorGUILayout.PropertyField(flintSpawnRate);

        EditorGUILayout.PropertyField(noneSpawnRate);

        GUILayout.Space(5);

        // ボタン
        if (GUILayout.Button("確率を100%に補正"))
        {
            ItemSpawn itemSpawn =
                (ItemSpawn)target;

            Undo.RecordObject(
                itemSpawn,
                "Normalize Rates"
            );

            itemSpawn.NormalizeRates();

            EditorUtility.SetDirty(itemSpawn);
        }

        GUILayout.Space(10);

        // プレハブ
        EditorGUILayout.PropertyField(rubblePrefab);

        EditorGUILayout.PropertyField(obsidianPrefab);

        EditorGUILayout.PropertyField(flintPrefab);

        serializedObject.ApplyModifiedProperties();
    }
}