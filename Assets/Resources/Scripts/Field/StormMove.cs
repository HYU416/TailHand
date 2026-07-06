using UnityEngine;

public class StormMove : MonoBehaviour
{
    [SerializeField]
    [Header("風の移動速度")]
    private Vector3 force = new Vector3(10f, 0f, 10f);
    private float destroyTime = 10f; // 破棄までの時間
    private Rigidbody rb;
    private Vector3 direction;
    private float gravity = -9.81f; // 重力加速度

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        //オブジェクトのみぎうしろを取得
        direction = (-transform.right - transform.forward).normalized;
        Destroy(gameObject, destroyTime); // 10秒後にオブジェクトを破棄
    }

    void Update()
    {
        //オブジェクトの位置を更新
        Vector3 velocity = rb.linearVelocity;
        velocity.x = direction.x * force.x;
        //重力を加える
       // velocity.y += gravity * Time.fixedDeltaTime;
        velocity.z = direction.z * force.z;
        rb.linearVelocity = velocity;
        //Debug.Log("StormMove FixedUpdate: " + rb.linearVelocity);
    }
}
