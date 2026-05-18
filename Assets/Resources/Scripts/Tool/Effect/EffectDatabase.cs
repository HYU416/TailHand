using System;
using System.Collections.Generic;
using UnityEngine;

//エフェクトの種類を定義する列挙型
public enum EffectType
{
    Explosion,          //爆発
    Slash,              //斬撃
    Fire                //炎
}

[Serializable]
public class EffectData
{
    //エフェクトの種類
    public EffectType type;
    //エフェクトのプレハブ
    public GameObject prefab;
    //エフェクトのタグ
    [TagSelector]
    public string targetTag;
}

//エフェクトデータベースを管理するScriptableObjectクラス
[CreateAssetMenu(
    fileName = "EffectDatabase",
    menuName = "Game/EffectDatabase"
)]
public class EffectDatabase :
    ScriptableObject
{
    //エフェクトデータのリスト
    public List<EffectData> effects =new();
}