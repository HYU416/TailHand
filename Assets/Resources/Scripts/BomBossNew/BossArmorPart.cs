using UnityEngine;

public class BossArmorPart : MonoBehaviour
{
    [Header("管理元")]
    [SerializeField] private BossPhaseController bossController;

    [Header("壁HP")]
    [SerializeField] private int maxHp = 30;

    [Header("プレイヤーが投げた不発弾だけ壁に有効にする")]
    [SerializeField] private bool requireThrownDudBomb = true;

    [Header("デバッグログ")]
    [SerializeField] private bool showDebugLog = true;

    private int currentHp;
    private bool isBroken;
    private Renderer[] renderers;

    public bool IsBroken => isBroken;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
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

        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckHit(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckHit(other.gameObject);
    }

    private void CheckHit(GameObject hitObject)
    {
        if (isBroken)
        {
            return;
        }

        if (hitObject == null)
        {
            return;
        }

        BossBombHitMarker bombMarker = hitObject.GetComponent<BossBombHitMarker>();

        if (bombMarker == null)
        {
            bombMarker = hitObject.GetComponentInParent<BossBombHitMarker>();
        }

        if (bombMarker == null)
        {
            bombMarker = hitObject.GetComponentInChildren<BossBombHitMarker>();
        }

        if (bombMarker == null)
        {
            return;
        }

        DudBomb dudBomb = hitObject.GetComponent<DudBomb>();

        if (dudBomb == null)
        {
            dudBomb = hitObject.GetComponentInParent<DudBomb>();
        }

        if (dudBomb == null)
        {
            dudBomb = hitObject.GetComponentInChildren<DudBomb>();
        }

        if (dudBomb != null && requireThrownDudBomb)
        {
            DudBombState dudBombState = hitObject.GetComponent<DudBombState>();

            if (dudBombState == null)
            {
                dudBombState = hitObject.GetComponentInParent<DudBombState>();
            }

            if (dudBombState == null)
            {
                dudBombState = hitObject.GetComponentInChildren<DudBombState>();
            }

            if (dudBombState == null || !dudBombState.IsThrownByPlayer)
            {
                if (showDebugLog)
                {
                    Debug.Log("投げられていない不発弾なので壁には効きません: " + hitObject.name);
                }

                return;
            }
        }

        TakeDamage(bombMarker.ArmorDamage);

        if (dudBomb != null)
        {
            dudBomb.ExplodeByBossHit();
        }
        else
        {
            bombMarker.Consume();
        }
    }

    private void TakeDamage(int damage)
    {
        if (isBroken)
        {
            return;
        }

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " HP: " + currentHp + " / " + maxHp);
        }

        if (currentHp <= 0)
        {
            BreakArmor();
        }
    }

    private void BreakArmor()
    {
        if (isBroken)
        {
            return;
        }

        isBroken = true;

        if (bossController != null)
        {
            bossController.OnArmorBroken(this);
        }

        if (showDebugLog)
        {
            Debug.Log(gameObject.name + " が破壊されました");
        }

        gameObject.SetActive(false);
    }

    private void SetVisible(bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.enabled = visible;
            }
        }
    }
}