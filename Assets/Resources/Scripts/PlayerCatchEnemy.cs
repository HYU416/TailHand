using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCatchEnemy : MonoBehaviour
{
    [Header("Catch")]
    public float catchRange = 8.0f;
    public Transform tailEnd;
    public KeyCode catchKey = KeyCode.K;

    [Header("Debug")]
    public bool drawVision = false;
    public int visionSegments = 40;

    LineRenderer visionLine;

    void Start()
    {
        if (drawVision)
        {
            visionLine = gameObject.AddComponent<LineRenderer>();

            visionLine.loop = true;
            visionLine.widthMultiplier = 0.05f;
            visionLine.positionCount = visionSegments;

            visionLine.material = new Material(Shader.Find("Sprites/Default"));
            visionLine.startColor = Color.green;
            visionLine.endColor = Color.green;
        }
    }

    public void OnCatch(InputValue value)
    {
        if (value.isPressed)
        {
            CatchEnemy();
        }
    }
    void Update()
    {
        if (drawVision && visionLine != null)
        {
            DrawVisionCircle();
        }
    }

    void CatchEnemy()
    {
        //既にEnemyがいるなら何もしない
        if (tailEnd.childCount > 0)
        {
            Debug.Log("キャッチしてますーーー！！！");
            return;
        }

        //範囲内にEnemyがいないなら検索もしない
        Collider[] hits = Physics.OverlapSphere(transform.position, catchRange);

        Transform closest = null;
        Enemy targetEnemy = null;
        float closestDist = catchRange;

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            Enemy e = hit.GetComponent<Enemy>();
            if (e == null || e.Catch) continue;

            float dist = DistanceXZ(transform.position, hit.transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = hit.transform;
                targetEnemy = e;
            }
        }

        if (closest != null)
        {
            targetEnemy.Catch = true;
            targetEnemy.OnCaught();

            Rigidbody rb = closest.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }

            Collider[] cols = closest.GetComponentsInChildren<Collider>();
            foreach (Collider col in cols)
            {
                col.enabled = false;
            }

            closest.SetParent(tailEnd, false);
            closest.localPosition = Vector3.zero;
            closest.localRotation = Quaternion.identity;
            Vector3 parentScale = tailEnd.lossyScale;
            closest.localScale = new Vector3(
                1f / parentScale.x,
                1f / parentScale.y,
                1f / parentScale.z);
        }
    }

    float DistanceXZ(Vector3 a, Vector3 b)
    {
        Vector2 a2 = new Vector2(a.x, a.z);
        Vector2 b2 = new Vector2(b.x, b.z);

        return Vector2.Distance(a2, b2);
    }

    void DrawVisionCircle()
    {
        float angle = 0f;

        for (int i = 0; i < visionSegments; i++)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * catchRange;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * catchRange;

            Vector3 pos = new Vector3(x, 0, z) + transform.position;

            visionLine.SetPosition(i, pos);

            angle += 360f / visionSegments;
        }
    }
}