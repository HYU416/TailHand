using UnityEngine;

/// <summary>
/// プレイヤーが投げ返したミサイル専用制御。
///
/// ・投げ返した位置のY座標を維持する
/// ・ボス方向へ水平方向だけ軌道補正する
/// ・固定速度で飛ばす
/// ・SphereCastですり抜けを防ぐ
/// ・BossWall / BossCoreを直接検出する
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MissileReturnController : MonoBehaviour
{
    [Header("必要コンポーネント")]
    [SerializeField]
    private Missile missile;

    [SerializeField]
    private Rigidbody rb;

    [Header("ボスターゲット")]
    [Tooltip("ボス本体のタグ")]
    [SerializeField]
    private string bossTargetTag = "Boss";

    [Tooltip("ボス壁のタグ")]
    [SerializeField]
    private string bossWallTag = "BossWall";

    [Tooltip("ボスコアのタグ")]
    [SerializeField]
    private string bossCoreTag = "BossCore";

    [Tooltip("ターゲット全体の中心を狙う")]
    [SerializeField]
    private bool useTargetBoundsCenter = true;

    [Header("Y軸固定")]
    [Tooltip("ONにすると、投げ返した瞬間のY座標を維持して飛びます")]
    [SerializeField]
    private bool lockHeightToThrowPosition = true;

    [Tooltip(
        "投げ返した瞬間の高さへ加える補正値です。" +
        "通常は0のままで大丈夫です"
    )]
    [SerializeField]
    private float throwHeightOffset = 0.0f;

    [Header("投げ返し飛行")]
    [Tooltip("投げ返したミサイルの固定速度")]
    [SerializeField]
    private float returnSpeed = 20.0f;

    [Tooltip("ボス方向へ曲がる速度")]
    [SerializeField]
    private float returnRotateSpeed = 720.0f;

    [Tooltip("投げた方向がボス方向からこの角度以内なら補正する")]
    [SerializeField]
    [Range(0.0f, 180.0f)]
    private float maxCorrectionAngle = 75.0f;

    [Tooltip("ボス方向への補正を使用する")]
    [SerializeField]
    private bool useBossCorrection = true;

    [Tooltip("飛行方向へミサイルの向きを合わせる")]
    [SerializeField]
    private bool alignRotationToDirection = true;

    [Header("すり抜け防止")]
    [Tooltip("高速移動時の先読み判定半径")]
    [SerializeField]
    private float sweepRadius = 0.25f;

    [Tooltip("1物理フレーム分より少し先まで判定する倍率")]
    [SerializeField]
    private float sweepDistanceMultiplier = 1.5f;

    [Tooltip("判定対象レイヤー")]
    [SerializeField]
    private LayerMask hitLayerMask = ~0;

    [Header("地面・障害物")]
    [SerializeField]
    private string groundTag = "Ground";

    [SerializeField]
    private string itemBoxTag = "ItemBox";

    [Tooltip("投げ返したミサイルが地面に当たったら爆発する")]
    [SerializeField]
    private bool explodeOnGroundHit = true;

    [Tooltip("投げ返したミサイルがアイテムボックスに当たったら爆発する")]
    [SerializeField]
    private bool explodeOnItemBoxHit = true;

    [Header("プレイヤー無視")]
    [SerializeField]
    private string playerTag = "Player";

    [SerializeField]
    private string tailLayerName = "Tail";

    [Header("デバッグ")]
    [SerializeField]
    private bool showDebugLog = true;

    private Transform bossTarget;
    private Vector3 bossAimLocalPosition;

    private Vector3 flightDirection;

    private bool returnFlightInitialized;
    private bool correctionEnabled;
    private bool hitProcessed;

    private float lockedFlightHeight;

    private Collider[] ownColliders;

    private RigidbodyConstraints originalConstraints;
    private bool constraintsCached;

    private void Awake()
    {
        if (missile == null)
        {
            missile = GetComponent<Missile>();
        }

        if (missile == null)
        {
            missile = GetComponentInParent<Missile>();
        }

        if (missile == null)
        {
            missile = GetComponentInChildren<Missile>();
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        if (rb == null)
        {
            rb = GetComponentInParent<Rigidbody>();
        }

        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }

        ownColliders =
            GetComponentsInChildren<Collider>(true);
    }

    private void FixedUpdate()
    {
        if (missile == null || rb == null)
        {
            return;
        }

        /*
         * プレイヤーが投げ返した状態でない間は、
         * 通常のMissile.csへ処理を任せる。
         */
        if (!missile.CanAffectBoss())
        {
            ResetReturnFlight();
            return;
        }

        if (hitProcessed)
        {
            return;
        }

        if (!returnFlightInitialized)
        {
            InitializeReturnFlight();
        }

        if (!returnFlightInitialized)
        {
            return;
        }

        /*
         * 投げ返し開始時のY座標を毎物理フレーム維持する。
         */
        KeepLockedHeight();

        UpdateFlightDirection();

        /*
         * 高速移動によるColliderのすり抜けを防ぐ。
         */
        if (CheckFuturePath())
        {
            return;
        }

        Vector3 velocity =
            flightDirection * returnSpeed;

        if (lockHeightToThrowPosition)
        {
            velocity.y = 0.0f;
        }

        rb.linearVelocity = velocity;

        AlignMissileRotation();
    }

    private void InitializeReturnFlight()
    {
        /*
         * 親から外され、プレイヤーが投げた直後の高さを保存する。
         */
        lockedFlightHeight =
            rb.position.y + throwHeightOffset;

        FindBossTarget();

        CacheOriginalPhysics();
        ConfigureThrownPhysics();
        SetOwnCollidersSolid();

        Vector3 currentVelocity =
            rb.linearVelocity;

        if (lockHeightToThrowPosition)
        {
            currentVelocity.y = 0.0f;
        }

        if (currentVelocity.sqrMagnitude > 0.01f)
        {
            flightDirection =
                currentVelocity.normalized;
        }
        else
        {
            Vector3 targetDirection =
                GetDirectionToBoss();

            if (targetDirection.sqrMagnitude > 0.0001f)
            {
                flightDirection =
                    targetDirection;
            }
            else
            {
                flightDirection =
                    GetCurrentNoseWorldDirection();

                if (lockHeightToThrowPosition)
                {
                    flightDirection.y = 0.0f;
                }
            }
        }

        if (flightDirection.sqrMagnitude <= 0.0001f)
        {
            flightDirection = transform.forward;

            if (lockHeightToThrowPosition)
            {
                flightDirection.y = 0.0f;
            }
        }

        flightDirection.Normalize();

        Vector3 directionToBoss =
            GetDirectionToBoss();

        correctionEnabled = false;

        if (useBossCorrection &&
            directionToBoss.sqrMagnitude > 0.0001f)
        {
            float angle =
                Vector3.Angle(
                    flightDirection,
                    directionToBoss
                );

            correctionEnabled =
                angle <= maxCorrectionAngle;

            if (showDebugLog)
            {
                Debug.Log(
                    "MissileReturnController: 投げ返し開始" +
                    " / 投擲角度 = " +
                    angle.ToString("F1") +
                    " / 軌道補正 = " +
                    correctionEnabled +
                    " / Speed = " +
                    returnSpeed.ToString("F1") +
                    " / 固定Y = " +
                    lockedFlightHeight.ToString("F2"),
                    gameObject
                );
            }
        }
        else if (showDebugLog)
        {
            Debug.Log(
                "MissileReturnController: 投げ返し開始" +
                " / ボスターゲットなし" +
                " / Speed = " +
                returnSpeed.ToString("F1") +
                " / 固定Y = " +
                lockedFlightHeight.ToString("F2"),
                gameObject
            );
        }

        KeepLockedHeight();

        Vector3 startVelocity =
            flightDirection * returnSpeed;

        if (lockHeightToThrowPosition)
        {
            startVelocity.y = 0.0f;
        }

        rb.linearVelocity =
            startVelocity;

        returnFlightInitialized = true;
    }

    private void CacheOriginalPhysics()
    {
        if (constraintsCached || rb == null)
        {
            return;
        }

        originalConstraints =
            rb.constraints;

        constraintsCached = true;
    }

    private void ConfigureThrownPhysics()
    {
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.detectCollisions = true;

        rb.linearDamping = 0.0f;
        rb.angularVelocity = Vector3.zero;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        if (lockHeightToThrowPosition)
        {
            rb.constraints =
                originalConstraints |
                RigidbodyConstraints.FreezePositionY;
        }
    }

    private void KeepLockedHeight()
    {
        if (!lockHeightToThrowPosition)
        {
            return;
        }

        Vector3 position =
            rb.position;

        position.y =
            lockedFlightHeight;

        rb.position =
            position;

        Vector3 velocity =
            rb.linearVelocity;

        velocity.y = 0.0f;

        rb.linearVelocity =
            velocity;
    }

    private void UpdateFlightDirection()
    {
        if (!correctionEnabled)
        {
            if (lockHeightToThrowPosition)
            {
                flightDirection.y = 0.0f;

                if (flightDirection.sqrMagnitude > 0.0001f)
                {
                    flightDirection.Normalize();
                }
            }

            return;
        }

        Vector3 desiredDirection =
            GetDirectionToBoss();

        if (desiredDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float maxRadians =
            returnRotateSpeed *
            Mathf.Deg2Rad *
            Time.fixedDeltaTime;

        flightDirection =
            Vector3.RotateTowards(
                flightDirection,
                desiredDirection,
                maxRadians,
                0.0f
            );

        if (lockHeightToThrowPosition)
        {
            flightDirection.y = 0.0f;
        }

        if (flightDirection.sqrMagnitude > 0.0001f)
        {
            flightDirection.Normalize();
        }
    }

    private Vector3 GetDirectionToBoss()
    {
        if (bossTarget == null)
        {
            FindBossTarget();
        }

        if (bossTarget == null || rb == null)
        {
            return Vector3.zero;
        }

        Vector3 aimPosition =
            bossTarget.TransformPoint(
                bossAimLocalPosition
            );

        /*
         * ボスの中心が上にあっても、
         * 投げ返したミサイルと同じY座標を狙わせる。
         */
        if (lockHeightToThrowPosition)
        {
            aimPosition.y =
                lockedFlightHeight;
        }

        Vector3 direction =
            aimPosition - rb.position;

        if (lockHeightToThrowPosition)
        {
            direction.y = 0.0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    private bool CheckFuturePath()
    {
        if (flightDirection.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        float distance =
            returnSpeed *
            Time.fixedDeltaTime *
            Mathf.Max(
                1.0f,
                sweepDistanceMultiplier
            );

        Collider[] overlaps =
            Physics.OverlapSphere(
                rb.position,
                Mathf.Max(0.01f, sweepRadius),
                hitLayerMask,
                QueryTriggerInteraction.Collide
            );

        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider overlap =
                overlaps[i];

            if (!IsRelevantHit(overlap))
            {
                continue;
            }

            if (ProcessHit(overlap))
            {
                return true;
            }
        }

        RaycastHit[] hits =
            Physics.SphereCastAll(
                rb.position,
                Mathf.Max(0.01f, sweepRadius),
                flightDirection,
                distance,
                hitLayerMask,
                QueryTriggerInteraction.Collide
            );

        Collider nearestCollider = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider =
                hits[i].collider;

            if (!IsRelevantHit(hitCollider))
            {
                continue;
            }

            if (hits[i].distance < nearestDistance)
            {
                nearestDistance =
                    hits[i].distance;

                nearestCollider =
                    hitCollider;
            }
        }

        if (nearestCollider != null)
        {
            return ProcessHit(nearestCollider);
        }

        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        ProcessHit(collision.collider);
    }

    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other);
    }

    private bool ProcessHit(Collider hitCollider)
    {
        if (hitProcessed)
        {
            return true;
        }

        if (missile == null)
        {
            return false;
        }

        if (!missile.CanAffectBoss())
        {
            return false;
        }

        if (hitCollider == null)
        {
            return false;
        }

        if (IsOwnCollider(hitCollider))
        {
            return false;
        }

        if (IsPlayerOrTail(hitCollider.gameObject))
        {
            return false;
        }

        GameObject wallObject =
            FindTaggedObject(
                hitCollider.gameObject,
                bossWallTag
            );

        if (wallObject != null)
        {
            return ProcessBossWallHit(
                hitCollider,
                wallObject
            );
        }

        GameObject coreObject =
            FindTaggedObject(
                hitCollider.gameObject,
                bossCoreTag
            );

        if (coreObject != null)
        {
            return ProcessBossCoreHit(
                hitCollider,
                coreObject
            );
        }

        if (explodeOnGroundHit)
        {
            GameObject groundObject =
                FindTaggedObject(
                    hitCollider.gameObject,
                    groundTag
                );

            if (groundObject != null)
            {
                hitProcessed = true;

                if (showDebugLog)
                {
                    Debug.Log(
                        "MissileReturnController: " +
                        "投げ返したミサイルが地面に命中 / " +
                        groundObject.name,
                        groundObject
                    );
                }

                missile.Explode();

                return true;
            }
        }

        if (explodeOnItemBoxHit)
        {
            GameObject itemBoxObject =
                FindTaggedObject(
                    hitCollider.gameObject,
                    itemBoxTag
                );

            if (itemBoxObject != null)
            {
                hitProcessed = true;

                if (showDebugLog)
                {
                    Debug.Log(
                        "MissileReturnController: " +
                        "投げ返したミサイルがアイテムに命中 / " +
                        itemBoxObject.name,
                        itemBoxObject
                    );
                }

                missile.Explode();

                return true;
            }
        }

        return false;
    }

    private bool ProcessBossWallHit(
        Collider hitCollider,
        GameObject wallObject
    )
    {
        BossArmorPart armorPart =
            hitCollider.GetComponent<BossArmorPart>();

        if (armorPart == null)
        {
            armorPart =
                hitCollider.GetComponentInParent<BossArmorPart>();
        }

        if (armorPart == null && wallObject != null)
        {
            armorPart =
                wallObject.GetComponent<BossArmorPart>();
        }

        if (armorPart == null && wallObject != null)
        {
            armorPart =
                wallObject.GetComponentInChildren<BossArmorPart>();
        }

        if (armorPart == null)
        {
            Debug.LogError(
                "MissileReturnController: BossWallを検出したが、" +
                "BossArmorPartが見つかりません / " +
                wallObject.name,
                wallObject
            );

            return false;
        }

        hitProcessed = true;

        if (showDebugLog)
        {
            Debug.Log(
                "MissileReturnController: " +
                "プレイヤーが投げたミサイルがボス壁に命中 / " +
                wallObject.name,
                wallObject
            );
        }

        armorPart.BreakArmor();
        missile.ExplodeByBossHit();

        return true;
    }

    private bool ProcessBossCoreHit(
        Collider hitCollider,
        GameObject coreObject
    )
    {
        BossPhaseController controller =
            hitCollider.GetComponentInParent<BossPhaseController>();

        if (controller == null && coreObject != null)
        {
            controller =
                coreObject.GetComponentInParent<BossPhaseController>();
        }

        if (controller == null)
        {
            controller =
                FindFirstObjectByType<BossPhaseController>();
        }

        if (controller == null)
        {
            Debug.LogError(
                "MissileReturnController: BossCoreを検出したが、" +
                "BossPhaseControllerが見つかりません / " +
                coreObject.name,
                coreObject
            );

            return false;
        }

        hitProcessed = true;

        int coreDamage =
            Mathf.Max(
                1,
                missile.CoreDamageByPlayerThrow
            );

        if (showDebugLog)
        {
            Debug.Log(
                "MissileReturnController: " +
                "プレイヤーが投げたミサイルがボスコアに命中" +
                " / Damage = " +
                coreDamage +
                " / Core = " +
                coreObject.name,
                coreObject
            );
        }

        controller.OnCoreHit(coreDamage);
        missile.ExplodeByBossHit();

        return true;
    }

    private void FindBossTarget()
    {
        bossTarget = null;

        if (string.IsNullOrEmpty(bossTargetTag))
        {
            return;
        }

        GameObject bossObject = null;

        try
        {
            bossObject =
                GameObject.FindGameObjectWithTag(
                    bossTargetTag
                );
        }
        catch (UnityException)
        {
            Debug.LogError(
                "MissileReturnController: Tag「" +
                bossTargetTag +
                "」が登録されていません",
                gameObject
            );

            return;
        }

        if (bossObject == null)
        {
            return;
        }

        bossTarget =
            bossObject.transform;

        Vector3 aimWorldPosition =
            useTargetBoundsCenter
                ? GetBoundsCenter(bossObject)
                : bossTarget.position;

        bossAimLocalPosition =
            bossTarget.InverseTransformPoint(
                aimWorldPosition
            );
    }

    private Vector3 GetBoundsCenter(
        GameObject targetObject
    )
    {
        Renderer[] renderers =
            targetObject.GetComponentsInChildren<Renderer>(
                true
            );

        if (renderers.Length > 0)
        {
            bool foundRenderer = false;
            Bounds bounds = new Bounds();

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer =
                    renderers[i];

                if (targetRenderer == null ||
                    !targetRenderer.enabled)
                {
                    continue;
                }

                if (!foundRenderer)
                {
                    bounds =
                        targetRenderer.bounds;

                    foundRenderer = true;
                }
                else
                {
                    bounds.Encapsulate(
                        targetRenderer.bounds
                    );
                }
            }

            if (foundRenderer)
            {
                return bounds.center;
            }
        }

        Collider[] colliders =
            targetObject.GetComponentsInChildren<Collider>(
                true
            );

        if (colliders.Length > 0)
        {
            bool foundCollider = false;
            Bounds bounds = new Bounds();

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider targetCollider =
                    colliders[i];

                if (targetCollider == null ||
                    !targetCollider.enabled)
                {
                    continue;
                }

                if (!foundCollider)
                {
                    bounds =
                        targetCollider.bounds;

                    foundCollider = true;
                }
                else
                {
                    bounds.Encapsulate(
                        targetCollider.bounds
                    );
                }
            }

            if (foundCollider)
            {
                return bounds.center;
            }
        }

        return targetObject.transform.position;
    }

    private void SetOwnCollidersSolid()
    {
        if (ownColliders == null ||
            ownColliders.Length == 0)
        {
            ownColliders =
                GetComponentsInChildren<Collider>(true);
        }

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider ownCollider =
                ownColliders[i];

            if (ownCollider == null)
            {
                continue;
            }

            ownCollider.enabled = true;
            ownCollider.isTrigger = false;
        }
    }

    private bool IsRelevantHit(
        Collider targetCollider
    )
    {
        if (targetCollider == null)
        {
            return false;
        }

        if (IsOwnCollider(targetCollider))
        {
            return false;
        }

        if (IsPlayerOrTail(targetCollider.gameObject))
        {
            return false;
        }

        if (FindTaggedObject(
                targetCollider.gameObject,
                bossWallTag
            ) != null)
        {
            return true;
        }

        if (FindTaggedObject(
                targetCollider.gameObject,
                bossCoreTag
            ) != null)
        {
            return true;
        }

        if (explodeOnGroundHit &&
            FindTaggedObject(
                targetCollider.gameObject,
                groundTag
            ) != null)
        {
            return true;
        }

        if (explodeOnItemBoxHit &&
            FindTaggedObject(
                targetCollider.gameObject,
                itemBoxTag
            ) != null)
        {
            return true;
        }

        return false;
    }

    private bool IsOwnCollider(
        Collider targetCollider
    )
    {
        if (targetCollider == null)
        {
            return false;
        }

        if (targetCollider.transform == transform)
        {
            return true;
        }

        return targetCollider.transform.IsChildOf(
            transform
        );
    }

    private bool IsPlayerOrTail(
        GameObject targetObject
    )
    {
        if (targetObject == null)
        {
            return false;
        }

        int tailLayer =
            LayerMask.NameToLayer(
                tailLayerName
            );

        Transform current =
            targetObject.transform;

        while (current != null)
        {
            if (tailLayer >= 0 &&
                current.gameObject.layer == tailLayer)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(playerTag))
            {
                try
                {
                    if (current.CompareTag(playerTag))
                    {
                        return true;
                    }
                }
                catch (UnityException)
                {
                }
            }

            TailCollisionDetector detector =
                current.GetComponent<TailCollisionDetector>();

            if (detector != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private GameObject FindTaggedObject(
        GameObject hitObject,
        string tagName
    )
    {
        if (hitObject == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(tagName))
        {
            return null;
        }

        Transform current =
            hitObject.transform;

        while (current != null)
        {
            try
            {
                if (current.CompareTag(tagName))
                {
                    return current.gameObject;
                }
            }
            catch (UnityException)
            {
                return null;
            }

            current = current.parent;
        }

        return null;
    }

    private void AlignMissileRotation()
    {
        if (!alignRotationToDirection)
        {
            return;
        }

        if (flightDirection.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        Vector3 localNoseDirection =
            GetLocalNoseDirection();

        Vector3 currentWorldNose =
            transform.TransformDirection(
                localNoseDirection
            );

        Quaternion correction =
            Quaternion.FromToRotation(
                currentWorldNose,
                flightDirection
            );

        Quaternion targetRotation =
            correction * transform.rotation;

        rb.MoveRotation(targetRotation);
    }

    private Vector3 GetCurrentNoseWorldDirection()
    {
        Vector3 direction =
            transform.TransformDirection(
                GetLocalNoseDirection()
            );

        if (lockHeightToThrowPosition)
        {
            direction.y = 0.0f;
        }

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return transform.forward;
        }

        return direction.normalized;
    }

    private Vector3 GetLocalNoseDirection()
    {
        if (missile == null)
        {
            return Vector3.forward;
        }

        switch (missile.noseAxis)
        {
            case Missile.MissileNoseAxis.Forward_Z:
                return Vector3.forward;

            case Missile.MissileNoseAxis.Back_Z:
                return Vector3.back;

            case Missile.MissileNoseAxis.Right_X:
                return Vector3.right;

            case Missile.MissileNoseAxis.Left_X:
                return Vector3.left;

            case Missile.MissileNoseAxis.Up_Y:
                return Vector3.up;

            case Missile.MissileNoseAxis.Down_Y:
                return Vector3.down;

            default:
                return Vector3.forward;
        }
    }

    private void ResetReturnFlight()
    {
        if (returnFlightInitialized &&
            constraintsCached &&
            rb != null)
        {
            rb.constraints =
                originalConstraints;
        }

        returnFlightInitialized = false;
        correctionEnabled = false;
        hitProcessed = false;

        constraintsCached = false;

        bossTarget = null;
        flightDirection = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(0.01f, sweepRadius)
        );
    }
}