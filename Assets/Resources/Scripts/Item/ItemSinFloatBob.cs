using UnityEngine;

/// <summary>
/// アイテムが出現してからプレイヤーにキャッチ・投げられるまでの間、
/// サインカーブで上下にふわふわさせ、Y軸を一定速度で回転させます。
/// Rubble / Obsidian / Flint などのプレハブにアタッチしてください。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(100)]
public class ItemSinFloatBob : MonoBehaviour
{
    const float TwoPi = Mathf.PI * 2f;

    enum FloatState
    {
        WaitingToSettle,
        Floating,
        Stopped,
    }

    [Header("開始条件")]
    [Tooltip("ON のとき着地待ちをせず、出現直後からふわふわを開始します")]
    [SerializeField] private bool startImmediately = false;

    [Tooltip("この速度以下で一定時間続いたら着地完了とみなします")]
    [SerializeField] private float settleVelocityThreshold = 0.15f;

    [Tooltip("着地判定に必要な秒数")]
    [SerializeField, Min(0f)] private float settleDuration = 0.3f;

    [Header("上下のふわふわ")]
    [SerializeField, Min(0f)] private float verticalAmplitude = 0.25f;

    [Tooltip("1秒あたりの周期数")]
    [SerializeField, Min(0f)] private float verticalFrequency = 1.2f;

    [Header("Y軸回転")]
    [Tooltip("Y軸を中心に回転する速度（度/秒）")]
    [SerializeField] private float yRotationSpeed = 90f;

    Rigidbody rb;
    FloatState state = FloatState.WaitingToSettle;
    float settleTimer;
    float phaseTime;
    float yRotationAngle;
    Vector3 anchorPosition;
    Quaternion anchorRotation;
    bool hadParent;

    public bool IsFloating => state == FloatState.Floating;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hadParent = transform.parent != null;
    }

    void Start()
    {
        if (startImmediately)
        {
            BeginFloating();
        }
    }

    void FixedUpdate()
    {
        if (state != FloatState.WaitingToSettle)
        {
            return;
        }

        if (rb.linearVelocity.sqrMagnitude <= settleVelocityThreshold * settleVelocityThreshold)
        {
            settleTimer += Time.fixedDeltaTime;

            if (settleTimer >= settleDuration)
            {
                BeginFloating();
            }
        }
        else
        {
            settleTimer = 0f;
        }
    }

    void LateUpdate()
    {
        if (state != FloatState.Floating)
        {
            return;
        }

        phaseTime += Time.deltaTime;

        float yOffset = Mathf.Sin(phaseTime * verticalFrequency * TwoPi) * verticalAmplitude;
        transform.position = anchorPosition + Vector3.up * yOffset;

        yRotationAngle += yRotationSpeed * Time.deltaTime;
        transform.rotation = anchorRotation * Quaternion.Euler(0f, yRotationAngle, 0f);
    }

    void OnTransformParentChanged()
    {
        bool hasParent = transform.parent != null;

        if (hasParent)
        {
            hadParent = true;
            StopFloating();
            return;
        }

        if (hadParent)
        {
            StopFloating();
        }
    }

    void OnDisable()
    {
        StopFloating();
    }

    /// <summary>
    /// ふわふわ浮遊を開始します。
    /// </summary>
    public void BeginFloating()
    {
        if (state == FloatState.Stopped || state == FloatState.Floating)
        {
            return;
        }

        state = FloatState.Floating;
        phaseTime = 0f;
        yRotationAngle = 0f;
        anchorPosition = transform.position;
        anchorRotation = transform.rotation;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    /// <summary>
    /// ふわふわ浮遊を停止します。
    /// </summary>
    public void StopFloating()
    {
        if (state == FloatState.Stopped)
        {
            return;
        }

        state = FloatState.Stopped;
    }
}
