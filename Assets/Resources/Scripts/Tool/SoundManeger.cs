using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.Audio;
using System;
using System.Collections.Generic;
using static SoundManeger;

//SEの種類
public enum SEList
{
   SE_CATCH,
   SE_CRUSH,
   SE_Missile,
   SE_GameChange1,
   SE_GameChange2,
   MAX,
}

//BGMの種類
public enum BGMList
{
    BGM_GAME,
    BGM_GAME_LOOP,
    BGM_TITLE,
    BGM_TITLE_LOOP,
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

        [SerializeField]
        [HideInInspector]
        public AudioFilterData audioFilterData;//オーディオフィルタの設定

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

    //オーディオフィルタの設定
    [System.Serializable]
    public struct AudioFilterData
    {
        // Chorus
        [SerializeField]
        [HideInInspector]
        public bool useChorus;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusDryMix;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusWetMix1;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusWetMix2;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusWetMix3;

        [Range(0.1f, 100.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusDelay;

        [Range(0.0f, 20.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusRate;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float chorusDepth;

        // Distortion
        [SerializeField]
        [HideInInspector]
        public bool useDistortion;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float distortionLevel;

        // Echo
        [SerializeField]
        [HideInInspector]
        public bool useEcho;

        [Range(10.0f, 5000.0f)]
        [SerializeField]
        [HideInInspector]
        public float echoDelay;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float echoDecayRatio;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float echoDryMix;

        [Range(0.0f, 1.0f)]
        [SerializeField]
        [HideInInspector]
        public float echoWetMix;

        // HighPass
        [SerializeField]
        [HideInInspector]
        public bool useHighPass;

        [Range(10.0f, 22000.0f)]
        [SerializeField]
        [HideInInspector]
        public float highPassCutoffFrequency;

        [Range(1.0f, 10.0f)]
        [SerializeField]
        [HideInInspector]
        public float highPassResonanceQ;

        // LowPass
        [SerializeField]
        [HideInInspector]
        public bool useLowPass;

        [Range(10.0f, 22000.0f)]
        [SerializeField]
        [HideInInspector]
        public float lowPassCutoffFrequency;

        [Range(1.0f, 10.0f)]
        [SerializeField]
        [HideInInspector]
        public float lowPassResonanceQ;
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
            maxDistance = 500.0f,
                audioFilterData = new AudioFilterData
                {
                    useChorus = false,
                    chorusDryMix = 0.5f,
                    chorusWetMix1 = 0.5f,
                    chorusWetMix2 = 0.5f,
                    chorusWetMix3 = 0.5f,
                    chorusDelay = 40.0f,
                    chorusRate = 1.0f,
                    chorusDepth = 0.5f,
    
                    useDistortion = false,
                    distortionLevel = 0.5f,
    
                    useEcho = false,
                    echoDelay = 500.0f,
                    echoDecayRatio = 0.5f,
                    echoDryMix = 0.5f,
                    echoWetMix = 0.5f,
    
                    useHighPass = false,
                    highPassCutoffFrequency = 5000.0f,
                    highPassResonanceQ = 1.0f,
    
                    useLowPass = false,
                    lowPassCutoffFrequency = 5000.0f,
                    lowPassResonanceQ = 1.0f
                }

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
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
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

    void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorSceneManager.sceneOpened -= OnSceneOpened;

        DestroyPreviewObject();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        DestroyPreviewObject();
    }

    private void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        DestroyPreviewObject();
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
                DrawAudioFilterProperties(element);
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

                    serializedObject.ApplyModifiedProperties();   
                    serializedObject.Update();                   

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

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.EndHorizontal();

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
                SerializedProperty element = BGMDataProperiy.GetArrayElementAtIndex((int)_target.bgmList);
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

                DrawAudioFilterProperties(element);
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
                    serializedObject.ApplyModifiedProperties();
                    serializedObject.Update();

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

            EditorGUILayout.BeginHorizontal();

           
            EditorGUILayout.EndHorizontal();

            GUILayout.EndVertical();

        }

        EditorGUILayout.BeginVertical("box");

        //ミキサーの設定
        EditorGUILayout.PropertyField(MixcerProperiy);
        if (MixcerProperiy.objectReferenceValue != null)
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

    private void DrawAudioFilterProperties(SerializedProperty element)
    {
        SerializedProperty filter = element.FindPropertyRelative("audioFilterData");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Audio Filter", EditorStyles.boldLabel);

        // Chorus
        EditorGUILayout.PropertyField(filter.FindPropertyRelative("useChorus"));
        if (filter.FindPropertyRelative("useChorus").boolValue)
        {
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusDryMix"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusWetMix1"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusWetMix2"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusWetMix3"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusDelay"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusRate"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("chorusDepth"));
        }

        // Distortion
        EditorGUILayout.PropertyField(filter.FindPropertyRelative("useDistortion"));
        if (filter.FindPropertyRelative("useDistortion").boolValue)
        {
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("distortionLevel"));
        }

        // Echo
        EditorGUILayout.PropertyField(filter.FindPropertyRelative("useEcho"));
        if (filter.FindPropertyRelative("useEcho").boolValue)
        {
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("echoDelay"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("echoDecayRatio"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("echoDryMix"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("echoWetMix"));
        }

        // HighPass
        EditorGUILayout.PropertyField(filter.FindPropertyRelative("useHighPass"));
        if (filter.FindPropertyRelative("useHighPass").boolValue)
        {
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("highPassCutoffFrequency"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("highPassResonanceQ"));
        }

        // LowPass
        EditorGUILayout.PropertyField(filter.FindPropertyRelative("useLowPass"));
        if (filter.FindPropertyRelative("useLowPass").boolValue)
        {
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("lowPassCutoffFrequency"));
            EditorGUILayout.PropertyField(filter.FindPropertyRelative("lowPassResonanceQ"));
        }
    }


    // エディタ上でのサウンド再生
    void PlayClip(AudioClip clip, AudioListData data, PreviewType type)
    {
        if (clip == null)
            return;

        // 毎回に作り直す
        DestroyPreviewObject();

        previewObject = new GameObject("PreviewAudio");
        // シーン上に表示しないようにする
        previewObject.hideFlags = HideFlags.HideAndDontSave;
        // シーン上に表示する場合は以下の行をコメントを外す
        //previewObject.hideFlags = HideFlags.None;
        previewSource = previewObject.AddComponent<AudioSource>();

        // AudioSource 設定
        previewSource.clip = clip;
        previewSource.priority = data.priority;
        previewSource.volume = data.volume;
        previewSource.pitch = data.pitch;
        previewSource.panStereo = data.StereoPan;

        previewSource.spatialBlend = data.spatialBlend;

        previewSource.reverbZoneMix = data.reverbZoneMix;
        previewSource.loop = data.isLoop;

        previewSource.outputAudioMixerGroup = data.audioMixerGroup;

        previewSource.bypassEffects = false;
        previewSource.bypassListenerEffects = true;
        previewSource.bypassReverbZones = true;

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

         ApplyPreviewAudioFilters(data);

       

        previewSource.Play();

        isSEPlaying = (type == PreviewType.SE);
        isBGMPlaying = (type == PreviewType.BGM);
    }




    void StopClip()
    {
        DestroyPreviewObject();
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

    private T GetOrAddPreviewComponent<T>() where T : Component
    {
        T comp = previewObject.GetComponent<T>();
        if (comp == null)
        {
            comp = previewObject.AddComponent<T>();
        }
        return comp;
    }

    private void DestroyPreviewObject()
    {
        if (previewSource != null)
        {
            previewSource.Stop();
        }

        if (previewObject != null)
        {
            DestroyImmediate(previewObject);
            previewObject = null;
            previewSource = null;
        }

        isSEPlaying = false;
        isBGMPlaying = false;
    }

    private void ApplyPreviewAudioFilters(AudioListData data)
    {
        AudioFilterData filter = data.audioFilterData;

        // 全部取得
        AudioChorusFilter chorus = GetOrAddPreviewComponent<AudioChorusFilter>();
        AudioDistortionFilter distortion = GetOrAddPreviewComponent<AudioDistortionFilter>();
        AudioEchoFilter echo = GetOrAddPreviewComponent<AudioEchoFilter>();
        AudioHighPassFilter highPass = GetOrAddPreviewComponent<AudioHighPassFilter>();
        AudioLowPassFilter lowPass = GetOrAddPreviewComponent<AudioLowPassFilter>();

        // 全部無効化
        chorus.enabled = false;
        distortion.enabled = false;
        echo.enabled = false;
        highPass.enabled = false;
        lowPass.enabled = false;

        // Chorus
        if (filter.useChorus)
        {
            chorus.dryMix = filter.chorusDryMix;
            chorus.wetMix1 = filter.chorusWetMix1;
            chorus.wetMix2 = filter.chorusWetMix2;
            chorus.wetMix3 = filter.chorusWetMix3;
            chorus.delay = filter.chorusDelay;
            chorus.rate = filter.chorusRate;
            chorus.depth = filter.chorusDepth;
            chorus.enabled = true;
        }

        // Distortion
        if (filter.useDistortion)
        {
            distortion.distortionLevel = filter.distortionLevel;
            distortion.enabled = true;
        }

        // Echo
        if (filter.useEcho)
        {
            echo.delay = filter.echoDelay;
            echo.decayRatio = filter.echoDecayRatio;
            echo.dryMix = filter.echoDryMix;
            echo.wetMix = filter.echoWetMix;
            echo.enabled = true;
        }

        // HighPass
        if (filter.useHighPass)
        {
            highPass.cutoffFrequency = filter.highPassCutoffFrequency;
            highPass.highpassResonanceQ = filter.highPassResonanceQ;
            highPass.enabled = true;
        }

        // LowPass
        if (filter.useLowPass)
        {
            lowPass.cutoffFrequency = filter.lowPassCutoffFrequency;
            lowPass.lowpassResonanceQ = filter.lowPassResonanceQ;
            lowPass.enabled = true;
        }
    }

    
}
#endif