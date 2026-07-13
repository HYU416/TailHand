using UnityEngine;

public class ThrowableBomb : MonoBehaviour
{
    [Header("ボス壁タグ")]
    [SerializeField] private string bossWallTag = "BossWall";

    [Header("ボスコアタグ")]
    [SerializeField] private string bossCoreTag = "BossCore";

    [Header("プレイヤーが投げた時だけボスに有効")]
    [SerializeField] private bool requirePlayerThrowForBossHit = true;

    [Header("Trigger Colliderではボス命中扱いにしない")]
    [Tooltip("ONの場合、Is Triggerが有効なColliderでは爆発せず、そのまま通過します")]
    [SerializeField] private bool ignoreTriggerForBossHit = true;

    private ThrownBombState thrownBombState;
    private BombExplosion bombExplosion;

    public bool IsThrownByPlayer
    {
        get
        {
            if (thrownBombState == null)
            {
                return false;
            }

            return thrownBombState.IsThrownByPlayer;
        }
    }

    public bool HasExploded
    {
        get
        {
            if (bombExplosion == null)
            {
                return false;
            }

            return bombExplosion.HasExploded;
        }
    }

    private void Awake()
    {
        thrownBombState = GetComponent<ThrownBombState>();

        if (thrownBombState == null)
        {
            thrownBombState = GetComponentInChildren<ThrownBombState>();
        }

        bombExplosion = GetComponent<BombExplosion>();

        if (bombExplosion == null)
        {
            bombExplosion = GetComponentInChildren<BombExplosion>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        Collider hitCollider = collision.collider;

        if (hitCollider == null)
        {
            return;
        }

        CheckBossHit(hitCollider);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
        {
            return;
        }

        if (ignoreTriggerForBossHit && other.isTrigger)
        {
            return;
        }

        CheckBossHit(other);
    }

    public void ArmByPlayerThrow()
    {
        if (thrownBombState != null)
        {
            thrownBombState.MarkThrownByPlayer();
        }

        Debug.Log(
            "ThrowableBomb: プレイヤー投げ状態になりました: " +
            gameObject.name,
            this
        );
    }

    public bool CanAffectBoss()
    {
        if (!requirePlayerThrowForBossHit)
        {
            return true;
        }

        return IsThrownByPlayer;
    }

    public void ExplodeByBossHit()
    {
        if (bombExplosion == null)
        {
            Debug.LogWarning(
                "ThrowableBomb: BombExplosionが見つかりません: " +
                gameObject.name,
                this
            );

            Destroy(gameObject);
            return;
        }

        if (bombExplosion.HasExploded)
        {
            return;
        }

        if (!CanAffectBoss())
        {
            Debug.Log(
                "爆弾はボスに当たりましたが、" +
                "プレイヤーが投げた物ではないため無視しました: " +
                gameObject.name,
                this
            );

            return;
        }

        bombExplosion.ExplodeByBossHit();
    }

    private void CheckBossHit(Collider hitCollider)
    {
        if (hitCollider == null)
        {
            return;
        }

        if (ignoreTriggerForBossHit && hitCollider.isTrigger)
        {
            return;
        }

        if (bombExplosion != null && bombExplosion.HasExploded)
        {
            return;
        }

        GameObject bossTarget =
            FindBossTargetObject(hitCollider.gameObject);

        if (bossTarget == null)
        {
            return;
        }

        ExplodeByBossHit();
    }

    private GameObject FindBossTargetObject(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return null;
        }

        if (IsSameTag(hitObject, bossWallTag) ||
            IsSameTag(hitObject, bossCoreTag))
        {
            return hitObject;
        }

        Transform current = hitObject.transform.parent;

        while (current != null)
        {
            if (IsSameTag(current.gameObject, bossWallTag) ||
                IsSameTag(current.gameObject, bossCoreTag))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsSameTag(GameObject target, string tagName)
    {
        if (target == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(tagName))
        {
            return false;
        }

        return target.CompareTag(tagName);
    }
}