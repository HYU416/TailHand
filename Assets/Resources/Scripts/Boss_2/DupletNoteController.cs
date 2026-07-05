using UnityEngine;

public class DupletNoteController : MonoBehaviour
{
    // ステータス
    [System.Serializable]
    public struct FStatus
    {
        public int speed;
        public int BouncingForce;
        public float lifeTime;
        [HideInInspector] public float lifeTimeDuration;
        [HideInInspector] public int boundCount;
        [HideInInspector] public Vector3 defaultLocalScale;
        [HideInInspector] public bool bAlive;
    }
    [SerializeField] private string hasPlayerCatchEnemyName;
    private PlayerCatchEnemy playerCatchEnemy;
    [SerializeField] private FStatus status;
    [SerializeField] private Vector3[] rayPoses;           // レイの出る位置
    [SerializeField] private float rayLength;              // レイの長さ
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameObject drummingShockWave;      // 衝撃波の出るプレファブを設定
    [SerializeField] private ThrowTrajectoryCorrector trajectory;
    [SerializeField] private string throwTargetName;
    private bool bCatching = false;
    private float absoluteLifeDuration = 10.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        status.boundCount = 2;
        status.lifeTimeDuration = status.lifeTime;
        status.defaultLocalScale = this.transform.localScale;
        status.bAlive = true;

        var tailEnd = GameObject.Find(hasPlayerCatchEnemyName);
        playerCatchEnemy = tailEnd.GetComponent<PlayerCatchEnemy>();

        if (playerCatchEnemy == null)
            Destroy(this.gameObject);
        if (trajectory == null)
            Destroy(this.gameObject);
        trajectory.SetThrowTarget(GameObject.Find(throwTargetName));
    }

    // Update is called once per frame
    void Update()
    {
        var delta = Time.deltaTime;
        absoluteLifeDuration -= delta;
        if (absoluteLifeDuration <= 0.0f)
            Destroy(this.gameObject);

        if (rb == null)
            return;
        if (playerCatchEnemy == null)
            return;

        if (!bCatching)
        {
            var obj = playerCatchEnemy.CatchingObjectPtr();
            if (obj == this.gameObject)
            {
                bCatching = true;
                gameObject.layer = LayerMask.NameToLayer("PlayerProjectile");
                rb.linearVelocity = Vector3.zero;
                this.transform.localScale = status.defaultLocalScale / 5;
                return;
            }

            Move(delta);
            gameObject.layer = LayerMask.NameToLayer("EnemyProjectile");
            if (status.boundCount <= 0)
            {
                if (status.lifeTimeDuration <= 0.0f)
                {
                    Destroy(this.gameObject);
                    return;
                }
                status.lifeTimeDuration -= delta;
                ScalingDown();
            }
        }
        if (bCatching && playerCatchEnemy.CatchingObjectPtr() == null)
            gameObject.tag = "Projectile";
        if (!status.bAlive)
            Destroy(this.gameObject);
    }

    private void Move(float delta)
    {
        var pos = this.transform.position;
        pos.x += transform.forward.x * status.speed * delta;
        pos.z += transform.forward.z * status.speed * delta;

        this.transform.position = pos;
        if (CheckGround())
        {
            if (rb != null)
            {
                if (status.boundCount > 0)
                {
                    if (rayPoses.Length > 0)
                    {
                        var rayPos = rayPoses[0];
                        var instancePos = this.transform.position;
                        instancePos.y = rayPos.y;
                        Instantiate(drummingShockWave, instancePos, Quaternion.Euler(0.0f, 0.0f, 0.0f));
                    }
                }

                var vel = rb.linearVelocity;
                vel.y = status.BouncingForce;
                rb.linearVelocity = vel;
                --status.boundCount;
            }
        }
    }

    private void ScalingDown()
    {
        var alpha = status.lifeTimeDuration / status.lifeTime;
        if (alpha < 0.0f)
            alpha = 0.0f;
        var newScale = Vector3.Lerp(Vector3.zero, status.defaultLocalScale, alpha);
        this.transform.localScale = newScale;
    }

    private bool CheckGround()
    {
        // 地面に足を付けていると認識する値
        const float toleranceGround = 1e-5f;

        if (rb.linearVelocity.y <= toleranceGround)
        {
            // レイ座標ごとにヒット判定
            foreach (var pos in rayPoses)
            {
                // ワールド座標に変換
                Vector3 wPos = pos + this.transform.position;

                Ray ray = new Ray(wPos, Vector3.down);
                Vector3 endPos = wPos + new Vector3(0.0f, -rayLength, 0.0f);
                Debug.DrawLine(wPos, endPos, Color.green);

                // 全ての判定を取る
                RaycastHit[] hits = Physics.RaycastAll(ray, rayLength);

                foreach (var hit in hits)
                {
                    if (hit.collider.CompareTag("Ground"))
                    {
                        return true;
                    }
                }

            }
        }
        return false;
    }

    private void OnDrawGizmos()
    {
        // Vector3を可視化
        Vector3[] wPos = new Vector3[rayPoses.Length];
        for (int i = 0; i < rayPoses.Length; ++i)
        {
            wPos[i] = this.transform.position + rayPoses[i];
        }

        // 色を赤にする
        Gizmos.color = Color.red;
        float radius = 0.5f;

        foreach (var pos in wPos)
        {
            Gizmos.DrawWireSphere(pos, radius);
        }
    }

    public void Hit()
    {
        status.bAlive = false;
    }
}
