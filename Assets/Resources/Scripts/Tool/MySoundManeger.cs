using UnityEngine;
using UnityEngine.Audio;

public static class MySoundManeger
{
    public static SoundManeger SM;
    public static AudioMixer AudioMixer;
    private static AudioMixer Mixer;

    // ボリュームのデシベル範囲。-60dBが最小、0dBが最大とする。これを超える値はクランプされる。
    private const float DB_MIN = -60f;
    private const float DB_MAX = 0f;
    // PlayerPrefsに保存する際のキーのプレフィックス。これに続けてMixerのパラメータ名が付加される。
    private const string PREF_PREFIX = "vol_";

    private static bool initialized;

    // 初期化。SoundManegerがResourcesからロードされ、AudioMixerがセットされる。失敗した場合はエラーログを出力。
    private static void Initialize()
    {
        if (initialized)
            return;
        // ResourcesからSoundManegerをロード
        SM = Resources.Load<SoundManeger>("Data/SoundData/SoundManager");

        if (SM == null)
        {
            Debug.LogError("SoundManager not found");
            return;
        }

        Mixer = SM.audioMixer;
        AudioMixer = Mixer;   
        initialized = true;
        // すでにSoundManegerがロードされている場合は、Mixerに保存されたボリュームを適用
        LoadVolumes();
    }

    // SoundManegerをセット。通常はSoundManegerのAwakeで呼び出される想定
    public static void SetSoundManeger(SoundManeger soundManeger)
    {
        SM = soundManeger;
        Mixer = SM.audioMixer;
        AudioMixer = Mixer;  

        if (SM == null || Mixer == null)
            return;

        // SoundManegerのMixerGroupNameとAllMixerDataをループして、Mixerにボリュームを適用
        for (int i = 0; i < SM.MixerGroupName.Count; i++)
        {
            string param = SM.MixerGroupName[i] + "_Volume";
            float db = Mathf.Clamp(SM.AllMixerData[i].Volume, DB_MIN, DB_MAX);
            Mixer.SetFloat(param, db);
        }

        LoadVolumes();
    }

    // Mixerにボリュームを設定し、PlayerPrefsにも保存
    public static void SetVolume(string name, float volume)
    {
        Initialize();

        if (Mixer == null)
            return;

        float db = Mathf.Clamp(volume, DB_MIN, DB_MAX);
        Mixer.SetFloat(name, db);

        PlayerPrefs.SetFloat(PREF_PREFIX + name, db);
        // PlayerPrefs.Save();
    }

    // Mixerからボリュームを取得。失敗した場合はPlayerPrefsから取得。どちらも失敗した場合は-999fを返す。
    public static float GetVolume(string name)
    {
        Initialize();

        if (Mixer != null && Mixer.GetFloat(name, out float volume))
        {
            return volume;
        }

        if (PlayerPrefs.HasKey(PREF_PREFIX + name))
        {
            return PlayerPrefs.GetFloat(PREF_PREFIX + name);
        }

        return -999f;
    }

    // 保存されたボリュームをロードしてMixerに適用
    public static void LoadVolumes()
    {
        if (SM == null || Mixer == null)
            return;

        for (int i = 0; i < SM.MixerGroupName.Count; i++)
        {
            string param = SM.MixerGroupName[i] + "_Volume";
            string key = PREF_PREFIX + param;

            if (PlayerPrefs.HasKey(key))
            {
                float db = Mathf.Clamp(PlayerPrefs.GetFloat(key), DB_MIN, DB_MAX);
                Mixer.SetFloat(param, db);
            }
        }
    }

    //SE再生
    public static void Play(GameObject obj, SEList soundList)
    {
        Initialize();

        if (SM == null || obj == null)
            return;

        AudioSource source = obj.GetComponent<AudioSource>();
        if (source == null)
            source = obj.AddComponent<AudioSource>();

        SoundManeger.AudioListData data = SM.seListData[(int)soundList];

        if (data.audioClip == null)
        {
            Debug.LogWarning($"SE {soundList} の AudioClip が設定されていません");
            return;
        }

        CopyAudioSourceSettings(source, data);
        ApplyAudioFilters(obj, data);
        source.Play();
    }

    //BGM再生
    public static void Play(GameObject obj, BGMList soundList)
    {
        Initialize();

        if (SM == null || obj == null)
            return;

        AudioSource source = obj.GetComponent<AudioSource>();
        if (source == null)
            source = obj.AddComponent<AudioSource>();

        SoundManeger.AudioListData data = SM.bgmListData[(int)soundList];


        if (data.audioClip == null)
        {
            Debug.LogWarning($"BGM {soundList} の AudioClip が設定されていません");
            return;
        }


        CopyAudioSourceSettings(source, data);
        ApplyAudioFilters(obj, data);
        source.Play();
    }

    //停止
    public static void Stop(GameObject obj)
    {
        if (obj == null)
            return;

        AudioSource source = obj.GetComponent<AudioSource>();
        if (source != null)
            source.Stop();
    }

    //AudioSourceの設定をSoundManeger.AudioListDataからコピー
    private static void CopyAudioSourceSettings(AudioSource source, SoundManeger.AudioListData data)
    {

        if (source == null)
            return;

        source.clip = data.audioClip;
        source.outputAudioMixerGroup = data.audioMixerGroup;
        source.bypassEffects = data.bypassEffects;
        source.bypassListenerEffects = data.bypassListenerEffects;
        source.bypassReverbZones = data.bypassReverbZones;
        source.loop = data.isLoop;
        source.priority = data.priority;
        source.volume = data.volume;
        source.pitch = data.pitch;
        source.panStereo = data.StereoPan;
        source.spatialBlend = data.spatialBlend;
        source.reverbZoneMix = data.reverbZoneMix;
        source.dopplerLevel = data.dopplerLevel;
        source.spread = data.spread;
        source.minDistance = data.minDistance;
        source.maxDistance = data.maxDistance;

        // ボリュームのロールオフタイプに応じてAudioSourceのrolloffModeを設定。Customの場合はカスタムカーブも設定
        switch (data.velumeRollOffType)
        {
            case VelumeRollOffType.Logarithmic:
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                break;

            case VelumeRollOffType.Linear:
                source.rolloffMode = AudioRolloffMode.Linear;
                break;

            case VelumeRollOffType.Custom:
                source.rolloffMode = AudioRolloffMode.Custom;
                if (data.customRolloffCurve != null)
                {
                    source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, data.customRolloffCurve);
                }
                break;
        }
    }

    private static void ApplyAudioFilters(GameObject obj, SoundManeger.AudioListData data)
    {
        if (obj == null)
            return;

        SoundManeger.AudioFilterData filter = data.audioFilterData;

        // Chorus
        AudioChorusFilter chorus = GetOrAddComponent<AudioChorusFilter>(obj);
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
        else
        {
            chorus.enabled = false;
        }

        // Distortion
        AudioDistortionFilter distortion = GetOrAddComponent<AudioDistortionFilter>(obj);
        if (filter.useDistortion)
        {
            distortion.distortionLevel = filter.distortionLevel;
            distortion.enabled = true;
        }
        else
        {
            distortion.enabled = false;
        }

        // Echo
        AudioEchoFilter echo = GetOrAddComponent<AudioEchoFilter>(obj);
        if (filter.useEcho)
        {
            echo.delay = filter.echoDelay;
            echo.decayRatio = filter.echoDecayRatio;
            echo.dryMix = filter.echoDryMix;
            echo.wetMix = filter.echoWetMix;
            echo.enabled = true;
        }
        else
        {
            echo.enabled = false;
        }

        // HighPass
        AudioHighPassFilter highPass = GetOrAddComponent<AudioHighPassFilter>(obj);
        if (filter.useHighPass)
        {
            highPass.cutoffFrequency = filter.highPassCutoffFrequency;
            highPass.highpassResonanceQ = filter.highPassResonanceQ;
            highPass.enabled = true;
        }
        else
        {
            highPass.enabled = false;
        }

        // LowPass
        AudioLowPassFilter lowPass = GetOrAddComponent<AudioLowPassFilter>(obj);
        if (filter.useLowPass)
        {
            lowPass.cutoffFrequency = filter.lowPassCutoffFrequency;
            lowPass.lowpassResonanceQ = filter.lowPassResonanceQ;
            lowPass.enabled = true;
        }
        else
        {
            lowPass.enabled = false;
        }
    }

    private static T GetOrAddComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null)
        {
            comp = obj.AddComponent<T>();
        }
        return comp;
    }

  
}
