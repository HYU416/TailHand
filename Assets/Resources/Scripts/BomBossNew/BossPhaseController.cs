using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossPhaseController : MonoBehaviour
{
    [Header("迴ｾ蝨ｨ縺ｮ谿ｵ髫")]
    [SerializeField] private int currentPhase = 1;

    [Header("1谿ｵ髫守岼")]
    [SerializeField] private GameObject bossStep1;
    [SerializeField] private BossArmorPart[] phase1Armors;
    [SerializeField] private BossCorePart phase1Core;
    [SerializeField] private int phase1ArmorHp = 30;

    [Header("2谿ｵ髫守岼")]
    [SerializeField] private GameObject bossStep2;
    [SerializeField] private BossArmorPart[] phase2Armors;
    [SerializeField] private BossCorePart phase2Core;
    [SerializeField] private int phase2ArmorHp = 40;

    [Header("3谿ｵ髫守岼")]
    [SerializeField] private GameObject bossStep3;
    [SerializeField] private BossArmorPart[] phase3Armors;
    [SerializeField] private BossCorePart phase3Core;
    [SerializeField] private int phase3ArmorHp = 50;

    [Header("鬆ｭ")]
    [SerializeField] private GameObject bossHead;

    [Header("謾ｻ謦�ｮ｡逅")]
    [SerializeField] private BossPhaseAttackController attackController;

    [Header("繧ｳ繧｢繧剃ｽ募屓謾ｻ謦�＠縺溘ｉ谿ｵ髫守ｴ螢翫☆繧九°")]
    [SerializeField] private int coreHitCountToBreakPhase = 3;

    [Header("螢√′螢翫ｌ縺ｦ縺�↑縺上※繧ゅさ繧｢繝偵ャ繝医ｒ險ｱ蜿ｯ縺吶ｋ")]
    [SerializeField] private bool allowCoreHitBeforeAllArmorsBroken = true;

    [Header("螢√′蜈ｨ驛ｨ螢翫ｌ縺溘ｉ縺昴�谿ｵ髫弱ｒ閾ｪ蜍慕ｴ螢翫☆繧")]
    [SerializeField] private bool breakPhaseWhenAllArmorsBroken = true;

    [Header("螢∝�遐ｴ螢頑凾縺ｫ繧ｳ繧｢縺ｸ蜈･繧後ｋ遐ｴ螢翫ム繝｡繝ｼ繧ｸ")]
    [SerializeField] private int allArmorBrokenCoreDamage = 999;

    [Header("螢∝�遐ｴ螢頑凾縺ｫ謾ｻ謦�ヱ繧ｿ繝ｼ繝ｳ繧貞�繧頑崛縺医ｋ")]
    [SerializeField] private bool notifyAttackControllerWhenAllArmorsBroken = false;

    [Header("谿ｵ遘ｻ蜍戊ｨｭ螳")]
    [SerializeField] private bool autoCalculateStepDownDistance = true;

    [Header("閾ｪ蜍戊ｨ育ｮ励ｒ菴ｿ繧上↑縺�ｴ蜷医�荳九￡繧玖ｷ晞屬")]
    [SerializeField] private float manualStepDownDistance = 3.0f;

    [Header("荳九′繧区凾髢")]
    [SerializeField] private float stepDownDuration = 1.0f;

    [Header("繝�ヰ繝�げ")]
    [SerializeField] private int currentCoreHitCount;
    [SerializeField] private bool coreOpened;
    [SerializeField] private bool allCurrentArmorsBroken;
    [SerializeField] private float calculatedStepDownDistance;

    private bool isChangingPhase;
    private bool isBossDefeated;

    private readonly HashSet<BossArmorPart> brokenArmors = new HashSet<BossArmorPart>();

    public int CurrentPhase
    {
        get { return currentPhase; }
    }

    public bool IsChangingPhase
    {
        get { return isChangingPhase; }
    }

    public bool IsBossDefeated
    {
        get { return isBossDefeated; }
    }

    public Transform Phase3CoreTransform
    {
        get
        {
            if (phase3Core == null)
            {
                return null;
            }

            return phase3Core.transform;
        }
    }

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

        Debug.Log("谿ｵ縺御ｸ九′繧玖ｷ晞屬: " + calculatedStepDownDistance);
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
            Debug.Log("1谿ｵ髫守岼髢句ｧ");
        }
        else if (phase == 2)
        {
            SetupArmors(phase2Armors, phase2ArmorHp);
            SetupCore(phase2Core);
            Debug.Log("2谿ｵ髫守岼髢句ｧ");
        }
        else if (phase == 3)
        {
            SetupArmors(phase3Armors, phase3ArmorHp);
            SetupCore(phase3Core);
            Debug.Log("3谿ｵ髫守岼髢句ｧ");
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
            Debug.Log("螢√′荳驛ｨ螢翫ｌ縺ｾ縺励◆縲よｮ九ｊ縺ｮ螢√′縺ゅｊ縺ｾ縺吶");
            return;
        }

        OnAllCurrentArmorsBroken();
    }

    private void OnAllCurrentArmorsBroken()
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

        if (notifyAttackControllerWhenAllArmorsBroken && attackController != null)
        {
            attackController.NotifyAllWallsBroken();
        }

        Debug.Log("迴ｾ蝨ｨ縺ｮ谿ｵ髫弱�螢√′縺吶∋縺ｦ遐ｴ螢翫＆繧後∪縺励◆縲ゅさ繧｢繧定�蜍慕ｴ螢頑桶縺�↓縺励∪縺吶");

        if (breakPhaseWhenAllArmorsBroken)
        {
            OnCoreHit(allArmorBrokenCoreDamage);
        }
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
            Debug.Log("繧ｳ繧｢縺ｫ蠖薙◆繧翫∪縺励◆縺後√∪縺謾ｻ謦�ｸ榊庄縺ｧ縺");
            return;
        }

        if (damage <= 0)
        {
            return;
        }

        currentCoreHitCount += damage;

        Debug.Log("繧ｳ繧｢縺ｫ蜻ｽ荳ｭ: " + currentCoreHitCount + " / " + coreHitCountToBreakPhase);

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

        Debug.Log(currentPhase + "谿ｵ髫守岼遐ｴ螢");

        if (currentPhase == 1)
        {
            EffectManager.Instance.Play(EffectType.CoreBreak, phase1Core.transform.position);

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
            EffectManager.Instance.Play(EffectType.CoreBreak, phase2Core.transform.position);

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
            // 繝輔ぉ繝ｼ繧ｺ4髢句ｧ区凾縺ｮ繧ｨ繝輔ぉ繧ｯ繝医� BossFinalAttackSequence��X�峨↓莉ｻ縺帙ｋ縲
            if (bossStep3 != null)
            {
                bossStep3.SetActive(false);
            }

            currentPhase = 4;

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
        Debug.Log("譛邨よｮｵ髫守ｴ螢翫るｭ繧定誠縺ｨ縺励※謗ｴ繧√ｋ繧医≧縺ｫ縺励∪縺吶");

        isBossDefeated = true;
        isChangingPhase = false;

        if (bossHead == null)
        {
            Debug.LogWarning("BossHead 縺瑚ｨｭ螳壹＆繧後※縺�∪縺帙ｓ");
            return;
        }

        bossHead.transform.SetParent(null);

        Collider col = bossHead.GetComponent<Collider>();

        if (col == null)
        {
            col = bossHead.AddComponent<BoxCollider>();
        }

        col.enabled = true;
        col.isTrigger = false;

        Rigidbody rb = bossHead.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = bossHead.AddComponent<Rigidbody>();
        }

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.mass = 1.0f;
        rb.linearDamping = 0.2f;
        rb.angularDamping = 0.2f;

        BossHeadCatchable catchable = bossHead.GetComponent<BossHeadCatchable>();

        if (catchable == null)
        {
            catchable = bossHead.AddComponent<BossHeadCatchable>();
        }

        catchable.SetCanCatch(true);
    }

#if UNITY_EDITOR
    [ContextMenu("Debug/Core Break Current Phase")]
    private void DebugCoreBreakCurrentPhase()
    {
        if (Application.isPlaying == false)
        {
            Debug.LogWarning("蜀咲函荳ｭ縺ｮ縺ｿ螳溯｡後〒縺阪∪縺");
            return;
        }

        StartCoroutine(BreakCurrentPhaseRoutine());
    }
#endif
}
