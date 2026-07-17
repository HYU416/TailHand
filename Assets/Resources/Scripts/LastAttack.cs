using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// 最終攻撃 QTE の回転入力・投げ演出を担当します。
/// </summary>
public class LastAttack : MonoBehaviour
{
    [Header("入力（つかむボタン連打）")]
    [Tooltip("固定される回転の向き。ON で逆回転にします")]
    [SerializeField] private bool invertSpinDirection = true;
    [Tooltip("つかむボタンを1回押すごとに加わる回転速度")]
    [SerializeField, Min(0f)] private float spinImpulsePerRevolution = 520f;
    [Tooltip("押していない間に回転速度が減衰する速さ")]
    [SerializeField, Min(0f)] private float spinDecay = 95f;
    [Tooltip("回転速度の上限")]
    [SerializeField, Min(0f)] private float maxSpinSpeed = 1800f;

    [Header("回転のしやすさ（連打回数連動）")]
    [Tooltip("この連打回数で最大のしやすさに到達します")]
    [SerializeField, Min(0.25f)] private float revolutionsForFullEase = 30f;
    [Tooltip("1より大きいほど最初の上昇が緩やかになります")]
    [SerializeField, Min(1f)] private float easeCurvePower = 3.8f;
    [SerializeField, Range(0.05f, 1f)] private float minAccelerationMultiplier = 0.12f;
    [SerializeField, Range(0.2f, 1f)] private float minMaxSpeedMultiplier = 0.28f;
    [SerializeField, Range(0.3f, 1f)] private float maxDecayMultiplierAtFullEase = 0.35f;

    [Header("投げ")]
    [Tooltip("この回転速度に達したら時間停止せずシームレスに投げへ移行します")]
    [SerializeField, Min(0f)] private float minThrowSpinSpeed = 360f;
    [SerializeField, Min(0f)] private float throwAnimationFallbackDuration = 2.0f;
    [SerializeField, Min(0f)] private float throwToIdleFallbackDuration = 1.5f;
    [SerializeField, Min(0f)] private float postThrowIdleFallbackDuration = 0.5f;

    [Header("見た目")]
    [SerializeField] private Vector3 spinAxis = Vector3.up;
    [SerializeField] private Transform spinVisualRoot;
    [SerializeField] private float lookHeightOffset = 1.5f;
    [SerializeField, Min(0f)] private float minLoopAnimSpeed = 0.2f;
    [SerializeField, Min(0f)] private float maxLoopAnimSpeed = 3.5f;

    [Header("カメラ")]
    [SerializeField, Min(0f)] private float maxExtraCameraBack = 8f;

    [Header("投げ演出")]
    [SerializeField] private QTEThrowAftermathSettings throwAftermath = new QTEThrowAftermathSettings();

    [Header("サウンド")]
    [SerializeField]
    private AnimationCurve rollSEIntervalCurve = new AnimationCurve(
    new Keyframe(0f, 0.28f),
    new Keyframe(0.25f, 0.15f),
    new Keyframe(0.5f, 0.08f),
    new Keyframe(1f, 0.025f)
);

    private float nextRollSETime;
    private AudioSource currentRollSE;

    LastAttackMotion motion;
    CameraFollow activeCameraFollow;
    PlayerInput playerInput;
    InputAction throwAction;

    float rotationSpeed;
    float cumulativeSpinDegrees;
    bool isRunning;
    bool hasThrown;
    bool maxSpinReached;
    bool waitingForThrowInput;
    float savedTimeScale = 1f;
    private AudioSource endBGM;
    private bool endBGMScheduled;

    public float LookHeightOffset => lookHeightOffset;

    public void ConfigureThrowAftermath(QTEThrowAftermathSettings settings)
    {
        if (settings == null)
        {
            return;
        }

        throwAftermath = settings;
    }

    /// <summary>
    /// 回転まわりの主要パラメータを外部（QTESceneController）から設定します。
    /// </summary>
    public void ConfigureSpin(
        float maxSpinSpeed,
        float spinImpulsePerRevolution,
        float spinDecay,
        float revolutionsForFullEase,
        float minThrowSpinSpeed)
    {
        this.maxSpinSpeed = Mathf.Max(0f, maxSpinSpeed);
        this.spinImpulsePerRevolution = Mathf.Max(0f, spinImpulsePerRevolution);
        this.spinDecay = Mathf.Max(0f, spinDecay);
        this.revolutionsForFullEase = Mathf.Max(0.25f, revolutionsForFullEase);
        this.minThrowSpinSpeed = Mathf.Max(0f, minThrowSpinSpeed);
    }

    public void InitializeVisuals()
    {
        if (spinVisualRoot == null)
        {
            SetupSpinPivot();
        }
    }

    void Awake()
    {
        motion = GetComponent<LastAttackMotion>();
        if (motion == null)
        {
            motion = gameObject.AddComponent<LastAttackMotion>();
        }
        GameObject cameraObject = Camera.main.gameObject;

        // 事前に生成して停止しておく
        endBGM = MySoundManeger.Play( cameraObject,BGMList.BGM_FINISHER_END);
        if (endBGM != null)
        {
            endBGM.Stop();
            endBGM.timeSamples = 0;
        }

       
    }

    void SetupSpinPivot()
    {
        Transform modelRoot = motion != null ? motion.GetBossSpinTransform() : null;

        if (modelRoot == null)
        {
            return;
        }

        Transform existingPivot = modelRoot.parent;

        if (existingPivot != null && existingPivot.name == "QTESpinPivot")
        {
            spinVisualRoot = existingPivot;
            return;
        }

        GameObject spinPivotObject = new GameObject("QTESpinPivot");
        Transform spinPivotTransform = spinPivotObject.transform;
        spinPivotTransform.SetParent(transform, false);
        spinPivotTransform.localPosition = modelRoot.localPosition;
        spinPivotTransform.localRotation = Quaternion.identity;
        spinPivotTransform.localScale = Vector3.one;
        modelRoot.SetParent(spinPivotTransform, true);
        spinVisualRoot = spinPivotTransform;
    }

    public IEnumerator RunMinigameRoutine(CameraFollow cameraFollow)
    {
        if (isRunning)
        {
            yield break;
        }

        isRunning = true;
        hasThrown = false;
        activeCameraFollow = cameraFollow;
        rotationSpeed = 0f;
        cumulativeSpinDegrees = 0f;
        maxSpinReached = false;
        waitingForThrowInput = false;
        savedTimeScale = 1f;

        BindInput();
        ResetAnimator();
        motion.SetState(LastAttackAnimeState.Loop);
        SetAnimatorSpeed(minLoopAnimSpeed);
        motion.CaptureStartOrientation();

        while (!hasThrown)
        {
            UpdateMashInput();
            ApplySpinVisuals();
            UpdateLoopAnimation();
            UpdateRollSEInterval();
            UpdateCameraPullback(cameraFollow);
            CheckThrowThresholdReached();

            yield return null;
        }

        yield return PlayThrowRoutine();

        isRunning = false;
    }

    public IEnumerator PlaySlowLoopPreviewRoutine(CameraFollow cameraFollow)
    {
        if (isRunning)
        {
            yield break;
        }

        isRunning = true;
        hasThrown = false;
        activeCameraFollow = cameraFollow;
        maxSpinReached = false;
        waitingForThrowInput = false;
        savedTimeScale = 1f;

        BindInput();
        ResetAnimator();
        InitializeVisuals();
        motion.CaptureStartOrientation();
        EnterMaxSpinPhase();

        while (!hasThrown)
        {
            UpdateMaxPhaseAnimation();
            UpdateCameraPullback(cameraFollow);
            TryThrowOnPress();
            yield return null;
        }

        RestoreTimeScale();
        yield return PlayThrowRoutine();
        isRunning = false;
    }

    void CheckThrowThresholdReached()
    {
        if (hasThrown)
        {
            return;
        }

        float threshold = minThrowSpinSpeed > 0f ? minThrowSpinSpeed : maxSpinSpeed;

        if (rotationSpeed >= threshold)
        {
            hasThrown = true;
        }
    }

    void EnterMaxSpinPhase()
    {
        maxSpinReached = true;
        waitingForThrowInput = true;
        rotationSpeed = 0f;

        savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        motion.SetState(LastAttackAnimeState.Loop);
        SetAnimatorSpeed(1f);
    }

    void UpdateMaxPhaseAnimation()
    {
        motion.SetState(LastAttackAnimeState.Loop);
    }

    void TryThrowOnPress()
    {
        if (throwAction == null)
        {
            return;
        }

        if (throwAction != null && (throwAction.triggered || throwAction.WasPressedThisFrame()))
        {
            waitingForThrowInput = false;
            hasThrown = true;
        }
    }

    void RestoreTimeScale()
    {
        Time.timeScale = savedTimeScale > 0f ? savedTimeScale : 1f;
    }

    void ResetSpinVisualRotation()
    {
        if (spinVisualRoot == null)
        {
            return;
        }

        spinVisualRoot.localRotation = Quaternion.identity;
    }

    void BindInput()
    {
        if (playerInput == null)
        {
            playerInput = Object.FindObjectOfType<PlayerInput>();
        }

        if (playerInput == null)
        {
            Debug.LogWarning("LastAttack: PlayerInput が見つかりません");
            return;
        }

        throwAction = playerInput.actions["Catch"];
        throwAction.Enable();
    }

    void ResetAnimator()
    {
        if (motion != null)
        {
            motion.ResetAnimators();
        }
    }

    void UpdateMashInput()
    {
        float ease = GetSpinEase();
        float decay = spinDecay * Mathf.Lerp(1f, maxDecayMultiplierAtFullEase, ease);

        // 押していない間は減衰。連打し続けることで速度を維持・上昇させる。
        rotationSpeed = Mathf.MoveTowards(rotationSpeed, 0f, decay * Time.deltaTime);

        if (throwAction != null && throwAction.WasPressedThisFrame())
        {
            // 連打1回を「1周ぶんの進捗」として扱い、連打回数に応じて回しやすさを上げる。
            cumulativeSpinDegrees += 360f;

            ease = GetSpinEase();
            float accelerationMultiplier = Mathf.Lerp(minAccelerationMultiplier, 1f, ease);
            rotationSpeed += spinImpulsePerRevolution * accelerationMultiplier;
        }

        rotationSpeed = Mathf.Clamp(rotationSpeed, 0f, maxSpinSpeed);
        UpdateRollSEInterval();
    }

    float GetSpinEase()
    {
        float targetDegrees = revolutionsForFullEase * 360f;

        if (targetDegrees <= 0f)
        {
            return 1f;
        }

        float t = Mathf.Clamp01(cumulativeSpinDegrees / targetDegrees);
        return Mathf.Pow(t, easeCurvePower);
    }

    void ApplySpinVisuals()
    {
        if (Mathf.Abs(rotationSpeed) <= 0.01f)
        {
            return;
        }

        Transform rotateTarget = spinVisualRoot != null ? spinVisualRoot : transform;
        float directionSign = invertSpinDirection ? -1f : 1f;
        rotateTarget.Rotate(spinAxis.normalized, directionSign * rotationSpeed * Time.deltaTime, Space.Self);
    }

    void UpdateLoopAnimation()
    {
        motion.SetState(LastAttackAnimeState.Loop);

        float normalized = Mathf.InverseLerp(0f, maxSpinSpeed, Mathf.Abs(rotationSpeed));
        SetAnimatorSpeed(Mathf.Lerp(minLoopAnimSpeed, maxLoopAnimSpeed, normalized));
    }

    void SetAnimatorSpeed(float speed)
    {
        if (motion != null)
        {
            motion.SetAnimatorSpeed(speed);
        }
    }

    void UpdateCameraPullback(CameraFollow cameraFollow)
    {
        if (cameraFollow == null)
        {
            return;
        }

        float normalized = Mathf.InverseLerp(0f, maxSpinSpeed, Mathf.Abs(rotationSpeed));
        cameraFollow.SetQTECameraBackExtra(normalized * maxExtraCameraBack);
    }

    void UpdateRollSEInterval()
    {
        float normalized = Mathf.InverseLerp(0f, maxSpinSpeed, Mathf.Abs(rotationSpeed));
        float interval = rollSEIntervalCurve.Evaluate(normalized);
        interval = Mathf.Max(0.01f, interval);

        if (Time.time < nextRollSETime)
        {
            return;
        }

        currentRollSE = MySoundManeger.Play(Camera.main.gameObject, SEList.SE_ROLL);
        nextRollSETime = Time.time + interval;
    }

    void StopRollSE()
    {
        if (currentRollSE == null)
        {
            return;
        }
        currentRollSE.Stop();
        Destroy(currentRollSE.gameObject);
        currentRollSE = null;
    }

    public void StartFinisherEndBGM()
    {
        if (endBGMScheduled)
        {
            return;
        }

        GameObject cameraObject = Camera.main.gameObject;
        AudioSource loopBGM = MySoundManeger.GetBGM(cameraObject, BGMList.BGM_FINISHER_LOOP);
        if (loopBGM == null || endBGM == null)
        {
            Debug.LogWarning("FINISHER用BGMを取得できません");
            return;
        }

        endBGMScheduled = true;
        // 現在の1周を最後にする
        loopBGM.loop = false;

        double remainingTime =
            (double)(loopBGM.clip.samples - loopBGM.timeSamples)
            / loopBGM.clip.frequency
            / Mathf.Abs(loopBGM.pitch);

        double switchTime = AudioSettings.dspTime + remainingTime;

        loopBGM.SetScheduledEndTime(switchTime);

        endBGM.Stop();
        endBGM.timeSamples = 0;
        endBGM.PlayScheduled(switchTime);
    }

    IEnumerator PlayThrowRoutine()
    {
        StopRollSE();

        Time.timeScale = 1f;
        rotationSpeed = 0f;
        ResetSpinVisualRotation();
        motion.RestoreStartOrientation();

        LastAttackUIManager uiManager = FindObjectOfType<LastAttackUIManager>(true);
        uiManager?.HideMashPrompt();

        if (motion == null || !motion.HasConfiguredAnimators())
        {
            Debug.LogError("LastAttack: Animator が未設定です。QTESceneController のモデル/アニメーション設定を確認してください。");
            yield break;
        }

        // 投げモーション開始時にカメラを Cube へ移動し、プレイヤーを配置する。
        QTEThrowAftermath.ApplyCameraAndPlayer(throwAftermath, activeCameraFollow, transform, motion);
        motion.PlayThrowImmediate();
        //メインカメラを取得
        StartFinisherEndBGM();



        yield return null;

        // Throw クリップ終了（ThrowToIdle 開始）を基準に、遅延で前後調整する。
        // 例: -0.3 → Throw 終了の 0.3 秒前に飛ばす / +0.2 → 終了の 0.2 秒後に飛ばす
        float throwLength = motion.GetPlayerThrowClipLength();

        if (throwLength <= 0f)
            throwLength = throwAnimationFallbackDuration;

        float launchDelay = throwAftermath != null ? throwAftermath.bossHeadLaunchDelay : 0f;
        float launchAt = Mathf.Max(0f, throwLength + launchDelay);
        float throwElapsed = 0f;

        while (throwElapsed < launchAt)
        {
            throwElapsed += Time.deltaTime;
            yield return null;
        }

        QTEThrowAftermath.LaunchHead(throwAftermath, activeCameraFollow, transform, motion);

        float maxWaitDuration =
            throwToIdleFallbackDuration
            + postThrowIdleFallbackDuration;
        float elapsed = 0f;

        while (elapsed < maxWaitDuration)
        {
            elapsed += Time.deltaTime;

            if (motion.IsThrowSequenceFinished())
            {
                yield break;
            }

            yield return null;
        }

        Debug.LogWarning(
            "LastAttack: Throw -> ThrowToIdle -> Idle の遷移が完了しませんでした。"
            + " クリップ設定を確認してください。"
        );
    }

    public IEnumerator PlayThrowPreviewRoutine()
    {
        yield return PlayThrowRoutine();
    }
}
