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

    [Header("ボス壁タグ")]
    public string bossWallTag = "BossWall";

    [Header("ボスコアタグ")]
    public string bossCoreTag = "BossCore";

    [Header("プレイヤーが投げた時だけボスに有効")]
    [SerializeField] private bool requirePlayerThrowForBossHit = true;

    [Header("未投げ状態でボス壁に当たった時は無視する")]
    [SerializeField] private bool ignoreBossHitWhenNotThrown = true;

    [Header("壁・コアに当たったら対象も消す")]
    [SerializeField] private bool destroyBossTargetOnHit = true;

    [Header("親にタグが付いている場合は親を対象にする")]
    [SerializeField] private bool useTaggedParentObject = true;

    private bool hasExploded = false;
    private bool wasThrownByPlayer = false;
    private DudBombState dudBombState;

    public bool HasExploded
    {
        get { return hasExploded; }
    }

    public bool IsThrownByPlayer
    {
        get
        {
            if (dudBombState != null)
            {
                return dudBombState.IsThrownByPlayer;
            }

            return wasThrownByPlayer;
        }
    }

    private void Awake()
    {
        dudBombState = GetComponent<DudBombState>();

        if (dudBombState == null)
        {
            dudBombState = GetComponentInChildren<DudBombState>();
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasExploded) return;

        if (enableChainExplosion && IsSameTag(other.gameObject, explosionEffectTag))
        {
            Explode("不発弾が爆発エフェクトに触れて誘爆しました");
            return;
        }

        CheckBossHit(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasExploded) return;

        CheckBossHit(collision.gameObject);
    }

    public void ArmByPlayerThrow()
    {
        wasThrownByPlayer = true;

        if (dudBombState != null)
        {
            dudBombState.MarkThrownByPlayer();
        }

        Debug.Log("不発弾がプレイヤー投げ状態になりました: " + gameObject.name);
    }

    public void ClearPlayerThrow()
    {
        wasThrownByPlayer = false;

        if (dudBombState != null)
        {
            dudBombState.ClearThrownByPlayer();
        }

        Debug.Log("不発弾のプレイヤー投げ状態を解除しました: " + gameObject.name);
    }

    public void ExplodeByBossHit()
    {
        if (hasExploded) return;

        if (!CanAffectBoss())
        {
            Debug.Log("不発弾はボスに当たりましたが、プレイヤーが投げた物ではないため無視しました: " + gameObject.name);
            return;
        }

        Explode("不発弾がボスの壁またはコアに当たって爆発しました");
    }

    private void CheckBossHit(GameObject hitObject)
    {
        if (hitObject == null) return;

        GameObject bossTarget = FindBossTargetObject(hitObject);

        if (bossTarget == null) return;

        if (!CanAffectBoss())
        {
            if (ignoreBossHitWhenNotThrown)
            {
                Debug.Log("未投げの不発弾がボス壁またはコアに当たりましたが、無視しました: " + gameObject.name);
                return;
            }
        }

        if (destroyBossTargetOnHit)
        {
            Debug.Log("プレイヤーが投げた不発弾が当たったため、ボス壁またはコアを消します: " + bossTarget.name);
            Destroy(bossTarget);
        }

        Explode("不発弾がボスの壁またはコアに当たって爆発しました");
    }

    public bool CanAffectBoss()
    {
        if (!requirePlayerThrowForBossHit)
        {
            return true;
        }

        return IsThrownByPlayer;
    }

    private GameObject FindBossTargetObject(GameObject hitObject)
    {
        if (hitObject == null) return null;

        if (IsSameTag(hitObject, bossWallTag) || IsSameTag(hitObject, bossCoreTag))
        {
            return hitObject;
        }

        if (!useTaggedParentObject) return null;

        Transform current = hitObject.transform.parent;

        while (current != null)
        {
            GameObject parentObject = current.gameObject;

            if (IsSameTag(parentObject, bossWallTag) || IsSameTag(parentObject, bossCoreTag))
            {
                return parentObject;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsSameTag(GameObject target, string tagName)
    {
        if (target == null) return false;
        if (string.IsNullOrEmpty(tagName)) return false;

        // CompareTagはタグ未登録だとエラーになるため、ここでは安全に文字列比較する
        return target.tag == tagName;
    }

    private void Explode(string logMessage)
    {
        if (hasExploded) return;

        hasExploded = true;

        Debug.Log(logMessage);

        SpawnExplosionEffect();
        CheckExplosionHit();

        Destroy(gameObject);
    }

    private void SpawnExplosionEffect()
    {
        EffectManager.Instance.Play(EffectType.Explosion, transform.position);
        //if (explosionEffectPrefab == null)
        //{
        //    Debug.LogWarning("不発弾の Explosion Effect Prefab が設定されていません");
        //    return;
        //}

        //GameObject effect = Instantiate(
        //    explosionEffectPrefab,
        //    transform.position,
        //    Quaternion.identity
        //);

        //effect.transform.localScale *= explosionEffectScaleMultiplier;

        //BombEffect bombEffect = effect.GetComponent<BombEffect>();

        //if (bombEffect == null)
        //{
        //    bombEffect = effect.GetComponentInChildren<BombEffect>();
        //}

        //if (bombEffect != null)
        //{
        //    bombEffect.maxScale *= explosionEffectScaleMultiplier;
        //}
    }

    private void CheckExplosionHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                Debug.Log("プレイヤーが不発弾の爆発に当たりました");

                // PlayerHealth playerHealth = hit.GetComponent<PlayerHealth>();
                //
                // if (playerHealth != null)
                // {
                //     playerHealth.TakeDamage(damage);
                // }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}