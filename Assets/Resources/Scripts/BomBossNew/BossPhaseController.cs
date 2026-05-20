using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    [Header("現在の段階")]
    [SerializeField] private int currentPhase = 1;

    [Header("1段階目")]
    [SerializeField] private GameObject bossStep1;
    [SerializeField] private BossArmorPart[] phase1Armors;
    [SerializeField] private BossCorePart phase1Core;
    [SerializeField] private int phase1ArmorHp = 30;

    [Header("2段階目")]
    [SerializeField] private GameObject bossStep2;
    [SerializeField] private BossArmorPart[] phase2Armors;
    [SerializeField] private BossCorePart phase2Core;
    [SerializeField] private int phase2ArmorHp = 40;

    [Header("3段階目")]
    [SerializeField] private GameObject bossStep3;
    [SerializeField] private BossArmorPart[] phase3Armors;
    [SerializeField] private BossCorePart phase3Core;
    [SerializeField] private int phase3ArmorHp = 50;

    [Header("頭")]
    [SerializeField] private GameObject bossHead;

    [Header("攻撃管理")]
    [SerializeField] private BossPhaseAttackController attackController;

    [Header("コアを何回攻撃したら段階破壊するか")]
    [SerializeField] private int coreHitCountToBreakPhase = 3;

    [Header("壁が壊れていなくてもコアヒットを許可する")]
    [SerializeField] private bool allowCoreHitBeforeAllArmorsBroken = true;

    [Header("段移動設定")]
    [SerializeField] private bool autoCalculateStepDownDistance = true;

    [Header("自動計算を使わない場合の下げる距離")]
    [SerializeField] private float manualStepDownDistance = 3.0f;

    [Header("下がる時間")]
    [SerializeField] private float stepDownDuration = 1.0f;

    [Header("デバッグ")]
    [SerializeField] private int currentCoreHitCount;
    [SerializeField] private bool coreOpened;
    [SerializeField] private bool allCurrentArmorsBroken;
    [SerializeField] private float calculatedStepDownDistance;

    private bool isChangingPhase;
    private bool isBossDefeated;

    private readonly HashSet<BossArmorPart> brokenArmors = new HashSet<BossArmorPart>();

    public int CurrentPhase => currentPhase;
    public bool IsChangingPhase => isChangingPhase;
    public bool IsBossDefeated => isBossDefeated;

    private Vector3 step1StartPosition;
    private Vector3 step2StartPosition;
    private Vector3 step3StartPosition;
    private Vector3 headStartPosition;

    private void Awake()
    {
        if (attackController == null)
        {
            attackController = GetComponent<BossPhaseAttackController>();
        }
    }

    private void Start()
    {
        CacheStartPositions();
        CalculateStepDownDistance();

        InitializeBoss();
        SetupPhase(1);
    }

    private void CacheStartPositions()
    {
        if (bossStep1 != null)
        {
            step1StartPosition = bossStep1.transform.position;
        }

        if (bossStep2 != null)
        {
            step2StartPosition = bossStep2.transform.position;
        }

        if (bossStep3 != null)
        {
            step3StartPosition = bossStep3.transform.position;
        }

        if (bossHead != null)
        {
            headStartPosition = bossHead.transform.position;
        }
    }

    private void CalculateStepDownDistance()
    {
        calculatedStepDownDistance = manualStepDownDistance;

        if (!autoCalculateStepDownDistance)
        {
            return;
        }

        if (bossStep1 == null || bossStep2 == null)
        {
            return;
        }

        float distance = Mathf.Abs(bossStep2.transform.position.y - bossStep1.transform.position.y);

        if (distance > 0.01f)
        {
            calculatedStepDownDistance = distance;
        }

        Debug.Log("段が下がる距離: " + calculatedStepDownDistance);
    }

    private void InitializeBoss()
    {
        isBossDefeated = false;
        isChangingPhase = false;

        if (bossStep1 != null)
        {
            bossStep1.SetActive(true);
            bossStep1.transform.position = step1StartPosition;
        }

        if (bossStep2 != null)
        {
            bossStep2.SetActive(true);
            bossStep2.transform.position = step2StartPosition;
        }

        if (bossStep3 != null)
        {
            bossStep3.SetActive(true);
            bossStep3.transform.position = step3StartPosition;
        }

        if (bossHead != null)
        {
            bossHead.SetActive(true);
            bossHead.transform.position = headStartPosition;
        }

        DisableArmors(phase1Armors);
        DisableArmors(phase2Armors);
        DisableArmors(phase3Armors);

        DisableCore(phase1Core);
        DisableCore(phase2Core);
        DisableCore(phase3Core);
    }

    private void SetupPhase(int phase)
    {
        currentPhase = phase;
        currentCoreHitCount = 0;
        coreOpened = allowCoreHitBeforeAllArmorsBroken;
        allCurrentArmorsBroken = false;
        isChangingPhase = false;
        brokenArmors.Clear();

        if (attackController != null)
        {
            attackController.ResetAllWallsBrokenState();
        }

        DisableArmors(phase1Armors);
        DisableArmors(phase2Armors);
        DisableArmors(phase3Armors);

        DisableCore(phase1Core);
        DisableCore(phase2Core);
        DisableCore(phase3Core);

        if (phase == 1)
        {
            SetupArmors(phase1Armors, phase1ArmorHp);
            SetupCore(phase1Core);
            Debug.Log("1段階目開始");
        }
        else if (phase == 2)
        {
            SetupArmors(phase2Armors, phase2ArmorHp);
            SetupCore(phase2Core);
            Debug.Log("2段階目開始");
        }
        else if (phase == 3)
        {
            SetupArmors(phase3Armors, phase3ArmorHp);
            SetupCore(phase3Core);
            Debug.Log("3段階目開始");
        }
    }

    private void SetupArmors(BossArmorPart[] armors, int hp)
    {
        if (armors == null)
        {
            return;
        }

        foreach (BossArmorPart armor in armors)
        {
            if (armor != null)
            {
                armor.Setup(this, hp);
            }
        }
    }

    private void SetupCore(BossCorePart core)
    {
        if (core == null)
        {
            return;
        }

        core.Setup(this);
        core.SetAttackable(allowCoreHitBeforeAllArmorsBroken);
        SetCollidersEnabled(core.gameObject, true);
    }

    private void DisableArmors(BossArmorPart[] armors)
    {
        if (armors == null)
        {
            return;
        }

        foreach (BossArmorPart armor in armors)
        {
            if (armor != null)
            {
                SetCollidersEnabled(armor.gameObject, false);
            }
        }
    }

    private void DisableCore(BossCorePart core)
    {
        if (core == null)
        {
            return;
        }

        core.SetAttackable(false);
        SetCollidersEnabled(core.gameObject, false);
    }

    private void SetCollidersEnabled(GameObject targetObject, bool enabled)
    {
        if (targetObject == null)
        {
            return;
        }

        Collider[] colliders = targetObject.GetComponentsInChildren<Collider>(true);

        foreach (Collider col in colliders)
        {
            if (col != null)
            {
                col.enabled = enabled;
            }
        }
    }

    public void OnArmorBroken(BossArmorPart brokenArmor)
    {
        if (isChangingPhase)
        {
            return;
        }

        if (brokenArmor != null && !brokenArmors.Contains(brokenArmor))
        {
            brokenArmors.Add(brokenArmor);
        }

        if (!AreAllCurrentArmorsBroken())
        {
            Debug.Log("壁が一部壊れました。残りの壁があります。");
            return;
        }

        OpenCoreAndForceAirStrikeOnly();
    }

    private void OpenCoreAndForceAirStrikeOnly()
    {
        if (allCurrentArmorsBroken)
        {
            return;
        }

        allCurrentArmorsBroken = true;
        coreOpened = true;

        BossCorePart currentCore = GetCurrentCore();

        if (currentCore != null)
        {
            currentCore.SetAttackable(true);
            SetCollidersEnabled(currentCore.gameObject, true);
        }

        if (attackController != null)
        {
            attackController.NotifyAllWallsBroken();
        }
        else
        {
            Debug.LogWarning("AttackController が設定されていないため、空爆のみに切り替えできません");
        }

        Debug.Log("壁がすべて壊れました。空爆のみになります。");
    }

    private bool AreAllCurrentArmorsBroken()
    {
        BossArmorPart[] currentArmors = GetCurrentArmors();

        if (currentArmors == null || currentArmors.Length == 0)
        {
            return true;
        }

        int validArmorCount = 0;

        for (int i = 0; i < currentArmors.Length; i++)
        {
            BossArmorPart armor = currentArmors[i];

            if (armor == null)
            {
                continue;
            }

            validArmorCount++;

            if (armor.IsBroken)
            {
                continue;
            }

            if (!armor.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (brokenArmors.Contains(armor))
            {
                continue;
            }

            return false;
        }

        return validArmorCount > 0;
    }

    private BossArmorPart[] GetCurrentArmors()
    {
        if (currentPhase == 1)
        {
            return phase1Armors;
        }

        if (currentPhase == 2)
        {
            return phase2Armors;
        }

        if (currentPhase == 3)
        {
            return phase3Armors;
        }

        return null;
    }

    public void OnCoreHit(int damage)
    {
        if (isChangingPhase)
        {
            return;
        }

        if (!coreOpened && !allowCoreHitBeforeAllArmorsBroken)
        {
            Debug.Log("コアに当たりましたが、まだ攻撃不可です");
            return;
        }

        currentCoreHitCount += damage;

        Debug.Log("コアに命中: " + currentCoreHitCount + " / " + coreHitCountToBreakPhase);

        if (currentCoreHitCount >= coreHitCountToBreakPhase)
        {
            StartCoroutine(BreakCurrentPhaseRoutine());
        }
    }

    private IEnumerator BreakCurrentPhaseRoutine()
    {
        if (isChangingPhase)
        {
            yield break;
        }

        isChangingPhase = true;

        Debug.Log(currentPhase + "段階目破壊");

        if (currentPhase == 1)
        {
            if (bossStep1 != null)
            {
                bossStep1.SetActive(false);
            }

            yield return MoveDownObjects(new GameObject[]
            {
                bossStep2,
                bossStep3,
                bossHead
            });

            SetupPhase(2);
            yield break;
        }

        if (currentPhase == 2)
        {
            if (bossStep2 != null)
            {
                bossStep2.SetActive(false);
            }

            yield return MoveDownObjects(new GameObject[]
            {
                bossStep3,
                bossHead
            });

            SetupPhase(3);
            yield break;
        }

        if (currentPhase == 3)
        {
            if (bossStep3 != null)
            {
                bossStep3.SetActive(false);
            }

            DropHead();
            yield break;
        }
    }

    private IEnumerator MoveDownObjects(GameObject[] objects)
    {
        if (objects == null || objects.Length == 0)
        {
            yield break;
        }

        Vector3[] startPositions = new Vector3[objects.Length];
        Vector3[] endPositions = new Vector3[objects.Length];

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            startPositions[i] = objects[i].transform.position;
            endPositions[i] = startPositions[i] + Vector3.down * calculatedStepDownDistance;
        }

        float timer = 0f;

        while (timer < stepDownDuration)
        {
            timer += Time.deltaTime;

            float t = timer / stepDownDuration;
            t = Mathf.Clamp01(t);

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] == null)
                {
                    continue;
                }

                objects[i].transform.position = Vector3.Lerp(startPositions[i], endPositions[i], t);
            }

            yield return null;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] == null)
            {
                continue;
            }

            objects[i].transform.position = endPositions[i];
        }
    }

    private BossCorePart GetCurrentCore()
    {
        if (currentPhase == 1)
        {
            return phase1Core;
        }

        if (currentPhase == 2)
        {
            return phase2Core;
        }

        if (currentPhase == 3)
        {
            return phase3Core;
        }

        return null;
    }

    private void DropHead()
    {
        Debug.Log("最終段階破壊。頭を落とします。");

        isBossDefeated = true;
        isChangingPhase = false;

        if (bossHead == null)
        {
            Debug.LogWarning("BossHead が設定されていません");
            return;
        }

        bossHead.transform.SetParent(null);

        Rigidbody rb = bossHead.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bossHead.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;

        Collider col = bossHead.GetComponent<Collider>();

        if (col == null)
        {
            col = bossHead.AddComponent<BoxCollider>();
        }

        col.enabled = true;
    }
}