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

    private DudBomb caughtDudBomb;
    private Missile caughtMissile;

    private void Start()
    {
        if (tailEnd != null)
        {
            prevTailPos = tailEnd.position;
        }
    }

    private void Update()
    {
        if (tailEnd == null)
        {
            return;
        }

        if (Time.deltaTime > 0.0f)
        {
            tailVelocity = (tailEnd.position - prevTailPos) / Time.deltaTime;
        }

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

    private void CatchTarget()
    {
        if (caughtTarget != null)
        {
            return;
        }

        if (touchingTarget == null)
        {
            return;
        }

        if (tailEnd == null)
        {
            return;
        }

        caughtDudBomb = FindDudBomb(touchingTarget);
        caughtTargetIsDudBomb = caughtDudBomb != null;

        caughtMissile = FindMissile(touchingTarget);

        if (caughtMissile != null)
        {
            caughtMissile.OnCaughtByPlayer();
        }

        Rigidbody rb = FindRigidbody(touchingTarget);

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

            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Collider[] cols = touchingTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = false;
        }

        Vector3 originalScale = touchingTarget.localScale;

        touchingTarget.SetParent(tailEnd, false);

        if (caughtMissile != null)
        {
            touchingTarget.localPosition = caughtMissile.CatchLocalPositionOffset;
            touchingTarget.localRotation = Quaternion.Euler(caughtMissile.CatchLocalRotationOffset);
        }
        else
        {
            BossHeadCatchable bossHeadCatchable = touchingTarget.GetComponent<BossHeadCatchable>();

            if (bossHeadCatchable == null)
            {
                bossHeadCatchable = touchingTarget.GetComponentInParent<BossHeadCatchable>();
            }

            if (bossHeadCatchable == null)
            {
                bossHeadCatchable = touchingTarget.GetComponentInChildren<BossHeadCatchable>();
            }

            if (bossHeadCatchable != null)
            {
                touchingTarget.localPosition = bossHeadCatchable.CatchLocalPositionOffset;
                touchingTarget.localRotation = Quaternion.Euler(bossHeadCatchable.CatchLocalRotationOffset);
            }
            else
            {
                touchingTarget.localPosition = Vector3.zero;
                touchingTarget.localRotation = Quaternion.identity;
            }
        }

        Vector3 parentScale = tailEnd.lossyScale;

        touchingTarget.localScale = new Vector3(
            parentScale.x != 0 ? originalScale.x / parentScale.x : originalScale.x,
            parentScale.y != 0 ? originalScale.y / parentScale.y : originalScale.y,
            parentScale.z != 0 ? originalScale.z / parentScale.z : originalScale.z
        );

        caughtTarget = touchingTarget;
        touchingTarget = null;

        Debug.Log("キャッチ！");

        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.Play(EffectType.Chatch, tailEnd.position);
        }

        MySoundManeger.Play(gameObject, SEList.SE_CATCH);
    }

    private void ReleaseTarget()
    {
        if (caughtTarget == null)
        {
            return;
        }

        Transform releasedTarget = caughtTarget;
        Rigidbody rb = FindRigidbody(releasedTarget);

        releasedTarget.SetParent(null);

        Collider[] cols = releasedTarget.GetComponentsInChildren<Collider>();

        foreach (Collider col in cols)
        {
            col.enabled = true;
        }

        if (caughtTargetIsDudBomb)
        {
            DudBomb dudBomb = caughtDudBomb;

            if (dudBomb == null)
            {
                dudBomb = FindDudBomb(releasedTarget);
            }

            if (dudBomb != null)
            {
                dudBomb.ArmByPlayerThrow();
                Debug.Log("PlayerCatchEnemy: 不発弾に投げ判定を付けました");
            }
            else
            {
                Debug.LogWarning("PlayerCatchEnemy: 不発弾扱いですが DudBomb が見つかりませんでした");
            }
        }

        ThrowableBomb throwableBomb = releasedTarget.GetComponent<ThrowableBomb>();

        if (throwableBomb == null)
        {
            throwableBomb = releasedTarget.GetComponentInParent<ThrowableBomb>();
        }

        if (throwableBomb == null)
        {
            throwableBomb = releasedTarget.GetComponentInChildren<ThrowableBomb>();
        }

        if (throwableBomb != null)
        {
            throwableBomb.ArmByPlayerThrow();
            Debug.Log("PlayerCatchEnemy: 通常爆弾に投げ判定を付けました");
        }

        Missile missile = caughtMissile;

        if (missile == null)
        {
            missile = FindMissile(releasedTarget);
        }

        if (missile != null)
        {
            missile.OnThrownByPlayer();
            Debug.Log("PlayerCatchEnemy: ミサイルに投げ判定を付けました");
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float baseMultiplier = caughtTargetIsDudBomb
                ? dudBombThrowMultiplier
                : normalThrowMultiplier;

            float speed = tailVelocity.magnitude;

            float extraMultiplier = baseMultiplier - 1f;

            float compressedExtra =
                extraMultiplier / (1f + speed * 0.05f);

            float finalMultiplier = 1f + compressedExtra;

            rb.linearVelocity = tailVelocity * finalMultiplier;

            if (caughtTargetIsDudBomb && massChangedRb == rb)
            {
                StartCoroutine(RestoreMassAfterDelay(rb, originalMass, dudBombMassRestoreTime));
            }
        }

        Debug.Log("投げた！");

        caughtTarget = null;
        caughtTargetIsDudBomb = false;
        caughtDudBomb = null;
        caughtMissile = null;
        massChangedRb = null;
    }

    private Rigidbody FindRigidbody(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        Rigidbody rb = target.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = target.GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            rb = target.GetComponentInChildren<Rigidbody>();
        }

        return rb;
    }

    private DudBomb FindDudBomb(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        DudBomb dudBomb = target.GetComponent<DudBomb>();

        if (dudBomb == null)
        {
            dudBomb = target.GetComponentInParent<DudBomb>();
        }

        if (dudBomb == null)
        {
            dudBomb = target.GetComponentInChildren<DudBomb>();
        }

        return dudBomb;
    }

    private Missile FindMissile(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        Missile missile = target.GetComponent<Missile>();

        if (missile == null)
        {
            missile = target.GetComponentInParent<Missile>();
        }

        if (missile == null)
        {
            missile = target.GetComponentInChildren<Missile>();
        }

        return missile;
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