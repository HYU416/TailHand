using UnityEngine;

public class ThrowableBomb : MonoBehaviour
{
    [Header("ボス壁タグ")]
    [SerializeField] private string bossWallTag = "BossWall";

    [Header("ボスコアタグ")]
    [SerializeField] private string bossCoreTag = "BossCore";

    [Header("プレイヤーが投げた時だけボスに有効")]
    [SerializeField] private bool requirePlayerThrowForBossHit = true;

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
        if (collision == null) return;

        CheckBossHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        CheckBossHit(other.gameObject);
    }

    public void ArmByPlayerThrow()
    {
        if (thrownBombState != null)
        {
            thrownBombState.MarkThrownByPlayer();
        }

        Debug.Log("ThrowableBomb: プレイヤー投げ状態になりました: " + gameObject.name);
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
            Debug.LogWarning("ThrowableBomb: BombExplosion が見つかりません: " + gameObject.name);
            Destroy(gameObject);
            return;
        }

        if (bombExplosion.HasExploded)
        {
            return;
        }

        if (!CanAffectBoss())
        {
            Debug.Log("爆弾はボスに当たりましたが、プレイヤーが投げた物ではないため無視しました: " + gameObject.name);
            return;
        }

        bombExplosion.ExplodeByBossHit();
    }

    private void CheckBossHit(GameObject hitObject)
    {
        if (hitObject == null) return;

        if (bombExplosion != null && bombExplosion.HasExploded)
        {
            return;
        }

        GameObject bossTarget = FindBossTargetObject(hitObject);

        if (bossTarget == null)
        {
            return;
        }

        ExplodeByBossHit();
    }

    private GameObject FindBossTargetObject(GameObject hitObject)
    {
        if (hitObject == null) return null;

        if (IsSameTag(hitObject, bossWallTag) || IsSameTag(hitObject, bossCoreTag))
        {
            return hitObject;
        }

        Transform current = hitObject.transform.parent;

        while (current != null)
        {
            if (IsSameTag(current.gameObject, bossWallTag) || IsSameTag(current.gameObject, bossCoreTag))
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private bool IsSameTag(GameObject target, string tagName)
    {
        if (target == null) return false;
        if (string.IsNullOrEmpty(tagName)) return false;

        return target.tag == tagName;
    }
}