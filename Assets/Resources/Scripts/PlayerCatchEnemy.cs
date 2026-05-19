using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCatchEnemy : MonoBehaviour
{
    public Transform tailEnd;

    [Header("投げる強さ")]
    [SerializeField] private float normalThrowMultiplier = 1.0f;

    [Header("不発弾を投げる時の設定")]
    [SerializeField] private float dudBombMassWhileCaught = 0.1f;
    [SerializeField] private float dudBombThrowMultiplier = 3.0f;
    [SerializeField] private float dudBombMassRestoreTime = 1.0f;

    [HideInInspector]
    public Transform touchingTarget;

    private Transform caughtTarget;

    private Vector3 prevTailPos;
    private Vector3 tailVelocity;

    private Rigidbody massChangedRb;
    private float originalMass;
    private bool caughtTargetIsDudBomb;

    void Start()
    {
        if (tailEnd != null)
        {
            prevTailPos = tailEnd.position;
        }
    }

    void Update()
    {
        if (tailEnd == null) return;

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
        if (tailEnd == null) return;

        Rigidbody rb = touchingTarget.GetComponent<Rigidbody>();

        caughtTargetIsDudBomb = IsDudBomb(touchingTarget);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (caughtTargetIsDudBomb)
            {
                massChangedRb = rb;
                originalMass = rb.mass;
                rb.mass = dudBombMassWhileCaught;
            }

            rb.isKinematic = true;
        }

        Collider[] cols = touchingTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        Vector3 originalScale = touchingTarget.localScale;

        touchingTarget.SetParent(tailEnd, false);

        touchingTarget.localPosition = Vector3.zero;
        touchingTarget.localRotation = Quaternion.identity;

        Vector3 parentScale = tailEnd.lossyScale;

        touchingTarget.localScale = new Vector3(
            parentScale.x != 0 ? originalScale.x / parentScale.x : originalScale.x,
            parentScale.y != 0 ? originalScale.y / parentScale.y : originalScale.y,
            parentScale.z != 0 ? originalScale.z / parentScale.z : originalScale.z
        );

        caughtTarget = touchingTarget;
        touchingTarget = null;

        Debug.Log("キャッチ！");
    }

    void ReleaseTarget()
    {
        if (caughtTarget == null) return;

        Transform releasedTarget = caughtTarget;
        Rigidbody rb = releasedTarget.GetComponent<Rigidbody>();

        releasedTarget.SetParent(null);

        Collider[] cols = releasedTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = true;
        }

        if (rb != null)
        {
            rb.isKinematic = false;

            float throwMultiplier = caughtTargetIsDudBomb
                ? dudBombThrowMultiplier
                : normalThrowMultiplier;

            rb.linearVelocity = tailVelocity * throwMultiplier;

            if (caughtTargetIsDudBomb && massChangedRb == rb)
            {
                StartCoroutine(RestoreMassAfterDelay(rb, originalMass, dudBombMassRestoreTime));
            }
        }

        Debug.Log("投げた！");

        caughtTarget = null;
        caughtTargetIsDudBomb = false;
        massChangedRb = null;
    }

    private bool IsDudBomb(Transform target)
    {
        if (target == null) return false;

        string objName = target.name.Replace("(Clone)", "").Trim();

        return objName == "NO BOM";
    }

    private IEnumerator RestoreMassAfterDelay(Rigidbody rb, float mass, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.mass = mass;
        }
    }
}