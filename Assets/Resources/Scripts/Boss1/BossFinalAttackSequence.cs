using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 4 移行を監視し、カメラ演出・頭の打ち上げ・落下演出を再生します。
/// </summary>
public class BossFinalAttackSequence : MonoBehaviour
{
    [Header("監視（どちらか一方を設定）")]
    [Tooltip("GameScene の BomBoss 用")]
    [SerializeField] private BossPhaseController phaseController;
    [Tooltip("BossStage_Big_G 用")]
    [SerializeField] private Big_G bigG;

    [Header("参照")]
    [SerializeField] private GameObject bossHead;
    [SerializeField] private Transform bossAnchor;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private Transform faceCenterOverride;
    [SerializeField] private Transform effectAnchor;
    [SerializeField] private GameObject tempEffectPrefab;

    [Header("Phase 4 開始演出")]
    [Tooltip("頭のローカル座標。X=右、Y=上、Z=前")]
    [SerializeField] private Vector3 introCameraOffsetFromHead = new Vector3(1.8f, 0.6f, 2.0f);
    [SerializeField, Min(0f)] private float introCameraMoveDuration = 0.35f;
    [SerializeField, Min(0f)] private float hitStopDuration = 0.12f;
    [Tooltip("ヒットストップ〜スロー中。ボス基準の右斜め前（X=右, Y=上, Z=前）")]
    [SerializeField] private Vector3 impactHoldCameraOffsetFromBoss = new Vector3(6f, 5f, 14f);
    [Tooltip("ヒットストップ〜スロー中の注視点（ボス位置からのY）")]
    [SerializeField] private float impactHoldLookHeight = 3f;
    [SerializeField, Range(0.01f, 1f)] private float slowMotionTimeScale = 0.25f;
    [SerializeField, Min(0f)] private float slowMotionDuration = 0.8f;
    [SerializeField, Min(0f)] private float introToWatchMoveDuration = 0.6f;
    [Tooltip("スロー解除後、頭上カメラへ移る時間")]
    [SerializeField] private GameObject exEffectPrefab;
    [Tooltip("EX エフェクトを出すオブジェクト名（Renderer の中心に配置）")]
    [SerializeField] private string effectSpawnObjectName = "Boss_Head_GP";
    [SerializeField, Min(0.1f)] private float introImpactEffectScale = 2.5f;
    [SerializeField, Min(0.1f)] private float launchEffectScale = 2.5f;

    [Header("タイミング")]
    [SerializeField, Min(0f)] private float delayBeforeLaunch = 1.0f;

    [Header("頭の打ち上げ")]
    [SerializeField, Min(0f)] private float launchUpwardSpeed = 8.0f;
    [Tooltip("Big_G 用。体全体を真上に持ち上げる高さ")]
    [SerializeField, Min(0.1f)] private float bigGAscendHeight = 12f;
    [Tooltip("Big_G 用。打ち上げにかける秒数")]
    [SerializeField, Min(0.05f)] private float bigGAscendDuration = 1.2f;

    [Header("頭の回転（コインはじき）")]
    [SerializeField] private string faceCenterObjectName = "Boss_Head";
    [SerializeField] private float coinFlipDegrees = 360f;
    [SerializeField] private Vector3 coinFlipAxisLocal = Vector3.right;
    [Tooltip("回転の中心をルートからどれだけ上にずらすか（ワールドY）")]
    [SerializeField] private float coinFlipPivotHeight = 2f;

    [Header("落下")]
    [Tooltip("1より大きいほど早く落ちます")]
    [SerializeField, Min(1f)] private float fallGravityMultiplier = 2.5f;

    [SerializeField, Min(0f)] private float settleVelocityThreshold = 0.5f;
    [SerializeField, Min(0f)] private float settleDuration = 0.2f;
    [SerializeField, Min(0f)] private float maxWaitForSettle = 8.0f;

    [Header("カメラ")]
    [SerializeField] private float cameraHeightAboveBoss = 5.5f;
    [SerializeField] private float headLookSmooth = 10f;

    [Header("演出中に止める（任意）")]
    [SerializeField] private BossPhaseAttackController attackController;
    [SerializeField] private Behaviour[] disableDuringSequence;

    [Header("QTEシーン移行")]
    [SerializeField] private bool loadQTESceneAfterSequence = true;
    [SerializeField] private string qteSceneName = SceneLoader.SceneName.QTEScene;
    [SerializeField, Min(0f)] private float delayBeforeQTESceneLoad = 0.5f;

    int lastObservedPhase = 1;
    bool isPlaying;
    bool phase4SequenceTriggered;
    float savedTimeScale = 1f;
    GameObject spawnedEffect;
    GameObject spawnedIntroEffect;
    Collider[] disabledRespawnColliders;

    void Start()
    {
        if (!HasPhaseSource())
            return;

        lastObservedPhase = GetCurrentPhaseNumber();

        if (ShouldStartSequence())
            TryStartPhase4Sequence();
    }

    void Update()
    {
        if (!HasPhaseSource() || phase4SequenceTriggered)
            return;

        if (phaseController != null)
        {
            int phase = phaseController.CurrentPhase;

            if (lastObservedPhase < 4 && phase >= 4)
                TryStartPhase4Sequence();

            lastObservedPhase = phase;
            return;
        }

        if (bigG != null && bigG.GetCurrentPhase() == EPhase.Phase4)
            TryStartPhase4Sequence();
    }

    bool HasPhaseSource()
    {
        return phaseController != null || bigG != null;
    }

    int GetCurrentPhaseNumber()
    {
        if (bigG != null)
            return (int)bigG.GetCurrentPhase() + 1;

        if (phaseController != null)
            return phaseController.CurrentPhase;

        return 1;
    }

    bool ShouldStartSequence()
    {
        if (phaseController != null)
            return phaseController.CurrentPhase >= 4;

        if (bigG != null)
            return bigG.GetCurrentPhase() == EPhase.Phase4;

        return false;
    }

    void TryStartPhase4Sequence()
    {
        if (phase4SequenceTriggered || isPlaying || !HasPhaseSource())
            return;

        if (phaseController != null && phaseController.CurrentPhase < 4)
            return;

        if (bigG != null && bigG.GetCurrentPhase() != EPhase.Phase4)
            return;

        phase4SequenceTriggered = true;

        EnsureTimeRunning();

        if (bigG != null)
            bigG.enabled = false;

        DisableGameClearTriggers();
        StartCoroutine(PlaySequenceRoutine());
    }

    void OnDisable()
    {
        RestoreTimeScale();
    }

    IEnumerator PlaySequenceRoutine()
    {
        isPlaying = true;
        savedTimeScale = Time.timeScale;

        GameObject headObject = ResolveHeadObject();

        if (headObject == null)
        {
            Debug.LogWarning("BossFinalAttackSequence: ボスの頭が未設定です");
            FinishSequence(null);
            yield break;
        }

        Transform head = headObject.transform;
        Transform faceCenter = ResolveFaceCenter(head);
        Transform anchor = ResolveBossAnchor();
        CameraFollow follow = ResolveCameraFollow();

        EnsureTimeRunning();

        if (follow == null)
        {
            Debug.LogWarning(
                "BossFinalAttackSequence: CameraFollow が見つかりません。"
                + " Main Camera に CameraFollow を付けるか、Inspector で割り当ててください。"
            );
        }

        PrepareLaunchTarget(headObject);
        Rigidbody headRb = headObject.GetComponent<Rigidbody>();

        if (headRb == null)
        {
            Debug.LogWarning("BossFinalAttackSequence: 頭に Rigidbody がありません");
            FinishSequence(follow);
            yield break;
        }

        EnsureHeadCatchable(headObject, false);
        SetSequenceObjectsActive(false);
        DisableGameClearTriggers();

        headRb.linearVelocity = Vector3.zero;
        headRb.angularVelocity = Vector3.zero;
        headRb.isKinematic = true;
        headRb.useGravity = false;

        if (follow != null && faceCenter != null)
        {
            follow.FollowEnabled = true;
            follow.BeginBossCinematic(faceCenter);
            yield return PlayPhase4Intro(follow, head, faceCenter, anchor);
        }
        else if (follow == null)
        {
            Debug.LogWarning("BossFinalAttackSequence: カメラ演出をスキップしました（CameraFollow 未設定）");
        }

        EnsureTimeRunning();

        if (follow != null && anchor != null && faceCenter != null)
        {
            follow.BeginBossHeadWatch(
                faceCenter,
                GetWatchCameraPosition(anchor),
                headLookSmooth
            );
        }

        if (delayBeforeLaunch > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLaunch);
        }

        SpawnHeadEffect(head, faceCenter);

        if (follow != null)
        {
            follow.LockBossHeadLookDirection();
        }

        if (bigG != null)
            yield return AscendBigGVertically(headRb, head);
        else
            yield return AscendHeadWithRotation(headRb, faceCenter, head.rotation, launchUpwardSpeed);

        if (follow != null)
        {
            follow.UnlockBossHeadLookDirection();
        }

        EnsureTimeRunning();

        if (bigG != null)
        {
            EnsureLaunchCollider(headObject);
            EnableFallPhysics(headRb);

            if (loadQTESceneAfterSequence)
            {
                yield return GoToQTESceneRoutine(follow, headRb);
            }
            else
            {
                yield return WaitForHeadLand(headRb);
                EnsureHeadCatchable(headObject, true);
                FinishSequence(follow);
            }
        }
        else
        {
            yield return WaitForHeadLand(headRb);

            if (loadQTESceneAfterSequence)
            {
                yield return GoToQTESceneRoutine(follow);
            }
            else
            {
                EnsureHeadCatchable(headObject, true);
                FinishSequence(follow);
            }
        }
    }

    IEnumerator GoToQTESceneRoutine(CameraFollow follow, Rigidbody fallingBody = null)
    {
        EnsureTimeRunning();

        if (delayBeforeQTESceneLoad > 0f)
        {
            float timer = 0f;
            float extraGravity = Mathf.Abs(Physics.gravity.y) * (fallGravityMultiplier - 1f);

            while (timer < delayBeforeQTESceneLoad)
            {
                timer += Time.unscaledDeltaTime;

                if (fallingBody != null && !fallingBody.isKinematic && extraGravity > 0f)
                    fallingBody.AddForce(Vector3.down * fallingBody.mass * extraGravity, ForceMode.Force);

                yield return null;
            }
        }

        CleanupHeadEffect();
        CleanupIntroEffect();

        if (follow != null)
        {
            follow.EndHeadFollow();
        }

        isPlaying = false;

        Debug.Log($"BossFinalAttackSequence: 着地完了 → QTEシーン '{qteSceneName}' へ移行");
        QTETransitionContext.MarkEnteredFromFinalAttack();
        SceneLoader.Load(qteSceneName);
    }

    void EnsureTimeRunning()
    {
        Time.timeScale = 1f;
        savedTimeScale = 1f;
    }

    IEnumerator PlayPhase4Intro(
        CameraFollow follow,
        Transform head,
        Transform faceCenter,
        Transform anchor)
    {
        EnsureTimeRunning();
        follow.FollowEnabled = true;

        Vector3 introPosition = GetIntroCameraPosition(head, faceCenter);
        Vector3 watchPosition = anchor != null
            ? GetWatchCameraPosition(anchor)
            : introPosition + Vector3.up * 3f;

        Vector3 startPosition = follow.transform.position;
        follow.BeginBossCinematic(faceCenter);

        yield return LerpBossCamera(follow, startPosition, introPosition, introCameraMoveDuration, true);

        Time.timeScale = 0f;
        SpawnIntroImpactEffect(faceCenter);

        Vector3 holdPosition = introPosition;
        Vector3 holdLookPoint = GetImpactHoldLookPosition(anchor, faceCenter);
        Vector3 widePosition = GetImpactHoldCameraPosition(head, anchor);

        if (hitStopDuration > 0f)
        {
            yield return LerpBossCamera(
                follow,
                introPosition,
                widePosition,
                hitStopDuration,
                true,
                holdLookPoint
            );
            holdPosition = widePosition;
        }
        else
        {
            follow.SetBossWatchCameraPosition(widePosition);
            SnapCameraLookAt(follow, holdLookPoint);
            holdPosition = widePosition;
        }

        follow.SetBossWatchCameraPosition(holdPosition);
        SnapCameraLookAt(follow, holdLookPoint);
        follow.LockBossHeadLookDirection();

        Time.timeScale = slowMotionTimeScale;

        if (slowMotionDuration > 0f)
        {
            yield return new WaitForSeconds(slowMotionDuration);
        }

        RestoreTimeScale();

        follow.UnlockBossHeadLookDirection();

        if (introToWatchMoveDuration > 0f)
        {
            yield return LerpBossCamera(
                follow,
                holdPosition,
                watchPosition,
                introToWatchMoveDuration,
                true
            );
        }
        else
        {
            follow.SetBossWatchCameraPosition(watchPosition);
            follow.SnapBossWatchLookAt();
        }
    }

    IEnumerator LerpBossCamera(
        CameraFollow follow,
        Vector3 from,
        Vector3 to,
        float duration,
        bool useUnscaledTime,
        Vector3? lookAtPoint = null)
    {
        if (duration <= 0f)
        {
            follow.SetBossWatchCameraPosition(to);
            SnapCameraLookAt(follow, lookAtPoint);
            yield break;
        }

        float timer = 0f;

        while (timer < duration)
        {
            float delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer += delta;
            float t = Mathf.Clamp01(timer / duration);
            float eased = t * t * (3f - 2f * t);

            follow.SetBossWatchCameraPosition(Vector3.Lerp(from, to, eased));
            SnapCameraLookAt(follow, lookAtPoint);

            yield return null;
        }

        follow.SetBossWatchCameraPosition(to);
        SnapCameraLookAt(follow, lookAtPoint);
    }

    static void SnapCameraLookAt(CameraFollow follow, Vector3? lookAtPoint = null)
    {
        if (!lookAtPoint.HasValue)
        {
            follow.SnapBossWatchLookAt();
            return;
        }

        Vector3 lookDirection = lookAtPoint.Value - follow.transform.position;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        follow.transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    Vector3 GetImpactHoldLookPosition(Transform anchor, Transform faceCenter)
    {
        if (anchor != null)
        {
            return anchor.position + Vector3.up * impactHoldLookHeight;
        }

        return faceCenter != null ? faceCenter.position : Vector3.zero;
    }

    Vector3 GetImpactHoldCameraPosition(Transform head, Transform anchor)
    {
        Transform basis = anchor != null ? anchor : head;
        Vector3 origin = basis.position;

        return origin
            + basis.right * impactHoldCameraOffsetFromBoss.x
            + basis.up * impactHoldCameraOffsetFromBoss.y
            + basis.forward * impactHoldCameraOffsetFromBoss.z;
    }

    Vector3 GetIntroCameraPosition(Transform head, Transform faceCenter)
    {
        Vector3 basisPosition = faceCenter != null ? faceCenter.position : head.position;

        return basisPosition
            + head.right * introCameraOffsetFromHead.x
            + head.up * introCameraOffsetFromHead.y
            + head.forward * introCameraOffsetFromHead.z;
    }

    Vector3 GetWatchCameraPosition(Transform anchor)
    {
        return new Vector3(
            anchor.position.x,
            anchor.position.y + cameraHeightAboveBoss,
            anchor.position.z
        );
    }

    void SpawnIntroImpactEffect(Transform faceCenter)
    {
        CleanupIntroEffect();

        GameObject prefab = exEffectPrefab != null
            ? exEffectPrefab
            : Resources.Load<GameObject>("prefabs/Effects/EX");

        if (prefab == null || !TryGetEffectSpawnPose(out Vector3 position, out _))
        {
            return;
        }

        spawnedIntroEffect = Instantiate(prefab, position, Quaternion.identity);
        ApplyEffectScale(spawnedIntroEffect, prefab.transform.localScale, introImpactEffectScale);
    }

    bool TryGetEffectSpawnPose(out Vector3 position, out Quaternion rotation)
    {
        Transform spawnTarget = ResolveEffectSpawnTransform();

        if (spawnTarget == null)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            return false;
        }

        position = GetTransformBoundsCenter(spawnTarget);
        rotation = Quaternion.identity;
        return true;
    }

    Transform ResolveEffectSpawnTransform()
    {
        Transform searchRoot = bigG != null
            ? bigG.transform
            : ResolveHeadObject()?.transform;

        if (searchRoot == null)
            return null;

        string[] candidateNames = bigG != null
            ? new[] { effectSpawnObjectName, "Boss_2_GP", "Boss_Head_GP" }
            : new[] { effectSpawnObjectName, "Boss_Head_GP" };

        foreach (string objectName in candidateNames)
        {
            if (string.IsNullOrEmpty(objectName))
                continue;

            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName)
                    return child;
            }
        }

        Debug.LogWarning(
            $"BossFinalAttackSequence: エフェクト出し位置 '{effectSpawnObjectName}' が見つかりません。"
            + (bigG != null ? " Boss_2_GP を確認してください。" : " Boss_Head_GP を確認してください。")
        );

        return ResolveFaceCenter(searchRoot);
    }

    static Vector3 GetTransformBoundsCenter(Transform target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return target.position;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    void CleanupIntroEffect()
    {
        if (spawnedIntroEffect == null)
        {
            return;
        }

        Destroy(spawnedIntroEffect);
        spawnedIntroEffect = null;
    }

    static void ApplyEffectScale(GameObject effect, Vector3 baseScale, float scaleMultiplier)
    {
        if (effect == null || Mathf.Approximately(scaleMultiplier, 1f))
        {
            return;
        }

        effect.transform.localScale = baseScale * scaleMultiplier;
    }

    void RestoreTimeScale()
    {
        Time.timeScale = savedTimeScale;
    }

    void FinishSequence(CameraFollow follow)
    {
        RestoreTimeScale();
        CleanupHeadEffect();
        CleanupIntroEffect();
        RestoreGameClearTriggers();

        if (follow != null)
        {
            follow.EndHeadFollow();
        }

        SetSequenceObjectsActive(true);
        if (bigG != null && !loadQTESceneAfterSequence)
            bigG.enabled = true;

        isPlaying = false;
    }

    void PrepareLaunchTarget(GameObject launchObject)
    {
        if (launchObject == null)
            return;

        if (bigG == null)
            return;

        Big_GMotion motion = bigG.GetComponent<Big_GMotion>();
        if (motion != null)
            motion.enabled = false;

        Big_GController controller = bigG.GetComponent<Big_GController>();
        if (controller != null)
            controller.enabled = false;

        foreach (Animator animator in bigG.GetComponentsInChildren<Animator>(true))
        {
            if (animator != null)
                animator.enabled = false;
        }

        Big_GArmController[] armControllers = bigG.GetComponentsInChildren<Big_GArmController>(true);
        foreach (Big_GArmController armController in armControllers)
        {
            if (armController != null)
                armController.enabled = false;
        }

        Rigidbody rb = launchObject.GetComponent<Rigidbody>();
        if (rb == null)
            rb = launchObject.AddComponent<Rigidbody>();

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    static void EnableFallPhysics(Rigidbody rb)
    {
        if (rb == null)
            return;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.detectCollisions = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.None;
    }

    static void EnsureLaunchCollider(GameObject launchObject)
    {
        if (launchObject == null)
            return;

        if (HasSolidCollider(launchObject))
            return;

        Renderer[] renderers = launchObject.GetComponentsInChildren<Renderer>(true);

        BoxCollider box = launchObject.AddComponent<BoxCollider>();

        if (renderers.Length == 0)
        {
            box.center = Vector3.up * 2f;
            box.size = new Vector3(4f, 6f, 4f);
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Transform root = launchObject.transform;
        Vector3 localScale = root.lossyScale;

        box.center = root.InverseTransformPoint(bounds.center);
        box.size = new Vector3(
            bounds.size.x / Mathf.Max(Mathf.Abs(localScale.x), 0.001f),
            bounds.size.y / Mathf.Max(Mathf.Abs(localScale.y), 0.001f),
            bounds.size.z / Mathf.Max(Mathf.Abs(localScale.z), 0.001f));
    }

    static bool HasSolidCollider(GameObject launchObject)
    {
        Collider[] colliders = launchObject.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            if (collider != null && !collider.isTrigger && collider.enabled)
                return true;
        }

        return false;
    }

    void DisableGameClearTriggers()
    {
        RespawnTrigger[] triggers = FindObjectsByType<RespawnTrigger>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Collider> disabled = new System.Collections.Generic.List<Collider>();

        foreach (RespawnTrigger trigger in triggers)
        {
            if (trigger == null)
            {
                continue;
            }

            Collider[] colliders = trigger.GetComponentsInChildren<Collider>(true);

            foreach (Collider collider in colliders)
            {
                if (collider != null && collider.enabled)
                {
                    collider.enabled = false;
                    disabled.Add(collider);
                }
            }
        }

        disabledRespawnColliders = disabled.ToArray();
    }

    void RestoreGameClearTriggers()
    {
        if (disabledRespawnColliders == null)
        {
            return;
        }

        foreach (Collider collider in disabledRespawnColliders)
        {
            if (collider != null)
            {
                collider.enabled = true;
            }
        }

        disabledRespawnColliders = null;
    }

    IEnumerator AscendBigGVertically(Rigidbody bodyRb, Transform bodyTransform)
    {
        if (bodyRb == null || bodyTransform == null)
            yield break;

        Vector3 startPosition = bodyRb.position;
        Quaternion startRotation = bodyTransform.rotation;
        Vector3 flipAxisWorld = startRotation * coinFlipAxisLocal.normalized;

        if (flipAxisWorld.sqrMagnitude < 0.0001f)
            flipAxisWorld = Vector3.right;

        float duration = Mathf.Max(0.05f, bigGAscendDuration);
        float height = Mathf.Max(0.1f, bigGAscendHeight);
        float pivotHeight = Mathf.Max(0f, coinFlipPivotHeight);
        Vector3 startPivot = startPosition + Vector3.up * pivotHeight;
        Vector3 startOffsetFromPivot = startPosition - startPivot;
        float timer = 0f;

        bodyRb.isKinematic = true;
        bodyRb.useGravity = false;
        bodyRb.constraints = RigidbodyConstraints.FreezeRotation;

        Debug.Log(
            $"BossFinalAttackSequence: Big_G 打ち上げ開始 pos={startPosition}, height={height}, "
            + $"duration={duration}, pivotHeight={pivotHeight}"
        );

        while (timer < duration)
        {
            timer += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            Quaternion targetRotation = Quaternion.AngleAxis(coinFlipDegrees * t, flipAxisWorld) * startRotation;
            Vector3 ascendedPivot = startPivot + Vector3.up * (height * t);
            Vector3 targetPosition = ascendedPivot + targetRotation * Quaternion.Inverse(startRotation) * startOffsetFromPivot;

            bodyRb.MovePosition(targetPosition);
            bodyRb.MoveRotation(targetRotation);

            yield return new WaitForFixedUpdate();
        }

        Quaternion finalRotation = Quaternion.AngleAxis(coinFlipDegrees, flipAxisWorld) * startRotation;
        Vector3 finalPivot = startPivot + Vector3.up * height;
        Vector3 finalPosition = finalPivot + finalRotation * Quaternion.Inverse(startRotation) * startOffsetFromPivot;

        bodyRb.MovePosition(finalPosition);
        bodyRb.MoveRotation(finalRotation);

        Debug.Log(
            $"BossFinalAttackSequence: Big_G 打ち上げ完了 pos={bodyRb.position}, movedY={bodyRb.position.y - startPosition.y:F2}"
        );

        bodyRb.isKinematic = false;
        bodyRb.useGravity = true;
        bodyRb.linearVelocity = Vector3.zero;
        bodyRb.angularVelocity = Vector3.zero;
        bodyRb.constraints = RigidbodyConstraints.None;
    }

    IEnumerator AscendHeadWithRotation(
        Rigidbody headRb,
        Transform faceCenter,
        Quaternion startRotation,
        float upwardSpeed)
    {
        float gravity = Mathf.Abs(Physics.gravity.y);

        if (gravity <= 0f || upwardSpeed <= 0f || faceCenter == null)
        {
            yield break;
        }

        Vector3 launchFaceCenter = faceCenter.position;
        Vector3 faceOffsetLocal = Quaternion.Inverse(startRotation) * (launchFaceCenter - headRb.position);
        Vector3 flipAxisWorld = startRotation * coinFlipAxisLocal.normalized;

        if (flipAxisWorld.sqrMagnitude < 0.0001f)
        {
            flipAxisWorld = Vector3.right;
        }

        float timeToApex = upwardSpeed / gravity;
        RigidbodyConstraints previousConstraints = headRb.constraints;
        headRb.constraints = RigidbodyConstraints.FreezeRotation;

        float timer = 0f;

        while (timer < timeToApex)
        {
            timer += Time.fixedDeltaTime;
            float t = timer / timeToApex;

            float height = upwardSpeed * timer - 0.5f * gravity * timer * timer;
            Vector3 desiredFaceCenter = launchFaceCenter + Vector3.up * height;
            Quaternion targetRotation = Quaternion.AngleAxis(coinFlipDegrees * t, flipAxisWorld) * startRotation;

            headRb.MoveRotation(targetRotation);
            headRb.MovePosition(desiredFaceCenter - targetRotation * faceOffsetLocal);

            yield return new WaitForFixedUpdate();
        }

        Quaternion apexRotation = Quaternion.AngleAxis(coinFlipDegrees, flipAxisWorld) * startRotation;
        float apexHeight = upwardSpeed * timeToApex - 0.5f * gravity * timeToApex * timeToApex;
        Vector3 apexFaceCenter = launchFaceCenter + Vector3.up * apexHeight;

        headRb.MoveRotation(apexRotation);
        headRb.MovePosition(apexFaceCenter - apexRotation * faceOffsetLocal);

        headRb.isKinematic = false;
        headRb.useGravity = true;
        headRb.linearVelocity = Vector3.zero;
        headRb.angularVelocity = Vector3.zero;
        headRb.constraints = previousConstraints;
    }

    IEnumerator WaitForHeadLand(Rigidbody headRb)
    {
        float settledTimer = 0f;
        float realTimer = 0f;
        float extraGravity = Mathf.Abs(Physics.gravity.y) * (fallGravityMultiplier - 1f);

        while (realTimer < maxWaitForSettle)
        {
            realTimer += Time.unscaledDeltaTime;

            if (extraGravity > 0f && headRb != null && !headRb.isKinematic)
            {
                headRb.AddForce(Vector3.down * headRb.mass * extraGravity, ForceMode.Force);
            }

            if (headRb != null && headRb.linearVelocity.magnitude <= settleVelocityThreshold)
            {
                settledTimer += Time.unscaledDeltaTime;

                if (settledTimer >= settleDuration)
                {
                    Debug.Log("BossFinalAttackSequence: 頭が着地しました");
                    yield break;
                }
            }
            else
            {
                settledTimer = 0f;
            }

            yield return null;
        }

        Debug.LogWarning("BossFinalAttackSequence: 着地待ちタイムアウト。QTEシーンへ移行します");
    }

    IEnumerator WaitForHeadSettle(Rigidbody headRb)
    {
        float totalTimer = 0f;
        float settledTimer = 0f;
        float extraGravity = Mathf.Abs(Physics.gravity.y) * (fallGravityMultiplier - 1f);

        while (totalTimer < maxWaitForSettle)
        {
            totalTimer += Time.fixedDeltaTime;

            if (extraGravity > 0f)
            {
                headRb.AddForce(Vector3.down * headRb.mass * extraGravity, ForceMode.Force);
            }

            if (headRb.linearVelocity.magnitude <= settleVelocityThreshold)
            {
                settledTimer += Time.fixedDeltaTime;

                if (settledTimer >= settleDuration)
                {
                    yield break;
                }
            }
            else
            {
                settledTimer = 0f;
            }

            yield return new WaitForFixedUpdate();
        }
    }

    Transform ResolveFaceCenter(Transform head)
    {
        if (faceCenterOverride != null)
            return faceCenterOverride;

        if (head == null)
            return null;

        if (bigG != null)
        {
            foreach (string objectName in new[] { "Bone_Boss_2_Head", "Boss_2_Head", "Boss_Head" })
            {
                foreach (Transform child in bigG.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name == objectName)
                        return child;
                }
            }
        }

        if (string.IsNullOrEmpty(faceCenterObjectName))
            return head;

        Transform[] children = head.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            if (child.name == faceCenterObjectName)
                return child;
        }

        return head;
    }

    Transform ResolveBossAnchor()
    {
        if (bossAnchor != null)
            return bossAnchor;

        if (bigG != null)
            return bigG.transform;

        return phaseController != null ? phaseController.transform : null;
    }

    GameObject ResolveHeadObject()
    {
        if (bossHead != null)
            return bossHead;

        if (bigG != null)
            return bigG.gameObject;

        return phaseController != null ? GameObject.FindGameObjectWithTag("BossHead") : null;
    }

    CameraFollow ResolveCameraFollow()
    {
        if (cameraFollow != null)
            return cameraFollow;

        Camera mainCamera = Camera.main;

        if (mainCamera != null)
        {
            CameraFollow follow = mainCamera.GetComponent<CameraFollow>();

            if (follow != null)
                return follow;
        }

        return FindFirstObjectByType<CameraFollow>();
    }

    static void EnsureHeadCatchable(GameObject headObject, bool canCatch)
    {
        BossHeadCatchable catchable = headObject.GetComponent<BossHeadCatchable>();

        if (catchable == null)
        {
            catchable = headObject.AddComponent<BossHeadCatchable>();
        }

        catchable.SetCanCatch(canCatch);
    }

    void SpawnHeadEffect(Transform head, Transform faceCenter)
    {
        CleanupHeadEffect();

        if (tempEffectPrefab == null)
            return;

        Vector3 spawnPosition = head.position;

        if (TryGetEffectSpawnPose(out Vector3 effectPosition, out _))
            spawnPosition = effectPosition;
        else if (faceCenter != null)
            spawnPosition = faceCenter.position;

        spawnedEffect = Instantiate(tempEffectPrefab, spawnPosition, Quaternion.identity);
        ApplyEffectScale(spawnedEffect, tempEffectPrefab.transform.localScale, launchEffectScale);
    }

    void CleanupHeadEffect()
    {
        if (spawnedEffect == null)
        {
            return;
        }

        Destroy(spawnedEffect);
        spawnedEffect = null;
    }

    void SetSequenceObjectsActive(bool active)
    {
        if (attackController != null)
            attackController.enabled = active;

        if (bigG != null)
        {
            Big_GController controller = bigG.GetComponent<Big_GController>();
            if (controller != null)
                controller.enabled = active;

            Big_GArmController[] armControllers = bigG.GetComponentsInChildren<Big_GArmController>(true);
            foreach (Big_GArmController armController in armControllers)
            {
                if (armController != null)
                    armController.enabled = active;
            }
        }

        if (disableDuringSequence == null)
            return;

        foreach (Behaviour behaviour in disableDuringSequence)
        {
            if (behaviour != null)
            {
                behaviour.enabled = active;
            }
        }
    }
}
