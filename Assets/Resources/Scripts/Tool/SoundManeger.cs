using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using static SoundManeger;

//SEの種類
public enum SEList
{
   SE_CATCH,
   SE_CRUSH,
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
    [System.Serializable]
    public struct AudioListData
    {
        [SerializeField]
        [HideInInspector]
        public AudioClip audioClip;//オーディオクリップ

        [SerializeField]
        [HideInInspector]
        public AudioMixerGroup audioMixerGroup;//オーディオミキサーグループ

        [SerializeField]
        [HideInInspector]
        public bool bypassEffects;//エフェクトをバイパスするかどうか

        [SerializeField]
        [HideInInspector]
        public bool bypassListenerEffects;//リスナーエフェクトをバイパスするかどうか

        [SerializeField]
        [HideInInspector]
        public bool bypassReverbZones;//リバーブゾーンをバイパスするかどうか

        [SerializeField]
        [HideInInspector]
        public bool isLoop;//ループするかどうか

        [Range(0, 256)]
        [SerializeField]
        [HideInInspector]
        public int priority;//優先度

        [Range(0, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float volume;//音量

        [Range(0, 3.0f)]
        [SerializeField]
        [HideInInspector]
        public float pitch;//ピッチ

        [Range(-1.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float StereoPan;//ステレオパン

        [Range(0, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float spatialBlend;//空間化の度合い

        [Range(0, 1.1f)]
        [SerializeField]
        [HideInInspector]
        public float reverbZoneMix;//リバーブゾーンミックス


        [Range(0.0f, 5.0f)]
        [SerializeField]
        [HideInInspector]
        public float dopplerLevel;//ドップラー効果のレベル

        [Range(0.0f, 360.0f)]
        [SerializeField]
        [HideInInspector]
        public float spread;//スプレッド


        [SerializeField]
        [HideInInspector]
        public VelumeRollOffType velumeRollOffType;//音量の減衰の種類


        [SerializeField]
        [HideInInspector]
        public float minDistance;//最小距離

        [SerializeField]
        [HideInInspector]
        public float maxDistance;//最大距離

        [SerializeField]
        [HideInInspector]
        public AnimationCurve customRolloffCurve;//カスタムロールオフ曲線

    }

    [SerializeField]
    [HideInInspector]
    public SEList seList;

    [SerializeField]
    [HideInInspector]
    public AudioListData[] seListData;

    [SerializeField]
    [HideInInspector]
    public BGMList bgmList;

    [SerializeField]
    [HideInInspector]
    public AudioListData[] bgmListData;

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
        EnsureAudioArraySize(ref seListData, (int)SEList.MAX);
        EnsureAudioArraySize(ref bgmListData, (int)BGMList.MAX);

        MixerGroupName = new List<string>();

        if (audioMixer != null)
        {
            foreach (var group in audioMixer.FindMatchingGroups(""))
            {
                MixerGroupName.Add(group.name);
            }
        }

        MixerGroupName.Add("None");

        if (MixerGroupName.Count != AllMixerData.Count)
        {
            for (int i = AllMixerData.Count; i < MixerGroupName.Count; i++)
            {
                AllMixerData.Add(new MixerData { Volume = 0.0f });
            }

            while (AllMixerData.Count > MixerGroupName.Count)
            {
                AllMixerData.RemoveAt(AllMixerData.Count - 1);
            }
        }
    }

    private AudioListData CreateDefaultAudioData()
    {
        return new AudioListData
        {
            audioClip = null,
            audioMixerGroup = null,
            bypassEffects = false,
            bypassListenerEffects = false,
            bypassReverbZones = false,
            isLoop = false,
            priority = 128,
            volume = 1.0f,
            pitch = 1.0f,
            StereoPan = 0.0f,
            spatialBlend = 0.0f,
            reverbZoneMix = 1.0f,
            dopplerLevel = 1.0f,
            spread = 0.0f,
            velumeRollOffType = VelumeRollOffType.Logarithmic,
            customRolloffCurve = AnimationCurve.Linear(0, 1, 1, 0),
            minDistance = 1.0f,
            maxDistance = 500.0f
           
        };
    }

    private void EnsureAudioArraySize(ref AudioListData[] array, int size)
    {
        if (array == null)
        {
            array = new AudioListData[size];
            for (int i = 0; i < size; i++)
            {
                array[i] = CreateDefaultAudioData();
            }
            return;
        }

        if (array.Length != size)
        {
            AudioListData[] newArr = new AudioListData[size];

            int copyCount = Mathf.Min(array.Length, size);
            for (int i = 0; i < copyCount; i++)
            {
                newArr[i] = array[i];
            }

            for (int i = copyCount; i < size; i++)
            {
                newArr[i] = CreateDefaultAudioData();
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

    private GameObject previewObject;
    private AudioSource previewSource;
    enum PreviewType
    {
        SE,
        BGM
    }
    private bool isSEPlaying;
    private bool isBGMPlaying;
    //表示するプレビューの種類
    PreviewType previewType;

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
        //base.OnInspectorGUI();
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

        //表示するプレビューの種類を切り替える
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("表示するサウンドの種類", GUILayout.Width(160));
        
            previewType = (PreviewType)EditorGUILayout.EnumPopup(previewType);
            GUILayout.EndHorizontal();
        if (previewType == PreviewType.SE)
        {
            EditorGUILayout.BeginVertical("box");
            //SE
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("SEの設定", GUILayout.Width(160));
            //SEの選択
            EditorGUILayout.PropertyField(seListProperiy, GUIContent.none);
            GUILayout.EndHorizontal();

            int index = (int)_target.seList;
            //Debug.Log("インデックス" + index + "a" + seDataProperiy.arraySize);
            if (index >= 0 && index < seDataProperiy.arraySize)
            {
                SerializedProperty element = seDataProperiy.GetArrayElementAtIndex(index);
                EditorGUILayout.PropertyField(element.FindPropertyRelative("audioClip"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("audioMixerGroup"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("bypassEffects"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("bypassListenerEffects"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("bypassReverbZones"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("isLoop"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("priority"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("volume"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("pitch"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("StereoPan"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("spatialBlend"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("reverbZoneMix"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("dopplerLevel"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("spread"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("velumeRollOffType"));
                if ((VelumeRollOffType)element.FindPropertyRelative("velumeRollOffType").enumValueIndex == VelumeRollOffType.Custom)
                {
                    EditorGUILayout.PropertyField(element.FindPropertyRelative("customRolloffCurve"));
                }
                EditorGUILayout.PropertyField(element.FindPropertyRelative("minDistance"));
                EditorGUILayout.PropertyField(element.FindPropertyRelative("maxDistance"));
                
            }
            else
            {
                EditorGUILayout.HelpBox("選択中のSEが範囲外です", MessageType.Warning);
            }

            //音の再生ボタン
            GUILayout.BeginHorizontal();

            //戻るボタン
            if (GUILayout.Button("|\u25C0"))
            {
                PrevSE();
            }

            if (GUILayout.Button(isSEPlaying ? "\u25A0" : "\u25B6"))
            {
                if (isSEPlaying)
                {
                    StopClip();
                }
                else
                {
                    AudioListData data =
                        _target.seListData[(int)_target.seList];

                    PlayClip(
                        data.audioClip,
                        data,
                        PreviewType.SE);
                }
            }
            //次へボタン
            if (GUILayout.Button("\u25B6|"))
            {
                NextSE();
            }

            GUILayout.EndHorizontal();


            GUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginVertical("box");
            //BGM
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("BGMの設定", GUILayout.Width(160));
            //BGMの選択
            EditorGUILayout.PropertyField(bgmListProperiy, GUIContent.none);
            GUILayout.EndHorizontal();

            int index = (int)_target.bgmList;
            //Debug.Log("インデックス" + index + "a" + BGMDataProperiy.arraySize);
            if (index >= 0 && index < BGMDataProperiy.arraySize)
            {
                SerializedProperty element2 = BGMDataProperiy.GetArrayElementAtIndex((int)_target.bgmList);
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("audioClip"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("audioMixerGroup"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("bypassEffects"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("bypassListenerEffects"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("bypassReverbZones"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("isLoop"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("priority"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("volume"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("pitch"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("StereoPan"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("spatialBlend"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("reverbZoneMix"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("dopplerLevel"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("spread"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("velumeRollOffType"));
                if ((VelumeRollOffType)element2.FindPropertyRelative("velumeRollOffType").enumValueIndex == VelumeRollOffType.Custom)
                {
                    EditorGUILayout.PropertyField(element2.FindPropertyRelative("customRolloffCurve"));
                }
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("minDistance"));
                EditorGUILayout.PropertyField(element2.FindPropertyRelative("maxDistance"));
               
            }
            else
            {
                EditorGUILayout.HelpBox("選択中のBGMが範囲外です", MessageType.Warning);
            }

            //音の再生ボタン
            GUILayout.BeginHorizontal();

            //戻るボタン
            if (GUILayout.Button("|\u25C0"))
            {
                PrevBGM();
            }

            if (GUILayout.Button(isBGMPlaying ? "\u25A0" : "\u25B6"))
            {
                if (isBGMPlaying)
                {
                    StopClip();
                }
                else
                {
                    AudioListData data =
                        _target.bgmListData[(int)_target.bgmList];

                    PlayClip(
                        data.audioClip,
                        data,
                        PreviewType.BGM);
                }
            }

            if (GUILayout.Button("\u25B6|"))
            {
                NextBGM();
            }
            GUILayout.EndHorizontal();

           

            GUILayout.EndVertical();

        }

        EditorGUILayout.BeginVertical("box");

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

        GUILayout.EndVertical();

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
    void PlayClip(AudioClip clip, AudioListData data, PreviewType type)
    {
        if (clip == null)
            return;

        if (previewObject == null)
        {
            previewObject = new GameObject("PreviewAudio");
            previewObject.hideFlags = HideFlags.HideAndDontSave;

            previewSource = previewObject.AddComponent<AudioSource>();
        }

        previewSource.Stop();


        previewSource.clip = clip;
        previewSource.priority = data.priority;
        previewSource.volume = data.volume;
        previewSource.pitch = data.pitch;
        previewSource.panStereo = data.StereoPan;
        previewSource.spatialBlend = data.spatialBlend;
        previewSource.reverbZoneMix = data.reverbZoneMix;
        previewSource.loop = data.isLoop;
        previewSource.outputAudioMixerGroup = data.audioMixerGroup;
        previewSource.bypassEffects = data.bypassEffects;
        previewSource.bypassListenerEffects = data.bypassListenerEffects;
        previewSource.bypassReverbZones = data.bypassReverbZones;
        previewSource.dopplerLevel = data.dopplerLevel;
        previewSource.spread = data.spread;
        previewSource.minDistance = data.minDistance;
        previewSource.maxDistance = data.maxDistance;

        // Rolloff
        switch (data.velumeRollOffType)
        {
            case VelumeRollOffType.Logarithmic:
                previewSource.rolloffMode = AudioRolloffMode.Logarithmic;
                break;

            case VelumeRollOffType.Linear:
                previewSource.rolloffMode = AudioRolloffMode.Linear;
                break;

            case VelumeRollOffType.Custom:
                previewSource.rolloffMode = AudioRolloffMode.Custom;
                if (data.customRolloffCurve != null)
                {
                    previewSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, data.customRolloffCurve);
                }
                break;
        }
        //再生
        previewSource.Play();

        isSEPlaying = (type == PreviewType.SE);
        isBGMPlaying = (type == PreviewType.BGM);

    }


    void StopClip()
    {
        if (previewSource != null)
        {
            previewSource.Stop();
        }

        isSEPlaying = false;
        isBGMPlaying = false;
    }

    // SEを次に移動する関数
    void PrevSE()
    {
        int count = (int)SEList.MAX;

        int index = (int)_target.seList - 1;

        if (index < 0)
            index = count - 1;

        _target.seList = (SEList)index;
    }

    void PrevBGM()
    {
       int count = (int)BGMList.MAX;
        int index = (int)_target.bgmList - 1;
        if (index < 0)
            index = count - 1;
        _target.bgmList = (BGMList)index;
    }

    // SEを前に移動する関数
    void NextSE()
    {
        int count = (int)SEList.MAX;

        int index = ((int)_target.seList + 1) % count;

        _target.seList = (SEList)index;
    }

    void NextBGM()
    {
        int count = (int)BGMList.MAX;
        int index = ((int)_target.bgmList + 1) % count;
        _target.bgmList = (BGMList)index;
    }




}
#endif