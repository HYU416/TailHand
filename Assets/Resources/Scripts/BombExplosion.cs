using UnityEngine;

public class BombExplosion : MonoBehaviour
{
    [Header("爆発までの時間")]
    public float explosionTime = 7.0f;

    [Header("爆発の範囲")]
    public float explosionRadius = 3.0f;

    [Header("プレイヤーへのダメージ")]
    public int damage = 20;

    [Header("爆発エフェクトPrefab")]
    public GameObject explosionEffectPrefab;

    [Header("爆発エフェクトの大きさ倍率")]
    public float explosionEffectScaleMultiplier = 1.0f;

    private bool hasExploded = false;

    void Start()
    {
        Invoke(nameof(Explode), explosionTime);
    }

    void Explode()
    {
        if (hasExploded) return;

        hasExploded = true;

        Debug.Log("BOM爆発: " + gameObject.name);

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
            Debug.LogWarning("Explosion Effect Prefab が設定されていません");
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("プレイヤーが爆発範囲に入りました");

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
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}