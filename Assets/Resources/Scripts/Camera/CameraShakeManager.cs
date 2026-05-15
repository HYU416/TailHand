using System.Collections;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    //シングルトンインスタンス
    private static CameraShakeManager instance;

    //カメラのTransformとCameraFollowコンポーネントへの参照
    private Transform targetCamera;
    //カメラの揺れを適用するためのCameraFollowコンポーネントへの参照
    private CameraFollow follow;
    //カメラのデフォルトのローカルポジションを保存するための変数
    private Vector3 defaultLocalPos;
    //現在の揺れコルーチンへの参照
    private Coroutine shakeCoroutine;

    //カメラシェイクマネージャーの初期化メソッド
    public static void Initialize(Camera cameraTarget)
    {
        //すでにインスタンスが存在する場合は初期化をスキップ
        if (instance != null)
            return;

        //新しいGameObjectを作成し、CameraShakeManagerコンポーネントを追加してシングルトンインスタンスを設定
        GameObject obj = new GameObject("CameraShakeManager");
        //シーンを跨いでも破壊されないようにする
        DontDestroyOnLoad(obj);
        //instanceをCameraShakeManagerコンポーネントに設定
        instance = obj.AddComponent<CameraShakeManager>();
        //instanceのtargetCameraをcameraTargetのTransformに設定
        instance.targetCamera = cameraTarget.transform;
        //instanceのfollowをcameraTargetのCameraFollowコンポーネントに設定
        instance.follow = cameraTarget.GetComponent<CameraFollow>();
        //instanceのdefaultLocalPosをtargetCameraのローカルポジションに保存
        instance.defaultLocalPos = Vector3.zero;
    }

    /// <summary>
    ///カメラのシェイクを開始
    /// </summary>
    /// <param name="power">シェイクの強さ</param>
    /// <param name="time">シェイクの持続時間</param>
    /// <param name="axis">シェイクの方向を制限するための軸（例：Vector3(1, 0, 1)はXとZ軸のみでシェイク）</param>
    /// <param name="curve">シェイクの強さを時間に応じて変化させるためのアニメーションカーブ（nullの場合は線形減衰）</param>
    public static void Shake(float power, float time, Vector3 axis, AnimationCurve curve)
    {
        //インスタンスが存在しない場合は、Main Cameraを探して初期化
        if (instance == null)
        {
            Camera cam = Camera.main;
            //Main Cameraが見つからない場合は警告を出して終了
            if (cam == null)
            {
                Debug.LogWarning("Main Camera not found");
                return;
            }
            Initialize(cam);
        }

        //すでにシェイクコルーチンが実行中の場合は停止してから新しいシェイクを開始
        if (instance.shakeCoroutine != null)
        {
            instance.StopCoroutine(instance.shakeCoroutine);
        }

        //新しいシェイクコルーチンを開始して、shakeCoroutineに保存
        instance.shakeCoroutine =
            instance.StartCoroutine(
                instance.ShakeCoroutine(
                    power,
                    time,
                    axis,
                    curve
                )
            );
    }

    //シェイクコルーチンの実装
    private IEnumerator ShakeCoroutine(float power, float time, Vector3 axis, AnimationCurve curve)
    {
        //シェイクの持続時間を管理するためのタイマーを初期化
        float timer = 0f;

        //シェイクの持続時間が経過するまでループ
        while (timer < time)
        {
            timer += Time.deltaTime;

            //シェイクの進行度を0から1の範囲で計算
            float t = Mathf.Clamp01(timer / time);

            //アニメーションカーブが指定されている場合はその値を使用し、指定されていない場合は線形減衰を使用してシェイクの強さを計算
            float curvePower =
                curve != null
                ? curve.Evaluate(t)
                : 1f - t;

            //ランダムな方向のベクトルを生成し、指定された軸でスケーリングしてシェイクのオフセットを計算
            Vector3 random =
                new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );
            random.Scale(axis);

            //CameraFollowコンポーネントが存在する場合は、shakeOffsetに計算したシェイクのオフセットを適用
            if (follow != null)
            {
                follow.shakeOffset = random.normalized * power * curvePower;
            }
            yield return null;
        }

        //シェイクが終了したら、shakeOffsetをゼロにリセットして、shakeCoroutineへの参照をクリア
        if (follow != null)
        {
            follow.shakeOffset = Vector3.zero;
        }
        //shakeCoroutineへの参照をクリア
        shakeCoroutine = null;
    }
}