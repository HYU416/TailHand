using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public float moveSpeed = 5.0f; //Playerの速さ
    public float rotateSpeed = 10.0f; //Playerの回転の速さ

    Rigidbody rb;
    Vector2 moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Input System の Move イベント
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    void FixedUpdate()
    {
        // 入力を3Dベクトルに変換
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (direction.sqrMagnitude > 0.01f)
        {
            // 向きの回転
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.fixedDeltaTime
            );

            // 前方向に移動
            Vector3 move = transform.forward * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }
}


