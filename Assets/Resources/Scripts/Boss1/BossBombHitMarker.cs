using UnityEngine;

public class BossBombHitMarker : MonoBehaviour
{
    [Header("ボスの壁に与えるダメージ")]
    [SerializeField] private int armorDamage = 10;

    [Header("コアに与えるダメージ")]
    [SerializeField] private int coreDamage = 1;

    [Header("当たった後に消すか")]
    [SerializeField] private bool destroyOnHit = true;

    [Header("不発弾の場合、プレイヤーが投げた時だけ有効")]
    [SerializeField] private bool requireThrownDudBomb = true;

    [Header("通常爆弾の場合、プレイヤーが投げた時だけ有効")]
    [SerializeField] private bool requireThrownNormalBomb = true;

    private DudBomb dudBomb;
    private ThrowableBomb throwableBomb;

    public int ArmorDamage
    {
        get
        {
            if (!CanDamageBoss())
            {
                return 0;
            }

            return armorDamage;
        }
    }

    public int CoreDamage
    {
        get
        {
            if (!CanDamageBoss())
            {
                return 0;
            }

            return coreDamage;
        }
    }

    public bool DestroyOnHit
    {
        get { return destroyOnHit; }
    }

    private void Awake()
    {
        dudBomb = FindDudBomb();
        throwableBomb = FindThrowableBomb();
    }

    public void Consume()
    {
        if (!destroyOnHit)
        {
            return;
        }

        if (dudBomb != null)
        {
            if (!CanDamageBoss())
            {
                Debug.Log("未投げの不発弾なので、Consumeされても爆発・消滅しません: " + gameObject.name);
                return;
            }

            dudBomb.ExplodeByBossHit();
            return;
        }

        if (throwableBomb != null)
        {
            if (!CanDamageBoss())
            {
                Debug.Log("未投げの通常爆弾なので、Consumeされても爆発・消滅しません: " + gameObject.name);
                return;
            }

            throwableBomb.ExplodeByBossHit();
            return;
        }

        Destroy(gameObject);
    }

    private bool CanDamageBoss()
    {
        if (dudBomb != null)
        {
            if (!requireThrownDudBomb)
            {
                return true;
            }

            return dudBomb.IsThrownByPlayer;
        }

        if (throwableBomb != null)
        {
            if (!requireThrownNormalBomb)
            {
                return true;
            }

            return throwableBomb.IsThrownByPlayer;
        }

        return true;
    }

    private DudBomb FindDudBomb()
    {
        DudBomb result = GetComponent<DudBomb>();

        if (result == null)
        {
            result = GetComponentInParent<DudBomb>();
        }

        if (result == null)
        {
            result = GetComponentInChildren<DudBomb>();
        }

        return result;
    }

    private ThrowableBomb FindThrowableBomb()
    {
        ThrowableBomb result = GetComponent<ThrowableBomb>();

        if (result == null)
        {
            result = GetComponentInParent<ThrowableBomb>();
        }

        if (result == null)
        {
            result = GetComponentInChildren<ThrowableBomb>();
        }

        return result;
    }
}