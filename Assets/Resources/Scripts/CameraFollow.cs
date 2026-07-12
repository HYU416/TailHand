using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Vector3 shakeOffset;
    [SerializeField] int StageNumber = 1;

    [SerializeField] GameObject player;
    [SerializeField] GameObject Boss;
    [SerializeField] GameOver gameOver;

    [SerializeField] float CameraDistance = 5.0f;
    [SerializeField] float CameraHeight = 3.0f;

    [SerializeField] float angleLimit = 30.0f;
    [SerializeField] float moveSmooth = 8.0f;
    [SerializeField] float lookSmooth = 10.0f;

    [SerializeField] float bossHeight = 10.0f;
    [SerializeField] Transform lookFocusOverride;

    [SerializeField] float introDistance = 4f;
    [SerializeField] float introHeight = 2f;
    [SerializeField] float stageDistance = 25f;
    [SerializeField] float stageHeight = 12f;

    [SerializeField ] GameObject StartSequenceUIObject;

    [SerializeField] BossPhaseAttackController bossAttack;

    private float introTime = 0f;

    private Vector3 bossLandPos;
    private Vector3 bossStartPos;
    private bool bossIntroInit = false;
    private Renderer[] bossRenderers;

    public bool Gamestart;

    bool followEnabled = true;
    bool bossHeadWatchEnabled;
    bool bossWatchLockLook;
    bool qteCameraEnabled;
    Transform bossWatchHead;
    Transform qteLookTarget;
    Vector3 bossWatchFixedCameraPosition;
    Quaternion bossWatchFixedRotation;
    float bossWatchLookSmooth = 10f;
    float qteBaseBackDistance = 6f;
    float qteExtraBackDistance;
    float qteLookHeightOffset = 1.5f;
    Vector3 qteCameraBackDirection = Vector3.back;

    bool qteFramingEnabled;
    Transform frameTargetA;
    Transform frameTargetB;
    Vector3 frameFixedCameraPosition;
    float frameHeightOffset = 1.5f;
    bool frameSmooth = true;

    public bool FollowEnabled
    {
        get { return followEnabled; }
        set { followEnabled = value; }
    }

    /// <summary>
    /// 繝懊せ鬆ｭ驛ｨ豕ｨ隕悶�貍泌�逕ｨ繝｢繝ｼ繝峨ｒ髢句ｧ九＠縺ｾ縺呻ｼ井ｽ咲ｽｮ縺ｯ螟夜Κ縺九ｉ險ｭ螳夲ｼ峨
    /// </summary>
    public void BeginBossCinematic(Transform headLookTarget)
    {
        if (headLookTarget == null)
        {
            return;
        }

        bossWatchHead = headLookTarget;
        bossWatchLockLook = false;
        bossHeadWatchEnabled = true;
        followEnabled = true;
    }

    /// <summary>
    /// 貍泌�荳ｭ縺ｮ繧ｫ繝｡繝ｩ菴咲ｽｮ繧定ｨｭ螳壹＠縺ｾ縺吶
    /// </summary>
    public void SetBossWatchCameraPosition(Vector3 worldPosition)
    {
        bossWatchFixedCameraPosition = worldPosition;
        transform.position = worldPosition + shakeOffset;
    }

    /// <summary>
    /// 豕ｨ隕也せ縺ｸ蜷代″繧貞叉蠎ｧ縺ｫ蜷医ｏ縺帙∪縺吶
    /// </summary>
    public void SnapBossWatchLookAt()
    {
        if (bossWatchHead == null)
        {
            return;
        }

        Vector3 lookDirection = bossWatchHead.position - transform.position;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    public void SnapCameraRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    public void LockCameraTransform(Vector3 position, Quaternion rotation)
    {
        EndHeadFollow();
        EndQTECamera();
        EndQTEFraming();
        followEnabled = false;
        transform.position = position + shakeOffset;
        transform.rotation = rotation;
    }

    public void LockCameraLookAt(Vector3 position, Vector3 lookTarget)
    {
        EndHeadFollow();
        EndQTECamera();
        EndQTEFraming();
        followEnabled = false;
        transform.position = position + shakeOffset;

        Vector3 lookDirection = lookTarget - transform.position;

        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        }
    }

    /// <summary>
    /// 繧ｫ繝｡繝ｩ菴咲ｽｮ縺ｯ蝗ｺ螳壹＠縲√く繝｣繝ｩ縺ｨ繝懊せ鬆ｭ縺ｮ 2 轤ｹ縺ｮ荳ｭ髢薙□縺代ｒ豕ｨ隕悶☆繧区兜縺呈ｼ泌�繧ｫ繝｡繝ｩ繧帝幕蟋九＠縺ｾ縺吶
    /// </summary>
    public void BeginQTEThrowFraming(
        Transform targetA,
        Transform targetB,
        Vector3 fixedCameraPosition,
        float heightOffset,
        bool smooth = true)
    {
        if (targetA == null && targetB == null)
        {
            return;
        }

        EndHeadFollow();
        EndQTECamera();

        frameTargetA = targetA;
        frameTargetB = targetB;
        frameFixedCameraPosition = fixedCameraPosition;
        frameHeightOffset = heightOffset;
        frameSmooth = smooth;

        qteFramingEnabled = true;
        followEnabled = true;

        UpdateQTEFraming(true);
    }

    public void EndQTEFraming()
    {
        qteFramingEnabled = false;
        frameTargetA = null;
        frameTargetB = null;
    }

    /// <summary>
    /// 迴ｾ蝨ｨ縺ｮ菴咲ｽｮ繝ｻ蜷代″縺ｮ縺ｾ縺ｾ繧ｫ繝｡繝ｩ繧貞ｮ悟�縺ｫ蝗ｺ螳壹＠縺ｾ縺呻ｼ郁ｿｽ蠕薙ｒ豁｢繧√ｋ�峨
    /// </summary>
    public void FreezeQTEFraming()
    {
        qteFramingEnabled = false;
        frameTargetA = null;
        frameTargetB = null;
        followEnabled = false;
    }

    public void SnapCameraTransform(Vector3 position, Quaternion rotation)
    {
        EndHeadFollow();
        EndQTECamera();
        EndQTEFraming();
        transform.position = position + shakeOffset;
        transform.rotation = rotation;
    }

    /// <summary>
    /// 繧ｫ繝｡繝ｩ菴咲ｽｮ繧貞崋螳壹＠縲。oss_Head 縺ｪ縺ｩ縺ｮ豕ｨ隕也せ縺縺題ｿｽ縺�∪縺吶
    /// </summary>
    public void BeginBossHeadWatch(
        Transform headLookTarget,
        Vector3 fixedCameraPosition,
        float lookSmooth = 10f)
    {
        if (headLookTarget == null)
        {
            return;
        }

        bossWatchHead = headLookTarget;
        bossWatchLookSmooth = lookSmooth;
        bossWatchFixedCameraPosition = fixedCameraPosition;
        bossWatchLockLook = false;
        bossHeadWatchEnabled = true;
        followEnabled = true;

        transform.position = bossWatchFixedCameraPosition + shakeOffset;
    }

    /// <summary>
    /// 豕ｨ隕匁婿蜷代ｒ迴ｾ蝨ｨ縺ｮ蜷代″縺ｧ蝗ｺ螳壹＠縺ｾ縺呻ｼ域遠縺｡荳翫￡荳ｭ縺ｪ縺ｩ�峨
    /// </summary>
    public void LockBossHeadLookDirection()
    {
        if (!bossHeadWatchEnabled)
        {
            return;
        }

        bossWatchLockLook = true;
        bossWatchFixedRotation = transform.rotation;
    }

    /// <summary>
    /// 豕ｨ隕匁婿蜷代�蝗ｺ螳壹ｒ隗｣髯､縺励�ｭ驛ｨ霑ｽ蠕薙↓謌ｻ縺励∪縺吶
    /// </summary>
    public void UnlockBossHeadLookDirection()
    {
        bossWatchLockLook = false;
    }

    /// <summary>
    /// 繝懊せ鬆ｭ驛ｨ豕ｨ隕悶き繝｡繝ｩ繝｢繝ｼ繝峨ｒ邨ゆｺ�＠縲�壼ｸｸ霑ｽ蠕薙↓謌ｻ縺励∪縺吶
    /// </summary>
    public void EndHeadFollow()
    {
        bossHeadWatchEnabled = false;
        bossWatchLockLook = false;
        bossWatchHead = null;
    }

    public void SetLookFocusOverride(Transform focus)
    {
        lookFocusOverride = focus;
    }

    public void BeginQTECamera(
        Transform lookTarget,
        float baseBackDistance,
        float lookHeightOffset = 1.5f)
    {
        if (lookTarget == null)
        {
            return;
        }

        EndHeadFollow();
        qteLookTarget = lookTarget;
        qteBaseBackDistance = baseBackDistance;
        qteLookHeightOffset = lookHeightOffset;
        qteExtraBackDistance = 0f;
        qteCameraBackDirection = ResolveQTECameraBackDirection(lookTarget);
        qteCameraEnabled = true;
        followEnabled = true;
        UpdateQTECamera(true);
    }

    Vector3 ResolveQTECameraBackDirection(Transform lookTarget)
    {
        Vector3 lookPoint = lookTarget.position + Vector3.up * qteLookHeightOffset;
        Vector3 cameraOffset = transform.position - lookPoint;
        cameraOffset.y = 0f;

        if (cameraOffset.sqrMagnitude > 0.0001f)
        {
            return cameraOffset.normalized;
        }

        Vector3 fallback = -lookTarget.forward;
        fallback.y = 0f;

        if (fallback.sqrMagnitude < 0.0001f)
        {
            return Vector3.back;
        }

        return fallback.normalized;
    }

    public void SetQTECameraBackExtra(float extraBackDistance)
    {
        qteExtraBackDistance = Mathf.Max(0f, extraBackDistance);
    }

    public void EndQTECamera()
    {
        qteCameraEnabled = false;
        qteLookTarget = null;
        qteExtraBackDistance = 0f;
    }


   

    public bool IsGameStart()
    {
        return Gamestart;
    }

    private void Start()
    {

        var intro = MySoundManeger.Play(gameObject, BGMList.BGM_GAME);
        //intro.time += 70.0f;
        var loop = MySoundManeger.Play(gameObject, BGMList.BGM_GAME_LOOP);
        loop.Stop();
        loop.PlayScheduled(AudioSettings.dspTime + intro.clip.length - intro.time);

        if (StageNumber == 0)
        {
            return;
        }
        Gamestart = false;

        bossIntroInit = false;

        bossLandPos = Boss.transform.position;
        bossStartPos = bossLandPos + Vector3.up * 35f;
        bossRenderers = Boss.GetComponentsInChildren<Renderer>();
        // Bossを非表示
        bossRenderers = Boss.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in bossRenderers)
        {
            r.enabled = false;
        }

        StartSequenceUIObject.SetActive(false);

        
        //player = GameObject.FindGameObjectWithTag("Player");
        //Boss = GameObject.FindGameObjectWithTag("Boss");
    }

    public void SetPlayerBossReferences(GameObject playerObject, GameObject bossObject)
    {
        player = playerObject;
        Boss = bossObject;
    }


    private void LateUpdate()
    {
        if (!followEnabled)
        {
            return;
        }

        if (qteFramingEnabled && (frameTargetA != null || frameTargetB != null))
        {
            UpdateQTEFraming(false);
            return;
        }

        if (bossHeadWatchEnabled && bossWatchHead != null)
        {
            UpdateBossHeadWatch();
            return;
        }

        if (qteCameraEnabled && qteLookTarget != null)
        {
            UpdateQTECamera(false);
            return;
        }

        if (player == null || Boss == null) return;
        if (!Gamestart)
        {
            if (player == null) return;

            introTime += Time.deltaTime;

            Vector3 center = player.transform.position + Vector3.up * introHeight;
            Vector3 forward = player.transform.forward;

            // Phase1
            if (introTime < 2.0f)
            {
                //プレイヤーの目の前に移動（0f）
                Vector3 pos = center + forward * introDistance;
                transform.position = pos;
                transform.LookAt(center);
                //Vector3 pos = center + forward * introDistance;

                //transform.position = Vector3.Lerp(
                //    transform.position,
                //    pos,
                //    Time.deltaTime * 5f);

                //transform.LookAt(center);
            }
            // Phase2
            else if (introTime < 5.0f)
            {
                float t = (introTime - 2.0f) / 3.0f;

                float angle2 = Mathf.Lerp(0f, 180f, t);

                Vector3 dir = Quaternion.Euler(0, angle2, 0) * forward;

                Vector3 pos = center + dir * introDistance;

                transform.position = pos;
                transform.LookAt(center);
            }
            // Phase3
            // Phase3
            else if (introTime < 9.0f)
            {
                float t = (introTime - 5.0f) / 4.0f;

                float dist = Mathf.Lerp(introDistance, stageDistance, t);
                float height = Mathf.Lerp(introHeight, stageHeight, t);

                Vector3 pos =
                    player.transform.position
                    - forward * dist
                    + Vector3.up * height;

                transform.position = pos;
                transform.LookAt(player.transform.position + Vector3.up * 2f);
            }
            // Phase4 Boss登場
            else if (introTime < 13.0f)
            {
                if (!bossIntroInit)
                {
                    bossIntroInit = true;

                    // Bossを表示
                    foreach (Renderer r in bossRenderers)
                    {
                        r.enabled = true;
                    }

                    Boss.transform.position = bossStartPos;
                }

                float t = (introTime - 9.0f) / 4.0f;
                t = Mathf.SmoothStep(0f, 1f, t);

                // Boss降下
                Boss.transform.position =
                    Vector3.Lerp(bossStartPos, bossLandPos, t);

                // プレイヤー斜め後ろ
                Vector3 offset =
                    -forward * 6f +
                    Vector3.left * 3f +
                    Vector3.up * 2.5f;

                transform.position =
                    player.transform.position + offset;

                // Bossを見る
                transform.LookAt(Boss.transform.position + Vector3.up * 2f);
            }
            // Phase5 Boss名表示
            // Phase5 Bossを見上げる
            // Phase5 Bossを下から上へ舐める
            // Phase5
            else if (introTime < 17.0f)
            {
                float t = (introTime - 13.0f) / 4.0f;
                t = Mathf.SmoothStep(0f, 1f, t);

                Vector3 basePos = Boss.transform.position - player.transform.forward * 10f;
                Vector3 startPos = new Vector3();
                Vector3 endPos = new Vector3();
                if (StageNumber == 1)
                {
                    basePos.x -= 4.2f;
                    basePos.z -= 2.0f;
                    startPos = basePos + Vector3.up * 1.5f;
                    endPos = basePos + Vector3.up * 14f;
                }
                if(StageNumber == 2)
                {
                    basePos.x -= 3.0f;
                    basePos.z += 6.0f;
                    startPos = basePos + Vector3.up * 1.5f;
                    endPos = basePos + Vector3.up * 10f;
                }


                // 位置だけ移動
                transform.position = Vector3.Lerp(startPos, endPos, t);
                // LookAtはしない
            }
            // Phase6 Boss名表示
            else if (introTime < 19.0f)
            {

                // Boss名表示
                StartSequenceUIObject.SetActive(true);
                //bossNameUI.SetActive(true);
            }
            else
            {
                StartSequenceUIObject.SetActive(false);
                //bossNameUI.SetActive(false);

                if(StageNumber == 1)bossAttack.StartBattle();
                Gamestart = true;
            }
            return;
        }


        if (player == null || Boss == null || gameOver.IsStart()) return;

        Vector3 bossPos = Boss.transform.position;
        bossPos.y = bossHeight;
        Vector3 playerPos = player.transform.position;

        Vector3 bossToPlayer = playerPos - bossPos;
        bossToPlayer.y = 0f;
        bossToPlayer.Normalize();

        Vector3 bossToCamera = transform.position - bossPos;
        bossToCamera.y = 0f;
        bossToCamera.Normalize();

        float angle = Vector3.SignedAngle(bossToPlayer, bossToCamera, Vector3.up);

        Vector3 targetDir = bossToCamera;

        if (angle > angleLimit)
        {
            targetDir = Quaternion.Euler(0f, angleLimit, 0f) * bossToPlayer;
        }
        else if (angle < -angleLimit)
        {
            targetDir = Quaternion.Euler(0f, -angleLimit, 0f) * bossToPlayer;
        }

        float distance = Vector3.Distance(bossPos, playerPos) + CameraDistance;

        Vector3 targetPos = bossPos + targetDir * distance;
        targetPos.y = bossPos.y + CameraHeight;

        Vector3 target = lookFocusOverride != null
            ? lookFocusOverride.position
            : (playerPos + bossPos) / 2.0f;

        transform.position = Vector3.Lerp(
            transform.position,
            targetPos + shakeOffset,
            moveSmooth * Time.deltaTime
        );

        Quaternion targetRot = Quaternion.LookRotation(target - transform.position);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            lookSmooth * Time.deltaTime
        );
    }

    private void UpdateBossHeadWatch()
    {
        transform.position = bossWatchFixedCameraPosition + shakeOffset;

        if (bossWatchLockLook)
        {
            transform.rotation = bossWatchFixedRotation;
            return;
        }

        Vector3 lookDirection = bossWatchHead.position - transform.position;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
        float lookDelta = Time.timeScale > 0f ? Time.deltaTime : Time.unscaledDeltaTime;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            bossWatchLookSmooth * lookDelta
        );
    }

    void UpdateQTEFraming(bool snap)
    {
        Vector3 pointA = frameTargetA != null
            ? frameTargetA.position
            : frameTargetB.position;
        Vector3 pointB = frameTargetB != null
            ? frameTargetB.position
            : frameTargetA.position;

        Vector3 midpoint = (pointA + pointB) * 0.5f;
        Vector3 lookPoint = midpoint + Vector3.up * frameHeightOffset;

        // 繧ｫ繝｡繝ｩ菴咲ｽｮ縺ｯ蝗ｺ螳壹ょ髄縺阪□縺 2 轤ｹ縺ｮ荳ｭ髢薙∈蜷代￠繧九
        transform.position = frameFixedCameraPosition + shakeOffset;

        Vector3 lookDir = lookPoint - transform.position;

        if (lookDir.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(lookDir, Vector3.up);

        if (snap || !frameSmooth)
        {
            transform.rotation = targetRot;
            return;
        }

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            lookSmooth * Time.deltaTime
        );
    }

    void UpdateQTECamera(bool snap)
    {
        Vector3 lookPoint = qteLookTarget.position + Vector3.up * qteLookHeightOffset;
        float totalBack = qteBaseBackDistance + qteExtraBackDistance;
        Vector3 desiredPos = lookPoint + qteCameraBackDirection * totalBack;

        if (snap)
        {
            transform.position = desiredPos + shakeOffset;
            transform.rotation = Quaternion.LookRotation(lookPoint - transform.position, Vector3.up);
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPos + shakeOffset,
            moveSmooth * Time.deltaTime
        );

        Vector3 lookDirection = lookPoint - transform.position;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            lookSmooth * Time.deltaTime
        );
    }
}
