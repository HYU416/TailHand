using System.Collections;
using UnityEngine;

public class BossArmorPart : MonoBehaviour
{
    [Header("ŠÇ—Œ³")]
    [SerializeField] private BossPhaseController bossController;

    [Header("•ÇHP")]
    [SerializeField] private int maxHp = 30;

    [Header("HP‚ª‚±‚Ì’lˆÈ‰º‚É‚È‚Á‚½‚ç“_–Å")]
    [SerializeField] private int blinkHpThreshold = 10;

    [Header("“_–ÅÝ’è")]
    [SerializeField] private float blinkInterval = 0.15f;

    private int currentHp;
    private bool isBroken;
    private Coroutine blinkCoroutine;
    private Renderer[] renderers;

    public bool IsBroken => isBroken;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
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

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isBroken)
        {
            return;
        }

        BossBombHitMarker bombMarker = collision.gameObject.GetComponent<BossBombHitMarker>();

        if (bombMarker == null)
        {
            bombMarker = collision.gameObject.GetComponentInParent<BossBombHitMarker>();
        }

        if (bombMarker == null)
        {
            return;
        }

        TakeDamage(bombMarker.ArmorDamage);

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

    private void TakeDamage(int damage)
    {
        if (isBroken)
        {
            return;
        }

        currentHp -= damage;
        currentHp = Mathf.Max(currentHp, 0);

        Debug.Log(gameObject.name + " HP: " + currentHp + " / " + maxHp);

        if (currentHp <= blinkHpThreshold && blinkCoroutine == null)
        {
            blinkCoroutine = StartCoroutine(Blink());
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

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        SetVisible(false);

        Collider[] colliders = GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        if (bossController != null)
        {
            bossController.OnArmorBroken(this);
        }
    }

    private IEnumerator Blink()
    {
        while (!isBroken)
        {
            SetVisible(false);
            yield return new WaitForSeconds(blinkInterval);

            SetVisible(true);
            yield return new WaitForSeconds(blinkInterval);
        }
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