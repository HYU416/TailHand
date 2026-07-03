using UnityEngine;

public class Big_GController : MonoBehaviour
{
    // ステータス
    [System.Serializable]
    public struct FStatus
    {
        public int moveSpeed;                               // 目的の座標にどの速度で移動させるか
        public int orbitSpeed;                              // 角速度(経路をn度毎秒で移動する)
        public int rotationSpeed;                           // 回転速度(回転処理が必要な場合に1秒間にY軸をどの速度で回転させるか)
        public int jumpForce;                               // ジャンプ力
        [HideInInspector] public float currentAngle;        // 現在のターゲットに対する周角度
    }

    // 攻撃用特殊パラメーター
    [System.Serializable]
    public struct FAttackStatus
    {
        [HideInInspector] public float startupTimeDuration;
        [HideInInspector] public float activeTimeDuration;
        [HideInInspector] public float recoveryTimeDuration;
        [HideInInspector] public float airTimeDuration;
        public float heightLimit;                           // 高さ制限
        public float dropForce;                             // 落下初速度
        public float airTimer;                              // 滞空時間(エディタ設定用)
        [HideInInspector] public bool bAttack;
        [HideInInspector] public int attackCounter;
    }


    // ドラミング攻撃の情報
    [System.Serializable]
    public struct FDrumming
    {
        public int attackCount;
        public float startupTime;                           // 予備動作時間
        public float activeTime;                            // 攻撃時間
        public float intervalTime;                          // 攻撃間隔(attackStatus.recoveryTimeと共有)
        public float recoveryTime;                          // 硬直時間
        public int weight;                                  // 攻撃が選択される重み付け
        [HideInInspector] public bool bTargetReached;
        [HideInInspector] public int[] currentAttackDirection;         // 今回の攻撃方向
        [HideInInspector] public int[] lastAttackDirection;            // 前回の攻撃方向
    }

    [System.Serializable]
    public struct FShockWave
    {
        [HideInInspector] public int attackCount;
        public float startupTime;                           // 予備動作時間
        public float activeTime;                            // 攻撃時間
        public float recoveryTime;                          // 硬直時間
        public int weight;                                // 攻撃が選択される重み付け
        [HideInInspector] public bool bTargetReached;
    }

    [System.Serializable]
    public struct FNoiseCannon
    {
        [HideInInspector] public int attackCount;
        public float startupTime;                           // 予備動作時間
        public float activeTime;                            // 攻撃時間
        public float recoveryTime;                          // 硬直時間
        public int weight;                                // 攻撃が選択される重み付け
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

    [SerializeField] private Big_G big_g;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Player player;
    [SerializeField] private GameObject target;
    // 各プレファブ
    [SerializeField] private GameObject shockWavePrefab;
    [SerializeField] private GameObject dupletNotePrefab;
    [SerializeField] private GameObject eighthNotePrefab;
    [SerializeField] private GameObject noiseCannonProjectilePrefab;

    [SerializeField] private ObjectFlasher objectFlasher;

    [SerializeField] private FStatus status;
    [SerializeField] private FAttackStatus attackStatus;
    [SerializeField] private FDrumming drumming;
    [SerializeField] private FShockWave shockWave;
    [SerializeField] private FNoiseCannon noiseCannon;
    [SerializeField] private EState state;
    [SerializeField] private EAttackState attackState;

    [SerializeField] private GameObject centerOfStage;                  // ステージ中央を設定
    [SerializeField] private Vector3[] footPoses;                       // レイの出る位置
    [SerializeField] private float footRayLength = 0.0f;                 // レイの長さ

    [SerializeField] private Vector3 eighthNotePosOffset;               // 8分音符の出現位置オフセット
    [SerializeField] private Vector3 dupletNotePosOffset;               // 2連符の出現位置オフセット
    [SerializeField] private Vector3 noiseCannonPosOffset;              // ノイズキャノンの出現位置オフセット
    [SerializeField] private float distanceTarget;                      // ターゲットとの距離間

    [SerializeField] private Vector2 attackTriggerRandomTim;            // 移動から攻撃に移行する時間のランダム値
    private float attackTriggerDuration = 0.0f;                         // 移動から攻撃に移行する時間
    private bool bJump = false;                                         // ジャンプ判定

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (player == null)
        {
            Debug.Log("Error ShockWaveFunction NoSetPlayer");
            Destroy(this);
        }
        Initialize();

        // フラッシュ挙動の初期化
        objectFlasher.InitializeFlashingData();
        shockWave.attackCount = 1;
        noiseCannon.attackCount = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (big_g != null)
            if (big_g.GetCurrentState() == EBig_GState.Normal)
            {
                SelectAttack();
                State();
            }
            else if (big_g.GetCurrentState() == EBig_GState.Core)
            {
                if (attackStatus.bAttack)
                {
                    ResetDrummingStatus();
                    ResetNoiseCannonStatus();
                    ResetShockWaveStatus();
                    ResetAttackStatus();
                    objectFlasher.ResetFlashing();

                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = true;
                }
                //Move();
            }
    }

    private void LateUpdate()
    {
        var pos = this.transform.position;
        // 高さ制限
        if (attackStatus.heightLimit <= pos.y)
        {
            pos.y = attackStatus.heightLimit;
            this.transform.position = pos;
        }
    }

    private void Initialize()
    {
        if (target != null)
        {
            Vector3 diff = this.transform.position - target.transform.position;
            float rad = Mathf.Atan2(diff.z, diff.x);
            status.currentAngle = rad * Mathf.Rad2Deg;
            if (status.currentAngle > 360.0f)
                status.currentAngle -= 360.0f;
            else if (status.currentAngle < 0.0f)
                status.currentAngle += 360.0f;
        }

        ResetDrummingStatus();
        ResetShockWaveStatus();
        ResetNoiseCannonStatus();
        ResetAttackStatus();
    }

    private void ResetAttackStatus()
    {

        bJump = false;
        attackStatus.bAttack = false;
        attackStatus.recoveryTimeDuration = 0.0f;
        attackStatus.airTimeDuration = attackStatus.airTimer;
        attackStatus.attackCounter = 0;
        state = EState.Idle;
    }

    private void ResetDrummingStatus()
    {
        attackStatus.recoveryTimeDuration = drumming.recoveryTime;
        drumming.bTargetReached = false;
        drumming.currentAttackDirection = new int[3];
        drumming.lastAttackDirection = new int[3];
    }

    private void ResetShockWaveStatus()
    {
        attackStatus.recoveryTimeDuration = shockWave.recoveryTime;
        shockWave.bTargetReached = false;
    }

    private void ResetNoiseCannonStatus()
    {
        attackStatus.recoveryTimeDuration = noiseCannon.recoveryTime;
    }

    private void State()
    {
        switch(state)
        {
            case EState.Idle:
                Idle(); break;
            case EState.Move:
                Move(); break;
            case EState.Attack:
                Attack(); break;
        }
    }

    private void Idle()
    {
        // 硬直時間
        if (IsTimerZero(ref attackStatus.recoveryTimeDuration))
        {
            var rand = Random.Range(attackTriggerRandomTim.x, attackTriggerRandomTim.y);
            attackTriggerDuration = rand;
            state = EState.Move;
        }
    }

    private void Move()
    {
        var deltaTime = Time.deltaTime;

        if (target != null)
        {
            var myPos = this.transform.position;
            var targetPos = target.transform.position;

            // 新しい角度を計算
            // ターゲットに対してどの角度に居るかを更新
            var newAngle = status.currentAngle + status.orbitSpeed * deltaTime;
            if (newAngle > 360.0f)
                newAngle -= 360.0f;
            else if (newAngle < 0.0f)
                newAngle += 360.0f;

            status.currentAngle = newAngle;
            // ラジアン角に変換
            var rad = newAngle * Mathf.Deg2Rad;
            Vector3 targetOrbitPos = new Vector3(
                targetPos.x + Mathf.Sin(rad) * distanceTarget,
                myPos.y,
                targetPos.z + Mathf.Cos(rad) * distanceTarget
                );

            // 滑らかに移動させる
            this.transform.position = Vector3.MoveTowards(myPos, targetOrbitPos, status.moveSpeed * deltaTime);
            // ターゲットの方を向くようにする
            RotateTowardsTarget(targetPos);
        }

        if (big_g.GetCurrentState() == EBig_GState.Normal)
            if (IsTimerZero(ref attackTriggerDuration))
                state = EState.Attack;
    }

    private void Attack()
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

    private void SelectAttack()
    {

        if (!attackStatus.bAttack)
        {
            attackStatus.bAttack = true;
            attackStatus.airTimeDuration = attackStatus.airTimer;

            int addWeight = 0;
            int totalWeight = drumming.weight + shockWave.weight + noiseCannon.weight;
            int randomWeight = Random.Range(0, totalWeight);

            if (SelectWeightAttack(randomWeight, drumming.weight, ref addWeight))
            {
                SetupAttack(drumming.startupTime, drumming.activeTime);
                attackState = EAttackState.Drumming;
            }
            else if (SelectWeightAttack(randomWeight, shockWave.weight, ref addWeight))
            { 
                SetupAttack(shockWave.startupTime, shockWave.activeTime);
                attackState = EAttackState.ShockWave;
            }
            else if (SelectWeightAttack(randomWeight, noiseCannon.weight, ref addWeight))
            {
                SetupAttack(noiseCannon.startupTime, noiseCannon.activeTime);
                attackState = EAttackState.NoiseCannon;
            }
        }
    }

    private bool SelectWeightAttack(int random, int addValue, ref int addWeight)
    {
        addWeight += addValue;
        if (addWeight > random)
            return true;
        return false;
    }

    private void SetupAttack(float startupTime, float activeTime)
    {
        attackStatus.startupTimeDuration = startupTime;
        attackStatus.activeTimeDuration = activeTime;
    }

    private void Drumming()
    {
        if (attackStatus.attackCounter >= drumming.attackCount)
        {
            ResetAttackStatus();
            ResetDrummingStatus();
            return;
        }

        if (!IsTimerZero(ref attackStatus.recoveryTimeDuration))
            return;

        if (drumming.bTargetReached)
        {
            // 対空処理
            if (!IsTimerZero(ref attackStatus.airTimeDuration))
                AntiAircraft();
            else
            {
                Drop();
                // 足が地に着いているか判定
                if (CheckGround())
                {
                    // 攻撃処理
                    {
                        if (IsTimerZero(ref attackStatus.activeTimeDuration))
                        {
                            for (int i = 0; i < 3; ++i)
                            {
                                // 飛ばす方向を指定
                                int dir = SelectDrummingDirection();
                                if (drumming.currentAttackDirection[i] == 0)
                                {
                                    drumming.currentAttackDirection[i] = dir;
                                    InstantiateNote(dir);
                                }
                            }
                            attackStatus.activeTimeDuration = drumming.activeTime;
                            attackStatus.recoveryTimeDuration = drumming.intervalTime;
                            attackStatus.attackCounter++;
                            drumming.currentAttackDirection.CopyTo(drumming.lastAttackDirection, 0);
                            drumming.currentAttackDirection = new int[3];
                        }
                    }
                }
            }
        }
        else if (centerOfStage != null)
        {
            if (IsTimerZero(ref attackStatus.startupTimeDuration))
            {

                // 目標地点に到達してるか
                if (JumpToTargetPos(centerOfStage.transform.position))
                {
                    attackStatus.activeTimeDuration -= Time.deltaTime;
                    drumming.bTargetReached = true;
                }
            }

        }
    }

    private int SelectDrummingDirection()
    {
        int v;
        while (true)
        {
            bool bHit = false;

            // 攻撃方向の決定
            v = Random.Range(1, 9);
            // 前回と同じ方向には攻撃しない
            for (int i = 0; i < 3; ++i)
            {
                if (drumming.lastAttackDirection[i] == v)
                { bHit = true; break; }
            }
            if (bHit)
                continue;
            // 隣り合う方向には攻撃しない
            for (int i = 0; i < 3; ++i)
            {
                int dir = drumming.currentAttackDirection[i];
                if (dir != 0)
                {
                    int leftDir, rightDir;
                    leftDir = dir == 1 ? 8 : dir - 1;
                    rightDir = dir == 8 ? 1 : dir + 1;
                    if (dir == v)
                    { bHit = true; break; }
                    else if (leftDir == v)
                    { bHit = true; break; }
                    else if (rightDir == v)
                    { bHit = true; break; }
                }
            }
            if (bHit)
                continue;
            return v;
        }
    }

    private  void InstantiateNote(int dirNum)
    {
        // 音符の生成
        float instanceRotY = (dirNum - 1) * 45.0f;
        instanceRotY += this.transform.eulerAngles.y;

        int r = Random.Range(0, 2);
        if (r == 0)
        {
            var instancePos = this.transform.position;
            instancePos += dupletNotePosOffset;

            Instantiate(dupletNotePrefab, instancePos, Quaternion.Euler(0.0f, instanceRotY, 0.0f));
        }
        else if (r == 1)
        {
            var instancePos = this.transform.position;
            instancePos += eighthNotePosOffset;

            Instantiate(eighthNotePrefab, instancePos, Quaternion.Euler(0.0f, instanceRotY, 0.0f));
        }
    }

    private void ShockWave()
    {
        if (attackStatus.attackCounter >= shockWave.attackCount)
        {
            ResetAttackStatus();
            ResetShockWaveStatus();
            return;
        }

        if (player != null && shockWavePrefab != null)
        {
            if (shockWave.bTargetReached)
            {
                if (!IsTimerZero(ref attackStatus.airTimeDuration))
                    AntiAircraft();
                else
                {
                    Drop();
                    if (CheckGround())
                    {
                        Vector3 instancePos = this.transform.position;
                        if (footPoses.Length > 0)
                            instancePos.y += footPoses[0].y;
                        // ショックウェーブの生成
                        Instantiate(shockWavePrefab, instancePos, this.transform.rotation);
                        attackStatus.attackCounter++;
                        attackStatus.recoveryTimeDuration = shockWave.recoveryTime;
                    }
                }
            }
            else if (centerOfStage != null)
            {
                if (IsTimerZero(ref attackStatus.startupTimeDuration))
                {
                    // 目標地点に到達してるか
                    if (JumpToTargetPos(player.transform.position))
                    {
                        shockWave.bTargetReached = true;
                    }
                }
            }
        }
    }

    private void NoiseCannon()
    {
        if (attackStatus.attackCounter >= noiseCannon.attackCount)
        {
            ResetAttackStatus();
            ResetNoiseCannonStatus();
            objectFlasher.ResetFlashingData();
            return;
        }

        // 腕組するアニメーション
        if (!IsTimerZero(ref attackStatus.startupTimeDuration))
        {

        }
        else
        {
            if (objectFlasher.HasFinishedFlashing())
            {
                // 点滅動作終了後の処理
                objectFlasher.ResetFlashing();

                // 腕のクロスを解くアニメーション挿入
                {

                }
                if (IsTimerZero(ref attackStatus.activeTimeDuration))
                {
                    // 攻撃処理
                    var instancePos = this.transform.position;
                    instancePos += noiseCannonPosOffset;
                    Instantiate(noiseCannonProjectilePrefab, instancePos, this.transform.rotation);
                    attackStatus.attackCounter++;
                    attackStatus.recoveryTimeDuration = noiseCannon.recoveryTime;
                }
            }
            else
            {
                // 点滅挙動の更新
                objectFlasher.UpdateFlashing();
                // プレイヤーに体の向きを合わせる
                RotateTowardsTarget(target.transform.position);
            }
        }
    }

    private bool CheckGround()
    {
        // 地面に足を付けていると認識する値
        const float toleranceGround = 1e-5f;

        if (rb.linearVelocity.y <= toleranceGround)
        {
            // レイ座標ごとにヒット判定
            foreach (var pos in footPoses)
            {
                // ワールド座標に変換
                Vector3 wPos = pos + this.transform.position;

                Ray ray = new Ray(wPos, Vector3.down);
                Vector3 endPos = wPos + new Vector3(0.0f, -footRayLength, 0.0f);
                Debug.DrawLine(wPos, endPos, Color.green);

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
        else                // ジャンプ中処理
        {
            const float toleranceDis = 1.0f;
            Vector3 diff = this.transform.position - targetPos;
            // Y軸は無視
            diff.y = 0.0f;

            // 目的地の許容範囲内に入ったか
            if (diff.magnitude <= toleranceDis)
            {
                return true;
            }
            else
            {
                // ターゲット地点まで移動
                float alpha = 2.5f * Time.deltaTime;
                Vector2 TargetPos = new Vector2(targetPos.x, targetPos.z);
                var pos = this.transform.position;
                Vector2 currentPos = new Vector2(pos.x, pos.z);
                var result = Vector2.Lerp(currentPos, TargetPos, alpha);
                this.transform.position = new Vector3(result.x, pos.y, result.y);
            }
        }
        return false;
    }

    // 対空処理
    private void AntiAircraft()
    {
        if (rb == null)
            return;

        // 空中で滞空させる
        var vel = rb.linearVelocity;
        vel.y = 0.0f;
        rb.linearVelocity = vel;
        rb.useGravity = false;
    }

    // 対空後の落下処理
    private void Drop()
    {
        if (rb == null)
            return;

        var vel = rb.linearVelocity;
        if (vel.y > -attackStatus.dropForce)
        {
            vel.y -= attackStatus.dropForce;
            rb.linearVelocity = vel;
            rb.useGravity = true;
        }
    }

    private bool IsTimerZero(ref float timer)
    {
        if (timer <= 0.0f)
            return true;

        timer -= Time.deltaTime;
        return false;
    }

    private void RotateTowardsTarget(Vector3 targetPos)
    {
        Vector3 targetDirection = new Vector3(targetPos.x, this.transform.position.y, targetPos.z) - this.transform.position;
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);

            // 現在の回転から目標の回転に向かって滑らかに回転させる！
            this.transform.rotation = Quaternion.RotateTowards(
                this.transform.rotation,
                targetRotation,
                status.rotationSpeed * Time.deltaTime
            );
        }
    }

    private void OnDrawGizmos()
    {
        // Vector3を可視化
        Vector3[] wPos = new Vector3[footPoses.Length + 3];
        for (int i = 0; i < footPoses.Length; ++i)
        {
            wPos[i] = this.transform.position + footPoses[i];
        }
        wPos[footPoses.Length] = this.transform.position + dupletNotePosOffset;
        wPos[footPoses.Length + 1] = this.transform.position + eighthNotePosOffset;
        wPos[footPoses.Length + 2] = this.transform.position + noiseCannonPosOffset;

        // 色を赤にする
        Gizmos.color = Color.red;
        float radius = 0.5f;

        foreach (var pos in wPos)
        {
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}
