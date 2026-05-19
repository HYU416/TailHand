using UnityEngine;

public class BossBombHitMarker : MonoBehaviour
{
    [Header("ボスの壁に与えるダメージ")]
    [SerializeField] private int armorDamage = 10;

    [Header("コアに与えるダメージ")]
    [SerializeField] private int coreDamage = 1;

    [Header("当たった後に消すか")]
    [SerializeField] private bool destroyOnHit = false;

    public int ArmorDamage => armorDamage;
    public int CoreDamage => coreDamage;
    public bool DestroyOnHit => destroyOnHit;

    public void Consume()
    {
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
    }
}