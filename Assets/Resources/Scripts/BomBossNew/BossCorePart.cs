using UnityEngine;

public class BossCorePart : MonoBehaviour
{
    [Header("ä«óùå≥")]
    [SerializeField] private BossPhaseController bossController;

    [Header("ï«Ç™âÛÇÍÇÈÇ‹Ç≈ÉRÉAÇçUåÇïsâ¬Ç…Ç∑ÇÈ")]
    [SerializeField] private bool requireArmorBroken = true;

    private bool isAttackable;

    public void Setup(BossPhaseController controller)
    {
        bossController = controller;
        isAttackable = !requireArmorBroken;
    }

    public void SetAttackable(bool value)
    {
        isAttackable = value;
    }

    private void OnCollisionEnter(Collision collision)
    {
        BossBombHitMarker bombMarker = collision.gameObject.GetComponent<BossBombHitMarker>();

        if (bombMarker == null)
        {
            bombMarker = collision.gameObject.GetComponentInParent<BossBombHitMarker>();
        }

        if (bombMarker == null)
        {
            return;
        }

        if (!isAttackable)
        {
            return;
        }

        if (bossController != null)
        {
            bossController.OnCoreHit(bombMarker.CoreDamage);
        }

        DudBomb dudBomb = collision.gameObject.GetComponent<DudBomb>();

        if (dudBomb == null)
        {
            dudBomb = collision.gameObject.GetComponentInParent<DudBomb>();
        }

        if (dudBomb != null)
        {
            dudBomb.ExplodeByBossHit();
        }
        else
        {
            bombMarker.Consume();
        }
    }
}