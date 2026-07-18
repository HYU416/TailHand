using UnityEngine;

/// <summary>
/// QTE 投げ確定時のカメラ・キャラ・ボス頭演出をまとめます。
/// </summary>
[System.Serializable]
public class QTEThrowAftermathSettings
{
    [Tooltip("投げ瞬間にカメラを移動する位置（Cube など）。角度の基準に使います")]
    public Transform throwCameraPoint;

    [Tooltip("ON: キャラとボス頭の両方がフレームに収まるようにする（ゲームカメラ風）")]
    public bool frameBothTargets = true;

    [Tooltip("2 点が離れるほどカメラを引く倍率")]
    [Min(0f)] public float frameDistanceFactor = 1.2f;

    [Tooltip("最低限のカメラ距離に加える余白")]
    [Min(0f)] public float framePadding = 4f;

    [Tooltip("カメラの最小距離")]
    [Min(0.1f)] public float frameMinDistance = 5f;

    [Tooltip("カメラを注視点より高くする量")]
    public float frameCameraHeight = 1.5f;

    [Tooltip("カメラが地面下に沈まないための最低ワールド高さ")]
    public float frameMinHeight = 1f;

    [Tooltip("ON: フレーミングをなめらかに追従。OFF: 即座に合わせる")]
    public bool frameSmooth = true;

    [Tooltip("ON: カメラをキャラクター中央に向ける。OFF: Cube の向きを使う（frameBothTargets が OFF のとき）")]
    public bool aimCameraAtPlayer = true;

    [Tooltip("注視点の高さオフセット")]
    public float cameraLookHeightOffset = 1.5f;

    [Tooltip("投げ瞬間に QTE プレイヤーモデルを移動する位置（任意）")]
    public Transform playerMovePoint;

    [Tooltip("飛ばすボスの頭オブジェクト（SkinnedMesh のボーンではなく頭単体のモデルを推奨）")]
    public Transform bossHeadOverride;

    [Tooltip("ThrowToIdle 基準のオフセット秒。負=前にずらす / 正=後にずらす")]
    public float bossHeadLaunchDelay = 0f;

    [Min(0f)] public float bossHeadLaunchSpeed = 12f;

    [Tooltip("ON: カメラ(Cube)と逆方向（奥）へ飛ばす。OFF: 下の固定方向を使う")]
    public bool launchAwayFromCamera = true;

    [Tooltip("カメラ逆方向に加える上向き成分（アーチの高さ）")]
    public float bossHeadLaunchUpward = 0.35f;

    [Tooltip("launchAwayFromCamera が OFF のときの固定方向")]
    public Vector3 bossHeadLaunchDirection = new Vector3(1f, 0f, -1f);
    public bool bossHeadUseGravity = true;

    [Tooltip("キャラからこの距離以上離れたら頭とカメラを止める（0 で無効）")]
    [Min(0f)] public float bossHeadStopDistance = 20f;

    [Tooltip("頭が止まったときに再生する爆発エフェクト（LAST_boom など）")]
    public GameObject bossHeadStopEffectPrefab;

    [Tooltip("エフェクト位置を頭の中心からずらすオフセット（ワールド座標）")]
    public Vector3 bossHeadStopEffectOffset = Vector3.zero;
}

public static class QTEThrowAftermath
{
    /// <summary>
    /// 投げモーション開始時に呼ぶ：カメラを Cube へ移動し、プレイヤーを移動します。
    /// 頭はまだ飛ばしません（このタイミングではボス本体を注視対象に使います）。
    /// </summary>
    public static void ApplyCameraAndPlayer(
        QTEThrowAftermathSettings settings,
        CameraFollow cameraFollow,
        Transform comboRoot,
        LastAttackMotion motion)
    {
        if (settings == null || comboRoot == null)
        {
            return;
        }

        Transform playerTransform = FindPlayerTransform(comboRoot, motion);
        Transform headTransform = ResolveHeadTransform(settings, comboRoot, motion);

        ApplyPlayerMove(settings, playerTransform);
        ApplyCamera(settings, cameraFollow, comboRoot, playerTransform, headTransform);
    }

    /// <summary>
    /// ThrowToIdle 開始時に呼ぶ：ボスの頭を固定してまっすぐ飛ばし、停止監視を始めます。
    /// </summary>
    public static void LaunchHead(
        QTEThrowAftermathSettings settings,
        CameraFollow cameraFollow,
        Transform comboRoot,
        LastAttackMotion motion)
    {
        if (settings == null || comboRoot == null)
        {
            return;
        }

        Transform playerTransform = FindPlayerTransform(comboRoot, motion);
        Transform headTransform = LaunchBossHead(settings, comboRoot, motion);

        if (headTransform != null && playerTransform != null && motion != null)
        {
            motion.BeginHeadStopWatch(
                headTransform,
                playerTransform,
                cameraFollow,
                settings.bossHeadStopDistance,
                settings.bossHeadStopEffectPrefab,
                settings.bossHeadStopEffectOffset
            );
        }
    }

    static Transform ResolveHeadTransform(
        QTEThrowAftermathSettings settings,
        Transform comboRoot,
        LastAttackMotion motion)
    {
        return settings.bossHeadOverride != null
            ? settings.bossHeadOverride
            : FindBossTransform(comboRoot, motion);
    }

    static void ApplyCamera(
        QTEThrowAftermathSettings settings,
        CameraFollow cameraFollow,
        Transform comboRoot,
        Transform playerTransform,
        Transform headTransform)
    {
        if (cameraFollow == null)
        {
            return;
        }

        Transform cameraPoint = settings.throwCameraPoint;

        if (settings.frameBothTargets
            && cameraPoint != null
            && playerTransform != null
            && headTransform != null)
        {
            // カメラ位置は Cube に固定し、向きだけ 2 点の中間へ向ける。
            cameraFollow.BeginQTEThrowFraming(
                playerTransform,
                headTransform,
                cameraPoint.position,
                settings.cameraLookHeightOffset,
                settings.frameSmooth
            );
            return;
        }

        if (cameraPoint == null)
        {
            return;
        }

        if (settings.aimCameraAtPlayer)
        {
            Transform lookRoot = playerTransform != null ? playerTransform : comboRoot;
            Vector3 lookTarget = lookRoot.position + Vector3.up * settings.cameraLookHeightOffset;

            cameraFollow.LockCameraLookAt(cameraPoint.position, lookTarget);
            return;
        }

        cameraFollow.LockCameraTransform(
            cameraPoint.position,
            cameraPoint.rotation
        );
    }

    static void ApplyPlayerMove(
        QTEThrowAftermathSettings settings,
        Transform playerTransform)
    {
        if (settings.playerMovePoint == null)
        {
            return;
        }

        if (playerTransform == null)
        {
            Debug.LogWarning("QTEThrowAftermath: プレイヤーモデルが見つかりません。");
            return;
        }

        playerTransform.SetPositionAndRotation(
            settings.playerMovePoint.position,
            settings.playerMovePoint.rotation
        );
    }

    static Transform LaunchBossHead(
        QTEThrowAftermathSettings settings,
        Transform comboRoot,
        LastAttackMotion motion)
    {
        // 明示指定があればそれを、無ければボスモデル本体を対象にする。
        // ※ ApplyCamera の注視対象と同じ Transform を使う。
        Transform headTransform = ResolveHeadTransform(settings, comboRoot, motion);

        if (headTransform == null)
        {
            Debug.LogWarning(
                "QTEThrowAftermath: ボスモデルが見つかりません。"
                + " Inspector の Boss Head Override に飛ばすオブジェクトを割り当ててください。"
            );
            return null;
        }

        // Playables グラフによるボス駆動を止める（Advance がフレームごとに姿勢を上書きするのを防ぐ）。
        if (motion != null)
        {
            motion.StopBossPlayback();
        }

        // アニメーション制御を切って物理に完全に委ねる（Animator がいると位置が上書きされる）。
        foreach (Animator animator in headTransform.GetComponentsInChildren<Animator>(true))
        {
            animator.enabled = false;
        }

        headTransform.SetParent(null, true);

        // 発射直後に周囲（プレイヤーや地面）のコライダーと衝突して弾かれないよう、
        // 頭側のコライダーはすべてトリガー化して物理衝突を無効にする。
        foreach (Collider existingCollider in headTransform.GetComponentsInChildren<Collider>(true))
        {
            existingCollider.isTrigger = true;
        }

        Rigidbody rigidbody = headTransform.GetComponent<Rigidbody>();

        if (rigidbody == null)
        {
            rigidbody = headTransform.gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.isKinematic = false;
        rigidbody.useGravity = settings.bossHeadUseGravity;
        rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;

        // 回転させず姿勢を保ったまま飛ばすため、回転を固定する。
        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        Vector3 launchDirection = ResolveLaunchDirection(settings, headTransform.position);
        rigidbody.linearVelocity = launchDirection * settings.bossHeadLaunchSpeed;
        rigidbody.angularVelocity = Vector3.zero;

        Debug.Log(
            $"QTEThrowAftermath: '{headTransform.name}' を方向 {launchDirection} "
            + $"速度 {settings.bossHeadLaunchSpeed} で飛ばします。"
        );

        return headTransform;
    }

    static Vector3 ResolveLaunchDirection(QTEThrowAftermathSettings settings, Vector3 headPosition)
    {
        if (settings.launchAwayFromCamera && settings.throwCameraPoint != null)
        {
            // カメラ(Cube)から頭へ向かう水平方向 ＝ カメラの逆方向（奥）。
            Vector3 away = headPosition - settings.throwCameraPoint.position;
            away.y = 0f;

            if (away.sqrMagnitude > 0.0001f)
            {
                away.Normalize();
                away += Vector3.up * settings.bossHeadLaunchUpward;
                return away.normalized;
            }
        }

        Vector3 launchDirection = settings.bossHeadLaunchDirection;

        if (launchDirection.sqrMagnitude < 0.0001f)
        {
            launchDirection = new Vector3(1f, 0f, -1f);
        }

        return launchDirection.normalized;
    }

    static Transform FindPlayerTransform(Transform comboRoot, LastAttackMotion motion)
    {
        Transform named = FindChildByKeyword(comboRoot, "player");

        if (named != null)
        {
            return named;
        }

        Animator playerAnimator = motion != null ? motion.GetPlayerAnimator() : null;
        return playerAnimator != null ? playerAnimator.transform : null;
    }

    static Transform FindBossTransform(Transform comboRoot, LastAttackMotion motion)
    {
        Transform named = FindChildByKeyword(comboRoot, "boss");

        if (named != null)
        {
            return named;
        }

        Animator bossAnimator = motion != null ? motion.GetBossAnimator() : null;
        return bossAnimator != null ? bossAnimator.transform : null;
    }

    static Transform FindChildByKeyword(Transform root, string keyword)
    {
        keyword = keyword.ToLowerInvariant();

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.ToLowerInvariant().Contains(keyword))
            {
                return child;
            }
        }

        return null;
    }
}
