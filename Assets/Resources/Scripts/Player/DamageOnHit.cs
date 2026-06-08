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

    private void TryDamage(Collider other)
    {
        if (other == null)
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
}