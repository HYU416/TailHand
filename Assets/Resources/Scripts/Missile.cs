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
    [Tooltip("ミサイルモデルのどの軸が先端かを選びます。Y軸上側が先端なら Up_Y にしてください")]
    public MissileNoseAxis noseAxis = MissileNoseAxis.Up_Y;

    [Header("【掴み設定】")]
    [Tooltip("掴んだ時のローカル位置補正です")]
    [SerializeField] private Vector3 catchLocalPositionOffset = Vector3.zero;

    [Tooltip("掴んだ時のローカル回転補正です。横向きにしたい場合はここを調整してください")]
    [SerializeField] private Vector3 catchLocalRotationOffset = Vector3.zero;

    [Tooltip("尻尾の掴み判定レイヤー名です。このレイヤーに当たっても爆発しません")]
    [SerializeField] private string tailLayerName = "Tail";

    [Header("【投げられた後の設定】")]
    [Tooltip("プレイヤーが投げた後、何秒後に自動爆発するかです")]
    [SerializeField] private float explosionTimeAfterPlayerThrow = 3.0f;

    [Tooltip("プレイヤーが投げたミサイルがボス壁に与えるダメージです")]
    [SerializeField] private int armorDamageByPlayerThrow = 999;

    [Tooltip("プレイヤーが投げたミサイルがボスコアに与えるダメージです")]
    [SerializeField] private int coreDamageByPlayerThrow = 999;

    [Header("【移動設定】")]
    [Tooltip("ミサイルの速度です")]
    public float speed = 10f;

    [Tooltip("ミサイルが曲がる速さです。高いほどプレイヤーを強く追います")]
    public float rotateSpeed = 180f;

    [Tooltip("ONにすると、常にプレイヤー方向へ曲がります")]
    public bool homing = true;

    [Tooltip("ONにすると、Rigidbodyの重力を使います")]
    public bool useGravity = false;

    [Tooltip("ミサイルの見た目サイズです")]
    public float missileScale = 1f;

    [Header("【爆発設定】")]
    [Tooltip("生成されてから何秒後に爆発するかです")]
    public float explosionTime = 5f;

    [Tooltip("ONにすると、時間経過で爆発します")]
    public bool useTimeExplosion = true;

    [Tooltip("この秒数が経過するまでは、何に当たっても爆発しません")]
    public float minimumExplosionDelay = 5f;

    [Tooltip("ONにすると、Minimum Explosion Delay 経過時にその場で爆発します")]
    public bool explodeWhenMinimumDelayPassed = true;

    [Tooltip("ONにすると、何かにぶつかった時に爆発判定を行います")]
    public bool explodeOnHit = true;

    [Tooltip("ONにすると、プレイヤーに当たった時も爆発対象にします")]
    public bool explodeOnPlayerHit = false;

    [Tooltip("ONにすると、Groundタグに当たった時に爆発対象にします")]
    public bool explodeOnGroundHit = true;

    [Tooltip("ONにすると、ItemBoxタグに当たった時に爆発対象にします")]
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

    [Tooltip("ONにすると、爆発エフェクトを出した後、指定秒数で自動削除します")]
    public bool destroyExplosionEffect = true;

    [Tooltip("爆発エフェクトを削除するまでの時間です")]
    public float explosionEffectDestroyTime = 3f;

    [Header("【消滅設定】")]
    [Tooltip("爆発後にミサイル本体を消すまでの時間です")]
    public float destroyDelayAfterExplosion = 0f;

    [Header("【デバッグ表示】")]
    [Tooltip("ONにすると、Sceneビューで爆発範囲を表示します")]
    public bool showExplosionRadius = true;

    [Tooltip("ONにすると、何に当たったかログを出します")]
    public bool showHitDebugLog = false;

    private Rigidbody rb;
    private bool exploded;
    private float timer;

    private bool isCaughtByPlayer;
    private bool isThrownByPlayer;
    private float thrownTimer;

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
    }

    private void Start()
    {
        transform.localScale = Vector3.one * missileScale;

        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.isKinematic = false;
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
                targetDirection.Normalize();
                RotateNoseToDirection(targetDirection, true);
            }
        }

        StoreOriginalSettings();
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

            if (explosionTimeAfterPlayerThrow > 0.0f &&
                thrownTimer >= explosionTimeAfterPlayerThrow)
            {
                ExplodeWithoutPlayerDamage();
            }

            return;
        }

        timer += Time.deltaTime;

        if (useTimeExplosion &&
            explodeWhenMinimumDelayPassed &&
            minimumExplosionDelay > 0.0f &&
            timer >= minimumExplosionDelay)
        {
            Explode();
            return;
        }

        if (useTimeExplosion &&
            !explodeWhenMinimumDelayPassed &&
            explosionTime > 0.0f &&
            timer >= explosionTime)
        {
            Explode();
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
                targetDirection.Normalize();
                RotateNoseToDirection(targetDirection, false);
            }
        }

        Vector3 moveDirection = GetWorldNoseDirection();

        if (rb != null)
        {
            rb.linearVelocity = moveDirection * speed;
        }
        else
        {
            transform.position += moveDirection * speed * Time.deltaTime;
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

        Quaternion targetRotation = addRotation * transform.rotation;

        if (instant)
        {
            transform.rotation = targetRotation;
        }
        else
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime
            );
        }
    }

    private Vector3 GetWorldNoseDirection()
    {
        Vector3 localDirection = GetLocalNoseDirection();

        return transform.TransformDirection(localDirection).normalized;
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
                Debug.Log("Missile: 尻尾判定に当たったので無視します / " + hitObject.name, hitObject);
            }

            return;
        }

        if (isThrownByPlayer)
        {
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
                    "Missile: まだ5秒経っていないので爆発しません / Hit = " +
                    hitObject.name +
                    " / Timer = " +
                    timer.ToString("F2"),
                    hitObject
                );
            }

            return;
        }

        bool shouldExplode = IsExplosionTarget(hitObject);

        if (!shouldExplode)
        {
            if (showHitDebugLog)
            {
                Debug.Log("Missile: 爆発対象ではないので無視します / " + hitObject.name, hitObject);
            }

            return;
        }

        Explode();
    }

    private bool IsExplosionTarget(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        if (explodeOnPlayerHit && IsPlayerObject(hitObject))
        {
            return true;
        }

        if (explodeOnGroundHit && HasTagInParent(hitObject, groundTag))
        {
            return true;
        }

        if (explodeOnItemBoxHit && HasTagInParent(hitObject, itemBoxTag))
        {
            return true;
        }

        return false;
    }

    private bool HasTagInParent(GameObject hitObject, string tagName)
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

        StoreOriginalSettings();

        homing = false;
        target = null;
        speed = 0f;
        explodeOnHit = false;
        useTimeExplosion = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        Debug.Log("Missile: 掴まれたので、ミサイル処理を停止してアイテム扱いにしました");
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

        homing = false;
        target = null;
        explodeOnHit = false;
        useTimeExplosion = false;

        if (rb != null)
        {
            rb.useGravity = originalUseGravity;
            rb.isKinematic = false;
        }

        Debug.Log("Missile: 投げられたので、3秒後に自爆する投擲物扱いにしました");
    }

    public bool CanAffectBoss()
    {
        return isThrownByPlayer && !isCaughtByPlayer && !exploded;
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

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        SpawnExplosionEffect();

        Destroy(gameObject, destroyDelayAfterExplosion);
    }

    public void Explode()
    {
        if (exploded)
        {
            return;
        }

        exploded = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        SpawnExplosionEffect();

        if (!isCaughtByPlayer && !isThrownByPlayer)
        {
            DamagePlayerInRadius();
        }

        Destroy(gameObject, destroyDelayAfterExplosion);
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

        TailCollisionDetector tailCollisionDetector = other.GetComponent<TailCollisionDetector>();

        if (tailCollisionDetector == null)
        {
            tailCollisionDetector = other.GetComponentInParent<TailCollisionDetector>();
        }

        if (tailCollisionDetector == null)
        {
            tailCollisionDetector = other.GetComponentInChildren<TailCollisionDetector>();
        }

        return tailCollisionDetector != null;
    }

    private bool IsTailCatchObject(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        int tailLayer = LayerMask.NameToLayer(tailLayerName);

        if (tailLayer >= 0 && hitObject.layer == tailLayer)
        {
            return true;
        }

        TailCollisionDetector tailCollisionDetector = hitObject.GetComponent<TailCollisionDetector>();

        if (tailCollisionDetector == null)
        {
            tailCollisionDetector = hitObject.GetComponentInParent<TailCollisionDetector>();
        }

        if (tailCollisionDetector == null)
        {
            tailCollisionDetector = hitObject.GetComponentInChildren<TailCollisionDetector>();
        }

        return tailCollisionDetector != null;
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

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (current.CompareTag(playerTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void PlayBossHitEffect()
    {
        if (!EffectManager.IsInitialized)
        {
            Debug.LogWarning("EffectManager.Instanceが見つからないため、ミサイル命中Hit2を再生できません");
            return;
        }

        EffectManager.Instance.Play(EffectType.Hit2, transform.position);
    }

    private void SpawnExplosionEffect()
    {
        if (!EffectManager.IsInitialized)
        {
            Debug.LogWarning("EffectManager.Instanceが見つからないため、ミサイル爆発エフェクトを再生できません");
            return;
        }

        EffectManager.Instance.Play(EffectType.Explosion_Missile, transform.position);
    }

    private void DamagePlayerInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

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

            SendDamage(hit.gameObject);
        }
    }

    private void SendDamage(GameObject targetObject)
    {
        targetObject.SendMessage(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        targetObject.SendMessage(
            "Damage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        targetObject.SendMessage(
            "ApplyDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );
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

        explosionEffectScaleMultiplier = newExplosionEffectScaleMultiplier;
        homing = newHoming;
        useGravity = newUseGravity;

        transform.localScale = Vector3.one * missileScale;

        timer = 0f;
        exploded = false;

        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        StoreOriginalSettings();

        if (target != null)
        {
            Vector3 targetDirection = target.position - transform.position;

            if (targetDirection.sqrMagnitude > 0.001f)
            {
                targetDirection.Normalize();
                RotateNoseToDirection(targetDirection, true);
            }
        }
    }

    private void StoreOriginalSettings()
    {
        originalHoming = homing;
        originalExplodeOnHit = explodeOnHit;
        originalExplodeOnPlayerHit = explodeOnPlayerHit;
        originalExplodeOnGroundHit = explodeOnGroundHit;
        originalExplodeOnItemBoxHit = explodeOnItemBoxHit;
        originalUseGravity = useGravity;
        originalUseTimeExplosion = useTimeExplosion;
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
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.position,
            transform.position + GetWorldNoseDirection() * 2f
        );
    }
}