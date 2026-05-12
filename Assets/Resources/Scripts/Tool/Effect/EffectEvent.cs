using System;
using UnityEngine;
using UnityEngine.Events;

public enum EffectEventType
{
    Hit,
    Sound,
    CameraShake,
    Function
}

public enum HitColliderType
{
    Sphere,
    Box,
    Capsule
}

public enum CapsuleDirection
{
    X,
    Y,
    Z
}

[Serializable]
public class EffectEvent
{
    public Color previewColor =
Color.red;
    public int frame;
    public EffectEventType type;
    public int hitId;

    public int endFrame;
    public HitColliderType colliderType;
    public Vector3 hitOffset;
    public Vector3 hitBoxSize =
        Vector3.one;
    public float hitRadius = 1f;
    public float capsuleRadius = 0.5f;
    public float capsuleHeight = 2f;
    public CapsuleDirection
    capsuleDirection =
        CapsuleDirection.Y;

    public bool useBGM;
    public SEList se;
    public BGMList bgm;

    public UnityEvent onEvent;

    public float shakePower = 1f;
    public float shakeTime = 0.2f;
    public AnimationCurve shakeCurve;
    public Vector3 shakeAxis =
        Vector3.one;

}