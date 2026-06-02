#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;


public class SoundEditor : EditorWindow
{
    private SoundManeger soundData;
    private Editor cachedEditor;
    [MenuItem("Tools/SoundManagerWindow")]
    public static void Open()
    {
        GetWindow<SoundEditor>("SoundManagerWindow");
    }

    private void OnGUI()
    {
        soundData = (SoundManeger)EditorGUILayout.ObjectField(
            "Sound Data",
            soundData,
            typeof(SoundManeger),
            false);

        if (soundData == null)
        {
            soundData = Resources.Load<SoundManeger>("Data/SoundData/SoundManager");
        }

        if(soundData == null)
        {
            EditorGUILayout.HelpBox("SoundManager asset not found in Resources/Data/SoundData folder.", MessageType.Warning);
            return;
        }


        if (cachedEditor == null ||
            cachedEditor.target != soundData)
        {
            cachedEditor = Editor.CreateEditor(soundData);
        }

        cachedEditor.OnInspectorGUI();
    }
}

#endif