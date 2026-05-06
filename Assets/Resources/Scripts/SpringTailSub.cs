using UnityEngine;

public class SpringTailSub : MonoBehaviour
{
    // 鎖の前後のつながりを自前で作成
    [System.Serializable]
    public struct FChainData
    {
        public Transform transform;                           // 自身のTransform情報
        public Rigidbody rb;

        [HideInInspector] public Vector3 currentPos;          // 現在の座標
        [HideInInspector] public Vector3 lastPos;             // 前回の座標
        [HideInInspector] public Vector3 attractiveForce;       // 後の鎖を引っ張る力(速度と鎖同士の張りで計算)

        [HideInInspector] public int prevChainIndex;           // 前の鎖の配列番号(根元方向)
        [HideInInspector] public int nextChainIndex;           // 後の鎖の配列番号(先端方向)
    }

    [SerializeField] private FChainData[] chainsData;

    private int chainCount;
    [Header("前の鎖との伸びの最大値")]
    [SerializeField] private float restLength = 0.4f;  // 前の鎖との伸びの最大値
    [Header("引っ張られた鎖にかかる力の倍率")]
    [SerializeField, Range(1.0f, 100.0f)] private float velocityMagnification = 4.0f;             // 引っ張られた鎖にかかる速度倍率
    [Header("浮力倍率")]
    [SerializeField, Range(0.0f, 5.0f)] private float bouncyForce = 0.65f;                         // 浮力倍率
    [Header("先頭チェーンの浮力倍率")]
    [SerializeField, Range(0.0f, 1.0f)] private float firstBouncyForceMag = 0.7f;                 // 先頭チェーンの浮力倍率(Lerp処理で倍率付け)
    [Header("浮力を強くしすぎてしっぽがプルプルする場合はここの数値を下げる")]
    [SerializeField, Range(0.0f, 1.0f)] private float decreaseRateXZ = 0.95f;                     // 浮力を強くしすぎてしっぽがプルプルする場合はここの数値を下げる

    [Header("XZ方向の加速の最大値")]
    [SerializeField] private float maxForceXZ = 120.0f;
    private const int loopCount = 5;

    void Start()
    {
        chainCount = chainsData.Length;
        // chainの要素数分配列を作成
        for (int i = 0; i < chainCount; ++i)
        {
            // chainデータの初期化
            chainsData[i].currentPos = chainsData[i].transform.position;
            chainsData[i].lastPos = chainsData[i].transform.position;
            chainsData[i].attractiveForce = new Vector3(0, 0, 0);
            chainsData[i].prevChainIndex = i - 1;         // 前後のつながりは固定
            chainsData[i].nextChainIndex = i + 1;

            // 根元以外は物理挙動を安定させる設定
            if (i != 0 && chainsData[i].rb != null)
            {
                chainsData[i].rb.interpolation = RigidbodyInterpolation.Interpolate;
                chainsData[i].rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }
        }
    }

    void FixedUpdate()
    {
        for (int i = 0; i < chainCount; ++i)
        {
            // 事前に物理処理があるのでcurrentPos情報の更新
            chainsData[i].currentPos = chainsData[i].transform.position;
            chainsData[i].attractiveForce = new Vector3(0, 0, 0);
        }

        for (int i = 0; i < chainCount; ++i)
        {
            ref var data = ref chainsData[i];

            // まずは引っ張られている成分があれば座標を再計算(i: 0は金具となる部分なのでコード上で動かさない)
            if (data.prevChainIndex != -1 && i != 0)
            {
                var prevData = chainsData[data.prevChainIndex];               // 1つ前の鎖
                if (IsMoreThanToleranceVector(prevData.attractiveForce, 0.001f))    // 引っ張っている力があるか(0.001fより小さければ0扱い)
                {
                    data.rb.linearVelocity += prevData.attractiveForce;            // 引っ張れた分だけ力を補正

                    for (int j = 0; j < loopCount; ++j)
                        ResolvePhysicsConstraint(ref data);                    // 鎖同士の距離に対して補正をかける
                    ClampForce(ref data);
                }
            }

            // 次の鎖を引っ張る力はi: 0も計算する
            if (chainCount - 1 != i)  // 先端以外なら実行
            {
                Vector3 vel = DiffVector(data.currentPos, data.lastPos);    // 前回からの移動量
                Vector3 nextChainVDis = DiffVector(data.currentPos, chainsData[data.nextChainIndex].currentPos);  // 次の鎖との距離
                float nextChainDis = nextChainVDis.magnitude;
                if (nextChainDis > restLength)   // 鎖が引き合っている状態なら実行
                {
                    float alpha = i / (float)(chainCount - 1);
                    // 先端ほど速度が乗らないようにする
                    float chainVelocityMultiplier = Mathf.Lerp(1.0f, 0.7f, alpha);

                    float bouncy = Mathf.Lerp(bouncyForce * firstBouncyForceMag, bouncyForce, alpha);
                    Vector3 chainBuoyancyVec = new Vector3(vel.normalized.x * vel.magnitude * chainVelocityMultiplier, vel.magnitude * bouncy * chainVelocityMultiplier, vel.normalized.z * vel.magnitude * chainVelocityMultiplier);
                    data.attractiveForce = chainBuoyancyVec * velocityMagnification;
                }
            }

            if (i != 0)
            {
                for (int j = 0; j < loopCount; ++j)
                    ResolvePhysicsConstraint(ref data);                            // 鎖同士の距離に対して補正をかける

                chainsData[i].transform.position = chainsData[i].currentPos;
            }

                DecreaseSpeed(ref data);
        }


        for (int i = 0; i < chainCount; ++i)
        {
            chainsData[i].lastPos = chainsData[i].currentPos;
        }
    }

    // 物理制約を解決させる
    private void ResolvePhysicsConstraint(ref FChainData data)
    {
        ResolveDistance(ref data);
        ResolveAngle(ref data);
    }

    private void ResolveDistance(ref FChainData data)
    {
        Vector3 parentPos = chainsData[data.prevChainIndex].currentPos;
        Vector3 myPos = data.transform.position;

        Vector3 diff = myPos - parentPos;   // 親方向と逆の距離成分
        float distance = diff.magnitude;    // 親方向と逆の距離

        // 最大距離を超えた場合のみ補正
        if (distance > restLength)
        {
            Vector3 direction = diff.normalized;

            // 逆方向への引っ張り成分だけ削除
            float velDot = Vector3.Dot(data.rb.linearVelocity, direction);

            if (velDot > 0)
            {
                // 離れる方向の速度だけをマイナスする
                data.rb.linearVelocity -= direction * velDot;
            }

            Vector3 targetPos = parentPos + (direction * restLength);
            data.rb.MovePosition(targetPos);

            data.currentPos = targetPos;
        }
        else
        {
            data.currentPos = myPos;
        }
    }

    private void ResolveAngle(ref FChainData data)
    {
        // 親への方向へ向くように補正
        var parentData = chainsData[data.prevChainIndex];
        Vector3 dir = parentData.currentPos - data.currentPos;

        if (dir.sqrMagnitude > 0.0001f)
        {
            var quat = Quaternion.LookRotation(dir);
            data.rb.MoveRotation(quat);
        }
    }

    private void ClampForce(ref FChainData data)
    {
        Vector3 vec = data.rb.linearVelocity;

        vec.x = Mathf.Clamp(vec.x, -maxForceXZ, maxForceXZ);
        vec.z = Mathf.Clamp(vec.z, -maxForceXZ, maxForceXZ);

        data.rb.linearVelocity = vec;
    }

    private void DecreaseSpeed(ref FChainData data)
    {
        Vector3 vec = data.rb.linearVelocity;
        vec.x *= decreaseRateXZ;
        vec.z *= decreaseRateXZ;

        if (IsLessThanToleranceVector(chainsData[0].currentPos - chainsData[0].lastPos, 0.01f))
            vec.y -= 9.81f * 0.1f;
        else
            vec.y -= 9.81f * 0.06f;

        data.rb.linearVelocity = vec;

        Debug.Log("aa");
    }

    // 指定した許容値よりおおきければTrueを返す
    private bool IsMoreThanToleranceVector(Vector3 vec, float tolerance)
    {
        float mag = vec.magnitude;
        return mag > tolerance;
    }

    private bool IsLessThanToleranceVector(Vector3 vec, float tolerance)
    {
        float mag = vec.magnitude;
        return mag < tolerance;
    }


    private Vector3 DiffVector(Vector3 vec1, Vector3 vec2)
    {
        return vec1 - vec2;
    }
}