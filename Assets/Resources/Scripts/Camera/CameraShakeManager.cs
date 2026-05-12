using System.Collections;
using UnityEngine;

public class CameraShakeManager : MonoBehaviour
{
    private static CameraShakeManager instance;

    private Transform targetCamera;
    private CameraFollow follow;
    private Vector3 defaultLocalPos;

    private Coroutine shakeCoroutine;

    public static void Initialize(
        Camera cameraTarget
    )
    {
        if (instance != null)
            return;

        GameObject obj =
            new GameObject(
                "CameraShakeManager"
            );

        DontDestroyOnLoad(obj);

        instance =
            obj.AddComponent
            <
                CameraShakeManager
            >();

        instance.targetCamera =
     cameraTarget.transform;

        instance.follow =
            cameraTarget.GetComponent
            <
                CameraFollow
            >();

        instance.defaultLocalPos =
            Vector3.zero;
    }

    public static void Shake(
        float power,
        float time,
        Vector3 axis,
        AnimationCurve curve
    )
    {
        if (instance == null)
        {
            Camera cam =
                Camera.main;

            if (cam == null)
            {
                Debug.LogWarning(
                    "Main Camera not found"
                );

                return;
            }

            Initialize(cam);
        }

        if (
            instance.shakeCoroutine != null
        )
        {
            instance.StopCoroutine(
                instance.shakeCoroutine
            );
        }

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

    private IEnumerator ShakeCoroutine(
        float power,
        float time,
        Vector3 axis,
        AnimationCurve curve
    )
    {
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.deltaTime;

            float t =
                Mathf.Clamp01(
                    timer / time
                );

            float curvePower =
                curve != null
                ? curve.Evaluate(t)
                : 1f - t;

            Vector3 random =
                new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                );

            random.Scale(axis);

            if (follow != null)
            {
                follow.shakeOffset =
                    random.normalized *
                    power *
                    curvePower;
            }

            yield return null;
        }

        if (follow != null)
        {
            follow.shakeOffset =
                Vector3.zero;
        }

        shakeCoroutine = null;
    }
}