using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.0f; //•’i‚Ì‘¬‚³
    public float chaseSpeed = 4.0f; //Player”­Œ©‚Ì‘¬‚³

    [Header("Vision")]
    public float visionRange = 8.0f; //‹–ì‚Ì‘å‚«‚³

    [Header("Wander")]
    public float wanderChangeTime = 2.0f; //œpœj‚ÌØ‚è‘Ö‚¦ƒ^ƒCƒ~ƒ“ƒO

    [Header("Debug")]
    public bool drawVision = false; //‹–ì‚ğ•`‰æ‚·‚é‚©‚Ç‚¤‚©
    public int visionSegments = 40; //‰~‚Ì•ªŠ„”

    Transform player;

    Vector3 wanderDir;
    float timer;
    public bool Catch;

    LineRenderer visionLine;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        Catch = false;
        PickRandomDirection();

        if (drawVision)
        {
            visionLine = gameObject.AddComponent<LineRenderer>();

            visionLine.loop = true;
            visionLine.widthMultiplier = 0.05f;
            visionLine.positionCount = visionSegments;

            visionLine.material = new Material(Shader.Find("Sprites/Default"));
            visionLine.startColor = Color.red;
            visionLine.endColor = Color.red;
        }
    }

    void Update()
    {
        if (Catch) return;
        float dist = DistanceXZ(player.position, transform.position);

        if (dist < visionRange)
        {
            ChasePlayer();
        }
        else
        {
            Wander();
        }

        if (drawVision && visionLine != null)
        {
            DrawVisionCircle();
        }
    }

    void Wander()
    {
        timer += Time.deltaTime;

        if (timer > wanderChangeTime)
        {
            PickRandomDirection();
            timer = 0;
        }

        transform.position += wanderDir * moveSpeed * Time.deltaTime;
    }

    void PickRandomDirection()
    {
        Vector2 dir = Random.insideUnitCircle.normalized;
        wanderDir = new Vector3(dir.x, 0, dir.y);
    }

    void ChasePlayer()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        transform.position += dir.normalized * chaseSpeed * Time.deltaTime;
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
            float x = Mathf.Cos(Mathf.Deg2Rad * angle) * visionRange;
            float z = Mathf.Sin(Mathf.Deg2Rad * angle) * visionRange;

            Vector3 pos = new Vector3(x, 0, z) + transform.position;

            visionLine.SetPosition(i, pos);

            angle += 360f / visionSegments;
        }
    }
    public void OnCaught()
    {
        //‹–ìü‚ğÁ‚·
        if (visionLine != null)
        {
            Destroy(visionLine);
        }
    }
}