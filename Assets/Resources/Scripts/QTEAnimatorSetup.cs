using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// QTE 用 Animator の Controller / Clip 割り当てを行います。
/// </summary>
public static class QTEAnimatorSetup
{
    const string DefaultControllerResourcePath = "Animators/Last_Attack";

    public static bool ResolveCharacterAnimators(
        GameObject comboRoot,
        out Animator playerAnimator,
        out Animator bossAnimator)
    {
        playerAnimator = null;
        bossAnimator = null;

        if (comboRoot == null)
        {
            return false;
        }

        Animator[] animators = comboRoot.GetComponentsInChildren<Animator>(true);
        List<Animator> unmatched = new List<Animator>();

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
                continue;
            }

            unmatched.Add(animator);
        }

        Transform playerTransform = FindChildTransform(comboRoot.transform, "player");
        Transform bossTransform = FindChildTransform(comboRoot.transform, "boss");

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = GetOrCreateAnimatorOnTransform(playerTransform);
        }

        if (bossAnimator == null && bossTransform != null)
        {
            bossAnimator = GetOrCreateAnimatorOnTransform(bossTransform);
        }

        if (playerAnimator == null && unmatched.Count > 0)
        {
            playerAnimator = GetOrCreateAnimatorOnTransform(unmatched[0].transform);
            unmatched.RemoveAt(0);
        }

        if (bossAnimator == null && unmatched.Count > 0)
        {
            bossAnimator = GetOrCreateAnimatorOnTransform(unmatched[0].transform);
        }

        if (bossAnimator == null && playerAnimator != null && animators.Length == 1)
        {
            bossAnimator = playerAnimator;
            playerAnimator = null;
        }

        return playerAnimator != null || bossAnimator != null;
    }

    public static Animator GetOrCreateAnimator(GameObject comboRoot)
    {
        if (ResolveCharacterAnimators(comboRoot, out Animator playerAnimator, out Animator bossAnimator))
        {
            return bossAnimator != null ? bossAnimator : playerAnimator;
        }

        SkinnedMeshRenderer skinnedMesh = comboRoot.GetComponentInChildren<SkinnedMeshRenderer>(true);
        Transform modelRoot = skinnedMesh != null ? skinnedMesh.transform : comboRoot.transform;
        return modelRoot.gameObject.AddComponent<Animator>();
    }

    static Animator GetOrCreateAnimatorOnTransform(Transform target)
    {
        Animator animator = target.GetComponent<Animator>();

        if (animator == null)
        {
            animator = target.gameObject.AddComponent<Animator>();
        }

        return animator;
    }

    static Transform FindChildTransform(Transform root, string keyword)
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

    public static RuntimeAnimatorController BuildController(
        RuntimeAnimatorController baseController,
        AnimationClip loopClip,
        AnimationClip throwClip,
        AnimationClip throwToIdleClip = null,
        AnimationClip postThrowIdleClip = null)
    {
        RuntimeAnimatorController resolvedController = baseController;

        if (resolvedController == null)
        {
            resolvedController = Resources.Load<RuntimeAnimatorController>(DefaultControllerResourcePath);
        }

        if (resolvedController == null)
        {
            return null;
        }

        if (loopClip == null
            && throwClip == null
            && throwToIdleClip == null
            && postThrowIdleClip == null)
        {
            return resolvedController;
        }

        AnimatorOverrideController overrideController = new AnimatorOverrideController(resolvedController);
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();

        foreach (AnimationClip originalClip in overrideController.animationClips)
        {
            AnimationClip replacement = ResolveReplacementClip(
                originalClip,
                loopClip,
                throwClip,
                throwToIdleClip,
                postThrowIdleClip
            );

            if (replacement != null)
            {
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(originalClip, replacement));
            }
        }

        if (overrides.Count > 0)
        {
            overrideController.ApplyOverrides(overrides);
        }

        return overrideController;
    }

    static AnimationClip ResolveReplacementClip(
        AnimationClip originalClip,
        AnimationClip loopClip,
        AnimationClip throwClip,
        AnimationClip throwToIdleClip,
        AnimationClip postThrowIdleClip)
    {
        if (originalClip == null)
        {
            return null;
        }

        string clipName = originalClip.name.ToLowerInvariant();

        if (clipName.Contains("throwtoidle") && throwToIdleClip != null)
        {
            return throwToIdleClip;
        }

        if (clipName.Contains("throw") && throwClip != null)
        {
            return throwClip;
        }

        if (clipName.Contains("idle") && postThrowIdleClip != null)
        {
            return postThrowIdleClip;
        }

        if (clipName.Contains("loop") && loopClip != null)
        {
            return loopClip;
        }

        return null;
    }

    public static void ApplyToAnimator(
        Animator animator,
        RuntimeAnimatorController baseController,
        AnimationClip loopClip,
        AnimationClip throwClip,
        AnimationClip throwToIdleClip = null,
        AnimationClip postThrowIdleClip = null)
    {
        if (animator == null)
        {
            return;
        }

        RuntimeAnimatorController runtimeController = BuildController(
            baseController,
            loopClip,
            throwClip,
            throwToIdleClip,
            postThrowIdleClip
        );

        if (runtimeController != null)
        {
            animator.runtimeAnimatorController = runtimeController;
        }
    }

    public static void ApplyDualCharacterAnimators(
        Animator playerAnimator,
        Animator bossAnimator,
        RuntimeAnimatorController baseController,
        AnimationClip playerLoopClip,
        AnimationClip playerThrowClip,
        AnimationClip playerThrowToIdleClip,
        AnimationClip playerPostThrowIdleClip,
        AnimationClip bossLoopClip,
        AnimationClip bossThrowClip)
    {
        ApplyToAnimator(
            playerAnimator,
            baseController,
            playerLoopClip,
            playerThrowClip,
            playerThrowToIdleClip,
            playerPostThrowIdleClip
        );

        ApplyToAnimator(
            bossAnimator,
            baseController,
            bossLoopClip,
            bossThrowClip
        );
    }
}
