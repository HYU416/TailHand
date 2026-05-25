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

    [Tooltip("ONにすると、何かにぶつかった時に爆発します")]
    public bool explodeOnHit = true;

    [Tooltip("ONにすると、プレイヤーにぶつかった時だけ爆発します")]
    public bool explodeOnlyPlayerHit = false;

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

    private Rigidbody rb;
    private bool exploded = false;
    private float timer = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }
    }

    void Start()
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
    }

    void Update()
    {
        if (exploded) return;

        timer += Time.deltaTime;

        if (timer >= explosionTime)
        {
            Explode();
            return;
        }

        MoveMissile();
    }

    void MoveMissile()
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

    void RotateNoseToDirection(Vector3 direction, bool instant)
    {
        if (direction.sqrMagnitude <= 0.001f) return;

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

    Vector3 GetWorldNoseDirection()
    {
        Vector3 localDirection = GetLocalNoseDirection();

        return transform.TransformDirection(localDirection).normalized;
    }

    Vector3 GetLocalNoseDirection()
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

    void OnCollisionEnter(Collision collision)
    {
        if (!explodeOnHit) return;
        if (exploded) return;

        if (explodeOnlyPlayerHit)
        {
            if (!collision.gameObject.CompareTag(playerTag))
            {
                return;
            }
        }

        Explode();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!explodeOnHit) return;
        if (exploded) return;

        if (explodeOnlyPlayerHit)
        {
            if (!other.CompareTag(playerTag))
            {
                return;
            }
        }

        Explode();
    }

    public void Explode()
    {
        if (exploded) return;

        exploded = true;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        SpawnExplosionEffect();
        DamagePlayerInRadius();

        Destroy(gameObject, destroyDelayAfterExplosion);
    }

    void SpawnExplosionEffect()
    {
        EffectManager.Instance.Play(EffectType.Explosion_Missile, transform.position);   
    }

    void DamagePlayerInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            explosionRadius
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (!hit.CompareTag(playerTag)) continue;

            SendDamage(hit.gameObject);
        }
    }

    void SendDamage(GameObject targetObject)
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
        target = newTarget;
        missileScale = newScale;
        speed = newSpeed;
        rotateSpeed = newRotateSpeed;
        explosionTime = newExplosionTime;
        explodeOnHit = newExplodeOnHit;
        explodeOnlyPlayerHit = newExplodeOnlyPlayerHit;
        explosionRadius = newExplosionRadius;
        damage = newDamage;

        //エフェクトの設定がいるなら後で追加
        ///*
        // * ここが重要。
        // * BossBombShooter側からエフェクトPrefabが渡された場合だけ上書きする。
        // * 空の場合は、ミサイルPrefab側に設定してあるエフェクトを残す。
        // */
        //if (newExplosionEffectPrefab != null)
        //{
        //    explosionEffectPrefab = newExplosionEffectPrefab;
        //}



        explosionEffectScaleMultiplier = newExplosionEffectScaleMultiplier;
        homing = newHoming;
        useGravity = newUseGravity;

        transform.localScale = Vector3.one * missileScale;

        if (rb != null)
        {
            rb.useGravity = useGravity;
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
    }

    void OnDrawGizmosSelected()
    {
        if (!showExplosionRadius) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(
            transform.position,
            transform.position + GetWorldNoseDirection() * 2f
        );
    }
}