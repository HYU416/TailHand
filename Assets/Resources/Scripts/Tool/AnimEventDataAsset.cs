using UnityEngine;
using System.Collections.Generic;

public enum AnimEventType
{
   Attack,
   SE,
   Effect,
}

[System.Serializable]
public class AnimEventData
{
    public int frame;
    public AnimEventType Type;
    public SEList list;
    //TODO:エフェクトのリストも必要かも
    //public EffectList effectList;
    //public string Param;
}

[CreateAssetMenu(
    fileName = "NewAnimEventData",
    menuName = "Game/Animation Event Data"
    )]
public class AnimEventDataAsset :ScriptableObject
{
    [Header("アニメーション")]
    public AnimationClip clip;
    [Header("1秒単位のフレーム数")]
    public int frameRate = 60;
    [Header("イベント")]
    public List<AnimEventData> events = new List<AnimEventData>();
}
