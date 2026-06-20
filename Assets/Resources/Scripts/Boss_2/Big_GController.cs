using UnityEngine;

public class Big_GController : MonoBehaviour
{
    // ステータス
    [System.Serializable]
    public struct FStatus
    {
        public int speed;
        public int jumpForce;
    }


    // ドラミング攻撃の情報
    public struct FDrumming
    {
        public float interval;
    }

    // 行動状態
    public enum EState
    {
        Idle,
        Move,
        Attack,
    }

    // 攻撃状態
    public enum EAttackState
    {
        Drumming,
        ShockWave,
        NoiseCannon
    }

    [SerializeField] private Rigidbody rb;
    [SerializeField] private Player player;
    [SerializeField] private GameObject shockWavePrefab;
    [SerializeField] private ObjectFlasher objectFlasher;

    [SerializeField] private LayerMask targetLayer;         // 攻撃対象とするレイヤー

    [SerializeField] private FStatus status;
    [SerializeField] private FDrumming drumming;
    [SerializeField] private EAttackState attackState;

    [SerializeField] private GameObject centerOfStage;      // ステージ中央を設定
    [SerializeField] private Vector3[] footPoses;           // レイの出る位置
    [SerializeField] private float footRayLength = 0.0f;    // レイの長さ

    private bool bJump = false;                             // ジャンプ判定
    private Vector3[] footWorldPoses;                       // レイのワールド座標

    private float totalDeltaTime = 0.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null)
        {
            Debug.Log("Error ShockWaveFunction NoSetPlayer");
            Destroy(this);
        }

        // 足元座標をワールド値に書き換え
        footWorldPoses = new Vector3[footPoses.Length];
        var myPos = transform.position;
        for (int i = 0; i < footPoses.Length; ++i)
        {
            var wPos = myPos + footPoses[i];
            footWorldPoses[i] = new Vector3(wPos.x, wPos.y, wPos.z);
        }

        // フラッシュ挙動の初期化
        objectFlasher.InitializeFlashingData();
    }

    // Update is called once per frame
    void Update()
    {
        totalDeltaTime += Time.deltaTime;
        SelectMove();
        SelectAttack();
    }

    private void SelectMove()
    {

    }

    private void SelectAttack()
    {
        switch (attackState)
        {
            case EAttackState.Drumming:
                Drumming();
                break;
            case EAttackState.ShockWave:
                ShockWave();
                break;
            case EAttackState.NoiseCannon:
                NoiseCannon();
                break;
        }
    }

    private void Drumming()
    {
        if (JumpToTargetPos(centerOfStage.transform.position))
        {

        }
    }

    private void ShockWave()
    {
        if (player != null && shockWavePrefab != null)
        {
            if (JumpToTargetPos(player.transform.position))
            {
                Debug.Log("ToTarget");
                if (CheckGround())
                {
                    Debug.Log("HitGround");
                    Vector3 instancePos = this.transform.position;
                    if (footWorldPoses.Length > 0)
                        instancePos.y = footWorldPoses[0].y;
                    // ショックウェーブの生成
                    GameObject waveObject = Instantiate(shockWavePrefab, instancePos, transform.rotation);
                }
            }
        }
    }

    private void NoiseCannon()
    {
        // 点滅挙動の更新
        objectFlasher.UpdateFlashing();


        // 点滅動作終了後の処理
        //objectFlasher.ResetFlashing();
    }

    private bool CheckGround()
    {
        // 地面に足を付けていると認識する値
        const float toleranceGround = 1e-5f;

        if (rb.linearVelocity.y <= toleranceGround)
        {
            // レイ座標ごとにヒット判定
            foreach (var pos in footWorldPoses)
            {
                Ray ray = new Ray(pos, Vector3.down);
                Vector3 endPos = pos + new Vector3(0.0f, -footRayLength, 0.0f);
                Debug.DrawLine(pos, endPos, Color.green);

                // 全ての判定を取る
                RaycastHit[] hits = Physics.RaycastAll(ray, footRayLength);

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

    private bool JumpToTargetPos(Vector3 targetPos)
    {
        if (!bJump)         // ジャンプ中ならスキップ
        {
            if (CheckGround())
            {
                rb.AddForce(Vector3.up * status.jumpForce, ForceMode.VelocityChange);
                bJump = true;
            }
        }
        else         // ジャンプ中処理
        {
            const float toleranceDis = 6;
            Vector3 diff = transform.position - targetPos;

            Debug.Log(diff.magnitude);
            Debug.Log(toleranceDis);
            // 目的地の許容範囲内に入ったか
            if (diff.magnitude <= toleranceDis)
            {

                return true;
            }
            else
            {
                // ターゲット地点まで移動
                const float alpha = 0.02f;
                Vector2 TargetPos = new Vector2(targetPos.x, targetPos.z);
                var pos = transform.position;
                Vector2 currentPos = new Vector2(pos.x, pos.z);
                var result = Vector2.Lerp(currentPos, TargetPos, alpha);
                transform.position = new Vector3(result.x, pos.y, result.y);
            }
        }
        return false;
    }



    private void OnDrawGizmos()
    {
        // Vector3を可視化
        Vector3[] wPos = new Vector3[footPoses.Length];
        for (int i = 0; i < footPoses.Length; ++i)
        {
            wPos[i] = transform.position + footPoses[i];
        }

        // 色を赤にする
        Gizmos.color = Color.red;
        float radius = 0.5f;

        foreach (var pos in wPos)
        {
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}
