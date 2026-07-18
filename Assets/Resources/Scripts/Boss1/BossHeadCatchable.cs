using UnityEngine;

public class BossHeadCatchable : MonoBehaviour
{
    [Header("この頭を掴めるか")]
    [SerializeField] private bool canCatch;

    [Header("掴んだ時の位置補正")]
    [SerializeField] private Vector3 catchLocalPositionOffset = new Vector3(0f, 1.0f, 0f);

    [Header("掴んだ時の回転補正")]
    [SerializeField] private Vector3 catchLocalRotationOffset = Vector3.zero;

    [Header("掴んでいる間、追加で上げる高さ")]
    [SerializeField] private float holdUpOffset = 1.0f;

    [Header("掴んでいる間、地面から最低どれだけ浮かせるか")]
    [SerializeField] private float groundKeepHeight = 0.15f;

    [Header("地面判定に使うLayer")]
    [SerializeField] private LayerMask groundLayerMask = ~0;

    [Header("地面判定Rayの開始高さ")]
    [SerializeField] private float groundRayStartHeight = 10.0f;

    [Header("地面判定Rayの距離")]
    [SerializeField] private float groundRayDistance = 50.0f;

    [Header("地面が見つからない時の最低Y座標")]
    [SerializeField] private float fallbackMinimumWorldY = 0.2f;

    [Header("掴んでいる間の追従速度")]
    [SerializeField] private float followSpeed = 30.0f;

    [Header("掴んでいる間の回転追従速度")]
    [SerializeField] private float rotateFollowSpeed = 30.0f;

    [Header("離した時の投げ飛ばし倍率")]
    [SerializeField] private float throwPowerMultiplier = 1.5f;

    [Header("離した時に少し上へ飛ばす力")]
    [SerializeField] private float releaseUpPower = 2.0f;

    [Header("離した時の最大速度")]
    [SerializeField] private float maxReleaseVelocity = 20.0f;

    [Header("掴んでいる間はColliderをTriggerにする")]
    [SerializeField] private bool makeColliderTriggerWhileCaught = true;

    [Header("デバッグ")]
    [SerializeField] private bool showDebugLog = true;

    private Rigidbody cachedRigidbody;
    private Collider[] cachedColliders;

    private bool isCaught;
    private Transform currentCatchPoint;

    private bool originalUseGravity;
    private bool originalIsKinematic;
    private bool[] originalColliderTriggerStates;

    private Vector3 previousCatchPointPosition;
    private Vector3 catchPointVelocity;

    public bool CanCatch
    {
        get { return canCatch; }
    }

    public Vector3 CatchLocalPositionOffset
    {
        get { return catchLocalPositionOffset; }
    }

    public Vector3 CatchLocalRotationOffset
    {
        get { return catchLocalRotationOffset; }
    }

    public bool IsCaught
    {
        get { return isCaught; }
    }

    private void Awake()
    {
        cachedRigidbody = GetComponent<Rigidbody>();

        if (cachedRigidbody == null)
        {
            cachedRigidbody = GetComponentInParent<Rigidbody>();
        }

        cachedColliders = GetComponentsInChildren<Collider>();

        if (cachedColliders != null && cachedColliders.Length > 0)
        {
            originalColliderTriggerStates = new bool[cachedColliders.Length];

            for (int i = 0; i < cachedColliders.Length; i++)
            {
                if (cachedColliders[i] != null)
                {
                    originalColliderTriggerStates[i] = cachedColliders[i].isTrigger;
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (!isCaught)
        {
            return;
        }

        if (currentCatchPoint == null)
        {
            ForceRelease();
            return;
        }

        UpdateCatchPointVelocity();
        FollowCatchPoint();
    }

    public void SetCanCatch(bool value)
    {
        canCatch = value;

        if (showDebugLog)
        {
            Debug.Log("BossHeadCatchable: 掴める状態 = " + canCatch);
        }
    }

    public void Catch(Transform catchPoint)
    {
        if (!canCatch)
        {
            return;
        }

        if (catchPoint == null)
        {
            return;
        }

        currentCatchPoint = catchPoint;
        isCaught = true;

        previousCatchPointPosition = currentCatchPoint.position;
        catchPointVelocity = Vector3.zero;

        if (cachedRigidbody != null)
        {
            originalUseGravity = cachedRigidbody.useGravity;
            originalIsKinematic = cachedRigidbody.isKinematic;

            cachedRigidbody.linearVelocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
            cachedRigidbody.useGravity = false;
            cachedRigidbody.isKinematic = true;
        }

        SetCollidersTrigger(true);

        FollowCatchPointImmediately();

        if (showDebugLog)
        {
            Debug.Log("BossHeadCatchable: 頭を掴みました");
        }
    }

    public void Release()
    {
        if (!isCaught)
        {
            return;
        }

        Vector3 releaseVelocity = catchPointVelocity * throwPowerMultiplier;
        releaseVelocity += Vector3.up * releaseUpPower;

        if (releaseVelocity.magnitude > maxReleaseVelocity)
        {
            releaseVelocity = releaseVelocity.normalized * maxReleaseVelocity;
        }

        isCaught = false;
        currentCatchPoint = null;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = originalIsKinematic;
            cachedRigidbody.useGravity = true;

            cachedRigidbody.linearVelocity = releaseVelocity;
            cachedRigidbody.angularVelocity = Random.insideUnitSphere * 8.0f;
        }

        SetCollidersTrigger(false);

        if (showDebugLog)
        {
            Debug.Log("BossHeadCatchable: 頭を離しました。速度 = " + releaseVelocity);
        }
    }

    public void ForceRelease()
    {
        if (!isCaught)
        {
            return;
        }

        isCaught = false;
        currentCatchPoint = null;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = originalIsKinematic;
            cachedRigidbody.useGravity = true;
        }

        SetCollidersTrigger(false);

        if (showDebugLog)
        {
            Debug.Log("BossHeadCatchable: 強制的に頭を離しました");
        }
    }

    private void UpdateCatchPointVelocity()
    {
        if (currentCatchPoint == null)
        {
            catchPointVelocity = Vector3.zero;
            return;
        }

        if (Time.deltaTime <= 0f)
        {
            catchPointVelocity = Vector3.zero;
            return;
        }

        catchPointVelocity =
            (currentCatchPoint.position - previousCatchPointPosition) / Time.deltaTime;

        previousCatchPointPosition = currentCatchPoint.position;
    }

    private void FollowCatchPoint()
    {
        Vector3 targetPosition =
            currentCatchPoint.TransformPoint(catchLocalPositionOffset) +
            Vector3.up * holdUpOffset;

        Quaternion targetRotation =
            currentCatchPoint.rotation *
            Quaternion.Euler(catchLocalRotationOffset);

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotateFollowSpeed * Time.deltaTime
        );

        KeepAboveGround();
    }

    private void FollowCatchPointImmediately()
    {
        if (currentCatchPoint == null)
        {
            return;
        }

        Vector3 targetPosition =
            currentCatchPoint.TransformPoint(catchLocalPositionOffset) +
            Vector3.up * holdUpOffset;

        Quaternion targetRotation =
            currentCatchPoint.rotation *
            Quaternion.Euler(catchLocalRotationOffset);

        transform.position = targetPosition;
        transform.rotation = targetRotation;

        KeepAboveGround();
    }

    private void KeepAboveGround()
    {
        float groundY = GetGroundY();
        float requiredBottomY = groundY + groundKeepHeight;
        float currentBottomY = GetCurrentBottomY();

        if (currentBottomY >= requiredBottomY)
        {
            return;
        }

        float pushUpAmount = requiredBottomY - currentBottomY;
        transform.position += Vector3.up * pushUpAmount;
    }

    private float GetGroundY()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * groundRayStartHeight;

        RaycastHit hit;

        bool hitGround = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hit,
            groundRayDistance,
            groundLayerMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitGround)
        {
            return hit.point.y;
        }

        return fallbackMinimumWorldY;
    }

    private float GetCurrentBottomY()
    {
        if (cachedColliders == null || cachedColliders.Length == 0)
        {
            return transform.position.y;
        }

        bool foundCollider = false;
        float bottomY = transform.position.y;

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] == null)
            {
                continue;
            }

            if (!cachedColliders[i].enabled)
            {
                continue;
            }

            if (!foundCollider)
            {
                bottomY = cachedColliders[i].bounds.min.y;
                foundCollider = true;
            }
            else
            {
                bottomY = Mathf.Min(bottomY, cachedColliders[i].bounds.min.y);
            }
        }

        if (!foundCollider)
        {
            return transform.position.y;
        }

        return bottomY;
    }

    private void SetCollidersTrigger(bool caught)
    {
        if (!makeColliderTriggerWhileCaught)
        {
            return;
        }

        if (cachedColliders == null)
        {
            return;
        }

        for (int i = 0; i < cachedColliders.Length; i++)
        {
            if (cachedColliders[i] == null)
            {
                continue;
            }

            if (caught)
            {
                cachedColliders[i].isTrigger = true;
            }
            else
            {
                if (originalColliderTriggerStates != null &&
                    i < originalColliderTriggerStates.Length)
                {
                    cachedColliders[i].isTrigger = originalColliderTriggerStates[i];
                }
            }
        }
    }
}