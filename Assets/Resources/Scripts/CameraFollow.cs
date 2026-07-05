using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Vector3 shakeOffset;

    [SerializeField] GameObject player;
    [SerializeField] GameObject Boss;

    [SerializeField] float CameraDistance = 5.0f;
    [SerializeField] float CameraHeight = 3.0f;

    [SerializeField] float angleLimit = 30.0f;
    [SerializeField] float moveSmooth = 8.0f;
    [SerializeField] float lookSmooth = 10.0f;

    [SerializeField] float bossHeight = 10.0f;
    [SerializeField] Transform lookFocusOverride;

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
    /// ボス頭部注視の演出用モードを開始します（位置は外部から設定）。
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
    /// 演出中のカメラ位置を設定します。
    /// </summary>
    public void SetBossWatchCameraPosition(Vector3 worldPosition)
    {
        bossWatchFixedCameraPosition = worldPosition;
        transform.position = worldPosition + shakeOffset;
    }

    /// <summary>
    /// 注視点へ向きを即座に合わせます。
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
    /// カメラ位置は固定し、キャラとボス頭の 2 点の中間だけを注視する投げ演出カメラを開始します。
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
    /// 現在の位置・向きのままカメラを完全に固定します（追従を止める）。
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
    /// カメラ位置を固定し、Boss_Head などの注視点だけ追います。
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
    /// 注視方向を現在の向きで固定します（打ち上げ中など）。
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
    /// 注視方向の固定を解除し、頭部追従に戻します。
    /// </summary>
    public void UnlockBossHeadLookDirection()
    {
        bossWatchLockLook = false;
    }

    /// <summary>
    /// ボス頭部注視カメラモードを終了し、通常追従に戻します。
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

    public void SetPlayerBossReferences(GameObject playerObject, GameObject bossObject)
    {
        player = playerObject;
        Boss = bossObject;
    }

    private void Start()
    {
        MySoundManeger.Play(gameObject, BGMList.BGM_GAME);
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

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            bossWatchLookSmooth * Time.deltaTime
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

        // カメラ位置は固定。向きだけ 2 点の中間へ向ける。
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
