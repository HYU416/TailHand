using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 投擲物に付与し、投擲方向がターゲット方向に近いときだけ
/// 飛行開始時に一度決めた滑らかな曲線軌道でゴールへ誘導する。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class ThrowTrajectoryCorrector : MonoBehaviour
{
    [Header("ターゲット")]
    [Tooltip("投擲時に照準するオブジェクト。投擲確定時の位置がゴールになる")]
    [SerializeField] private GameObject throwTarget;

    [Tooltip("未設定のとき、投擲開始時にシーン内からターゲットを探す（プレハブはシーンの BOSS を直接入れられないため）")]
    [SerializeField] private bool findTargetIfUnset = true;

    [Tooltip("findTargetIfUnset 時に GameObject.Find で探す名前")]
    [SerializeField] private string targetObjectName = "BOSS";

    [Tooltip("ON のときはタグで探す（名前より優先）")]
    [SerializeField] private bool findByTag = true;

    [SerializeField] private string targetTag = "Boss";

    [Header("ゴール位置")]
    [Tooltip("ON のとき、ターゲットの見た目／当たり判定の中心を XZ の基準にする")]
    [SerializeField] private bool useTargetBoundsCenter = true;

    [Tooltip("飛行・ゴールの固定高さ（ワールド Y）。軌道は XZ 平面のみで移動する")]
    [SerializeField] private float goalHeight = 1f;

    [Header("速度")]
    [Tooltip("ON: 投擲直後1フレームから求めた速度を使う。OFF: Flight Speed を使う")]
    [SerializeField] private bool useThrowMeasuredSpeed = true;

    [Tooltip("useThrowMeasuredSpeed が OFF のときの移動速度")]
    [SerializeField, Min(0f)] private float flightSpeed = 10f;

    [Tooltip("最終速度への倍率（投擲測定・固定のどちらにも適用）")]
    [SerializeField, Min(0f)] private float speedMultiplier = 1f;

    [Header("補正判定")]
    [SerializeField, Range(0f, 90f)] private float maxCorrectionAngle = 30f;

    [Header("曲線軌道")]
    [SerializeField, Range(0.1f, 1f)] private float curveTension = 0.45f;
    [SerializeField, Min(8)] private int pathSampleCount = 64;
    [SerializeField] private float maxFlightDistance = 200f;

    [Header("物理")]
    [SerializeField] private bool disableGravityOnFlight = true;
    [SerializeField] private bool alignRotationToVelocity = true;
    [SerializeField] private bool stopFlightOnCollision = true;

    [Header("デバッグ")]
    [SerializeField] private bool drawPathGizmo = true;

    [SerializeField] private List<Component> deleteScripts = new List<Component>();

    enum FlightState
    {
        Idle,
        WaitingForNextFrame,
        Flying,
    }

    FlightState state = FlightState.Idle;

    Rigidbody rb;
    bool hadParent;

    Vector3 p0;
    Vector3 p1;
    Vector3 throwDirection;
    Vector3 goalPosition;
    bool hasLockedGoal;
    float lockedFlightHeight;
    float moveSpeed;

    Vector3[] pathPoints;
    float[] cumulativeLengths;
    float totalPathLength;
    float traveledDistance;
    bool isCorrectedPath;

    bool cachedUseGravity;
    bool cachedIsKinematic;
    float cachedLinearDamping;
    RigidbodyConstraints cachedConstraints;
    CollisionDetectionMode cachedCollisionDetection;

    public GameObject ThrowTarget => throwTarget;
    public bool IsFlying => state == FlightState.Flying;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = GetComponentInChildren<Rigidbody>();
        }

        hadParent = transform.parent != null;
    }

    void OnTransformParentChanged()
    {
        bool hasParent = transform.parent != null;

        if (hasParent)
        {
            hadParent = true;
            EndFlight();
            return;
        }

        if (hadParent)
        {
            BeginThrowCapture();
        }

        hadParent = hasParent;
    }

    void FixedUpdate()
    {
        if (state == FlightState.WaitingForNextFrame)
        {
            FinalizeThrowDirectionAndStartFlight();
        }
        else if (state == FlightState.Flying)
        {
            AdvanceAlongPath(Time.fixedDeltaTime);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (state != FlightState.Flying || !stopFlightOnCollision)
        {
            return;
        }

        EndFlight();
    }

    public void SetThrowTarget(GameObject target)
    {
        throwTarget = target;
    }

    /// <summary>
    /// 親の変更を使わず、手動で投擲開始を通知する場合に呼ぶ。
    /// </summary>
    public void NotifyThrown()
    {
        if (state != FlightState.Idle)
        {
            return;
        }
        BeginThrowCapture();
    }

    void BeginThrowCapture()
    {
        if (state != FlightState.Idle)
        {
            return;
        }

        p0 = transform.position;
        ResolveThrowTarget();
        LockGoalPosition();
        state = FlightState.WaitingForNextFrame;

        if (rb != null && disableGravityOnFlight)
        {
            gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
            rb.useGravity = false;
        }
    }

    void FinalizeThrowDirectionAndStartFlight()
    {
        lockedFlightHeight = goalHeight;
        p1 = FlattenToFlightHeight(transform.position);

        Vector3 displacement = p1 - FlattenToFlightHeight(p0);
        float deltaTime = Time.fixedDeltaTime;

        if (displacement.sqrMagnitude > 0.0001f)
        {
            throwDirection = displacement.normalized;
            moveSpeed = displacement.magnitude / deltaTime;
        }
        else if (rb != null)
        {
            Vector3 flatVelocity = FlattenDirection(rb.linearVelocity);

            if (flatVelocity.sqrMagnitude > 0.0001f)
            {
                throwDirection = flatVelocity;
                moveSpeed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
            }
            else
            {
                ResetFlight();
                return;
            }
        }
        else
        {
            ResetFlight();
            return;
        }

        ApplyFlightSpeed();

        isCorrectedPath = ShouldApplyCorrection();

        BuildPath(isCorrectedPath);
        PrepareRigidbodyForFlight();
        SnapToFlightHeight();
        traveledDistance = 0f;
        state = FlightState.Flying;
        ApplyVelocityAlongPath(GetPathTangent(0f));
    }

    void ResolveThrowTarget()
    {
        if (throwTarget != null)
        {
            return;
        }

        if (!findTargetIfUnset)
        {
            return;
        }

        if (findByTag && !string.IsNullOrEmpty(targetTag))
        {
            throwTarget = GameObject.FindGameObjectWithTag(targetTag);
            return;
        }

        if (!string.IsNullOrEmpty(targetObjectName))
        {
            throwTarget = GameObject.Find(targetObjectName);
        }
    }

    void LockGoalPosition()
    {
        hasLockedGoal = false;

        if (throwTarget == null)
        {
            return;
        }

        Vector3 center = useTargetBoundsCenter
            ? GetTargetBoundsCenter(throwTarget)
            : throwTarget.transform.position;

        goalPosition = new Vector3(center.x, goalHeight, center.z);
        hasLockedGoal = true;
    }

    static Vector3 GetTargetBoundsCenter(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds.center;
        }

        Collider[] colliders = target.GetComponentsInChildren<Collider>();

        if (colliders.Length > 0)
        {
            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++)
            {
                bounds.Encapsulate(colliders[i].bounds);
            }

            return bounds.center;
        }

        return target.transform.position;
    }

    void ApplyFlightSpeed()
    {
        if (!useThrowMeasuredSpeed)
        {
            moveSpeed = flightSpeed;
        }

        moveSpeed *= speedMultiplier;
    }

    bool ShouldApplyCorrection()
    {
        if (!hasLockedGoal)
        {
            return false;
        }

        Vector3 toGoal = goalPosition - FlattenToFlightHeight(p0);
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            return false;
        }

        Vector3 targetDirection = FlattenDirection(toGoal);
        float angle = Vector3.Angle(throwDirection, targetDirection);

        return angle < maxCorrectionAngle;
    }

    void BuildPath(bool useCorrection)
    {
        if (useCorrection)
        {
            BuildCorrectedBezierPath();
        }
        else
        {
            BuildStraightPath();
        }

        BuildArcLengthTable();
    }

    void BuildStraightPath()
    {
        Vector3 flatStart = FlattenToFlightHeight(p1);

        pathPoints = new Vector3[2];
        pathPoints[0] = flatStart;
        pathPoints[1] = flatStart + throwDirection * maxFlightDistance;
    }

    void BuildCorrectedBezierPath()
    {
        Vector3 flatStart = FlattenToFlightHeight(p1);
        Vector3 flatGoal = FlattenToFlightHeight(goalPosition);
        Vector3 targetDirection = FlattenDirection(flatGoal - FlattenToFlightHeight(p0));
        float chord = Vector3.Distance(flatStart, flatGoal);
        float controlLength = Mathf.Max(chord * curveTension, 0.5f);

        Vector3 c1 = flatStart + throwDirection * controlLength;
        Vector3 c2 = flatGoal - targetDirection * controlLength;

        pathPoints = new Vector3[pathSampleCount];

        for (int i = 0; i < pathSampleCount; i++)
        {
            float t = i / (pathSampleCount - 1f);
            pathPoints[i] = FlattenToFlightHeight(
                EvaluateCubicBezier(flatStart, c1, c2, flatGoal, t)
            );
        }
    }

    Vector3 FlattenToFlightHeight(Vector3 position)
    {
        return new Vector3(position.x, lockedFlightHeight, position.z);
    }

    static Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return direction.normalized;
    }

    static Vector3 EvaluateCubicBezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
    {
        float u = 1f - t;
        float uu = u * u;
        float tt = t * t;

        return uu * u * a
             + 3f * uu * t * b
             + 3f * u * tt * c
             + tt * t * d;
    }

    void BuildArcLengthTable()
    {
        cumulativeLengths = new float[pathPoints.Length];
        cumulativeLengths[0] = 0f;

        for (int i = 1; i < pathPoints.Length; i++)
        {
            float segment = Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
            cumulativeLengths[i] = cumulativeLengths[i - 1] + segment;
        }

        totalPathLength = cumulativeLengths[cumulativeLengths.Length - 1];
    }

    void PrepareRigidbodyForFlight()
    {
        if (rb == null)
        {
            return;
        }

        cachedUseGravity = rb.useGravity;
        cachedIsKinematic = rb.isKinematic;
        cachedLinearDamping = rb.linearDamping;
        cachedConstraints = rb.constraints;
        cachedCollisionDetection = rb.collisionDetectionMode;

        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    void RestorePhysics()
    {
        if (rb == null)
        {
            return;
        }

        rb.useGravity = cachedUseGravity;
        rb.isKinematic = cachedIsKinematic;
        rb.linearDamping = cachedLinearDamping;
        rb.constraints = cachedConstraints;
        rb.collisionDetectionMode = cachedCollisionDetection;
    }

    void EndFlight()
    {
        ResetFlight();
        RestorePhysics();
    }

    void SnapToFlightHeight()
    {
        if (rb != null)
        {
            rb.position = FlattenToFlightHeight(rb.position);
            return;
        }

        transform.position = FlattenToFlightHeight(transform.position);
    }

    void AdvanceAlongPath(float deltaTime)
    {
        if (totalPathLength <= 0.0001f)
        {
            return;
        }

        traveledDistance += moveSpeed * deltaTime;

        if (isCorrectedPath && traveledDistance >= totalPathLength)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
            }

            EndFlight();
            return;
        }

        Vector3 tangent = traveledDistance >= totalPathLength
            ? throwDirection
            : GetPathTangent(traveledDistance);

        ApplyVelocityAlongPath(tangent);
    }

    void ApplyVelocityAlongPath(Vector3 direction)
    {
        direction = FlattenDirection(direction);

        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = throwDirection;
        }

        if (rb != null)
        {
            rb.linearVelocity = direction * moveSpeed;
        }

        if (alignRotationToVelocity)
        {
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    Vector3 GetPathTangent(float distance)
    {
        float sampleOffset = Mathf.Min(0.2f, totalPathLength * 0.05f);
        float aheadDistance = Mathf.Min(distance + sampleOffset, totalPathLength);
        Vector3 current = SamplePositionAtDistance(distance);
        Vector3 ahead = SamplePositionAtDistance(aheadDistance);

        return FlattenDirection(ahead - current);
    }

    Vector3 SamplePositionAtDistance(float distance)
    {
        if (pathPoints.Length == 1)
        {
            return pathPoints[0];
        }

        for (int i = 1; i < cumulativeLengths.Length; i++)
        {
            if (distance > cumulativeLengths[i])
            {
                continue;
            }

            float segmentStart = cumulativeLengths[i - 1];
            float segmentLength = cumulativeLengths[i] - segmentStart;

            if (segmentLength <= 0.0001f)
            {
                return pathPoints[i];
            }

            float t = (distance - segmentStart) / segmentLength;
            return Vector3.Lerp(pathPoints[i - 1], pathPoints[i], t);
        }

        return pathPoints[pathPoints.Length - 1];
    }

    void ResetFlight()
    {
        state = FlightState.Idle;
        traveledDistance = 0f;
        isCorrectedPath = false;
        hasLockedGoal = false;
        pathPoints = null;
        cumulativeLengths = null;
        totalPathLength = 0f;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawPathGizmo || pathPoints == null || pathPoints.Length < 2)
        {
            return;
        }

        Gizmos.color = Color.cyan;

        for (int i = 1; i < pathPoints.Length; i++)
        {
            Gizmos.DrawLine(pathPoints[i - 1], pathPoints[i]);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(p0, 0.12f);

        if (state != FlightState.Idle)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(goalPosition, 0.15f);
        }
    }

    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetupFalse()
    {
        findTargetIfUnset = false;
        findByTag = false;
    }
}
