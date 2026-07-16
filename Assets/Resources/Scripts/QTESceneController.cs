using System.Collections;
using UnityEngine;

/// <summary>
/// QTEScene の初期配置と QTE 本体の切り替えを管理します。
/// </summary>
public class QTESceneController : MonoBehaviour
{
    [Header("QTE モデル")]
    [Tooltip("一体 Prefab を使う場合はこちら。未設定なら Player/Boss モデルを組み立てます")]
    [SerializeField] private GameObject comboModelPrefab;
    [Tooltip("キャラモデル（fbx_Player_Fin など）")]
    [SerializeField] private GameObject playerModelPrefab;
    [Tooltip("ボスモデル（fbx_Boss_1_Fin など）")]
    [SerializeField] private GameObject bossModelPrefab;
    [SerializeField] private Transform comboSpawnPoint;
    [SerializeField, Min(0.1f)] private float comboModelScale = 5f;

    [Header("QTE アニメーション共通")]
    [Tooltip("未設定なら Resources/Animators/Last_Attack を使用")]
    [SerializeField] private RuntimeAnimatorController qteAnimatorController;

    [Header("QTE プレイヤーアニメ")]
    [SerializeField] private AnimationClip playerLoopClip;
    [SerializeField] private AnimationClip playerThrowClip;
    [SerializeField] private AnimationClip playerThrowToIdleClip;
    [SerializeField] private AnimationClip playerPostThrowIdleClip;

    [Header("QTE ボスアニメ")]
    [SerializeField] private AnimationClip bossLoopClip;
    [SerializeField] private AnimationClip bossThrowClip;

    [Header("参照")]
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject boss;
    [SerializeField] private Transform bossHead;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private GameClear gameClear;

    [Header("初期配置")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform bossSpawnPoint;
    [SerializeField] private Transform qteCenterPoint;
    [SerializeField] private Vector3 fallbackPlayerSpawn = new Vector3(-25f, 0f, 0f);
    [SerializeField] private Vector3 fallbackBossSpawn = new Vector3(0f, -13f, 0f);
    [SerializeField] private Vector3 fallbackQteCenter = new Vector3(0f, 0f, 0f);

    [Header("初期カメラ")]
    [Tooltip("シーン開始時のカメラ位置。Empty を置いて割り当ててください")]
    [SerializeField] private Transform initialCameraPoint;
    [Tooltip("ON なら Initial Camera Point の向きを使う。OFF なら Boss Head を見る")]
    [SerializeField] private bool useInitialCameraRotation;
    [Tooltip("QTE 開始までカメラを固定する")]
    [SerializeField] private bool lockInitialCameraUntilQTE = true;

    [Header("QTE 開始")]
    [SerializeField] private bool autoStartQTEOnSceneLoad = true;
    [SerializeField, Min(0f)] private float autoStartDelay = 0.35f;
    [SerializeField, Min(0f)] private float contactDistance = 2.5f;
    [SerializeField, Min(0f)] private float qteStartCameraBackOffset = 2.5f;

    [Header("完了後")]
    [SerializeField] private bool useGameClearOnComplete = true;
    [SerializeField] private bool loadSceneOnComplete = false;
    [SerializeField] private string returnSceneName = SceneLoader.SceneName.GameScene;

    [Header("QTE 回転")]
    [Tooltip("回転速度の上限")]
    [SerializeField, Min(0f)] private float maxSpinSpeed = 1800f;
    [Tooltip("スティック1周ごとに加わる回転速度")]
    [SerializeField, Min(0f)] private float spinImpulsePerRevolution = 520f;
    [Tooltip("回転の減衰（放置で緩む速さ）")]
    [SerializeField, Min(0f)] private float spinDecay = 95f;
    [Tooltip("この回転数で最大到達（回しやすさMAX）")]
    [SerializeField, Min(0.25f)] private float revolutionsForFullEase = 30f;
    [Tooltip("投げに必要な最低回転速度")]
    [SerializeField, Min(0f)] private float minThrowSpinSpeed = 360f;

    [Header("投げ演出")]
    [Tooltip("投げ瞬間にカメラを移動する位置（Cube など）。角度の基準に使います")]
    [SerializeField] private Transform throwCameraPoint;
    [Tooltip("ON: キャラとボス頭の両方がフレームに収まるようにする（ゲームカメラ風）")]
    [SerializeField] private bool frameBothTargets = true;
    [Tooltip("2 点が離れるほどカメラを引く倍率")]
    [SerializeField, Min(0f)] private float frameDistanceFactor = 1.2f;
    [Tooltip("最低限のカメラ距離に加える余白")]
    [SerializeField, Min(0f)] private float framePadding = 4f;
    [Tooltip("カメラの最小距離")]
    [SerializeField, Min(0.1f)] private float frameMinDistance = 5f;
    [Tooltip("カメラを注視点より高くする量")]
    [SerializeField] private float frameCameraHeight = 1.5f;
    [Tooltip("カメラが地面下に沈まないための最低ワールド高さ")]
    [SerializeField] private float frameMinHeight = 1f;
    [Tooltip("ON: フレーミングをなめらかに追従。OFF: 即座に合わせる")]
    [SerializeField] private bool frameSmooth = true;
    [Tooltip("ON: カメラをキャラクター中央に向ける。OFF: Cube の向きを使う（frameBothTargets が OFF のとき）")]
    [SerializeField] private bool aimCameraAtPlayer = true;
    [Tooltip("注視点の高さオフセット")]
    [SerializeField] private float throwCameraLookHeight = 1.5f;
    [SerializeField] private Transform playerThrowMovePoint;
    [Tooltip("飛ばすボス頭オブジェクト（頭単体の Mesh 付きモデルを推奨）")]
    [SerializeField] private Transform bossHeadOverride;
    [Tooltip("ThrowToIdle 基準のオフセット秒。負=前にずらす / 正=後にずらす（例: -0.3 で 0.3 秒早く飛ばす）")]
    [SerializeField] private float bossHeadLaunchDelay = 0f;
    [SerializeField, Min(0f)] private float bossHeadLaunchSpeed = 35f;
    [Tooltip("ON: カメラ(Cube)と逆方向（奥）へ飛ばす。OFF: 下の固定方向を使う")]
    [SerializeField] private bool launchAwayFromCamera = false;
    [Tooltip("カメラ逆方向に加える上向き成分（0 に近いほど直進的）")]
    [SerializeField] private float bossHeadLaunchUpward = 0f;
    [Tooltip("頭を飛ばすワールド固定方向。まっすぐ飛ばすため水平方向を推奨。カメラ(Cube)手前(+X)ではなく画面を横切る方向にすると両方フレームに残ります")]
    [SerializeField] private Vector3 bossHeadLaunchDirection = new Vector3(0.73f, 0f, 0.68f);
    [Tooltip("OFF: 重力を無視してワールド座標基準でまっすぐ飛ばす")]
    [SerializeField] private bool bossHeadUseGravity = false;
    [Tooltip("キャラからこの距離以上離れたら頭とカメラを止める（0 で無効）")]
    [SerializeField, Min(0f)] private float bossHeadStopDistance = 16f;
    [Tooltip("頭が止まったときに再生する爆発エフェクト（LAST_boom）")]
    [SerializeField] private GameObject bossHeadStopEffectPrefab;
    [Tooltip("エフェクト位置を頭の中心からずらすオフセット（ワールド座標）。Y を上げるとエフェクトが上に出ます")]
    [SerializeField] private Vector3 bossHeadStopEffectOffset = Vector3.zero;

    [Header("QTE UI")]
    [SerializeField] private LastAttackUIManager mashPromptUI;
    [Tooltip("ON: WIN 後の NEXT / QUIT 選択 UI を使う（GameClear は使わない）")]
    [SerializeField] private bool useWinSelectionUI = true;

    [Header("デバッグ")]
    [Tooltip("ON なら QTE をスキップして最大到達停止 + Loop を確認します（Catch で投げへ）")]
    [SerializeField] private bool previewSlowLoopOnly;
    [Tooltip("ON なら QTE をスキップして投げモーションだけ再生します")]
    [SerializeField] private bool previewThrowMotionOnly;

    GameObject comboInstance;
    LastAttack activeLastAttack;
    bool qteStarted;
    bool skipContactCheck;

    void Awake()
    {
        ResolveSpawnPoints();
        PrepareComboModel();
    }

    void Start()
    {
        Time.timeScale = 1f;
        ResetSceneActors();
        SetupCamera();
        ApplyInitialCamera();

        if (previewSlowLoopOnly)
        {
            StartCoroutine(PreviewSlowLoopRoutine());
            return;
        }

        if (previewThrowMotionOnly)
        {
            StartCoroutine(PreviewThrowMotionRoutine());
            return;
        }

        if (ShouldAutoStartQTE())
        {
            DisableBehaviours(player, typeof(Player), typeof(PlayerCatchEnemy));
            skipContactCheck = true;
            StartCoroutine(AutoStartQTERoutine());
        }
    }

    void ApplyInitialCamera()
    {
        if (cameraFollow == null || initialCameraPoint == null)
        {
            return;
        }

        Transform lookTarget = bossHead != null ? bossHead : boss != null ? boss.transform : null;

        if (lookTarget == null)
        {
            cameraFollow.SnapCameraTransform(
                initialCameraPoint.position,
                initialCameraPoint.rotation
            );
            return;
        }

        cameraFollow.BeginBossHeadWatch(
            lookTarget,
            initialCameraPoint.position,
            20f
        );

        if (useInitialCameraRotation)
        {
            cameraFollow.SnapCameraRotation(initialCameraPoint.rotation);
        }
        else
        {
            cameraFollow.SnapBossWatchLookAt();
        }

        if (lockInitialCameraUntilQTE)
        {
            cameraFollow.LockBossHeadLookDirection();
        }
    }

    void UnlockInitialCameraIfNeeded()
    {
        if (lockInitialCameraUntilQTE && cameraFollow != null)
        {
            cameraFollow.UnlockBossHeadLookDirection();
        }
    }

    void Update()
    {
        if (skipContactCheck || qteStarted || player == null || bossHead == null)
        {
            return;
        }

        Vector3 playerPos = player.transform.position;
        Vector3 headPos = bossHead.position;
        playerPos.y = 0f;
        headPos.y = 0f;

        if (Vector3.Distance(playerPos, headPos) <= contactDistance)
        {
            NotifyPlayerContact();
        }
    }

    bool ShouldAutoStartQTE()
    {
        if (QTETransitionContext.ConsumeEnteredFromFinalAttack())
        {
            return true;
        }

        return autoStartQTEOnSceneLoad;
    }

    IEnumerator AutoStartQTERoutine()
    {
        yield return null;

        if (autoStartDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(autoStartDelay);
        }

        NotifyPlayerContact();
    }

    void ResolveSpawnPoints()
    {
        if (playerSpawnPoint == null)
        {
            GameObject spawnObject = GameObject.Find("RespawnPosition");
            if (spawnObject != null)
            {
                playerSpawnPoint = spawnObject.transform;
            }
        }

        if (qteCenterPoint == null)
        {
            qteCenterPoint = comboSpawnPoint;
        }

        if (qteCenterPoint == null)
        {
            GameObject centerObject = GameObject.Find("QTECenter");

            if (centerObject != null)
            {
                qteCenterPoint = centerObject.transform;
            }
        }
    }

    void SetupCamera()
    {
        if (cameraFollow == null)
        {
            return;
        }

        cameraFollow.SetPlayerBossReferences(player, boss);

        if (bossHead != null)
        {
            cameraFollow.SetLookFocusOverride(bossHead);
        }
    }

    void ResetSceneActors()
    {
        TeleportActor(player, GetSpawnPosition(playerSpawnPoint, fallbackPlayerSpawn));
        TeleportActor(boss, GetSpawnPosition(bossSpawnPoint, fallbackBossSpawn));

        if (player != null && !player.CompareTag("Player"))
        {
            player.tag = "Player";
        }

        Physics.SyncTransforms();
    }

    static Vector3 GetSpawnPosition(Transform spawnPoint, Vector3 fallback)
    {
        return spawnPoint != null ? spawnPoint.position : fallback;
    }

    static void TeleportActor(GameObject target, Vector3 worldPosition)
    {
        if (target == null)
        {
            return;
        }

        Rigidbody rb = target.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            rb.position = worldPosition;
            rb.rotation = Quaternion.identity;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            return;
        }

        target.transform.position = worldPosition;
        target.transform.rotation = Quaternion.identity;
    }

    void PrepareComboModel()
    {
        Transform parent = GetComboParent();

        if (comboModelPrefab != null)
        {
            comboInstance = Instantiate(comboModelPrefab, parent);
        }
        else if (playerModelPrefab != null && bossModelPrefab != null)
        {
            comboInstance = new GameObject("QTECombo");

            if (parent != null)
            {
                comboInstance.transform.SetParent(parent, false);
            }

            GameObject playerInstance = Instantiate(playerModelPrefab, comboInstance.transform);
            playerInstance.name = "Player";

            GameObject bossInstance = Instantiate(bossModelPrefab, comboInstance.transform);
            bossInstance.name = "Boss";
        }
        else
        {
            Debug.LogWarning(
                "QTESceneController: Combo Model Prefab か Player/Boss Model Prefab を設定してください。"
            );
            return;
        }

        ApplyComboTransform(comboInstance.transform);
        EnsureComboRenderersEnabled(comboInstance, true);
        comboInstance.SetActive(false);
        EnsureComboComponents(comboInstance);
    }

    Transform GetComboParent()
    {
        if (qteCenterPoint != null)
        {
            return qteCenterPoint;
        }

        if (comboSpawnPoint != null)
        {
            return comboSpawnPoint;
        }

        return null;
    }

    void ApplyComboTransform(Transform target)
    {
        target.localPosition = Vector3.zero;
        target.localRotation = Quaternion.identity;
        target.localScale = Vector3.one * comboModelScale;
    }

    static void EnsureComboRenderersEnabled(GameObject combo, bool enabled)
    {
        if (combo == null)
        {
            return;
        }

        Renderer[] renderers = combo.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = enabled;
        }
    }

    void EnsureComboComponents(GameObject combo)
    {
        if (!QTEAnimatorSetup.ResolveCharacterAnimators(combo, out Animator playerAnimator, out Animator bossAnimator))
        {
            Animator fallbackAnimator = QTEAnimatorSetup.GetOrCreateAnimator(combo);
            bossAnimator = fallbackAnimator;
        }

        activeLastAttack = combo.GetComponent<LastAttack>();
        if (activeLastAttack == null)
        {
            activeLastAttack = combo.AddComponent<LastAttack>();
        }

        LastAttackMotion motion = combo.GetComponent<LastAttackMotion>();
        if (motion == null)
        {
            motion = combo.AddComponent<LastAttackMotion>();
        }

        motion.BindCharacters(playerAnimator, bossAnimator);
        motion.ConfigureClips(
            playerLoopClip,
            playerThrowClip,
            playerThrowToIdleClip,
            playerPostThrowIdleClip,
            bossLoopClip,
            bossThrowClip
        );

        if (playerLoopClip == null && bossLoopClip == null)
        {
            Debug.LogError(
                "QTESceneController: QTE の Loop クリップが未設定です。"
                + " Inspector のアニメーション設定を確認してください。"
            );
        }

        activeLastAttack.ConfigureSpin(
            maxSpinSpeed,
            spinImpulsePerRevolution,
            spinDecay,
            revolutionsForFullEase,
            minThrowSpinSpeed
        );
        activeLastAttack.ConfigureThrowAftermath(CreateThrowAftermathSettings());
        activeLastAttack.InitializeVisuals();
    }

    QTEThrowAftermathSettings CreateThrowAftermathSettings()
    {
        return new QTEThrowAftermathSettings
        {
            throwCameraPoint = throwCameraPoint,
            frameBothTargets = frameBothTargets,
            frameDistanceFactor = frameDistanceFactor,
            framePadding = framePadding,
            frameMinDistance = frameMinDistance,
            frameCameraHeight = frameCameraHeight,
            frameMinHeight = frameMinHeight,
            frameSmooth = frameSmooth,
            aimCameraAtPlayer = aimCameraAtPlayer,
            cameraLookHeightOffset = throwCameraLookHeight,
            playerMovePoint = playerThrowMovePoint,
            bossHeadOverride = bossHeadOverride,
            bossHeadLaunchDelay = bossHeadLaunchDelay,
            bossHeadLaunchSpeed = bossHeadLaunchSpeed,
            launchAwayFromCamera = launchAwayFromCamera,
            bossHeadLaunchUpward = bossHeadLaunchUpward,
            bossHeadLaunchDirection = bossHeadLaunchDirection,
            bossHeadUseGravity = bossHeadUseGravity,
            bossHeadStopDistance = bossHeadStopDistance,
            bossHeadStopEffectPrefab = bossHeadStopEffectPrefab,
            bossHeadStopEffectOffset = bossHeadStopEffectOffset,
        };
    }

    public void NotifyPlayerContact()
    {
        if (qteStarted)
        {
            return;
        }

        StartCoroutine(BeginQTERoutine());
    }

    IEnumerator BeginQTERoutine()
    {
        qteStarted = true;
        Debug.Log("QTESceneController: QTE を開始します");

        HideNormalActors();
        ShowComboModel();

        if (activeLastAttack == null)
        {
            Debug.LogError("QTESceneController: 一体モデルの LastAttack が見つかりません。Combo Model Prefab を確認してください。");
            yield break;
        }

        if (cameraFollow != null)
        {
            UnlockInitialCameraIfNeeded();

            Vector3 lookPoint = activeLastAttack.transform.position
                + Vector3.up * activeLastAttack.LookHeightOffset;

            Camera mainCamera = Camera.main;
            float currentBack = qteStartCameraBackOffset;

            if (mainCamera != null)
            {
                Vector3 toCamera = mainCamera.transform.position - lookPoint;
                toCamera.y = 0f;
                currentBack = toCamera.magnitude + qteStartCameraBackOffset;
            }

            cameraFollow.BeginQTECamera(
                activeLastAttack.transform,
                currentBack,
                activeLastAttack.LookHeightOffset
            );
            cameraFollow.EndHeadFollow();
        }

        EnsureMashPromptUI();
        mashPromptUI?.ShowMashPrompt();

        yield return activeLastAttack.RunMinigameRoutine(cameraFollow);
        FinishQTE();
    }

    void HideNormalActors()
    {
        HideRenderers(player);
        HideRenderers(boss);
        DisableBehaviours(player, typeof(Player), typeof(PlayerCatchEnemy));
        DisableBehaviours(boss);
    }

    void ShowComboModel()
    {
        if (comboInstance == null)
        {
            Debug.LogWarning("QTESceneController: 一体モデル Prefab が未設定です");
            return;
        }

        Transform parent = GetComboParent();

        if (parent != null && comboInstance.transform.parent != parent)
        {
            comboInstance.transform.SetParent(parent, false);
        }
        else if (parent == null)
        {
            Vector3 spawnPosition = GetSpawnPosition(qteCenterPoint, fallbackQteCenter);
            comboInstance.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
        }

        ApplyComboTransform(comboInstance.transform);
        EnsureComboRenderersEnabled(comboInstance, true);
        activeLastAttack?.InitializeVisuals();
        comboInstance.SetActive(true);
    }

    void FinishQTE()
    {
        if (cameraFollow != null)
        {
            cameraFollow.EndHeadFollow();
            cameraFollow.EndQTECamera();
        }

        if (useWinSelectionUI && mashPromptUI != null)
        {
            Debug.Log("QTESceneController: QTE 完了（WIN 後のシーン選択 UI を待機）");
            return;
        }

        if (useGameClearOnComplete && gameClear != null)
        {
            gameClear.StartWin();
            return;
        }

        if (loadSceneOnComplete)
        {
            SceneLoader.Load(returnSceneName);
            return;
        }

        Debug.Log("QTESceneController: QTE 完了");
    }

    [ContextMenu("最大到達停止をプレビュー")]
    public void PreviewSlowLoopFromMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("QTESceneController: 最大到達停止プレビューは Play モードで実行してください。");
            return;
        }

        StopAllCoroutines();
        qteStarted = false;
        StartCoroutine(PreviewSlowLoopRoutine());
    }

    [ContextMenu("投げモーションをプレビュー")]
    public void PreviewThrowMotionFromMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("QTESceneController: 投げモーションプレビューは Play モードで実行してください。");
            return;
        }

        StopAllCoroutines();
        qteStarted = false;
        StartCoroutine(PreviewThrowMotionRoutine());
    }

    IEnumerator PreviewSlowLoopRoutine()
    {
        if (!BeginPreviewSetup("最大到達停止"))
        {
            yield break;
        }

        SetupPreviewCamera();
        EnsureMashPromptUI();
        mashPromptUI?.ShowMashPrompt();
        yield return activeLastAttack.PlaySlowLoopPreviewRoutine(cameraFollow);
        mashPromptUI?.HideAll();
        Debug.Log("QTESceneController: 最大到達停止プレビュー完了");
    }

    IEnumerator PreviewThrowMotionRoutine()
    {
        if (!BeginPreviewSetup("投げモーション"))
        {
            yield break;
        }

        yield return activeLastAttack.PlayThrowPreviewRoutine();
        Debug.Log("QTESceneController: 投げモーションプレビュー完了");
    }

    bool BeginPreviewSetup(string previewName)
    {
        Time.timeScale = 1f;
        qteStarted = true;
        skipContactCheck = true;

        HideNormalActors();
        ShowComboModel();

        if (activeLastAttack == null)
        {
            Debug.LogError($"QTESceneController: 一体モデルが未設定のため{previewName}プレビューできません。");
            return false;
        }

        activeLastAttack.InitializeVisuals();
        Debug.Log($"QTESceneController: {previewName}プレビューを開始します");
        return true;
    }

    void SetupPreviewCamera()
    {
        if (cameraFollow == null || activeLastAttack == null)
        {
            return;
        }

        UnlockInitialCameraIfNeeded();

        Vector3 lookPoint = activeLastAttack.transform.position
            + Vector3.up * activeLastAttack.LookHeightOffset;

        Camera mainCamera = Camera.main;
        float currentBack = qteStartCameraBackOffset;

        if (mainCamera != null)
        {
            Vector3 toCamera = mainCamera.transform.position - lookPoint;
            toCamera.y = 0f;
            currentBack = toCamera.magnitude + qteStartCameraBackOffset;
        }

        cameraFollow.BeginQTECamera(
            activeLastAttack.transform,
            currentBack,
            activeLastAttack.LookHeightOffset
        );
        cameraFollow.EndHeadFollow();
    }

    void EnsureMashPromptUI()
    {
        if (mashPromptUI != null)
        {
            return;
        }

        mashPromptUI = FindObjectOfType<LastAttackUIManager>(true);
    }

    static void HideRenderers(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    static void DisableBehaviours(GameObject target, params System.Type[] onlyTheseTypes)
    {
        if (target == null)
        {
            return;
        }

        Behaviour[] behaviours = target.GetComponentsInChildren<Behaviour>(true);

        foreach (Behaviour behaviour in behaviours)
        {
            if (behaviour == null || !behaviour.enabled)
            {
                continue;
            }

            if (onlyTheseTypes != null && onlyTheseTypes.Length > 0)
            {
                bool match = false;

                foreach (System.Type type in onlyTheseTypes)
                {
                    if (type.IsInstanceOfType(behaviour))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    continue;
                }
            }

            behaviour.enabled = false;
        }
    }
}
