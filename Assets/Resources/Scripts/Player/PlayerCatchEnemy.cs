using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCatchEnemy : MonoBehaviour
{
    [Header("掴む基準位置")]
    public Transform tailEnd;

    [Header("掴んだ時の基本位置補正")]
    [Tooltip("tailEndから見たローカル位置です。手の中に収まる位置に調整してください")]
    [SerializeField] private Vector3 defaultCatchLocalPositionOffset = new Vector3(0.15f, 0.0f, 0.0f);

    [Header("掴んだ時の基本回転補正")]
    [Tooltip("tailEndから見たローカル回転です")]
    [SerializeField] private Vector3 defaultCatchLocalRotationOffset = Vector3.zero;

    [Header("掴んでいる間の当たり判定")]
    [Tooltip("ON推奨。掴んでいる間、掴んだ物のColliderをOFFにして暴れを防ぎます")]
    [SerializeField] private bool disableCollidersWhileCaught = true;

    [Header("掴んでいる間に位置を強制固定")]
    [Tooltip("ON推奨。別スクリプトや物理で動いても毎フレーム手元に戻します")]
    [SerializeField] private bool forceHoldPositionEveryFrame = true;

    [Header("投げる強さ")]
    [SerializeField] private float normalThrowMultiplier = 1.0f;

    [Header("速度が小さすぎる時の最低投げ速度")]
    [Tooltip("しっぽの速度がほぼ0でも、離した時に少しは前へ飛ばすための最低速度です")]
    [SerializeField] private float minimumThrowSpeed = 2.0f;

    [Header("最低投げ速度を使う判定")]
    [Tooltip("tailVelocityがこの値未満ならminimumThrowSpeedを使います")]
    [SerializeField] private float minimumThrowThreshold = 0.2f;

    [Header("不発弾を投げる時の設定")]
    [SerializeField] private float dudBombMassWhileCaught = 0.1f;
    [SerializeField] private float dudBombThrowMultiplier = 3.0f;
    [SerializeField] private float dudBombMassRestoreTime = 1.0f;

    [HideInInspector]
    public Transform touchingTarget;

    public Transform caughtTarget;

    private Vector3 prevTailPos;
    private Vector3 tailVelocity;

    private Rigidbody caughtRigidbody;
    private Rigidbody massChangedRb;

    private float originalMass;
    private bool originalUseGravity;

    private bool caughtTargetIsDudBomb;

    private DudBomb caughtDudBomb;
    private Missile caughtMissile;

    private GameObject catchingObject = null;

    private Vector3 caughtWorldScale;
    private Vector3 currentCatchLocalPositionOffset;
    private Vector3 currentCatchLocalRotationOffset;

    private Collider[] caughtColliders;
    private bool[] caughtColliderEnabledStates;

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

    private void LateUpdate()
    {
        if (!forceHoldPositionEveryFrame)
        {
            return;
        }

        if (caughtTarget == null)
        {
            return;
        }

        ForceCaughtTargetToHoldPosition();
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

        caughtRigidbody = FindRigidbody(touchingTarget);

        if (caughtRigidbody != null)
        {
            originalUseGravity = caughtRigidbody.useGravity;

            caughtRigidbody.linearVelocity = Vector3.zero;
            caughtRigidbody.angularVelocity = Vector3.zero;

            if (caughtTargetIsDudBomb)
            {
                massChangedRb = caughtRigidbody;
                originalMass = caughtRigidbody.mass;
                caughtRigidbody.mass = dudBombMassWhileCaught;
            }

            caughtRigidbody.useGravity = false;
            caughtRigidbody.isKinematic = true;
        }

        caughtWorldScale = touchingTarget.lossyScale;

        currentCatchLocalPositionOffset = defaultCatchLocalPositionOffset;
        currentCatchLocalRotationOffset = defaultCatchLocalRotationOffset;

        ApplySpecialCatchOffset(touchingTarget);

        caughtTarget = touchingTarget;
        catchingObject = caughtTarget.gameObject;
        touchingTarget = null;

        CacheAndDisableCaughtColliders(caughtTarget);

        caughtTarget.SetParent(tailEnd, false);
        caughtTarget.localPosition = currentCatchLocalPositionOffset;
        caughtTarget.localRotation = Quaternion.Euler(currentCatchLocalRotationOffset);
        SetWorldScale(caughtTarget, caughtWorldScale);

        ForceCaughtTargetToHoldPosition();

        Debug.Log("キャッチ！");

        if (EffectManager.IsInitialized)
        {
            EffectManager.Instance.Play(EffectType.Chatch, tailEnd.position);
        }

        MySoundManeger.Play(gameObject, SEList.SE_CATCH);
    }

    private void ApplySpecialCatchOffset(Transform target)
    {
        if (target == null)
        {
            return;
        }

        if (caughtMissile != null)
        {
            currentCatchLocalPositionOffset = caughtMissile.CatchLocalPositionOffset;
            currentCatchLocalRotationOffset = caughtMissile.CatchLocalRotationOffset;
            return;
        }

        BossHeadCatchable bossHeadCatchable = target.GetComponent<BossHeadCatchable>();

        if (bossHeadCatchable == null)
        {
            bossHeadCatchable = target.GetComponentInParent<BossHeadCatchable>();
        }

        if (bossHeadCatchable == null)
        {
            bossHeadCatchable = target.GetComponentInChildren<BossHeadCatchable>();
        }

        if (bossHeadCatchable != null)
        {
            currentCatchLocalPositionOffset = bossHeadCatchable.CatchLocalPositionOffset;
            currentCatchLocalRotationOffset = bossHeadCatchable.CatchLocalRotationOffset;
        }
    }

    private void ForceCaughtTargetToHoldPosition()
    {
        if (caughtTarget == null)
        {
            return;
        }

        if (tailEnd == null)
        {
            return;
        }

        if (caughtTarget.parent != tailEnd)
        {
            caughtTarget.SetParent(tailEnd, false);
        }

        caughtTarget.localPosition = currentCatchLocalPositionOffset;
        caughtTarget.localRotation = Quaternion.Euler(currentCatchLocalRotationOffset);
        SetWorldScale(caughtTarget, caughtWorldScale);

        if (caughtRigidbody != null)
        {
            caughtRigidbody.linearVelocity = Vector3.zero;
            caughtRigidbody.angularVelocity = Vector3.zero;
            caughtRigidbody.useGravity = false;
            caughtRigidbody.isKinematic = true;
        }
    }

    private void ReleaseTarget()
    {
        if (caughtTarget == null)
        {
            return;
        }

        Transform releasedTarget = caughtTarget;
        Rigidbody rb = caughtRigidbody;

        RestoreCaughtColliders();

        releasedTarget.SetParent(null, true);

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
            rb.useGravity = originalUseGravity;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            float baseMultiplier = caughtTargetIsDudBomb
                ? dudBombThrowMultiplier
                : normalThrowMultiplier;

            Vector3 throwVelocity = tailVelocity;

            if (throwVelocity.magnitude < minimumThrowThreshold)
            {
                throwVelocity = GetFallbackThrowDirection() * minimumThrowSpeed;
            }

            float speed = throwVelocity.magnitude;
            float extraMultiplier = baseMultiplier - 1.0f;
            float compressedExtra = extraMultiplier / (1.0f + speed * 0.05f);
            float finalMultiplier = 1.0f + compressedExtra;

            rb.linearVelocity = throwVelocity * finalMultiplier;
        }

        if (caughtTargetIsDudBomb && massChangedRb != null)
        {
            StartCoroutine(RestoreMassAfterDelay(massChangedRb, originalMass, dudBombMassRestoreTime));
        }

        Debug.Log("投げた！");

        caughtTarget = null;
        caughtRigidbody = null;
        caughtTargetIsDudBomb = false;
        caughtDudBomb = null;
        caughtMissile = null;
        massChangedRb = null;
        catchingObject = null;

        caughtColliders = null;
        caughtColliderEnabledStates = null;
    }

    private Vector3 GetFallbackThrowDirection()
    {
        if (tailEnd == null)
        {
            return transform.forward;
        }

        Vector3 direction = tailEnd.forward;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();
        return direction;
    }

    private void CacheAndDisableCaughtColliders(Transform target)
    {
        caughtColliders = null;
        caughtColliderEnabledStates = null;

        if (!disableCollidersWhileCaught)
        {
            return;
        }

        if (target == null)
        {
            return;
        }

        caughtColliders = target.GetComponentsInChildren<Collider>(true);
        caughtColliderEnabledStates = new bool[caughtColliders.Length];

        for (int i = 0; i < caughtColliders.Length; i++)
        {
            if (caughtColliders[i] == null)
            {
                continue;
            }

            caughtColliderEnabledStates[i] = caughtColliders[i].enabled;
            caughtColliders[i].enabled = false;
        }
    }

    private void RestoreCaughtColliders()
    {
        if (caughtColliders == null)
        {
            return;
        }

        for (int i = 0; i < caughtColliders.Length; i++)
        {
            if (caughtColliders[i] == null)
            {
                continue;
            }

            if (caughtColliderEnabledStates != null && i < caughtColliderEnabledStates.Length)
            {
                caughtColliders[i].enabled = caughtColliderEnabledStates[i];
            }
            else
            {
                caughtColliders[i].enabled = true;
            }
        }
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

    private void SetWorldScale(Transform target, Vector3 worldScale)
    {
        if (target == null)
        {
            return;
        }

        Transform parent = target.parent;

        if (parent == null)
        {
            target.localScale = worldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;

        target.localScale = new Vector3(
            parentScale.x != 0.0f ? worldScale.x / parentScale.x : worldScale.x,
            parentScale.y != 0.0f ? worldScale.y / parentScale.y : worldScale.y,
            parentScale.z != 0.0f ? worldScale.z / parentScale.z : worldScale.z
        );
    }

    private IEnumerator RestoreMassAfterDelay(Rigidbody rb, float mass, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (rb != null)
        {
            rb.mass = mass;
        }
    }

    public GameObject CatchingObjectPtr()
    {
        return catchingObject;
    }

    public bool IsHolding
    {
        get { return caughtTarget != null; }
    }
}