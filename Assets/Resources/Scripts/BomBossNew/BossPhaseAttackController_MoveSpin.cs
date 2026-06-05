using System.Collections;
using UnityEngine;

public partial class BossPhaseAttackController
{
    [Header("ボス回転速度上限")]
    [SerializeField] private bool limitBossRotationSpeed = true;

    [SerializeField] private float maxBossRotationSpeed = 720.0f;

    [Header("スピン攻撃ダメージ")]
    [SerializeField] private int spinDamage = 10;

    [SerializeField] private bool useSpinDamage = true;

    [SerializeField] private float spinDamageInterval = 0.5f;

    [SerializeField] private string spinDamageMethodName = "TakeDamage";

    [Header("スピン攻撃ノックバック")]
    [SerializeField] private bool useSpinKnockback = true;

    [SerializeField] private float spinKnockbackInterval = 0.5f;

    [SerializeField] private float characterControllerKnockbackTime = 0.2f;

    [SerializeField] private float characterControllerKnockbackSpeedMultiplier = 1.0f;

    private float rotationLimitPreviousY;
    private bool hasRotationLimitPreviousY;

    private Coroutine characterControllerKnockbackCoroutine;

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
        float spinDamageTimer = 0f;
        float spinKnockbackTimer = 0f;

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

            UpdateSpinDamage(ref spinDamageTimer);
            UpdateSpinKnockback(ref spinKnockbackTimer);

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

    private void UpdateSpinDamage(ref float spinDamageTimer)
    {
        if (!useSpinDamage)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(spinDamageMethodName))
        {
            return;
        }

        if (spinDamageInterval <= 0f)
        {
            spinDamageInterval = 0.5f;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > spinDetectRange)
        {
            spinDamageTimer = 0f;
            return;
        }

        spinDamageTimer -= Time.deltaTime;

        if (spinDamageTimer > 0f)
        {
            return;
        }

        ApplySpinDamage();

        spinDamageTimer = spinDamageInterval;
    }

    private void ApplySpinDamage()
    {
        if (player == null)
        {
            return;
        }

        GameObject damageTarget = player.gameObject;

        damageTarget.SendMessage(
            spinDamageMethodName,
            spinDamage,
            SendMessageOptions.DontRequireReceiver
        );

        Transform parent = player.parent;

        while (parent != null)
        {
            parent.gameObject.SendMessage(
                spinDamageMethodName,
                spinDamage,
                SendMessageOptions.DontRequireReceiver
            );

            parent = parent.parent;
        }

        if (showDebugLog)
        {
            Debug.Log("スピン攻撃：" + spinDamage + " ダメージを与えました");
        }
    }

    private void UpdateSpinKnockback(ref float spinKnockbackTimer)
    {
        if (!useSpinKnockback)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (spinKnockbackInterval <= 0f)
        {
            spinKnockbackInterval = 0.5f;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > spinDetectRange)
        {
            spinKnockbackTimer = 0f;
            return;
        }

        spinKnockbackTimer -= Time.deltaTime;

        if (spinKnockbackTimer > 0f)
        {
            return;
        }

        ApplySpinKnockback();

        spinKnockbackTimer = spinKnockbackInterval;
    }

    private void ApplySpinKnockback()
    {
        if (player == null)
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
            playerRb.AddForce(
                direction * spinKnockbackPower + Vector3.up * spinKnockbackUpPower,
                ForceMode.Impulse
            );

            if (showDebugLog)
            {
                Debug.Log("スピン攻撃：Rigidbodyプレイヤーを吹き飛ばしました");
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
            if (characterControllerKnockbackCoroutine != null)
            {
                StopCoroutine(characterControllerKnockbackCoroutine);
            }

            Vector3 knockbackVelocity =
                direction * spinKnockbackPower * characterControllerKnockbackSpeedMultiplier +
                Vector3.up * spinKnockbackUpPower;

            characterControllerKnockbackCoroutine = StartCoroutine(
                CharacterControllerKnockbackRoutine(controller, knockbackVelocity)
            );

            if (showDebugLog)
            {
                Debug.Log("スピン攻撃：CharacterControllerプレイヤーを吹き飛ばしました");
            }
        }
    }

    private IEnumerator CharacterControllerKnockbackRoutine(
        CharacterController controller,
        Vector3 knockbackVelocity
    )
    {
        float timer = 0f;

        while (timer < characterControllerKnockbackTime)
        {
            timer += Time.deltaTime;

            if (controller == null)
            {
                yield break;
            }

            controller.Move(knockbackVelocity * Time.deltaTime);

            yield return null;
        }

        characterControllerKnockbackCoroutine = null;
    }
}