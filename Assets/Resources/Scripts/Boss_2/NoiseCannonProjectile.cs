using System.Collections.Generic;
using UnityEngine;

public class NoiseCannonProjectile : MonoBehaviour
{
    [System.Serializable]
    public struct FStatus
    {
        [Header("1回あたりの攻撃力")]
        public int atk;

        [Header("移動速度")]
        public float speed;

        [Header("生存時間")]
        public float lifeTime;

        [Header("最大拡大倍率")]
        public float maxScaleMagnification;

        [HideInInspector]
        public Vector3 defaultScale;
    }

    [Header("ステータス")]
    [SerializeField]
    private FStatus status;

    [Header("継続ダメージ設定")]
    [Tooltip("ONにすると、プレイヤーがビームに触れている間、一定間隔でダメージを与えます")]
    [SerializeField]
    private bool useContinuousDamage = true;

    [Tooltip("ダメージを与える間隔です。1なら1秒ごとです")]
    [Min(0.05f)]
    [SerializeField]
    private float damageInterval = 1.0f;

    [Tooltip("ビームに触れた瞬間にもダメージを与えます")]
    [SerializeField]
    private bool damageImmediatelyOnEnter = true;

    [Header("命中後の設定")]
    [Tooltip("ONにすると、プレイヤーへダメージを与えた時点でビームを消します")]
    [SerializeField]
    private bool destroyOnPlayerHit = false;

    [Header("デバッグ")]
    [SerializeField]
    private bool showDebugLog = false;

    private Rigidbody rb;

    private float remainingLifeTime;
    private float totalDeltaTime;

    /*
     * プレイヤーごとに、
     * 次にダメージを与えられる時刻を記録する。
     *
     * プレイヤーに複数Colliderが付いていても、
     * 同じPlayerには一定間隔でしかダメージを与えない。
     */
    private readonly Dictionary<Player, float> nextDamageTimes =
        new Dictionary<Player, float>();

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
        status.defaultScale = transform.localScale;

        remainingLifeTime = status.lifeTime;
        totalDeltaTime = 0f;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (rb.isKinematic)
            {
                rb.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousSpeculative;
            }
            else
            {
                rb.collisionDetectionMode =
                    CollisionDetectionMode.ContinuousDynamic;

                rb.linearVelocity =
                    transform.forward * status.speed;
            }
        }
    }

    private void Update()
    {
        UpdateScale();
        UpdateLifeTime();
    }

    private void FixedUpdate()
    {
        MoveProjectile();
    }

    private void MoveProjectile()
    {
        Vector3 velocity =
            transform.forward * status.speed;

        if (rb == null)
        {
            transform.position +=
                velocity * Time.fixedDeltaTime;

            return;
        }

        if (rb.isKinematic)
        {
            Vector3 nextPosition =
                rb.position +
                velocity * Time.fixedDeltaTime;

            rb.MovePosition(nextPosition);
        }
        else
        {
            rb.linearVelocity = velocity;
        }
    }

    private void UpdateScale()
    {
        totalDeltaTime += Time.deltaTime;

        float scaleRate;

        if (status.lifeTime <= 0f)
        {
            scaleRate = 1f;
        }
        else
        {
            scaleRate = Mathf.Clamp01(
                totalDeltaTime / status.lifeTime
            );
        }

        Vector3 targetScale =
            status.defaultScale *
            status.maxScaleMagnification;

        transform.localScale =
            Vector3.Lerp(
                status.defaultScale,
                targetScale,
                scaleRate
            );
    }

    private void UpdateLifeTime()
    {
        remainingLifeTime -= Time.deltaTime;

        if (remainingLifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Player player = FindPlayer(other);

        if (player == null)
        {
            return;
        }

        float interval =
            Mathf.Max(0.05f, damageInterval);

        /*
         * 初めて接触したプレイヤーを登録する。
         */
        if (!nextDamageTimes.ContainsKey(player))
        {
            if (damageImmediatelyOnEnter)
            {
                ApplyDamage(player);

                nextDamageTimes[player] =
                    Time.time + interval;
            }
            else
            {
                /*
                 * 接触した瞬間には与えず、
                 * 設定時間後に最初のダメージを与える。
                 */
                nextDamageTimes[player] =
                    Time.time + interval;
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!useContinuousDamage)
        {
            return;
        }

        Player player = FindPlayer(other);

        if (player == null)
        {
            return;
        }

        float interval =
            Mathf.Max(0.05f, damageInterval);

        /*
         * OnTriggerEnterを通らずにStayから始まった場合にも対応。
         */
        if (!nextDamageTimes.ContainsKey(player))
        {
            if (damageImmediatelyOnEnter)
            {
                ApplyDamage(player);

                nextDamageTimes[player] =
                    Time.time + interval;
            }
            else
            {
                nextDamageTimes[player] =
                    Time.time + interval;
            }

            return;
        }

        /*
         * 次回ダメージ時刻になるまでは何もしない。
         */
        if (Time.time < nextDamageTimes[player])
        {
            return;
        }

        ApplyDamage(player);

        nextDamageTimes[player] =
            Time.time + interval;
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = FindPlayer(other);

        if (player == null)
        {
            return;
        }

        /*
         * ここでは記録を消さない。
         *
         * プレイヤーに複数Colliderがある場合、
         * 1つのColliderだけがExitした瞬間に記録を消すと、
         * 残っている別のColliderから連続ダメージが入るため。
         *
         * このビームが破棄されればDictionaryも消える。
         */
    }

    private Player FindPlayer(Collider other)
    {
        if (other == null)
        {
            return null;
        }

        Player player =
            other.GetComponent<Player>();

        if (player == null)
        {
            player =
                other.GetComponentInParent<Player>();
        }

        if (player == null &&
            other.attachedRigidbody != null)
        {
            player =
                other.attachedRigidbody
                    .GetComponent<Player>();
        }

        return player;
    }

    private void ApplyDamage(Player player)
    {
        if (player == null)
        {
            return;
        }

        if (showDebugLog)
        {
            Debug.Log(
                "NoiseCannonProjectile: プレイヤーへ継続ダメージ / Damage = " +
                status.atk +
                " / Time = " +
                Time.time.ToString("F2"),
                player
            );
        }

        player.TakeDamage((float)status.atk);

        if (destroyOnPlayerHit)
        {
            Destroy(gameObject);
        }
    }
}