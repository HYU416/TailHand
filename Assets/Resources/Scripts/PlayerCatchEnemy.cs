using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCatchEnemy : MonoBehaviour
{
    public Transform tailEnd;

    [HideInInspector]
    public Transform touchingTarget;

    private Transform caughtTarget;

    private Vector3 prevTailPos;
    private Vector3 tailVelocity;

    void Start()
    {
        prevTailPos = tailEnd.position;
    }

    void Update()
    {
        tailVelocity = (tailEnd.position - prevTailPos) / Time.deltaTime;
        prevTailPos = tailEnd.position;
    }

    public void OnCatch(InputValue value)
    {
        if (value.isPressed)
        {
            CatchTarget();
        }
        else
        {
            ReleaseTarget();
        }
    }

    void CatchTarget()
    {
        if (caughtTarget != null) return;

        if (touchingTarget == null) return;

        Rigidbody rb = touchingTarget.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        Collider[] cols = touchingTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        // 元のスケール保存
        Vector3 originalScale = touchingTarget.localScale;

        touchingTarget.SetParent(tailEnd, false);

        touchingTarget.localPosition = Vector3.zero;
        touchingTarget.localRotation = Quaternion.identity;

        // 親スケール打ち消し
        Vector3 parentScale = tailEnd.lossyScale;

        touchingTarget.localScale = new Vector3(
            originalScale.x / parentScale.x,
            originalScale.y / parentScale.y,
            originalScale.z / parentScale.z);

        caughtTarget = touchingTarget;
        touchingTarget = null;

        Debug.Log("キャッチ！");
    }

    void ReleaseTarget()
    {
        if (caughtTarget == null) return;

        Rigidbody rb = caughtTarget.GetComponent<Rigidbody>();

        caughtTarget.SetParent(null);

        Collider[] cols = caughtTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = tailVelocity;
        }

        Debug.Log("投げた！");

        caughtTarget = null;
    }
}
