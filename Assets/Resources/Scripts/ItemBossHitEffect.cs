using UnityEngine;

/// <summary>
/// Flint・Obsidian・Rubbleなどのアイテム専用。
/// ボスの壁またはコアに接触した位置へHit2エフェクトを再生します。
/// </summary>
public class ItemBossHitEffect : MonoBehaviour
{
    private const string BossWallTag = "BossWall";
    private const string BossCoreTag = "BossCore";

    private bool hasPlayedHitEffect;

    private void OnEnable()
    {
        // オブジェクトプールなどで再利用された場合に備えて初期化
        hasPlayedHitEffect = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPlayedHitEffect)
        {
            return;
        }

        if (collision == null || collision.gameObject == null)
        {
            return;
        }

        if (!IsBossTarget(collision.gameObject))
        {
            return;
        }

        Vector3 effectPosition = transform.position;

        if (collision.contactCount > 0)
        {
            effectPosition = collision.GetContact(0).point;
        }

        PlayHitEffect(effectPosition);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPlayedHitEffect)
        {
            return;
        }

        if (other == null)
        {
            return;
        }

        if (!IsBossTarget(other.gameObject))
        {
            return;
        }

        Vector3 effectPosition = other.ClosestPoint(transform.position);

        PlayHitEffect(effectPosition);
    }

    /// <summary>
    /// 接触したオブジェクト、またはその親に
    /// BossWall・BossCoreタグが付いているか確認します。
    /// </summary>
    private bool IsBossTarget(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return false;
        }

        Transform current = hitObject.transform;

        while (current != null)
        {
            if (HasTargetTag(current.gameObject))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool HasTargetTag(GameObject target)
    {
        if (target == null)
        {
            return false;
        }

        // タグがUnity側に登録されていない場合の例外を避けるため文字列で比較
        string targetTag = target.tag;

        return targetTag == BossWallTag ||
               targetTag == BossCoreTag;
    }

    private void PlayHitEffect(Vector3 effectPosition)
    {
        if (hasPlayedHitEffect)
        {
            return;
        }

        hasPlayedHitEffect = true;

        if (!EffectManager.IsInitialized)
        {
            Debug.LogWarning(
                "EffectManager.Instanceが見つからないため、" +
                "アイテム命中エフェクトを再生できません: " +
                gameObject.name
            );

            return;
        }

        EffectManager.Instance.Play(
            EffectType.Hit2,
            effectPosition
        );

        Debug.Log(
            "アイテムがボスに命中したためHit2を再生しました: " +
            gameObject.name
        );
    }
}