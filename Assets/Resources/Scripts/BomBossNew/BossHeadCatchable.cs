using UnityEngine;

public class BossHeadCatchable : MonoBehaviour
{
    [Header("‚±‚Ì“ª‚ğ’Í‚ß‚é‚©")]
    [SerializeField] private bool canCatch;

    [Header("’Í‚ñ‚¾‚ÌˆÊ’u•â³")]
    [SerializeField] private Vector3 catchLocalPositionOffset = Vector3.zero;

    [Header("’Í‚ñ‚¾‚Ì‰ñ“]•â³")]
    [SerializeField] private Vector3 catchLocalRotationOffset = Vector3.zero;

    public bool CanCatch
    {
        get { return canCatch; }
    }

    public Vector3 CatchLocalPositionOffset
    {
        get { return catchLocalPositionOffset; }
    }

    public Vector3 CatchLocalRotationOffset
    {
        get { return catchLocalRotationOffset; }
    }

    public void SetCanCatch(bool value)
    {
        canCatch = value;
        Debug.Log("BossHeadCatchable: ’Í‚ß‚éó‘Ô = " + canCatch);
    }
}