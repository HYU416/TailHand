using System;
using System.Collections.Generic;
using UnityEngine;

//エフェクトの種類を定義する列挙型
public enum EffectType
{
    Explosion = 0,           //爆発
    Explosion2 = 1,          //爆発2
    Explosion_Missile = 2,   //ミサイル爆発
    CoreBreak = 3,           //コア破壊
    DamageZone = 4,          //ダメージゾーン 
    Beam,                    //ビーム

    Hit,                     //ヒット             
    Dash,                    //ダッシュ
    Chatch, //キャッチ
    Hit2 ,         //ヒット２       
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
    //エフェクト再生速度
    public float playSpeed = 1f;

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