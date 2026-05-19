using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    [Header("現在の段階")]
    [SerializeField] private int currentPhase = 1;

    [Header("1段階目のオブジェクト")]
    [SerializeField] private GameObject bossStep1;

    [Header("1段階目の壁")]
    [SerializeField] private BossArmorPart[] phase1Armors;

    [Header("1段階目のコア")]
    [SerializeField] private BossCorePart phase1Core;

    [Header("1段階目の壁HP")]
    [SerializeField] private int phase1ArmorHp = 30;

    [Header("コアを何回攻撃したら段階破壊するか")]
    [SerializeField] private int coreHitCountToBreakPhase = 3;

    [Header("デバッグ")]
    [SerializeField] private int currentCoreHitCount;

    private bool coreOpened;

    private void Start()
    {
        SetupPhase1();
    }

    private void SetupPhase1()
    {
        currentPhase = 1;
        currentCoreHitCount = 0;
        coreOpened = false;

        if (bossStep1 != null)
        {
            bossStep1.SetActive(true);
        }

        foreach (BossArmorPart armor in phase1Armors)
        {
            if (armor != null)
            {
                armor.Setup(this, phase1ArmorHp);
            }
        }

        if (phase1Core != null)
        {
            phase1Core.Setup(this);
            phase1Core.SetAttackable(false);
        }
    }

    public void OnArmorBroken(BossArmorPart brokenArmor)
    {
        if (currentPhase != 1)
        {
            return;
        }

        if (coreOpened)
        {
            return;
        }

        coreOpened = true;

        if (phase1Core != null)
        {
            phase1Core.SetAttackable(true);
        }

        Debug.Log("壁が壊れました。コア攻撃可能です。");
    }

    public void OnCoreHit(int damage)
    {
        if (currentPhase != 1)
        {
            return;
        }

        if (!coreOpened)
        {
            return;
        }

        currentCoreHitCount += damage;

        Debug.Log("コアに命中: " + currentCoreHitCount + " / " + coreHitCountToBreakPhase);

        if (currentCoreHitCount >= coreHitCountToBreakPhase)
        {
            BreakPhase1();
        }
    }

    private void BreakPhase1()
    {
        Debug.Log("1段階目破壊");

        if (bossStep1 != null)
        {
            bossStep1.SetActive(false);
        }

        currentPhase = 2;

        Debug.Log("2段階目へ移行予定");
    }
}