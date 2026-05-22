using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    [Header("ボス回転速度上限")]
    [SerializeField] private bool limitBossRotationSpeed = true;

    [SerializeField] private float maxBossRotationSpeed = 720.0f;

    private float rotationLimitPreviousY;
    private bool hasRotationLimitPreviousY;

    private IEnumerator Attack_Move(PhaseAttackSetting setting)
    {
        Transform targetPoint = GetNextMovePoint();

        if (targetPoint == null)
        {
            yield break;
        }

        while (Vector3.Distance(transform.position, targetPoint.position) > setting.moveStopDistance)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPoint.position,
                setting.moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }

    private IEnumerator Attack_MoveAndAirStrike(PhaseAttackSetting setting)
    {
        Coroutine airStrikeCoroutine = StartCoroutine(Attack_AirStrike(setting));

        yield return StartCoroutine(Attack_Move(setting));

        if (airStrikeCoroutine != null)
        {
            yield return airStrikeCoroutine;
        }
    }

    private Transform GetNextMovePoint()
    {
        if (movePoints == null || movePoints.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < movePoints.Length; i++)
        {
            int index = nextMovePointIndex;
            nextMovePointIndex = (nextMovePointIndex + 1) % movePoints.Length;

            if (movePoints[index] != null)
            {
                return movePoints[index];
            }
        }

        return null;
    }

    private void UpdateSpinAttack()
    {
        if (ShouldUseAfterAllWallsAttackPattern())
        {
            return;
        }

        if (!useSpinAttack)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (phaseController != null && phaseController.IsBossDefeated)
        {
            return;
        }

        if (spinCooldownTimer > 0f)
        {
            spinCooldownTimer -= Time.deltaTime;

            if (spinCooldownTimer < 0f)
            {
                spinCooldownTimer = 0f;
            }
        }

        if (isSpinRunning)
        {
            return;
        }

        if (spinCooldownTimer > 0f)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= spinDetectRange)
        {
            StartCoroutine(SpinAttackRoutine());
        }
    }

    private IEnumerator SpinAttackRoutine()
    {
        isSpinRunning = true;
        spinCooldownTimer = spinCooldown;

        if (showDebugLog)
        {
            Debug.Log("スピン攻撃開始");
        }

        float timer = 0f;
        bool rigidbodyKnockbackApplied = false;

        while (timer < spinAttackTime)
        {
            timer += Time.deltaTime;

            Transform targetRoot = rotateRoot;

            if (targetRoot == null)
            {
                targetRoot = transform;
            }

            float currentSpinSpeed = spinRotateSpeed;

            if (limitBossRotationSpeed)
            {
                currentSpinSpeed = Mathf.Min(currentSpinSpeed, maxBossRotationSpeed);
            }

            targetRoot.Rotate(
                0f,
                currentSpinSpeed * Time.deltaTime,
                0f,
                Space.World
            );

            TryApplySpinKnockback(ref rigidbodyKnockbackApplied);

            yield return null;
        }

        if (showDebugLog)
        {
            Debug.Log("スピン攻撃終了");
        }

        isSpinRunning = false;
    }

    private void LateUpdate()
    {
        ApplyBossRotationSpeedLimit();
    }

    private void ApplyBossRotationSpeedLimit()
    {
        if (!limitBossRotationSpeed)
        {
            return;
        }

        Transform targetRoot = rotateRoot;

        if (targetRoot == null)
        {
            targetRoot = transform;
        }

        if (targetRoot == null)
        {
            return;
        }

        float currentY = targetRoot.eulerAngles.y;

        if (!hasRotationLimitPreviousY)
        {
            rotationLimitPreviousY = currentY;
            hasRotationLimitPreviousY = true;
            return;
        }

        float deltaY = Mathf.DeltaAngle(rotationLimitPreviousY, currentY);
        float maxDelta = maxBossRotationSpeed * Time.deltaTime;

        if (Mathf.Abs(deltaY) > maxDelta)
        {
            float clampedDelta = Mathf.Clamp(deltaY, -maxDelta, maxDelta);

            Vector3 euler = targetRoot.eulerAngles;
            euler.y = rotationLimitPreviousY + clampedDelta;
            targetRoot.eulerAngles = euler;

            rotationLimitPreviousY = euler.y;
        }
        else
        {
            rotationLimitPreviousY = currentY;
        }
    }

    private void TryApplySpinKnockback(ref bool rigidbodyKnockbackApplied)
    {
        if (player == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > spinDetectRange)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();

        Rigidbody playerRb = player.GetComponent<Rigidbody>();

        if (playerRb == null)
        {
            playerRb = player.GetComponentInParent<Rigidbody>();
        }

        if (playerRb != null)
        {
            if (!rigidbodyKnockbackApplied)
            {
                playerRb.AddForce(
                    direction * spinKnockbackPower + Vector3.up * spinKnockbackUpPower,
                    ForceMode.Impulse
                );

                rigidbodyKnockbackApplied = true;

                if (showDebugLog)
                {
                    Debug.Log("スピン攻撃：Rigidbodyプレイヤーを吹き飛ばしました");
                }
            }

            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();

        if (controller == null)
        {
            controller = player.GetComponentInParent<CharacterController>();
        }

        if (controller != null)
        {
            Vector3 move =
                direction * spinKnockbackPower +
                Vector3.up * spinKnockbackUpPower;

            controller.Move(move * Time.deltaTime);
        }
    }
}