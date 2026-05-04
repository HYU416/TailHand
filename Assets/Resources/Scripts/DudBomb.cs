using UnityEngine;

public class DudBomb : MonoBehaviour
{
    [Header("通常時に消えるまでの時間")]
    public float lifeTime = 99.0f;

    [Header("誘爆を有効にする")]
    public bool enableChainExplosion = true;

    [Header("誘爆時の爆発範囲")]
    public float explosionRadius = 3.0f;

    [Header("誘爆時のダメージ")]
    public int damage = 20;

    [Header("誘爆エフェクトPrefab")]
    public GameObject explosionEffectPrefab;

    [Header("爆発エフェクトの大きさ倍率")]
    public float explosionEffectScaleMultiplier = 1.0f;

    [Header("誘爆判定に使うタグ名")]
    public string explosionEffectTag = "ExplosionEffect";

    private bool hasExploded = false;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enableChainExplosion) return;
        if (hasExploded) return;

        if (other.CompareTag(explosionEffectTag))
        {
            ChainExplode();
        }
    }

    void ChainExplode()
    {
        if (hasExploded) return;

        hasExploded = true;

        Debug.Log("不発弾が爆発エフェクトに触れて誘爆しました");

        if (explosionEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                explosionEffectPrefab,
                transform.position,
                Quaternion.identity
            );

            effect.transform.localScale *= explosionEffectScaleMultiplier;

            BombEffect bombEffect = effect.GetComponent<BombEffect>();

            if (bombEffect == null)
            {
                bombEffect = effect.GetComponentInChildren<BombEffect>();
            }

            if (bombEffect != null)
            {
                bombEffect.maxScale *= explosionEffectScaleMultiplier;
            }
        }
        else
        {
            Debug.LogWarning("不発弾の Explosion Effect Prefab が設定されていません");
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("プレイヤーが不発弾の誘爆に当たりました");

                //PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();

                //if (playerHealth != null)
                //{
                //    playerHealth.TakeDamage(damage);
                //}
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}