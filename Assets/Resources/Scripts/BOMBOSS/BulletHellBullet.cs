/*
 * ==========================================================
 * 制作責任者：小林大悟
 *
 * 弾幕攻撃用の弾本体スクリプトです。
 *
 * 【機能】
 * ・指定方向へ直進
 * ・一定時間後に小さくなりながら消える
 * ・プレイヤーに当たった時のダメージ処理にも対応
 *
 * ※爆弾とは別のPrefabに付けてください。
 * ==========================================================
 */

using UnityEngine;

public class BulletHellBullet : MonoBehaviour
{
    [Header("【移動設定】")]
    [Tooltip("弾の速度です")]
    public float speed = 10.0f;

    [Tooltip("弾の進行方向です")]
    public Vector3 moveDirection = Vector3.forward;

    [Header("【見た目設定】")]
    [Tooltip("弾の大きさです")]
    public float bulletScale = 1.0f;

    [Header("【消滅設定】")]
    [Tooltip("生成されてから消え始めるまでの時間です")]
    public float lifeTime = 3.0f;

    [Tooltip("小さくなりながら消える時間です")]
    public float shrinkTime = 0.5f;

    [Header("【当たり判定設定】")]
    [Tooltip("ONにすると、プレイヤーに当たった時に消えます")]
    public bool destroyOnPlayerHit = true;

    [Tooltip("プレイヤーに与えるダメージです")]
    public int damage = 10;

    [Tooltip("プレイヤーのタグ名です")]
    public string playerTag = "Player";

    private Rigidbody rb;
    private float timer = 0f;
    private bool shrinking = false;
    private Vector3 startScale;

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
        moveDirection.Normalize();

        transform.localScale = Vector3.one * bulletScale;
        startScale = transform.localScale;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.linearVelocity = moveDirection * speed;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (rb == null)
        {
            transform.position += moveDirection * speed * Time.deltaTime;
        }

        if (!shrinking && timer >= lifeTime)
        {
            shrinking = true;
            timer = 0f;
        }

        if (shrinking)
        {
            UpdateShrink();
        }
    }

    void UpdateShrink()
    {
        if (shrinkTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        float t = timer / shrinkTime;
        t = Mathf.Clamp01(t);

        transform.localScale = Vector3.Lerp(
            startScale,
            Vector3.zero,
            t
        );

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        CheckHit(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    void CheckHit(GameObject hitObject)
    {
        if (!hitObject.CompareTag(playerTag)) return;

        hitObject.SendMessage(
            "TakeDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        hitObject.SendMessage(
            "Damage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        hitObject.SendMessage(
            "ApplyDamage",
            damage,
            SendMessageOptions.DontRequireReceiver
        );

        if (destroyOnPlayerHit)
        {
            Destroy(gameObject);
        }
    }

    public void SetBulletData(
        Vector3 newDirection,
        float newSpeed,
        float newScale,
        float newLifeTime,
        float newShrinkTime,
        int newDamage,
        bool newDestroyOnPlayerHit
    )
    {
        moveDirection = newDirection.normalized;
        speed = newSpeed;
        bulletScale = newScale;
        lifeTime = newLifeTime;
        shrinkTime = newShrinkTime;
        damage = newDamage;
        destroyOnPlayerHit = newDestroyOnPlayerHit;

        transform.localScale = Vector3.one * bulletScale;
        startScale = transform.localScale;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.linearVelocity = moveDirection * speed;
        }
    }
}