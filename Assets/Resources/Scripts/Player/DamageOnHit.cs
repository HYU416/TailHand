using UnityEngine;

public class DamageOnHit : MonoBehaviour
{
    [Header("ダメージ量")]
    [SerializeField] private float damage = 10f;

    [Header("一度当たったら消えるか")]
    [SerializeField] private bool destroyOnHit = false;

    [Header("同じ相手に連続ヒットさせるか")]
    [SerializeField] private bool canHitSameTargetMultipleTimes = false;

    [Header("連続ヒット間隔")]
    [SerializeField] private float hitInterval = 0.5f;

    [Header("このタグを持つ部位へのダメージを無効化する")]
    [Tooltip("例：PlayerTail。しっぽの掴み判定Collider側に付けるタグです")]
    [SerializeField] private string ignoreTargetTag = "PlayerTail";

    [Header("上のタグを無視できる攻撃側タグ")]
    [Tooltip("例：IgnorePlayerTail。このタグが付いた攻撃だけ、PlayerTailに当たってもダメージを与えません")]
    [SerializeField] private string attackTagThatCanIgnoreTarget = "IgnorePlayerTail";

    private PlayerHPBar lastHitPlayer;
    private float lastHitTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryDamage(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamage(collision.collider);
    }

    private void TryDamage(Collider other)
    {
        if (other == null) return;

        if (ShouldIgnoreThisHit(other))
        {
            return;
        }

        PlayerHPBar playerHP = other.GetComponentInParent<PlayerHPBar>();

        if (playerHP == null)
        {
            return;
        }

        if (!canHitSameTargetMultipleTimes)
        {
            if (lastHitPlayer == playerHP)
            {
                return;
            }
        }
        else
        {
            if (lastHitPlayer == playerHP && Time.time < lastHitTime + hitInterval)
            {
                return;
            }
        }

        lastHitPlayer = playerHP;
        lastHitTime = Time.time;

        playerHP.TakeDamage(damage);

        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }

    private bool ShouldIgnoreThisHit(Collider other)
    {
        if (string.IsNullOrEmpty(ignoreTargetTag))
        {
            return false;
        }

        if (string.IsNullOrEmpty(attackTagThatCanIgnoreTarget))
        {
            return false;
        }

        if (!ThisAttackHasIgnoreTag())
        {
            return false;
        }

        if (ColliderOrParentHasTag(other, ignoreTargetTag))
        {
            return true;
        }

        return false;
    }

    private bool ThisAttackHasIgnoreTag()
    {
        Transform current = transform;

        while (current != null)
        {
            if (current.CompareTag(attackTagThatCanIgnoreTarget))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool ColliderOrParentHasTag(Collider other, string targetTag)
    {
        Transform current = other.transform;

        while (current != null)
        {
            if (current.CompareTag(targetTag))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}