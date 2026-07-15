using System;
using System.Reflection;
using UnityEngine;

public class Missile : MonoBehaviour
{
    public enum MissileNoseAxis
    {
        Forward_Z,
        Back_Z,
        Right_X,
        Left_X,
        Up_Y,
        Down_Y
    }

    [Header("【ターゲット設定】")]
    [Tooltip("ミサイルが向かっていく対象です。基本はプレイヤーを入れます")]
    public Transform target;

    [Tooltip("targetが未設定のとき、Playerタグを探します")]
    public bool findPlayerByTag = true;

    [Tooltip("探すプレイヤーのタグ名です")]
    public string playerTag = "Player";

    [Header("【ミサイルの向き設定】")]
    [Tooltip("ミサイルモデルのどの軸が先端かを選びます")]
    public MissileNoseAxis noseAxis = MissileNoseAxis.Up_Y;

    [Header("【掴み設定】")]
    [Tooltip("掴んだ時のローカル位置補正です")]
    [SerializeField]
    private Vector3 catchLocalPositionOffset = Vector3.zero;

    [Tooltip("掴んだ時のローカル回転補正です")]
    [SerializeField]
    private Vector3 catchLocalRotationOffset = Vector3.zero;

    [Tooltip("尻尾の掴み判定レイヤー名です")]
    [SerializeField]
    private string tailLayerName = "Tail";

    [Header("【プレイヤー直撃設定】")]
    [Tooltip("敵ミサイルがプレイヤーに直接当たった時、爆発待機時間を無視して必ずダメージを与えます")]
    [SerializeField]
    private bool alwaysDamagePlayerOnDirectHit = true;

    [Tooltip("プレイヤーへの直撃時に即座に爆発します")]
    [SerializeField]
    private bool explodeImmediatelyOnPlayerDirectHit = true;

    [Tooltip("敵ミサイル飛行中のColliderをTriggerにして、プレイヤーを物理的に押さないようにします")]
    [SerializeField]
    private bool preventPhysicalPushWhileEnemyMissile = true;

    [Header("【投げられた後の設定】")]
    [Tooltip("プレイヤーが投げた後、何秒後に自動爆発するかです")]
    [SerializeField]
    private float explosionTimeAfterPlayerThrow = 3.0f;

    [Tooltip("プレイヤーが投げたミサイルがボス壁に与えるダメージです")]
    [SerializeField]
    private int armorDamageByPlayerThrow = 999;

    [Tooltip("プレイヤーが投げたミサイルがボスコアに与えるダメージです")]
    [SerializeField]
    private int coreDamageByPlayerThrow = 999;

    [Header("【移動設定】")]
    [Tooltip("ミサイルの速度です")]
    public float speed = 10f;

    [Tooltip("ミサイルが曲がる速さです")]
    public float rotateSpeed = 180f;

    [Tooltip("ONにすると常にプレイヤー方向へ曲がります")]
    public bool homing = true;

    [Tooltip("ONにするとRigidbodyの重力を使います")]
    public bool useGravity = false;

    [Tooltip("ミサイルの見た目サイズです")]
    public float missileScale = 1f;

    [Header("【爆発設定】")]
    [Tooltip("生成されてから何秒後に爆発するかです")]
    public float explosionTime = 5f;

    [Tooltip("ONにすると時間経過で爆発します")]
    public bool useTimeExplosion = true;

    [Tooltip("この秒数が経過するまでは、床やアイテムボックスに当たっても爆発しません")]
    public float minimumExplosionDelay = 5f;

    [Tooltip("ONにするとMinimum Explosion Delay経過時にその場で爆発します")]
    public bool explodeWhenMinimumDelayPassed = true;

    [Tooltip("ONにすると何かにぶつかった時に爆発判定を行います")]
    public bool explodeOnHit = true;

    [Tooltip("ONにするとプレイヤーに当たった時も爆発対象にします")]
    public bool explodeOnPlayerHit = false;

    [Tooltip("ONにするとGroundタグに当たった時に爆発対象にします")]
    public bool explodeOnGroundHit = true;

    [Tooltip("ONにするとItemBoxタグに当たった時に爆発対象にします")]
    public bool explodeOnItemBoxHit = true;

    [Tooltip("床タグです")]
    public string groundTag = "Ground";

    [Tooltip("アイテムボックス、岩などのタグです")]
    public string itemBoxTag = "ItemBox";

    [Tooltip("爆発の範囲です")]
    public float explosionRadius = 3f;

    [Tooltip("プレイヤーに与えるダメージです")]
    public int damage = 20;

    [Tooltip("爆発エフェクトのサイズ倍率です")]
    public float explosionEffectScaleMultiplier = 1f;

    [Tooltip("ONにすると爆発エフェクトを自動削除します")]
    public bool destroyExplosionEffect = true;

    [Tooltip("爆発エフェクトを削除するまでの時間です")]
    public float explosionEffectDestroyTime = 3f;

    [Header("【消滅設定】")]
    [Tooltip("爆発後にミサイル本体を消すまでの時間です")]
    public float destroyDelayAfterExplosion = 0f;

    [Header("【デバッグ表示】")]
    [Tooltip("ONにするとSceneビューで爆発範囲を表示します")]
    public bool showExplosionRadius = true;

    [Tooltip("ONにすると何に当たったかログを出します")]
    public bool showHitDebugLog = false;

    private Rigidbody rb;

    private Collider[] missileColliders;
    private bool[] originalColliderTriggerStates;

    private bool exploded;
    private float timer;

    private bool isCaughtByPlayer;
    private bool isThrownByPlayer;
    private float thrownTimer;

    private bool playerDirectHitProcessed;

    private bool originalHoming;
    private bool originalExplodeOnHit;
    private bool originalExplodeOnPlayerHit;
    private bool originalExplodeOnGroundHit;
    private bool originalExplodeOnItemBoxHit;
    private bool originalUseGravity;
    private bool originalUseTimeExplosion;
    private float originalSpeed;
    private Transform originalTarget;

    public Vector3 CatchLocalPositionOffset
    {
        get { return catchLocalPositionOffset; }
    }

    public Vector3 CatchLocalRotationOffset
    {
        get { return catchLocalRotationOffset; }
    }

    public int ArmorDamageByPlayerThrow
    {
        get { return armorDamageByPlayerThrow; }
    }

    public int CoreDamageByPlayerThrow
    {
        get { return coreDamageByPlayerThrow; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }

        CacheMissileColliders();
    }

    private void Start()
    {
        transform.localScale = Vector3.one * missileScale;

        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (target == null && findPlayerByTag)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);

            if (player != null)
            {
                target = player.transform;
            }
        }

        if (target != null)
        {
            Vector3 targetDirection = target.position - transform.position;

            if (targetDirection.sqrMagnitude > 0.001f)
            {
                RotateNoseToDirection(targetDirection.normalized, true);
            }
        }

        StoreOriginalSettings();

        SetEnemyMissileTriggerState(true);
    }

    private void Update()
    {
        if (exploded)
        {
            return;
        }

        if (isCaughtByPlayer)
        {
            return;
        }

        if (isThrownByPlayer)
        {
            thrownTimer += Time.deltaTime;

            if (explosionTimeAfterPlayerThrow > 0f &&
                thrownTimer >= explosionTimeAfterPlayerThrow)
            {
                ExplodeWithoutPlayerDamage();
            }

            return;
        }

        timer += Time.deltaTime;

        if (useTimeExplosion &&
            explodeWhenMinimumDelayPassed &&
            minimumExplosionDelay > 0f &&
            timer >= minimumExplosionDelay)
        {
            Explode();
            return;
        }

        if (useTimeExplosion &&
            !explodeWhenMinimumDelayPassed &&
            explosionTime > 0f &&
            timer >= explosionTime)
        {
            Explode();
        }
    }

    private void FixedUpdate()
    {
        if (exploded)
        {
            return;
        }

        if (isCaughtByPlayer)
        {
            return;
        }

        if (isThrownByPlayer)
        {
            return;
        }

        MoveMissile();
    }

    private void MoveMissile()
    {
        if (homing && target != null)
        {
            Vector3 targetDirection = target.position - transform.position;

            if (targetDirection.sqrMagnitude > 0.001f)
            {
                RotateNoseToDirection(targetDirection.normalized, false);
            }
        }

        Vector3 moveDirection = GetWorldNoseDirection();

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
        else
        {
            transform.position +=
                moveDirection * speed * Time.fixedDeltaTime;
        }
    }

    private void RotateNoseToDirection(Vector3 direction, bool instant)
    {
        if (direction.sqrMagnitude <= 0.001f)
        {
            return;
        }

        direction.Normalize();

        Vector3 currentNoseDirection = GetWorldNoseDirection();

        Quaternion addRotation = Quaternion.FromToRotation(
            currentNoseDirection,
            direction
        );

        Quaternion targetRotation =
            addRotation * transform.rotation;

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );
        }
    }

    private Vector3 GetWorldNoseDirection()
    {
        Vector3 localDirection = GetLocalNoseDirection();

        return transform
            .TransformDirection(localDirection)
            .normalized;
    }

    private Vector3 GetLocalNoseDirection()
    {
        switch (noseAxis)
        {
            case MissileNoseAxis.Forward_Z:
                return Vector3.forward;

            case MissileNoseAxis.Back_Z:
                return Vector3.back;

            case MissileNoseAxis.Right_X:
                return Vector3.right;

            case MissileNoseAxis.Left_X:
                return Vector3.left;

            case MissileNoseAxis.Up_Y:
                return Vector3.up;

            case MissileNoseAxis.Down_Y:
                return Vector3.down;

            default:
                return Vector3.forward;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            return;
        }

        HandleHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        HandleHit(other.gameObject);
    }

    private void HandleHit(GameObject hitObject)
    {
        if (exploded)
        {
            return;
        }

        if (isCaughtByPlayer)
        {
            return;
        }

        if (hitObject == null)
        {
            return;
        }

        if (IsTailCatchObject(hitObject))
        {
            if (showHitDebugLog)
            {
                Debug.Log(
                    "Missile: 尻尾判定に当たったので無視 / " +
                    hitObject.name,
                    hitObject
                );
            }

            return;
        }

        if (isThrownByPlayer)
        {
            return;
        }

        /*
         * プレイヤー直撃判定は、
         * minimumExplosionDelayやexplodeOnHitより先に処理する。
         */
        if (IsPlayerObject(hitObject))
        {
            HandlePlayerDirectHit(hitObject);
            return;
        }

        if (!explodeOnHit)
        {
            return;
        }

        if (timer < minimumExplosionDelay)
        {
            if (showHitDebugLog)
            {
                Debug.Log(
                    "Missile: まだ爆発可能時間ではありません / Hit = " +
                    hitObject.name +
                    " / Timer = " +
                    timer.ToString("F2"),
                    hitObject
                );
            }

            return;
        }

        if (!IsExplosionTarget(hitObject))
        {
            if (showHitDebugLog)
            {
                Debug.Log(
                    "Missile: 爆発対象ではないので無視 / " +
                    hitObject.name,
                    hitObject
                );
            }

            return;
        }

        Explode();
    }

    private void HandlePlayerDirectHit(GameObject hitObject)
    {
        if (playerDirectHitProcessed)
        {
            return;
        }

        playerDirectHitProcessed = true;

        /*
         * 直撃した瞬間に速度を止める。
         * Triggerなのでプレイヤーへ物理的な力は加わらない。
         */
        StopMissilePhysics();

        bool damageSent = false;

        if (alwaysDamagePlayerOnDirectHit || explodeOnPlayerHit)
        {
            damageSent = SendDamageOnceToPlayer(hitObject);
        }

        if (showHitDebugLog)
        {
            Debug.Log(
                "Missile: プレイヤーへ直撃 / Damage = " +
                damage +
                " / ダメージ処理成功 = " +
                damageSent,
                hitObject
            );
        }

        if (explodeImmediatelyOnPlayerDirectHit ||
            explodeOnPlayerHit)
        {
            /*
             * 直撃ダメージを既に与えているため、
             * 爆発範囲ダメージは重ねない。
             */
            ExplodeWithoutPlayerDamage();
        }
    }

    private bool SendDamageOnceToPlayer(GameObject hitObject)
    {
        Transform playerRoot = FindPlayerTransform(hitObject);

        if (playerRoot == null)
        {
            return false;
        }

        MonoBehaviour[] behaviours =
            playerRoot.GetComponentsInChildren<MonoBehaviour>(true);

        string[] damageMethodNames =
        {
            "TakeDamage",
            "Damage",
            "ApplyDamage"
        };

        for (int methodIndex = 0;
             methodIndex < damageMethodNames.Length;
             methodIndex++)
        {
            string methodName =
                damageMethodNames[methodIndex];

            for (int behaviourIndex = 0;
                 behaviourIndex < behaviours.Length;
                 behaviourIndex++)
            {
                MonoBehaviour behaviour =
                    behaviours[behaviourIndex];

                if (behaviour == null)
                {
                    continue;
                }

                MethodInfo method = behaviour
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(int) },
                        null
                    );

                if (method != null)
                {
                    method.Invoke(
                        behaviour,
                        new object[] { damage }
                    );

                    return true;
                }

                method = behaviour
                    .GetType()
                    .GetMethod(
                        methodName,
                        BindingFlags.Instance |
                        BindingFlags.Public |
                        BindingFlags.NonPublic,
                        null,
                        new Type[] { typeof(float) },
                        null
                    );

                if (method != null)
                {
                    method.Invoke(
                        behaviour,
                        new object[] { (float)damage }
                    );

                    return true;
                }
            }
        }

        Debug.LogWarning(
            "Missile: プレイヤー側にTakeDamage、Damage、ApplyDamageの" +
            "いずれも見つかりませんでした",
            playerRoot
        );

        return false;
    }

    private Transform FindPlayerTransform(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return null;
        }

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return current;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsExplosionTarget(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        if (explodeOnPlayerHit &&
            IsPlayerObject(hitObject))
        {
            return true;
        }

        if (explodeOnGroundHit &&
            HasTagInParent(hitObject, groundTag))
        {
            return true;
        }

        if (explodeOnItemBoxHit &&
            HasTagInParent(hitObject, itemBoxTag))
        {
            return true;
        }

        return false;
    }

    private bool HasTagInParent(
        GameObject hitObject,
        string tagName
    )
    {
        if (hitObject == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag(tagName))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    public void OnCaughtByPlayer()
    {
        if (exploded)
        {
            return;
        }

        isCaughtByPlayer = true;
        isThrownByPlayer = false;

        thrownTimer = 0f;
        playerDirectHitProcessed = false;

        StoreOriginalSettings();

        homing = false;
        target = null;
        speed = 0f;
        explodeOnHit = false;
        useTimeExplosion = false;

        /*
         * 掴んでいる最中もColliderはTriggerのまま。
         * プレイヤー自身を押さないようにする。
         */
        SetEnemyMissileTriggerState(true);

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Debug.Log(
            "Missile: 掴まれたのでミサイル処理を停止しました"
        );
    }

    public void OnThrownByPlayer()
    {
        if (exploded)
        {
            return;
        }

        isCaughtByPlayer = false;
        isThrownByPlayer = true;

        thrownTimer = 0f;
        playerDirectHitProcessed = false;

        homing = false;
        target = null;
        explodeOnHit = false;
        useTimeExplosion = false;

        /*
         * プレイヤーが投げた後は、
         * Prefab本来のCollider設定へ戻す。
         */
        RestoreOriginalColliderTriggerStates();

        if (rb != null)
        {
            rb.useGravity = originalUseGravity;
            rb.isKinematic = false;
            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
        }

        Debug.Log(
            "Missile: 投げられたので投擲物扱いにしました"
        );
    }

    public bool CanAffectBoss()
    {
        return isThrownByPlayer &&
               !isCaughtByPlayer &&
               !exploded;
    }

    public void ExplodeByBossHit()
    {
        if (exploded)
        {
            return;
        }

        PlayBossHitEffect();
        ExplodeWithoutPlayerDamage();
    }

    private void ExplodeWithoutPlayerDamage()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        StopMissilePhysics();
        DisableAllMissileColliders();

        SpawnExplosionEffect();

        Destroy(
            gameObject,
            Mathf.Max(0f, destroyDelayAfterExplosion)
        );
    }

    public void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        StopMissilePhysics();
        DisableAllMissileColliders();

        SpawnExplosionEffect();

        if (!isCaughtByPlayer && !isThrownByPlayer)
        {
            DamagePlayerInRadius();
        }

        Destroy(
            gameObject,
            Mathf.Max(0f, destroyDelayAfterExplosion)
        );
    }

    private void StopMissilePhysics()
    {
        if (rb == null)
        {
            return;
        }

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    private void CacheMissileColliders()
    {
        missileColliders =
            GetComponentsInChildren<Collider>(true);

        if (missileColliders == null)
        {
            missileColliders =
                Array.Empty<Collider>();
        }

        originalColliderTriggerStates =
            new bool[missileColliders.Length];

        for (int i = 0;
             i < missileColliders.Length;
             i++)
        {
            Collider missileCollider =
                missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            originalColliderTriggerStates[i] =
                missileCollider.isTrigger;
        }
    }

    private void SetEnemyMissileTriggerState(
        bool triggerState
    )
    {
        if (!preventPhysicalPushWhileEnemyMissile)
        {
            return;
        }

        if (missileColliders == null)
        {
            CacheMissileColliders();
        }

        for (int i = 0;
             i < missileColliders.Length;
             i++)
        {
            Collider missileCollider =
                missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            missileCollider.isTrigger = triggerState;
        }
    }

    private void RestoreOriginalColliderTriggerStates()
    {
        if (missileColliders == null ||
            originalColliderTriggerStates == null)
        {
            return;
        }

        int count = Mathf.Min(
            missileColliders.Length,
            originalColliderTriggerStates.Length
        );

        for (int i = 0; i < count; i++)
        {
            Collider missileCollider =
                missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            missileCollider.isTrigger =
                originalColliderTriggerStates[i];
        }
    }

    private void DisableAllMissileColliders()
    {
        if (missileColliders == null)
        {
            return;
        }

        for (int i = 0;
             i < missileColliders.Length;
             i++)
        {
            Collider missileCollider =
                missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            missileCollider.enabled = false;
        }
    }

    private bool IsTailCatchCollider(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (IsTailCatchObject(other.gameObject))
        {
            return true;
        }

        TailCollisionDetector detector =
            other.GetComponent<TailCollisionDetector>();

        if (detector == null)
        {
            detector =
                other.GetComponentInParent<TailCollisionDetector>();
        }

        if (detector == null)
        {
            detector =
                other.GetComponentInChildren<TailCollisionDetector>();
        }

        return detector != null;
    }

    private bool IsTailCatchObject(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        int tailLayer =
            LayerMask.NameToLayer(tailLayerName);

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (tailLayer >= 0 &&
                current.gameObject.layer == tailLayer)
            {
                return true;
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

    private bool IsPlayerObject(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        if (IsTailCatchObject(hitObject))
        {
            return false;
        }

        return FindPlayerTransform(hitObject) != null;
    }

    private void PlayBossHitEffect()
    {
        if (!EffectManager.IsInitialized)
        {
            Debug.LogWarning(
                "EffectManager.Instanceが見つからないため、" +
                "ミサイル命中Hit2を再生できません"
            );

            return;
        }

        EffectManager.Instance.Play(
            EffectType.Hit2,
            transform.position
        );
    }

    private void SpawnExplosionEffect()
    {
        if (!EffectManager.IsInitialized)
        {
            Debug.LogWarning(
                "EffectManager.Instanceが見つからないため、" +
                "ミサイル爆発エフェクトを再生できません"
            );

            return;
        }

        EffectManager.Instance.Play(
            EffectType.Explosion_Missile,
            transform.position
        );
    }

    private void DamagePlayerInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        Transform damagedPlayer = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (hit == null)
            {
                continue;
            }

            if (IsTailCatchCollider(hit))
            {
                continue;
            }

            if (!IsPlayerObject(hit.gameObject))
            {
                continue;
            }

            Transform playerTransform =
                FindPlayerTransform(hit.gameObject);

            if (playerTransform == null)
            {
                continue;
            }

            /*
             * プレイヤーに複数Colliderがあっても、
             * 爆発1回につきダメージは1回だけ。
             */
            if (damagedPlayer == playerTransform)
            {
                continue;
            }

            damagedPlayer = playerTransform;

            SendDamageOnceToPlayer(
                playerTransform.gameObject
            );
        }
    }

    public void SetMissileSetting(
        Transform newTarget,
        float newScale,
        float newSpeed,
        float newRotateSpeed,
        float newExplosionTime,
        bool newExplodeOnHit,
        bool newExplodeOnlyPlayerHit,
        float newExplosionRadius,
        int newDamage,
        GameObject newExplosionEffectPrefab,
        float newExplosionEffectScaleMultiplier,
        bool newHoming,
        bool newUseGravity
    )
    {
        if (isCaughtByPlayer || isThrownByPlayer)
        {
            return;
        }

        target = newTarget;
        missileScale = newScale;
        speed = newSpeed;
        rotateSpeed = newRotateSpeed;

        explosionTime = newExplosionTime;
        minimumExplosionDelay = newExplosionTime;

        explodeOnHit = newExplodeOnHit;
        explodeOnPlayerHit = newExplodeOnlyPlayerHit;
        explodeOnGroundHit = true;
        explodeOnItemBoxHit = true;

        explosionRadius = newExplosionRadius;
        damage = newDamage;

        explosionEffectScaleMultiplier =
            newExplosionEffectScaleMultiplier;

        homing = newHoming;
        useGravity = newUseGravity;

        transform.localScale =
            Vector3.one * missileScale;

        timer = 0f;
        exploded = false;
        playerDirectHitProcessed = false;

        EnableAllMissileColliders();
        SetEnemyMissileTriggerState(true);

        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.collisionDetectionMode =
                CollisionDetectionMode.ContinuousDynamic;
        }

        StoreOriginalSettings();

        if (target != null)
        {
            Vector3 targetDirection =
                target.position - transform.position;

            if (targetDirection.sqrMagnitude > 0.001f)
            {
                RotateNoseToDirection(
                    targetDirection.normalized,
                    true
                );
            }
        }
    }

    private void EnableAllMissileColliders()
    {
        if (missileColliders == null)
        {
            CacheMissileColliders();
        }

        for (int i = 0;
             i < missileColliders.Length;
             i++)
        {
            Collider missileCollider =
                missileColliders[i];

            if (missileCollider == null)
            {
                continue;
            }

            missileCollider.enabled = true;
        }
    }

    private void StoreOriginalSettings()
    {
        originalHoming = homing;
        originalExplodeOnHit = explodeOnHit;
        originalExplodeOnPlayerHit =
            explodeOnPlayerHit;

        originalExplodeOnGroundHit =
            explodeOnGroundHit;

        originalExplodeOnItemBoxHit =
            explodeOnItemBoxHit;

        originalUseGravity = useGravity;
        originalUseTimeExplosion =
            useTimeExplosion;

        originalSpeed = speed;
        originalTarget = target;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showExplosionRadius)
        {
            return;
        }

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            explosionRadius
        );

        Gizmos.color = Color.blue;

        Gizmos.DrawLine(
            transform.position,
            transform.position +
            GetWorldNoseDirection() * 2f
        );
    }
}