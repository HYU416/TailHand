using UnityEngine;

public class BossArmorPart : MonoBehaviour
{
    [Header("管理元")]
    [SerializeField] private BossPhaseController bossController;

    [Header("壁HP")]
    [SerializeField] private int maxHp = 30;

    [Header("プレイヤーが投げた不発弾だけ壁に有効にする")]
    [SerializeField] private bool requireThrownDudBomb = true;

    [Header("プレイヤーが投げた通常爆弾だけ壁に有効にする")]
    [SerializeField] private bool requireThrownNormalBomb = true;

    [Header("破壊時にオブジェクトを非表示にする")]
    [SerializeField] private bool hideOnBroken = true;

    [Header("デバッグログ")]
    [SerializeField] private bool showDebugLog = true;

    private int currentHp;
    private bool isBroken;
    private Renderer[] renderers;
    private Collider[] colliders;

    public bool IsBroken
    {
        get { return isBroken; }
    }

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        currentHp = maxHp;
    }

    public void Setup(BossPhaseController controller, int hp)
    {
        bossController = controller;
        maxHp = hp;
        currentHp = maxHp;
        isBroken = false;

        gameObject.SetActive(true);
        SetVisible(true);
        SetCollidersEnabled(true);

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " 壁セットアップ / HP: " + currentHp);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision == null) return;

        CheckHit(
            collision.gameObject,
            collision.collider,
            collision.rigidbody
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        CheckHit(
            other.gameObject,
            other,
            other.attachedRigidbody
        );
    }

    private void CheckHit(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        if (isBroken) return;

        BossBombHitMarker bombMarker = FindBombMarker(hitObject, hitCollider, hitRigidbody);

        if (bombMarker == null)
        {
            return;
        }

        DudBomb dudBomb = FindDudBomb(hitObject, hitCollider, hitRigidbody);
        ThrowableBomb throwableBomb = FindThrowableBomb(hitObject, hitCollider, hitRigidbody);

        if (dudBomb != null && requireThrownDudBomb)
        {
            if (!dudBomb.CanAffectBoss())
            {
                if (showDebugLog)
                {
                    Debug.Log("投げられていない不発弾なので壁には効きません: " + GetHitName(hitObject));
                }

                return;
            }
        }

        if (throwableBomb != null && requireThrownNormalBomb)
        {
            if (!throwableBomb.CanAffectBoss())
            {
                if (showDebugLog)
                {
                    Debug.Log("投げられていない通常爆弾なので壁には効きません: " + GetHitName(hitObject));
                }

                return;
            }
        }

        int damage = bombMarker.ArmorDamage;

        if (damage <= 0)
        {
            if (showDebugLog)
            {
                Debug.Log("壁ダメージが0以下なので無視しました: " + gameObject.name);
            }

            return;
        }

        TakeDamage(damage);

        if (dudBomb != null)
        {
            dudBomb.ExplodeByBossHit();
            return;
        }

        if (throwableBomb != null)
        {
            throwableBomb.ExplodeByBossHit();
            return;
        }

        bombMarker.Consume();
    }

    private void TakeDamage(int damage)
    {
        if (isBroken) return;

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " 壁HP: " + currentHp + " / " + maxHp + " / Damage: " + damage);
        }

        if (currentHp <= 0)
        {
            BreakArmor();
        }
    }

    public void BreakArmor()
    {
        if (isBroken) return;

        isBroken = true;

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " が破壊されました");
        }

        if (bossController != null)
        {
            bossController.OnArmorBroken(this);
        }
        else
        {
            Debug.LogWarning(gameObject.name + " BossPhaseController が設定されていません");
        }

        if (hideOnBroken)
        {
            SetVisible(false);
            SetCollidersEnabled(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public void ForceBreakArmor()
    {
        BreakArmor();
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

    private ThrowableBomb FindThrowableBomb(GameObject hitObject, Collider hitCollider, Rigidbody hitRigidbody)
    {
        ThrowableBomb throwableBomb = null;

        if (hitObject != null)
        {
            throwableBomb = hitObject.GetComponent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitObject.GetComponentInParent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitObject.GetComponentInChildren<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;
        }

        if (hitCollider != null)
        {
            throwableBomb = hitCollider.GetComponent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitCollider.GetComponentInParent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitCollider.GetComponentInChildren<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;
        }

        if (hitRigidbody != null)
        {
            throwableBomb = hitRigidbody.GetComponent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitRigidbody.GetComponentInParent<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;

            throwableBomb = hitRigidbody.GetComponentInChildren<ThrowableBomb>();
            if (throwableBomb != null) return throwableBomb;
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

    private void SetVisible(bool visible)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (colliders == null)
        {
            colliders = GetComponentsInChildren<Collider>(true);
        }

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
    }

    private string GetHitName(GameObject hitObject)
    {
        if (hitObject == null)
        {
            return "null";
        }

        return hitObject.name;
    }
}