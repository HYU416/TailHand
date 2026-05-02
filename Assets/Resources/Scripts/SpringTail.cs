using UnityEngine;

public class SpringTail : MonoBehaviour
{
    public Transform[] bones; //しっぽのbone配列(根元から順に入れる)
    public Transform root; //しっぽの基準になるTransform

    public float followSpeed = 5.0f; //尻尾がプレイヤーの回転についていく速さ(大きいほどピタッと追従する)
    public float returnSpeed = 30.0f; //尻尾が元の形に戻ろうとする速さ(大きいほど素早く元の姿勢に戻る)
    public float delay = 0.2f; //ボーンごとの遅れ量(大きいほど後ろのボーンが遅れて動く)

    public float inertia = 8.0f; //慣性の強さ(大きいほど振り子のように大きく揺れる)
    public float damping = 1.0f; //慣性の減衰(大きいほど揺れが早く止まる)
    public float rootAmplify = 5.0f; //回転増幅(プレイヤーの回転を何倍にして尻尾に伝えるか)

    public float maxAngle = 15.0f;      //ボーンが曲がれる最大角度(巻きすぎて尻尾がぐるぐるになるのを防ぐ)
    public float maxVelocity = 4.0f;    //慣性上限(回転し続けたときに揺れが暴走するのを防ぐ)

    Quaternion[] baseRotations;
    Quaternion lastRootRotation;
    Vector3[] angularVelocity;

    void Start()
    {
        // ボーンの数分Quaternionの要素を作成
        baseRotations = new Quaternion[bones.Length];
        angularVelocity = new Vector3[bones.Length];

        for (int i = 0; i < bones.Length; i++)
        {
            baseRotations[i] = bones[i].localRotation;  // ボーンごとのローカルローテーションを格納
        }

        lastRootRotation = root.rotation;   // 付け根部分のローテーション
    }

    void LateUpdate()
    {
        Quaternion delta = root.rotation * Quaternion.Inverse(lastRootRotation);

        delta.ToAngleAxis(out float rootAngle, out Vector3 rootAxis);

        if (rootAngle > 180.0f) rootAngle -= 360.0f;

        rootAngle *= rootAmplify;

        delta = Quaternion.AngleAxis(rootAngle, rootAxis);

        for (int i = 0; i < bones.Length; i++)
        {
            float weight = 1.0f + i * delay; //先端ほど揺れる

            Quaternion follow = delta * bones[i].rotation;
            Quaternion baseRot = bones[i].parent.rotation * baseRotations[i];

            // ラープ処理で回転をなめらかに
            Quaternion target = Quaternion.Slerp(
                follow,
                baseRot,
                returnSpeed * Time.deltaTime
            );

            Quaternion current = bones[i].rotation;                     // 現在のクオータニオン
            Quaternion diff = target * Quaternion.Inverse(current);     // ターゲットと現在の回転の差異

            diff.ToAngleAxis(out float angle, out Vector3 axis);        // 回転量と回転軸に分解

            if (angle > 180.0f) angle -= 360.0f;

            //巻きすぎ防止
            angle = Mathf.Clamp(angle, -maxAngle, maxAngle);

            if (axis == Vector3.zero)
                axis = Vector3.up;

            Vector3 accel = axis * angle * Mathf.Deg2Rad * inertia;

            angularVelocity[i] += accel * Time.deltaTime;

            //慣性の最大速度制限
            angularVelocity[i] = Vector3.ClampMagnitude(
                angularVelocity[i],
                maxVelocity
            );

            angularVelocity[i] *= Mathf.Exp(-damping * Time.deltaTime);

            Quaternion inertiaRot =
                Quaternion.AngleAxis(
                    angularVelocity[i].magnitude * Mathf.Rad2Deg * Time.deltaTime,
                    angularVelocity[i].normalized
                );

            bones[i].rotation = inertiaRot * current;
        }

        lastRootRotation = root.rotation;
    }
}