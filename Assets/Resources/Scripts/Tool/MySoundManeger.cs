using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public static class MySoundManeger
{
    private class PlayingAudio
    {
        public AudioSource Source;
        public bool IsBGM;
    }

    public static SoundManeger SM;
    public static AudioMixer AudioMixer;
    private static AudioMixer Mixer;

    // ボリュームのデシベル範囲。-60dBが最小、0dBが最大とする。これを超える値はクランプされる。
    private const float DB_MIN = -60f;
    private const float DB_MAX = 0f;
    // PlayerPrefsに保存する際のキーのプレフィックス。これに続けてMixerのパラメータ名が付加される。
    private const string PREF_PREFIX = "vol_";

    private static bool initialized;
    private static Dictionary<GameObject, List<PlayingAudio>> playingSources  = new Dictionary<GameObject, List<PlayingAudio>>();

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

    //シーンロード時に初期化
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnAfterSceneLoad()
    {
        Cleanup();
    }

    private static void Cleanup()
    {
        var removeKeys = new List<GameObject>();

        foreach (var pair in playingSources)
        {
            if (pair.Key == null)
            {
                removeKeys.Add(pair.Key);
                continue;
            }

            pair.Value.RemoveAll(audio =>
     audio == null ||
     audio.Source == null ||
     (!audio.IsBGM && !audio.Source.isPlaying));
        }

        foreach (var key in removeKeys)
        {
            playingSources.Remove(key);
        }
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
    public static AudioSource Play(GameObject parent, SEList soundList)
    {
        Initialize();

        if (SM == null || parent == null)
            return null;

        SoundManeger.AudioListData data =
            SM.seListData[(int)soundList];

        if (data.audioClip == null)
        {
            Debug.LogWarning($"SE {soundList} の AudioClip が設定されていません");
            return null;
        }

        GameObject audioObj =
            new GameObject($"Audio_{soundList}");

        audioObj.transform.SetParent(parent.transform);
        audioObj.transform.localPosition = Vector3.zero;

        AudioSource source = audioObj.AddComponent<AudioSource>();
        RegisterAudio(parent, source, false);

        CopyAudioSourceSettings(source, data);
        ApplyAudioFilters(audioObj, data);

        source.Play();

        if (!data.isLoop)
        {
            Object.Destroy(audioObj,data.audioClip.length + 0.5f);
        }

        return source;
    }

    //BGM再生
    public static AudioSource Play(GameObject obj, BGMList soundList)
    {
        Initialize();

        if (SM == null || obj == null)
            return null;

        GameObject audioObj = new GameObject($"BGM_{soundList}");

        audioObj.transform.SetParent(obj.transform);
        audioObj.transform.localPosition = Vector3.zero;

        AudioSource source = audioObj.AddComponent<AudioSource>();

        RegisterAudio(obj, source, true);

        SoundManeger.AudioListData data = SM.bgmListData[(int)soundList];


        if (data.audioClip == null)
        {
            Debug.LogWarning($"BGM {soundList} の AudioClip が設定されていません");
            return null;
        }


        CopyAudioSourceSettings(source, data);
        ApplyAudioFilters(audioObj, data);
        source.Play();

        return source;
    }

    //全停止
    public static void Stop(GameObject obj)
    {
        if (!playingSources.TryGetValue(obj, out var sources))
            return;

        foreach (var audio in sources)
        {
            if (audio?.Source != null)
            {
                audio.Source.Stop();
                Object.Destroy(audio.Source.gameObject);
            }
        }

        sources.Clear();
    }

    //特定のSEを停止
    public static void Stop(GameObject obj, SEList soundList)
    {
        if (!playingSources.TryGetValue(obj, out var sources))
            return;
        SoundManeger.AudioListData data = SM.seListData[(int)soundList];
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PlayingAudio audio = sources[i];
            if (audio?.Source != null && !audio.IsBGM && audio.Source.clip == data.audioClip)
            {
                audio.Source.Stop();
                Object.Destroy(audio.Source.gameObject);
                sources.RemoveAt(i);
            }
        }
    }

    //特定のBGMを停止
    public static void Stop(GameObject obj, BGMList soundList)
    {
        if (!playingSources.TryGetValue(obj, out var sources))
            return;

        SoundManeger.AudioListData data =
            SM.bgmListData[(int)soundList];

        for (int i = sources.Count - 1; i >= 0; i--)
        {
            PlayingAudio audio = sources[i];

            if (audio?.Source != null &&
                audio.IsBGM &&
                audio.Source.clip == data.audioClip)
            {
                audio.Source.Stop();
                Object.Destroy(audio.Source.gameObject);
                sources.RemoveAt(i);
            }
        }
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

    private static void RegisterAudio(GameObject owner, AudioSource source,bool isBGM)
    {
        if (!playingSources.ContainsKey(owner))
        {
            playingSources[owner] = new List<PlayingAudio>();
        }

        playingSources[owner].Add(
            new PlayingAudio
            {
                Source = source,
                IsBGM = isBGM
            });
    }
}
