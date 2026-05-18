using System;
using UnityEngine;
using UnityEngine.Events;

//エフェクトイベントの種類
public enum EffectEventType
{
    Hit,
    Sound,
    CameraShake,
    Function
}
//当たり判定のコライダーの種類
public enum HitColliderType
{
    Sphere,
    Box,
    Capsule
}
//カプセルコライダーの向き
public enum CapsuleDirection
{
    X,
    Y,
    Z
}

[Serializable]
public class EffectEvent
{
    //当たり判定イベントの場合の対象タグ
    public string hitTag;
    //エディタ上でのプレビュー用の色
    public Color previewColor =Color.red;
    //イベントが発生するフレーム
    public int frame;
    //イベントの種類
    public EffectEventType type;
    //当たり判定イベントの場合の識別ID（複数の当たり判定を区別するため）
    public int hitId;
    //当たり判定イベントの終了フレーム
    public int endFrame;
    //当たり判定の種類
    public HitColliderType colliderType;
    //当たり判定のオフセット位置
    public Vector3 hitOffset;
    //当たり判定のサイズ
    public Vector3 hitBoxSize = Vector3.one;
    //スフィアコライダーの半径
    public float hitRadius = 1f;
    //当たり判定のカプセルコライダー半径
    public float capsuleRadius = 0.5f;
    //当たり判定のカプセルコライダーの高さ
    public float capsuleHeight = 2f;
    //当たり判定のカプセルコライダーの向き
    public CapsuleDirection capsuleDirection = CapsuleDirection.Y;

    //BGMかSE、どっちを再生するか
    public bool useBGM;
    //再生するSEのタイプ
    public SEList se;
    //再生するBGMのタイプ
    public BGMList bgm;

    //関数イベントで呼び出すUnityEvent
    public UnityEvent onEvent;

    //カメラシェイクイベントのパラメータ
    //シェイクの強さ
    public float shakePower = 1f;
    //シェイクの持続時間
    public float shakeTime = 0.2f;
    //シェイクの強さを時間に応じて変化させるためのアニメーションカーブ
    public AnimationCurve shakeCurve;
    //シェイクの方向を制限するための軸（例：Vector3(1, 0, 1)はXとZ軸のみでシェイク）
    public Vector3 shakeAxis =Vector3.one;

}