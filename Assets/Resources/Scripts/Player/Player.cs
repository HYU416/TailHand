using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public enum AnimeState
{
    Idle = 0,
    Run = 1,
    Knockback = 2,
}


public class Player : MonoBehaviour
{
    public float rotateSpeed = 10.0f; //Playerの回転の速さ
    public float currentSpeed = 0f;
    public float deceleration = 3.0f;// 1秒あたりの減速量

    [SerializeField] private CameraFollow cameraFollow;

    [Header("Camera")]
    public Transform cameraTransform;

    [Header("各ギアの最低速度")]
    [SerializeField]
    private List<float> gearSpeeds = new List<float>()
    {
        1.0f,3.0f,6.0f,10.0f
    };

    [Header("ギア上昇に必要な秒数")]
    [SerializeField]
    private List<float> gearUpTimes = new List<float>()
    {
        50.0f,30.0f,20.0f
    };

    [Header("Runアニメーションへ移行する値")]
    [SerializeField]
    private float runMotionSpeed = 1e-4f;

    [Header("スピードメーターの角度")]
    [SerializeField] private GameObject speedMeterAllow;
    [SerializeField] private float minAngle = 120.0f;
    [SerializeField] private float maxAngle = -120.0f;

    Rigidbody rb;
    Vector2 moveInput;
    bool movebutton;
    bool isAccelerating = false;
    int currentGear = 0;
    float startSpeed;
    float targetSpeed;
    float gearTimer;
    float gearUpTime;

    [SerializeField] private PlayerMotion playerMotion;
    [SerializeField] private AnimeState animeState;

    [Header("ステージ範囲")]
    [SerializeField] private Transform stageCenter;
    [SerializeField] private float stageRadius = 50f;

    //速度によって砂ぼこりエフェクトの数を変えるためのカーブ
    [Header("速度とエフェクトの数の関係")]
    [SerializeField] private AnimationCurve EffectSpawnCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    //エフェクトのスポーンカウント
    private int EffectSpawnCounter = 0;
    [SerializeField] private PlayerHPBar hpBar;
    [SerializeField] private float invincibilityTime = 0.0f;
    private float invincibilityDuration = 0.0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed  = gearSpeeds[0];
        StartGearUp();
        if (cameraTransform == null){
            cameraTransform = Camera.main.transform;
        }
        speedMeterAllow.transform.localRotation = Quaternion.Euler(0f, 0f, minAngle);
    }

    // Input System の Move イベント
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnMove2(InputValue value)
    {
        float input = value.Get<float>();
        movebutton = input > 0.5f;
    }

    void FixedUpdate()
    {
        //if (!cameraFollow.Gamestart)
        //{
        //    rb.linearVelocity = Vector3.zero;   // Unity6なら linearVelocity
        //    rb.angularVelocity = Vector3.zero;
        //    return;
        //}

        rb.angularVelocity = Vector3.zero;

        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;

        camForward.y = 0f;
        camRight.y = 0f;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 direction =
            camForward * moveInput.y +
            camRight * moveInput.x;

        // 回転
        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );
        }

        // 加速・減速
        if(movebutton || Input.GetKey(KeyCode.LeftShift))
        {
            if(!isAccelerating)
            {
                StartGearUp();
                isAccelerating = true;
            }

            UpdateGearSpeed();
        }
        else
        {
            isAccelerating = false;
            // ギアを下げる
            if(currentGear > 0 && currentSpeed <= gearSpeeds[currentGear - 1])
            {
                currentGear--;
                startSpeed = gearSpeeds[currentGear];
                targetSpeed = gearSpeeds[Mathf.Min(currentGear + 1,gearSpeeds.Count - 1)];
                gearTimer = 0.0f;
                int debugGear = currentGear + 1;
                Debug.Log("ギアが下がりました : " + debugGear);
            }
            // 原則処理　：　0に向かって減速
            currentSpeed -= deceleration * Time.deltaTime;
            // 最低速度で制限をかけて下回ったら停止する
            currentSpeed = Mathf.Max(currentSpeed, 0);
        }

        // スピードメーターの動作
        float speedRate = currentSpeed / gearSpeeds[gearSpeeds.Count - 1];
        speedRate = Mathf.Clamp01(speedRate);

        float angle = Mathf.Lerp(minAngle, maxAngle, speedRate);

        speedMeterAllow.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        //移動
        Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;

        //capsuleキャストで前方に衝突するオブジェクトを検出
        RaycastHit hit;
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        Vector3 point1 = transform.position;
        Vector3 point2 = transform.position + Vector3.up * 2f;
        float radius = 0.5f;
        if (capsule != null)
        {
            //カプセルの中心から上方向に半分の高さを加えた位置を始点とする
            point1 = transform.position + capsule.center + Vector3.up * (capsule.height / 2 - capsule.radius);
            //カプセルの中心から下方向に半分の高さを加えた位置を終点とする
            point2 = transform.position + capsule.center - Vector3.up * (capsule.height / 2 - capsule.radius);
            radius = capsule.radius;
        }
        if (Physics.CapsuleCast(point1, point2, radius, transform.forward, out hit, currentSpeed * Time.deltaTime, ~0, QueryTriggerInteraction.Ignore))
        {
            //Debug.Log("Hit: " + hit.collider.name);
            //貫通対策
            //反射する
            if (hit.collider.gameObject.CompareTag("Boss") || hit.collider.gameObject.CompareTag("ItemBox"))
            {

                float skin = 0.05f;

                rb.MovePosition(
                    rb.position +
                    transform.forward *
                    Mathf.Max(0f, hit.distance - skin)
                );
            }
        }
        else
        {
            rb.MovePosition(rb.position + move);
        }

        // 任意の範囲からPlayerを出れなくする
        LimitStageArea();

        // アニメーション切り替え
        SwitchAnimation(move);
        if (playerMotion)
            playerMotion.SwitchMotion(animeState);

        invincibilityDuration -= Time.deltaTime;
    }

    private void StartGearUp()
    {
        // 最高ギアでギアアップを止める
        if (currentGear >= gearSpeeds.Count - 1) return;
        // 次のギアへの初期化
        startSpeed = currentSpeed;
        targetSpeed = gearSpeeds[currentGear + 1];
        gearUpTime = gearUpTimes[currentGear];
        gearTimer = 0.0f; 
    }

    private void UpdateGearSpeed()
    {
        if (currentGear >= gearSpeeds.Count - 1)
        {
            currentSpeed = gearSpeeds[currentGear];
             return;
        }

        gearTimer += Time.deltaTime; ;
        // 各ギアのスピードアップ時間をtにコピー
        float t = gearTimer / gearUpTime;
        // ｔの値で線形補間
        currentSpeed = Mathf.Lerp(startSpeed, targetSpeed, t);

        if(t >= 1.0f)
        {
            currentGear++;
            currentSpeed = gearSpeeds[currentGear];
            StartGearUp();

            int debugGear = currentGear + 1;
            if (currentGear >= gearSpeeds.Count - 1)
            {
                Debug.Log("最高速度です : " + debugGear);
            }
            else
            {
                Debug.Log("ギアが上がりました : " + debugGear);
            }
        }
    }

    private void SwitchAnimation(Vector3 move)
    {
        move.y = 0.0f;
        if (move.magnitude >= runMotionSpeed)
        {
            // 速度に応じて砂ぼこりエフェクトの数を変える
            EffectSpawnCounter++;
            float spawnInterval = EffectSpawnCurve.Evaluate(currentSpeed / gearSpeeds[gearSpeeds.Count - 1]);
            if (EffectSpawnCounter >= spawnInterval)
            {
                EffectSpawnCounter = 0;
                EffectManager.Instance.Play(EffectType.Dash, transform.position);
            }
            animeState = AnimeState.Run;
        }
        else
            animeState = AnimeState.Idle;
    }

    public void SwitchAnimation(AnimeState state)
    {
        animeState = state;
        if (playerMotion)
            playerMotion.SwitchMotion(animeState);
    }

    private void LimitStageArea()
    {
        if (stageCenter == null) return;

        Vector3 offset = transform.position - stageCenter.position;

        // 高さは無視
        offset.y = 0f;

        float distance = offset.magnitude;

        if (distance > stageRadius)
        {
            Vector3 clampedPos =
                stageCenter.position +
                offset.normalized * stageRadius;

            clampedPos.y = transform.position.y;

            rb.position = clampedPos;
        }
    }
    private void OnDrawGizmosSelected()
    {
        if (stageCenter == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(stageCenter.position, stageRadius);
    }

    public void TakeDamage(float damage)
    {
        if (hpBar != null)
            if (invincibilityDuration <= 0.0f)
            {
                hpBar.TakeDamage(damage);
                invincibilityDuration = invincibilityTime;
            }
    }
}