// EffectDatabase.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public enum EffectType
{
    Explosion,
    Slash,
    Fire
}

[Serializable]
public class EffectData
{
    public EffectType type;

    public GameObject prefab;
}

[CreateAssetMenu(
    fileName = "EffectDatabase",
    menuName = "Game/EffectDatabase"
)]
public class EffectDatabase :
    ScriptableObject
{
    public List<EffectData> effects =
        new();
}