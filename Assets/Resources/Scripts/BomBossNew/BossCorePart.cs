using UnityEngine;

public class BossCorePart : MonoBehaviour
{
    [Header("管理元")]
    [SerializeField] private BossPhaseController bossController;

    [Header("壁が壊れるまでコアを攻撃不可にする")]
    [SerializeField] private bool requireArmorBroken = true;

    [Header("テスト用：不発弾がコアに当たったら一撃で段階破壊")]
    [SerializeField] private bool dudBombOneShotCore = true;

    [Header("通常時：不発弾でコアに与えるダメージ")]
    [SerializeField] private int dudBombCoreDamage = 1;

    [Header("一撃破壊時に与えるダメージ")]
    [SerializeField] private int oneShotCoreDamage = 999;

    [Header("未投げの不発弾はコアに効かない")]
    [SerializeField] private bool requireThrownDudBomb = true;

    [Header("Flint / Obsidian / Rubble が当たったらコアにもダメージを与える")]
    [SerializeField] private bool damageCoreByItemHit = true;

    [Header("アイテム命中時、コアを一撃で壊す")]
    [SerializeField] private bool itemOneShotCore = true;

    [Header("一撃で壊さない場合のアイテムダメージ")]
    [SerializeField] private int itemCoreDamage = 1;

    [Header("デバッグログ")]
    [SerializeField] private bool showDebugLog = true;

    private bool isAttackable;

    public void Setup(BossPhaseController controller)
    {
        bossController = controller;
        isAttackable = !requireArmorBroken;
    }

    public void SetAttackable(bool value)
    {
        isAttackable = value;

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " コア攻撃可能状態: " + isAttackable);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null)
        {
            return;
        }

        CheckHit(collision.gameObject, collision.collider, collision.rigidbody);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckHit(
            other != null ? other.gameObject : null,
            other,
            other != null ? other.attachedRigidbody : null
        );
    }

    private void CheckHit(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        if (damageCoreByItemHit)
        {
            GameObject itemObject = FindDestroyableItemObject(hitObject, hitCollider, hitRigidbody);

            if (itemObject != null)
            {
                if (showDebugLog)
                {
                    Debug.Log("アイテムがコアに当たりました。アイテムを消してコアにダメージを与えます: " + itemObject.name);
                }

                Destroy(itemObject);

                if (!isAttackable)
                {
                    if (showDebugLog)
                    {
                        Debug.Log("アイテムがコアに当たりましたが、コアはまだ攻撃不可です。");
                    }

                    return;
                }

                if (itemOneShotCore)
                {
                    HitCore(oneShotCoreDamage);
                }
                else
                {
                    HitCore(itemCoreDamage);
                }

                return;
            }
        }

        DudBomb dudBomb = FindDudBomb(hitObject, hitCollider, hitRigidbody);
        BossBombHitMarker bombMarker = FindBombMarker(hitObject, hitCollider, hitRigidbody);

        if (dudBomb == null && bombMarker == null)
        {
            if (showDebugLog && hitObject != null)
            {
                Debug.Log("コアに何か当たったが、不発弾でも爆弾マーカーでもありません: " + hitObject.name);
            }

            return;
        }

        if (showDebugLog)
        {
            string hitName = hitObject != null ? hitObject.name : "null";
            Debug.Log(
                "コアに命中判定: " + hitName +
                " / DudBomb: " + (dudBomb != null) +
                " / Marker: " + (bombMarker != null) +
                " / 攻撃可能: " + isAttackable
            );
        }

        if (dudBomb != null)
        {
            if (requireThrownDudBomb && !dudBomb.CanAffectBoss())
            {
                if (showDebugLog)
                {
                    Debug.Log("未投げの不発弾がコアに当たりましたが、コアダメージを無視しました");
                }

                return;
            }

            if (!isAttackable)
            {
                if (showDebugLog)
                {
                    Debug.Log("不発弾がコアに当たりましたが、コアはまだ攻撃不可です。");
                }

                dudBomb.ExplodeByBossHit();
                return;
            }

            int damage = dudBombCoreDamage;

            if (dudBombOneShotCore)
            {
                damage = oneShotCoreDamage;
            }
            else if (bombMarker != null)
            {
                damage = bombMarker.CoreDamage;
            }

            if (damage <= 0)
            {
                if (showDebugLog)
                {
                    Debug.Log("不発弾のコアダメージが0以下なので無視しました");
                }

                return;
            }

            if (showDebugLog)
            {
                Debug.Log("プレイヤーが投げた不発弾がコアに命中。コアにダメージ: " + damage);
            }

            HitCore(damage);
            dudBomb.ExplodeByBossHit();
            return;
        }

        if (bombMarker != null)
        {
            int damage = bombMarker.CoreDamage;

            if (damage <= 0)
            {
                if (showDebugLog)
                {
                    Debug.Log("爆弾マーカーのコアダメージが0以下なので無視しました");
                }

                return;
            }

            if (!isAttackable)
            {
                if (showDebugLog)
                {
                    Debug.Log("爆弾マーカーがコアに当たりましたが、コアはまだ攻撃不可です。");
                }

                return;
            }

            if (showDebugLog)
            {
                Debug.Log("爆弾マーカーがコアに命中。コアにダメージ: " + damage);
            }

            HitCore(damage);
            bombMarker.Consume();
        }
    }

    private GameObject FindDestroyableItemObject(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        GameObject target = null;

        if (hitRigidbody != null)
        {
            target = hitRigidbody.gameObject;

            if (IsDestroyableItemName(target.name))
            {
                return target;
            }
        }

        if (hitObject != null)
        {
            target = hitObject;

            if (IsDestroyableItemName(target.name))
            {
                return target;
            }

            Transform parent = hitObject.transform.parent;

            while (parent != null)
            {
                if (IsDestroyableItemName(parent.name))
                {
                    return parent.gameObject;
                }

                parent = parent.parent;
            }
        }

        if (hitCollider != null)
        {
            target = hitCollider.gameObject;

            if (IsDestroyableItemName(target.name))
            {
                return target;
            }

            Transform parent = hitCollider.transform.parent;

            while (parent != null)
            {
                if (IsDestroyableItemName(parent.name))
                {
                    return parent.gameObject;
                }

                parent = parent.parent;
            }
        }

        return null;
    }

    private bool IsDestroyableItemName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string fixedName = objectName.Replace("(Clone)", "").Trim();

        return fixedName == "Flint" ||
               fixedName == "Obsidian" ||
               fixedName == "Rubble";
    }

    private DudBomb FindDudBomb(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        DudBomb dudBomb = null;

        if (hitObject != null)
        {
            dudBomb = hitObject.GetComponent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitObject.GetComponentInParent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitObject.GetComponentInChildren<DudBomb>();
            if (dudBomb != null) return dudBomb;
        }

        if (hitCollider != null)
        {
            dudBomb = hitCollider.GetComponent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitCollider.GetComponentInParent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitCollider.GetComponentInChildren<DudBomb>();
            if (dudBomb != null) return dudBomb;
        }

        if (hitRigidbody != null)
        {
            dudBomb = hitRigidbody.GetComponent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitRigidbody.GetComponentInParent<DudBomb>();
            if (dudBomb != null) return dudBomb;

            dudBomb = hitRigidbody.GetComponentInChildren<DudBomb>();
            if (dudBomb != null) return dudBomb;
        }

        return null;
    }

    private BossBombHitMarker FindBombMarker(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        BossBombHitMarker marker = null;

        if (hitObject != null)
        {
            marker = hitObject.GetComponent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitObject.GetComponentInParent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitObject.GetComponentInChildren<BossBombHitMarker>();
            if (marker != null) return marker;
        }

        if (hitCollider != null)
        {
            marker = hitCollider.GetComponent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitCollider.GetComponentInParent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitCollider.GetComponentInChildren<BossBombHitMarker>();
            if (marker != null) return marker;
        }

        if (hitRigidbody != null)
        {
            marker = hitRigidbody.GetComponent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitRigidbody.GetComponentInParent<BossBombHitMarker>();
            if (marker != null) return marker;

            marker = hitRigidbody.GetComponentInChildren<BossBombHitMarker>();
            if (marker != null) return marker;
        }

        return null;
    }

    private void HitCore(int damage)
    {
        if (damage <= 0)
        {
            return;
        }

        if (bossController != null)
        {
            bossController.OnCoreHit(damage);
        }
        else
        {
            Debug.LogWarning("BossCorePart: bossController が設定されていません");
        }
    }
}