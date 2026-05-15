using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5.0f; //Playerの速さ
    public float rotateSpeed = 10.0f; //Playerの回転の速さ
    public float currentSpeed = 0f;
    public float deceleration = 3.0f;// 1秒あたりの減速量

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

    Rigidbody rb;
    Vector2 moveInput;
    bool movebutton;
    bool isAccelerating = false;
    int currentGear = 0;
    float startSpeed;
    float targetSpeed;
    float gearTimer;
    float gearUpTime;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed  = gearSpeeds[0];
        StartGearUp();
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
        //rb.linearVelocity = Vector3.zero;
        //rb.angularVelocity = Vector3.zero;
        //// 入力を3Dベクトルに変換
        //Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        //if (direction.sqrMagnitude > 0.01f)
        //{
        //    // 向きの回転
        //    Quaternion targetRotation = Quaternion.LookRotation(direction);
        //    transform.rotation = Quaternion.Slerp(
        //        transform.rotation,
        //        targetRotation,
        //        rotateSpeed * Time.fixedDeltaTime
        //    );
        //    Debug.Log(movebutton);

        //    // 前方向に移動
        //    Vector3 move = transform.forward * moveSpeed * Time.fixedDeltaTime;
        //    if (!movebutton) move = Vector3.zero;
        //    rb.MovePosition(rb.position + move);
        //}

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

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

        // 移動
        Vector3 move = transform.forward * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

        //Debug.Log("ギア：　"　+ currentGear);
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
}

