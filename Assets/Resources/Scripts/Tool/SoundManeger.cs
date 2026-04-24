using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using System.Reflection;
using System.Media;
#endif
using Unity.VisualScripting;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;

//SEの種類
public enum SEList
{
    SE_PLAYER_JUMP,
    SE_PLAYER_LAND,
    MAX,
}

//BGMの種類
public enum BGMList
{
    BGM_GAME,
    MAX,
}

//BGMの種類
public enum VelumeRollOffType
{
    Logarithmic,
    Linear,
    Custom,
}

[CreateAssetMenu(menuName = "Audio/AudioConfig")]
public class SoundManeger : ScriptableObject
{

    //Audioのデータ
    [System.Serializable]
    public struct AudioListData
    {
        [SerializeField]
        [HideInInspector]
        public AudioClip audioClip;
        [Range(0, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float volume;
        [Range(0, 3.0f)]
        [SerializeField]
        [HideInInspector]
        public float pitch;
        [Range(0, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float spatialBlend;
        [SerializeField]
        [HideInInspector]
        public VelumeRollOffType velumeRollOffType;
        [SerializeField]
        [HideInInspector]
        public bool isLoop;
        [SerializeField]
        [HideInInspector]
        public AudioMixerGroup audioMixerGroup;
    }
    //SE
    //enum変数
    [SerializeField]
    [HideInInspector]
    public SEList seList;

    [SerializeField]
    [HideInInspector]
    public AudioListData[] seListData;


    //BGM
    //enum変数
    [SerializeField]
    [HideInInspector]
    public BGMList bgmList;
    [SerializeField]
    [HideInInspector]
    public AudioListData[] bgmListData;

    // 音のミキサー
    [SerializeField]
    [HideInInspector]
    public AudioMixer audioMixer;

    [System.Serializable]
    public struct MixerData
    {
        [SerializeField]
        [HideInInspector]
        [Range(-80.0f, 20.0f)]
        public float Volume;

    }


    [SerializeField]
    [HideInInspector]
    public List<string> MixerGroupName;

    [SerializeField]
    [HideInInspector]
    public List<MixerData> AllMixerData = new List<MixerData>();

    [SerializeField]
    [HideInInspector]
    public int selectIndex = 0;



    private void OnEnable()
    {
        EnsureArraySize(ref seListData, (int)SEList.MAX);
        EnsureArraySize(ref bgmListData, (int)BGMList.MAX);
        MixerGroupName = new List<string>();
        foreach (var group in audioMixer.FindMatchingGroups(""))
        {
            MixerGroupName.Add(group.name);
        }
        MixerGroupName.Add("None");
        if (MixerGroupName.Count != AllMixerData.Count)
        {
            for (int i = AllMixerData.Count; i < MixerGroupName.Count; i++)
            {
                AllMixerData.Add(new MixerData { Volume = 1.0f });
            }
            for (int i = MixerGroupName.Count; i < AllMixerData.Count; i++)
            {
                AllMixerData.RemoveAt(i);
            }
        }

        //Debug.Log("ミキサーのグループ数: " + AllMixerData.Count);
    }

    private void EnsureArraySize<T>(ref T[] array, int size)
    {
        if (array == null || array.Length != size)
        {
            T[] newArr = new T[size];
            if (array != null)
            {
                Array.Copy(array, newArr, Math.Min(array.Length, size));
            }
            array = newArr;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SoundManeger))]
public class SoundManegerInspector : Editor
{
    SoundManeger _target;
    //SE 
    private SerializedProperty seDataProperiy;

    private SerializedProperty seListProperiy;
    private SEList seList;
    bool isseListChanged = false;

    //BGM
    private SerializedProperty BGMDataProperiy;

    private SerializedProperty bgmListProperiy;
    private BGMList bgmList;
    bool isbgmListChanged = false;

    //ミキサー
    private SerializedProperty MixcerProperiy;
    private SerializedProperty AllMixerData;


    private SerializedProperty AudioMixerGroupProperiy;
    private SerializedProperty selectIndex;


    void OnEnable()
    {
        _target = target as SoundManeger;
        //SEの初期化
        seDataProperiy = serializedObject.FindProperty("seListData");
        seListProperiy = serializedObject.FindProperty("seList");

        //BGMの初期化
        BGMDataProperiy = serializedObject.FindProperty("bgmListData");

        bgmListProperiy = serializedObject.FindProperty("bgmList");

        //ミキサーの初期化
        MixcerProperiy = serializedObject.FindProperty("audioMixer");
        AllMixerData = serializedObject.FindProperty("AllMixerData");



        selectIndex = serializedObject.FindProperty("selectIndex");

    }
    //インスペクターに表示
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        //SE
        if (_target.seList != seList)
        {
            isseListChanged = true;
        }
        if (isseListChanged)
        {

            seList = _target.seList;
            isseListChanged = false;
        }
        //BGM
        if (_target.bgmList != bgmList)
        {
            isbgmListChanged = true;
        }
        if (isbgmListChanged)
        {

            bgmList = _target.bgmList;
            isbgmListChanged = false;
        }
        //SE
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("SEの設定", GUILayout.Width(160));
        //SEの選択
        EditorGUILayout.PropertyField(seListProperiy, GUIContent.none);
        GUILayout.EndHorizontal();

        int index = (int)_target.seList;
        Debug.Log("インデックス" + index + "a" + seDataProperiy.arraySize);
        if (index >= 0 && index < seDataProperiy.arraySize)
        {
            SerializedProperty element = seDataProperiy.GetArrayElementAtIndex(index);
            EditorGUILayout.PropertyField(element.FindPropertyRelative("audioClip"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("volume"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("pitch"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("spatialBlend"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("velumeRollOffType"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("isLoop"));
            EditorGUILayout.PropertyField(element.FindPropertyRelative("audioMixerGroup"));
        }
        else
        {
            EditorGUILayout.HelpBox("選択中のSEが範囲外です", MessageType.Warning);
        }



        //音の再生ボタン
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev"))
        {

            PlayClip(_target.seListData[(int)_target.seList].audioClip);
        }
        if (GUILayout.Button("Next"))
        {
            _target.seList++;
        }

        GUILayout.EndHorizontal();

        //BGM
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("BGMの設定", GUILayout.Width(160));
        //BGMの選択
        EditorGUILayout.PropertyField(bgmListProperiy, GUIContent.none);
        GUILayout.EndHorizontal();

        index = (int)_target.bgmList;
        Debug.Log("インデックス" + index + "a" + BGMDataProperiy.arraySize);
        if (index >= 0 && index < BGMDataProperiy.arraySize)
        {
            SerializedProperty element2 = BGMDataProperiy.GetArrayElementAtIndex((int)_target.bgmList);
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("audioClip"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("volume"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("pitch"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("spatialBlend"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("velumeRollOffType"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("isLoop"));
            EditorGUILayout.PropertyField(element2.FindPropertyRelative("audioMixerGroup"));
        }
        else
        {
            EditorGUILayout.HelpBox("選択中のBGMが範囲外です", MessageType.Warning);
        }

        //音の再生ボタン
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev"))
        {

            PlayClip(_target.bgmListData[(int)_target.bgmList].audioClip);
        }
        if (GUILayout.Button("Next"))
        {
            _target.bgmList++;
        }
        GUILayout.EndHorizontal();

        //ミキサーの設定
        EditorGUILayout.PropertyField(MixcerProperiy);
        if (MixcerProperiy != null)
        {
            _target.audioMixer = MixcerProperiy.objectReferenceValue as AudioMixer;



            //PrintGroups();
            AudioMixerGroupProperiy = serializedObject.FindProperty("MixerGroupName");
            GUILayoutOption[] options = _target.MixerGroupName.Count > 0 ? new GUILayoutOption[] { } : new GUILayoutOption[] { };
            string[] name = new string[_target.MixerGroupName.Count];
            for (int i = 0; i < _target.MixerGroupName.Count; i++)
            {
                name[i] = _target.MixerGroupName[i];
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("ミキサーのグループ", GUILayout.Width(160));
            selectIndex.intValue = EditorGUILayout.Popup(selectIndex.intValue, name, options);
            GUILayout.EndHorizontal();

            // Debug.Log("選択されたグループ: " + _target.MixerGroupName[selectIndex.intValue]);
        }
        SerializedProperty elementmixer = AllMixerData.GetArrayElementAtIndex(selectIndex.intValue);
        EditorGUILayout.PropertyField(elementmixer.FindPropertyRelative("Volume"), new GUIContent("ミキサーの音量"));

        ////マスターの設定
        //GUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("マスターの音量", GUILayout.Width(160));
        //EditorGUILayout.PropertyField(masterMixcerVolumeProperiy,GUIContent.none);
        //GUILayout.EndHorizontal();


        ////SEの設定
        //GUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("SEの音量", GUILayout.Width(160));
        //EditorGUILayout.PropertyField(seMixcerVolumeProperiy,GUIContent.none);
        //GUILayout.EndHorizontal();


        ////BGMの設定
        //GUILayout.BeginHorizontal();
        //EditorGUILayout.LabelField("BGMの音量", GUILayout.Width(160));
        //EditorGUILayout.PropertyField(bgmMixcerVolumeProperiy,GUIContent.none);
        //GUILayout.EndHorizontal();

        if (Application.isPlaying)
        {
            if (MySoundManeger.AudioMixer != null)
            {
                Debug.Log("セット");
                _target.audioMixer = MixcerProperiy.objectReferenceValue as AudioMixer;
                //_target.audioMixer.SetFloat("Master_Volume", 0);
                //_target.audioMixer.SetFloat("SE_Volume", -30);
                //_target.audioMixer.SetFloat("BGM_Volume", -40);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }



    // エディタ上でのサウンド再生.
    void PlayClip(
        AudioClip clip)
    {
        if (clip == null) return;
        string path = AssetDatabase.GetAssetPath(clip);
        if (!path.Contains(".wav"))
        {
            Debug.Log("wavにしてください");
        }
        var SoundPlayer = new SoundPlayer(AssetDatabase.GetAssetPath(clip));
        SoundPlayer.Load();
        SoundPlayer.Play();
    }


}
#endif