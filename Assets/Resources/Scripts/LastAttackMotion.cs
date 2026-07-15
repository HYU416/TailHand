using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public enum LastAttackAnimeState
{
    Idle = 0,
    Loop = 1,
    Throw = 2,
}

/// <summary>
/// QTE モデル（プレイヤー・ボス）のアニメーションを Playables で直接制御します。
/// Loop はインポート設定に依存せず確実にループし、速度を可変できます。
/// 投げ後は Throw -> ThrowToIdle -> Idle と自動遷移します。
/// </summary>
public class LastAttackMotion : MonoBehaviour
{
    enum Phase
    {
        Loop,
        Throw,
        ThrowToIdle,
        Idle,
        Frozen,
    }

    class CharacterPlayback
    {
        public Animator animator;
        public bool isPlayer;
        public Phase phase = Phase.Loop;
        public AnimationPlayableOutput output;
        public AnimationClipPlayable current;
        public AnimationClip currentClip;
        public bool sequenceFinished;
        public float localTime;
        public float speed = 1f;
        public bool hasStartPose;
        public Quaternion startWorldRotation = Quaternion.identity;
    }

    [SerializeField] private Animator playerAnimator;
    [SerializeField] private Animator bossAnimator;

    AnimationClip playerLoopClip;
    AnimationClip playerThrowClip;
    AnimationClip playerThrowToIdleClip;
    AnimationClip playerIdleClip;
    AnimationClip bossLoopClip;
    AnimationClip bossThrowClip;

    PlayableGraph graph;
    CharacterPlayback player;
    CharacterPlayback boss;
    bool graphReady;
    float loopSpeed = 1f;

    Transform watchHead;
    Transform watchOrigin;
    CameraFollow watchCamera;
    float watchStopDistance;
    GameObject watchStopEffectPrefab;
    bool watchingHead;

    public void BindCharacters(Animator player, Animator boss)
    {
        playerAnimator = player;
        bossAnimator = boss;
    }

    public void ConfigureClips(
        AnimationClip playerLoop,
        AnimationClip playerThrow,
        AnimationClip playerThrowToIdle,
        AnimationClip playerIdle,
        AnimationClip bossLoop,
        AnimationClip bossThrow)
    {
        playerLoopClip = playerLoop;
        playerThrowClip = playerThrow;
        playerThrowToIdleClip = playerThrowToIdle;
        playerIdleClip = playerIdle;
        bossLoopClip = bossLoop;
        bossThrowClip = bossThrow;
    }

    public bool HasConfiguredAnimators()
    {
        EnsureAnimators();
        return playerAnimator != null || bossAnimator != null;
    }

    public Animator GetPlayerAnimator()
    {
        EnsureAnimators();
        return playerAnimator;
    }

    public Animator GetBossAnimator()
    {
        EnsureAnimators();
        return bossAnimator;
    }

    public Transform GetBossSpinTransform()
    {
        EnsureAnimators();

        if (bossAnimator != null)
        {
            return bossAnimator.transform;
        }

        if (playerAnimator != null)
        {
            return playerAnimator.transform;
        }

        return null;
    }

    /// <summary>
    /// 現在は Loop 指定のみ使用します（回転中のループ再生）。
    /// </summary>
    public void SetState(LastAttackAnimeState state)
    {
        EnsureGraph();

        if (state == LastAttackAnimeState.Loop)
        {
            EnsureLoopPhase(player, playerLoopClip);
            EnsureLoopPhase(boss, bossLoopClip);
        }
    }

    public void SetAnimatorSpeed(float speed)
    {
        loopSpeed = speed;
        EnsureGraph();
        ApplyLoopSpeed(player);
        ApplyLoopSpeed(boss);
    }

    public void PlayThrowImmediate()
    {
        EnsureGraph();

        if (player != null)
        {
            SwitchClip(player, playerThrowClip, Phase.Throw, 1f, false);
        }

        if (boss != null)
        {
            SwitchClip(boss, bossThrowClip, Phase.Throw, 1f, false);
        }
    }

    /// <summary>
    /// プレイヤー Throw クリップの長さ（未設定なら 0）。
    /// </summary>
    public float GetPlayerThrowClipLength()
    {
        return playerThrowClip != null ? Mathf.Max(0f, playerThrowClip.length) : 0f;
    }

    /// <summary>
    /// QTE 開始時の向きを記録します（スピン前に呼ぶ）。
    /// </summary>
    public void CaptureStartOrientation()
    {
        EnsureGraph();
        CaptureStartPose(player);
        CaptureStartPose(boss);
    }

    static void CaptureStartPose(CharacterPlayback c)
    {
        if (c == null || c.animator == null)
        {
            return;
        }

        c.startWorldRotation = c.animator.transform.rotation;
        c.hasStartPose = true;
    }

    /// <summary>
    /// 投げ開始時に、記録した開始姿勢へキャラ・ボスの向きを強制的に戻します。
    /// これで投げモーションの開始向きが毎回一定になります。
    /// </summary>
    public void RestoreStartOrientation()
    {
        RestoreStartPose(player);
        RestoreStartPose(boss);
    }

    static void RestoreStartPose(CharacterPlayback c)
    {
        if (c == null || c.animator == null || !c.hasStartPose)
        {
            return;
        }

        c.animator.transform.rotation = c.startWorldRotation;
    }

    public void ResetAnimators()
    {
        EnsureGraph();

        if (player != null)
        {
            SwitchClip(player, playerLoopClip, Phase.Loop, loopSpeed, true);
        }

        if (boss != null)
        {
            SwitchClip(boss, bossLoopClip, Phase.Loop, loopSpeed, true);
        }
    }

    /// <summary>
    /// 投げ演出（Throw -> ThrowToIdle -> Idle / ボスは Throw で停止）が完了したか。
    /// </summary>
    public bool IsThrowSequenceFinished()
    {
        bool playerDone = player == null || player.sequenceFinished;
        bool bossDone = boss == null || boss.sequenceFinished;
        return playerDone && bossDone;
    }

    /// <summary>
    /// プレイヤーが ThrowToIdle 以降（Throw 再生完了後）に入ったか。
    /// 頭を飛ばし始めるタイミングの判定に使います。
    /// </summary>
    public bool HasPlayerStartedThrowToIdle()
    {
        if (player == null)
        {
            return true;
        }

        return player.phase == Phase.ThrowToIdle
            || player.phase == Phase.Idle
            || player.phase == Phase.Frozen;
    }

    /// <summary>
    /// ボスのアニメーション再生を完全に停止します（頭を物理で飛ばすとき用）。
    /// Playable を破棄し、Advance の対象から外して Animator も無効化します。
    /// </summary>
    public void StopBossPlayback()
    {
        if (boss == null)
        {
            return;
        }

        if (boss.current.IsValid())
        {
            graph.DestroyPlayable(boss.current);
        }

        if (boss.animator != null)
        {
            boss.animator.enabled = false;
        }

        boss.sequenceFinished = true;
        boss = null;
    }

    /// <summary>
    /// 飛ばした頭がキャラ(origin)から stopDistance 以上離れたら、
    /// 頭の移動を止め、カメラも固定します。
    /// </summary>
    public void BeginHeadStopWatch(
        Transform head,
        Transform origin,
        CameraFollow camera,
        float stopDistance,
        GameObject stopEffectPrefab = null)
    {
        if (head == null || origin == null || stopDistance <= 0f)
        {
            return;
        }

        watchHead = head;
        watchOrigin = origin;
        watchCamera = camera;
        watchStopDistance = stopDistance;
        watchStopEffectPrefab = stopEffectPrefab;
        watchingHead = true;
    }

    void Update()
    {
        if (watchingHead)
        {
            UpdateHeadStopWatch();
        }

        if (!graphReady)
        {
            return;
        }

        Advance(player);
        Advance(boss);
    }

    void UpdateHeadStopWatch()
    {
        if (watchHead == null || watchOrigin == null)
        {
            watchingHead = false;
            return;
        }

        float distance = Vector3.Distance(watchOrigin.position, watchHead.position);

        if (distance < watchStopDistance)
        {
            return;
        }

        Rigidbody rigidbody = watchHead.GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.isKinematic = true;
        }

        if (watchCamera != null)
        {
            watchCamera.FreezeQTEFraming();
        }

        Vector3 effectPosition = GetHeadBoundsCenter(watchHead);
        SpawnHeadStopEffect(effectPosition);
        watchHead.gameObject.SetActive(false);

        LastAttackUIManager uiManager = FindObjectOfType<LastAttackUIManager>(true);
        uiManager?.ShowWin();

        watchingHead = false;
    }

    void SpawnHeadStopEffect(Vector3 position)
    {
        GameObject prefab = watchStopEffectPrefab != null
            ? watchStopEffectPrefab
            : Resources.Load<GameObject>("prefabs/Effects/LAST_boom");

        if (prefab == null)
        {
            return;
        }

        GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        EffectPlayer player = effect.GetComponent<EffectPlayer>();

        if (player != null)
        {
            player.EffectStart();
        }
    }

    static Vector3 GetHeadBoundsCenter(Transform head)
    {
        Renderer[] renderers = head.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            return head.position;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center;
    }

    void Advance(CharacterPlayback c)
    {
        if (c == null || !c.current.IsValid() || c.currentClip == null)
        {
            return;
        }

        float length = c.currentClip.length;

        if (length <= 0f)
        {
            return;
        }

        // 時間は自前で進めてサンプル位置を毎フレーム指定する（確実にループさせるため）。
        c.localTime += Time.deltaTime * c.speed;

        switch (c.phase)
        {
            case Phase.Loop:
            case Phase.Idle:
                c.current.SetTime(Mathf.Repeat(c.localTime, length));
                break;

            case Phase.Throw:
                if (c.localTime >= length)
                {
                    c.current.SetTime(length);

                    if (c.isPlayer)
                    {
                        if (playerThrowToIdleClip != null)
                        {
                            SwitchClip(c, playerThrowToIdleClip, Phase.ThrowToIdle, 1f, false);
                        }
                        else if (playerIdleClip != null)
                        {
                            SwitchClip(c, playerIdleClip, Phase.Idle, 1f, true);
                        }
                        else
                        {
                            FreezeAtEnd(c, length);
                        }
                    }
                    else
                    {
                        FreezeAtEnd(c, length);
                    }
                }
                else
                {
                    c.current.SetTime(c.localTime);
                }
                break;

            case Phase.ThrowToIdle:
                if (c.localTime >= length)
                {
                    c.current.SetTime(length);

                    if (playerIdleClip != null)
                    {
                        SwitchClip(c, playerIdleClip, Phase.Idle, 1f, true);
                    }
                    else
                    {
                        FreezeAtEnd(c, length);
                    }
                }
                else
                {
                    c.current.SetTime(c.localTime);
                }
                break;

            case Phase.Frozen:
                c.current.SetTime(length);
                break;
        }
    }

    void FreezeAtEnd(CharacterPlayback c, float length)
    {
        c.localTime = length;
        c.current.SetTime(length);
        c.speed = 0f;
        c.phase = Phase.Frozen;
        c.sequenceFinished = true;
    }

    void EnsureLoopPhase(CharacterPlayback c, AnimationClip loopClip)
    {
        if (c == null)
        {
            return;
        }

        if (c.phase != Phase.Loop)
        {
            SwitchClip(c, loopClip, Phase.Loop, loopSpeed, true);
        }
    }

    void ApplyLoopSpeed(CharacterPlayback c)
    {
        if (c == null)
        {
            return;
        }

        if (c.phase == Phase.Loop)
        {
            c.speed = loopSpeed;
        }
    }

    void SwitchClip(
        CharacterPlayback c,
        AnimationClip clip,
        Phase phase,
        float speed,
        bool loop)
    {
        if (c == null || clip == null)
        {
            return;
        }

        if (c.current.IsValid())
        {
            graph.DestroyPlayable(c.current);
        }

        // 時間は Advance() で自前管理するため、Playable 自体の速度は 0 に固定。
        c.current = AnimationClipPlayable.Create(graph, clip);
        c.currentClip = clip;
        c.current.SetSpeed(0d);
        c.current.SetTime(0d);
        c.localTime = 0f;
        c.speed = speed;

        c.output.SetSourcePlayable(c.current);
        c.phase = phase;
        c.sequenceFinished = phase == Phase.Idle || phase == Phase.Frozen;
    }

    void EnsureGraph()
    {
        if (graphReady)
        {
            return;
        }

        EnsureAnimators();

        graph = PlayableGraph.Create(gameObject.name + "_LastAttackMotion");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        if (playerAnimator != null)
        {
            player = CreatePlayback(playerAnimator, true, playerLoopClip);
        }

        if (bossAnimator != null && bossAnimator != playerAnimator)
        {
            boss = CreatePlayback(bossAnimator, false, bossLoopClip);
        }

        graph.Play();
        graphReady = true;
    }

    CharacterPlayback CreatePlayback(Animator animator, bool isPlayer, AnimationClip startClip)
    {
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        CharacterPlayback playback = new CharacterPlayback
        {
            animator = animator,
            isPlayer = isPlayer,
            phase = Phase.Loop,
            speed = loopSpeed,
        };

        playback.output = AnimationPlayableOutput.Create(
            graph,
            isPlayer ? "PlayerOutput" : "BossOutput",
            animator
        );

        if (startClip != null)
        {
            playback.current = AnimationClipPlayable.Create(graph, startClip);
            playback.currentClip = startClip;
            playback.current.SetSpeed(0d);
            playback.current.SetTime(0d);
            playback.output.SetSourcePlayable(playback.current);
        }

        return playback;
    }

    void EnsureAnimators()
    {
        if (playerAnimator != null && bossAnimator != null)
        {
            return;
        }

        Animator[] animators = GetComponentsInChildren<Animator>(true);

        foreach (Animator animator in animators)
        {
            if (animator == null)
            {
                continue;
            }

            string name = animator.gameObject.name.ToLowerInvariant();

            if (playerAnimator == null && name.Contains("player"))
            {
                playerAnimator = animator;
                continue;
            }

            if (bossAnimator == null && name.Contains("boss"))
            {
                bossAnimator = animator;
            }
        }

        if (playerAnimator == null && bossAnimator == null && animators.Length > 0)
        {
            bossAnimator = animators[0];
        }
    }

    void OnDestroy()
    {
        if (graph.IsValid())
        {
            graph.Destroy();
        }
    }
}
