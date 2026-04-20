using UnityEngine;

public class ChainSim : MonoBehaviour
{
    //public Transform[] bones;
    //public Transform root;

    //public float stiffness = 0.8f;   // 長さ維持の強さ
    //public float drag = 0.98f;       // 減衰
    //public Vector3 gravity = new Vector3(0, 0, 0); // 重力

    //Vector3[] positions;
    //Vector3[] prevPositions;
    //float[] lengths;

    //void Start()
    //{
    //    int count = bones.Length;

    //    positions = new Vector3[count];
    //    prevPositions = new Vector3[count];
    //    lengths = new float[count - 1];

    //    for (int i = 0; i < count; i++)
    //    {
    //        positions[i] = bones[i].position;
    //        prevPositions[i] = positions[i];

    //        if (i > 0)
    //        {
    //            lengths[i - 1] = Vector3.Distance(
    //                bones[i - 1].position,
    //                bones[i].position
    //            );
    //        }
    //    }
    //}

    //void LateUpdate()
    //{
    //    float dt = Time.deltaTime;

    //    // ① 物理っぽく動かす（Verlet）
    //    for (int i = 1; i < positions.Length; i++)
    //    {
    //        Vector3 velocity = (positions[i] - prevPositions[i]) * drag;

    //        prevPositions[i] = positions[i];
    //        positions[i] += velocity;
    //        positions[i] += gravity * dt * dt;
    //    }

    //    // rootは固定
    //    positions[0] = root.position;

    //    // 長さ制約（これがチェーン感の核）
    //    for (int iteration = 0; iteration < 5; iteration++)
    //    {
    //        for (int i = 0; i < lengths.Length; i++)
    //        {
    //            Vector3 dir = positions[i + 1] - positions[i];
    //            float dist = dir.magnitude;
    //            float diff = (dist - lengths[i]) / dist;

    //            Vector3 move = dir * diff * stiffness;

    //            if (i != 0)
    //                positions[i] += move * 0.5f;

    //            positions[i + 1] -= move * 0.5f;
    //        }

    //        positions[0] = root.position;
    //    }

    //    // ③ Transformに反映
    //    for (int i = 0; i < bones.Length; i++)
    //    {
    //        bones[i].position = positions[i];

    //        if (i < bones.Length - 1)
    //        {
    //            Vector3 dir = positions[i + 1] - positions[i];
    //            if (dir != Vector3.zero)
    //                bones[i].rotation = Quaternion.LookRotation(dir);
    //        }
    //    }
    //}
}