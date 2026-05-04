using UnityEngine;
using UnityEditor;
using Unity.VisualScripting;
using UnityEngine.Audio;
using UnityEngine.Rendering;

public static class MySoundManeger
{
    public static SoundManeger SM;
    public static AudioMixer AudioMixer;
    private static AudioMixer Mixer;

    // ★ 追加：dBレンジ定数 & PlayerPrefsキー
    private const float DB_MIN = -60f;
    private const float DB_MAX = 0f;
    private const string PREF_PREFIX = "vol_"; // 例: "vol_Master_Volume"

    public static void SetSoundManeger(SoundManeger soundManeger)
    {
        SM = soundManeger;
        Mixer = SM.audioMixer;

        // ★ まずは ScriptableObject の値を適用（-60?0にクランプ）
        for (int i = 0; i < SM.MixerGroupName.Count; i++)
        {
            string param = SM.MixerGroupName[i] + "_Volume"; // 例: "Master_Volume"
            float db = Mathf.Clamp(SM.AllMixerData[i].Volume, DB_MIN, DB_MAX);
            Mixer.SetFloat(param, db);
        }

        // ★ 次に、保存されていれば復元で上書き
        LoadVolumes();
    }

    public static void SetVolume(string name, float volume)
    {
        // ★ -60?0dB にクランプ
        float db = Mathf.Clamp(volume, DB_MIN, DB_MAX);
        Mixer.SetFloat(name, db);

        // ★ 保存
        PlayerPrefs.SetFloat(PREF_PREFIX + name, db);
        // 頻繁に呼ぶなら Save は省略可。確実に残したいなら以下を有効化:
        // PlayerPrefs.Save();
    }

    public static float GetVolume(string name)
    {
        float volume;
        if (Mixer.GetFloat(name, out volume))
        {
            return volume; // dB (-60?0)
        }

        // ★ Mixer未準備でも保存値があれば返す
        if (PlayerPrefs.HasKey(PREF_PREFIX + name))
        {
            return PlayerPrefs.GetFloat(PREF_PREFIX + name);
        }
        return -999f; // 取得できなかったとき
    }

    // ★ 追加：保存値の一括復元
    public static void LoadVolumes()
    {
        if (SM == null || Mixer == null) return;

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


    public static void Play(GameObject obj, SEList soundList)
    {
        AudioSource source = obj.GetComponent<AudioSource>();
        if (source == null) source = obj.AddComponent<AudioSource>();
        Debug.Log(SM.seListData[(int)soundList].audioClip + " " + SM.seListData[(int)soundList].volume + " " + SM.seListData[(int)soundList].pitch + " " + SM.seListData[(int)soundList].isLoop + " " + SM.seListData[(int)soundList].audioMixerGroup);
        if (source != null)
        {
            //Debug.Log(source+ ""+ SEaudioClip[(int)soundList] + "" + SEvolume[(int)soundList] + "" + SEPitch[(int)soundList] + "" + SEisLoop[(int)soundList] + "" + SEaudioMixerGroup[(int)soundList]);
            CopyAudioSourceSettings(source, SM.seListData[(int)soundList].audioClip, SM.seListData[(int)soundList].volume, SM.seListData[(int)soundList].pitch, SM.seListData[(int)soundList].isLoop, SM.seListData[(int)soundList].audioMixerGroup);

            source.Play();
        }
    }
    public static void Play(GameObject obj, BGMList soundList)
    {
        AudioSource source = obj.GetComponent<AudioSource>();
        if (source == null) source = obj.AddComponent<AudioSource>();
        if (source != null)
        {

            CopyAudioSourceSettings(source, SM.bgmListData[(int)soundList].audioClip, SM.bgmListData[(int)soundList].volume, SM.bgmListData[(int)soundList].pitch, SM.bgmListData[(int)soundList].isLoop, SM.bgmListData[(int)soundList].audioMixerGroup);

            source.Play();
        }
    }

    public static void Stop(GameObject obj)
    {
        AudioSource source = obj.GetComponent<AudioSource>();
        if (source != null) source.Stop();
    }

    private static void CopyAudioSourceSettings(AudioSource source, AudioClip clip, float volume, float Pitch, bool loop, AudioMixerGroup Group)
    {

        source.clip = clip;
        source.outputAudioMixerGroup = Group;
        source.loop = loop;
        source.volume = volume;
        source.pitch = Pitch;

    }
    private static void CopyAudioMixerSettings(AudioMixer mixer, float volume, float Pitch, bool loop, AudioMixerGroup Group)
    {


    }

}