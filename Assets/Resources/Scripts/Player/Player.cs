using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum AnimeState
{
    Idle = 0,
    Run = 1,
    Knockback = 2,
}

public class Player : MonoBehaviour
{
    public float rotateSpeed = 10.0f;
    public float currentSpeed = 0f;
    public float deceleration = 3.0f;

    [SerializeField]
    private CameraFollow cameraFollow;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("各ギアの最低速度")]
    [SerializeField]
    private List<float> gearSpeeds = new List<float>()
    {
        1.0f,
        3.0f,
        6.0f,
        10.0f
    };

    [Header("ギア上昇に必要な秒数")]
    [SerializeField]
    private List<float> gearUpTimes = new List<float>()
    {
        50.0f,
        30.0f,
        20.0f
    };

    [Header("Runアニメーションへ移行する値")]
    [SerializeField]
    private float runMotionSpeed = 1e-4f;

    [Header("スピードメーターの角度")]
    [SerializeField]
    private GameObject speedMeterAllow;

    [SerializeField]
    private float minAngle = 120.0f;

    [SerializeField]
    private float maxAngle = -120.0f;

    private Rigidbody rb;
    private Vector2 moveInput;
    private bool movebutton;
    private bool isAccelerating;
    private int currentGear;
    private float startSpeed;
    private float targetSpeed;
    private float gearTimer;
    private float gearUpTime;

    [SerializeField]
    private PlayerMotion playerMotion;

    [SerializeField]
    private AnimeState animeState;

    [Header("ステージ範囲")]
    [SerializeField]
    private Transform stageCenter;

    [SerializeField]
    private float stageRadius = 50f;

    [Header("速度とエフェクトの数の関係")]
    [SerializeField]
    private AnimationCurve effectSpawnCurve =
        AnimationCurve.Linear(
            0f,
            0f,
            1f,
            1f
        );

    private int effectSpawnCounter;

    [Header("HP設定")]
    [SerializeField]
    private PlayerHPBar hpBar;

    [Header("ダメージ後の無敵時間")]
    [Tooltip(
        "0なら無敵時間なしです。" +
        "ビームを0.1秒ごとに当てる場合は0～0.1以下にします"
    )]
    [Min(0f)]
    [SerializeField]
    private float invincibilityTime = 0.0f;

    private float invincibilityDuration;

    [Header("死亡状態")]
    [SerializeField]
    private bool isDead;

    private bool hasLoggedMissingHPBar;

    public bool IsDead
    {
        get { return isDead; }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        /*
         * 同じオブジェクトまたは子オブジェクトに
         * PlayerHPBarがある場合は自動取得する。
         */
        if (hpBar == null)
        {
            hpBar = GetComponent<PlayerHPBar>();
        }

        if (hpBar == null)
        {
            hpBar = GetComponentInChildren<PlayerHPBar>(true);
        }
    }

    private void Start()
    {
        if (gearSpeeds != null &&
            gearSpeeds.Count > 0)
        {
            currentSpeed = gearSpeeds[0];
            StartGearUp();
        }
        else
        {
            currentSpeed = 0f;

            Debug.LogError(
                "Player: Gear Speedsが設定されていません",
                this
            );
        }

        if (cameraTransform == null &&
            Camera.main != null)
        {
            cameraTransform =
                Camera.main.transform;
        }

        if (speedMeterAllow != null)
        {
            speedMeterAllow.transform.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    minAngle
                );
        }

        /*
         * ゲーム開始時点ですでにHPが0の場合も
         * 死亡済みとして扱う。
         */
        if (hpBar != null &&
            hpBar.GetHP() <= 0f)
        {
            isDead = true;
        }
    }

    public void OnMove(InputValue value)
    {
        if (value == null)
        {
            return;
        }

        moveInput = value.Get<Vector2>();
    }

    public void OnMove2(InputValue value)
    {
        if (value == null)
        {
            return;
        }

        float input = value.Get<float>();

        movebutton = input > 0.5f;
    }

    private void FixedUpdate()
    {
        UpdateInvincibilityTime();

        if (isDead)
        {
            StopMovement();
            return;
        }

        if (rb == null)
        {
            return;
        }

        if (cameraFollow != null &&
            !cameraFollow.Gamestart)
        {
            StopMovement();
            return;
        }

        rb.angularVelocity =
            Vector3.zero;

        if (cameraTransform == null)
        {
            return;
        }

        Vector3 camForward =
            cameraTransform.forward;

        Vector3 camRight =
            cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction =
            camForward * moveInput.y +
            camRight * moveInput.x;

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotateSpeed *
                    Time.fixedDeltaTime
                );
        }

        UpdateMovementSpeed();

        UpdateSpeedMeter();

        Vector3 move =
            transform.forward *
            currentSpeed *
            Time.fixedDeltaTime;

        MoveWithCollisionCheck(move);

        LimitStageArea();

        SwitchAnimation(move);

        if (playerMotion != null)
        {
            playerMotion.SwitchMotion(
                animeState
            );
        }
    }

    private void UpdateInvincibilityTime()
    {
        if (invincibilityDuration <= 0f)
        {
            invincibilityDuration = 0f;
            return;
        }

        invincibilityDuration -=
            Time.fixedDeltaTime;

        if (invincibilityDuration < 0f)
        {
            invincibilityDuration = 0f;
        }
    }

    private void StopMovement()
    {
        currentSpeed = 0f;

        if (rb == null)
        {
            return;
        }

        if (!rb.isKinematic)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;
        }
    }

    private void UpdateMovementSpeed()
    {
        if (gearSpeeds == null ||
            gearSpeeds.Count == 0)
        {
            currentSpeed = 0f;
            return;
        }

        if (movebutton ||
            Input.GetKey(KeyCode.LeftShift))
        {
            if (!isAccelerating)
            {
                StartGearUp();
                isAccelerating = true;
            }

            UpdateGearSpeed();
        }
        else
        {
            isAccelerating = false;

            if (currentGear > 0 &&
                currentSpeed <=
                gearSpeeds[currentGear - 1])
            {
                currentGear--;

                startSpeed =
                    gearSpeeds[currentGear];

                targetSpeed =
                    gearSpeeds[
                        Mathf.Min(
                            currentGear + 1,
                            gearSpeeds.Count - 1
                        )
                    ];

                gearTimer = 0.0f;

                int debugGear =
                    currentGear + 1;

                Debug.Log(
                    "ギアが下がりました : " +
                    debugGear
                );
            }

            currentSpeed -=
                deceleration *
                Time.fixedDeltaTime;

            currentSpeed =
                Mathf.Max(
                    currentSpeed,
                    0f
                );
        }
    }

    private void UpdateSpeedMeter()
    {
        if (speedMeterAllow == null)
        {
            return;
        }

        if (gearSpeeds == null ||
            gearSpeeds.Count == 0)
        {
            return;
        }

        float maxSpeed =
            gearSpeeds[
                gearSpeeds.Count - 1
            ];

        float speedRate;

        if (maxSpeed <= 0f)
        {
            speedRate = 0f;
        }
        else
        {
            speedRate =
                currentSpeed / maxSpeed;
        }

        speedRate =
            Mathf.Clamp01(speedRate);

        float angle =
            Mathf.Lerp(
                minAngle,
                maxAngle,
                speedRate
            );

        speedMeterAllow
            .transform
            .localRotation =
            Quaternion.Euler(
                0f,
                0f,
                angle
            );
    }

    private void MoveWithCollisionCheck(
        Vector3 move
    )
    {
        if (rb == null)
        {
            return;
        }

        RaycastHit hit;

        CapsuleCollider capsule =
            GetComponent<CapsuleCollider>();

        Vector3 point1 =
            transform.position;

        Vector3 point2 =
            transform.position +
            Vector3.up * 2f;

        float radius = 0.5f;

        if (capsule != null)
        {
            Vector3 worldCenter =
                transform.TransformPoint(
                    capsule.center
                );

            float halfHeight =
                Mathf.Max(
                    capsule.height * 0.5f -
                    capsule.radius,
                    0f
                );

            point1 =
                worldCenter +
                transform.up *
                halfHeight;

            point2 =
                worldCenter -
                transform.up *
                halfHeight;

            float maxScale =
                Mathf.Max(
                    transform.lossyScale.x,
                    transform.lossyScale.z
                );

            radius =
                capsule.radius *
                maxScale;
        }

        float castDistance =
            currentSpeed *
            Time.fixedDeltaTime;

        bool hasHit =
            Physics.CapsuleCast(
                point1,
                point2,
                radius,
                transform.forward,
                out hit,
                castDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

        if (hasHit)
        {
            GameObject hitObject =
                hit.collider.gameObject;

            if (hitObject.CompareTag("Boss") ||
                hitObject.CompareTag("ItemBox") ||
                hitObject.CompareTag("FieldWall"))
            {
                float skin = 0.05f;

                rb.MovePosition(
                    rb.position +
                    transform.forward *
                    Mathf.Max(
                        0f,
                        hit.distance - skin
                    )
                );

                return;
            }
        }

        rb.MovePosition(
            rb.position + move
        );
    }

    private void StartGearUp()
    {
        if (gearSpeeds == null ||
            gearSpeeds.Count == 0)
        {
            return;
        }

        if (currentGear >=
            gearSpeeds.Count - 1)
        {
            return;
        }

        startSpeed =
            currentSpeed;

        targetSpeed =
            gearSpeeds[currentGear + 1];

        if (gearUpTimes != null &&
            currentGear <
            gearUpTimes.Count)
        {
            gearUpTime =
                Mathf.Max(
                    0.01f,
                    gearUpTimes[currentGear]
                );
        }
        else
        {
            gearUpTime = 0.01f;
        }

        gearTimer = 0.0f;
    }

    private void UpdateGearSpeed()
    {
        if (gearSpeeds == null ||
            gearSpeeds.Count == 0)
        {
            return;
        }

        if (currentGear >=
            gearSpeeds.Count - 1)
        {
            currentSpeed =
                gearSpeeds[currentGear];

            return;
        }

        gearTimer +=
            Time.fixedDeltaTime;

        float safeGearUpTime =
            Mathf.Max(
                gearUpTime,
                0.01f
            );

        float t =
            gearTimer /
            safeGearUpTime;

        currentSpeed =
            Mathf.Lerp(
                startSpeed,
                targetSpeed,
                t
            );

        if (t < 1.0f)
        {
            return;
        }

        currentGear++;

        currentSpeed =
            gearSpeeds[currentGear];

        StartGearUp();

        int debugGear =
            currentGear + 1;

        if (currentGear >=
            gearSpeeds.Count - 1)
        {
            Debug.Log(
                "最高速度です : " +
                debugGear
            );
        }
        else
        {
            Debug.Log(
                "ギアが上がりました : " +
                debugGear
            );
        }
    }

    private void SwitchAnimation(
        Vector3 move
    )
    {
        move.y = 0.0f;

        if (move.magnitude >=
            runMotionSpeed)
        {
            effectSpawnCounter++;

            float maxSpeed = 1f;

            if (gearSpeeds != null &&
                gearSpeeds.Count > 0)
            {
                maxSpeed =
                    Mathf.Max(
                        gearSpeeds[
                            gearSpeeds.Count - 1
                        ],
                        0.01f
                    );
            }

            float spawnInterval =
                effectSpawnCurve.Evaluate(
                    currentSpeed /
                    maxSpeed
                );

            /*
             * 元コードの挙動を維持しつつ、
             * 0以下の間隔にならないようにする。
             */
            spawnInterval =
                Mathf.Max(
                    1f,
                    spawnInterval
                );

            if (effectSpawnCounter >=
                spawnInterval)
            {
                effectSpawnCounter = 0;

                if (EffectManager.IsInitialized)
                {
                    EffectManager.Instance.Play(
                        EffectType.Dash,
                        transform.position
                    );
                }
            }

            animeState =
                AnimeState.Run;
        }
        else
        {
            animeState =
                AnimeState.Idle;
        }
    }

    public void SwitchAnimation(
        AnimeState state
    )
    {
        animeState = state;

        if (playerMotion != null)
        {
            playerMotion.SwitchMotion(
                animeState
            );
        }
    }

    private void LimitStageArea()
    {
        if (stageCenter == null ||
            rb == null)
        {
            return;
        }

        Vector3 offset =
            transform.position -
            stageCenter.position;

        offset.y = 0f;

        float distance =
            offset.magnitude;

        if (distance <= stageRadius)
        {
            return;
        }

        Vector3 clampedPos =
            stageCenter.position +
            offset.normalized *
            stageRadius;

        clampedPos.y =
            transform.position.y;

        rb.position =
            clampedPos;
    }

    private void OnDrawGizmosSelected()
    {
        if (stageCenter == null)
        {
            return;
        }

        Gizmos.color =
            Color.green;

        Gizmos.DrawWireSphere(
            stageCenter.position,
            stageRadius
        );
    }

    public void TakeDamage(float damage)
    {
        /*
         * 死亡後にミサイルやビームが当たっても
         * HP処理を再実行しない。
         */
        if (isDead)
        {
            return;
        }

        if (damage <= 0f)
        {
            return;
        }

        /*
         * GetHPを呼ぶより先に
         * 必ずNullチェックする。
         */
        if (hpBar == null)
        {
            if (!hasLoggedMissingHPBar)
            {
                hasLoggedMissingHPBar = true;

                Debug.LogError(
                    "Player: HP Barが設定されていません。" +
                    "PlayerのInspectorにPlayerHPBarを設定してください。",
                    this
                );
            }

            return;
        }

        float currentHP =
            hpBar.GetHP();

        if (currentHP <= 0f)
        {
            isDead = true;
            return;
        }

        if (invincibilityDuration > 0f)
        {
            return;
        }

        /*
         * 致死ダメージの場合は、
         * HPバー側の死亡イベントが動く前に
         * Playerを死亡状態にする。
         *
         * 死亡イベント中に別の攻撃が当たっても
         * TakeDamageを再実行しなくなる。
         */
        bool isLethalDamage =
            damage >= currentHP;

        if (isLethalDamage)
        {
            isDead = true;
        }

        hpBar.TakeDamage(damage);

        invincibilityDuration =
            Mathf.Max(
                0f,
                invincibilityTime
            );

        /*
         * HPBar.TakeDamage内の死亡処理で
         * HPBar自体が削除された場合にも対応する。
         */
        if (hpBar == null)
        {
            isDead = true;
            return;
        }

        if (hpBar.GetHP() <= 0f)
        {
            isDead = true;
        }
    }
}