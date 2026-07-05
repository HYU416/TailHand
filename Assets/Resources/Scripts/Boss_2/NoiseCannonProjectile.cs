using UnityEngine;

public class NoiseCannonProjectile : MonoBehaviour
{
    [System.Serializable]
    public struct FStatus
    {
        public int atk;
        public int speed;
        public float lifeTime;
        public float maxScaleMagnification;
        [HideInInspector] public Vector3 defaultScale;
    }

    [SerializeField] private FStatus status;
    private float lifeTime;
    private float totalDeltaTime;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        status.defaultScale = this.transform.localScale;
        lifeTime = status.lifeTime;
    }

    // Update is called once per frame
    void Update()
    {
        var deltaTime = Time.deltaTime;
        // 位置更新
        transform.Translate(Vector3.forward * status.speed * deltaTime);


        // スケール更新
        totalDeltaTime += deltaTime;
        var alpha = totalDeltaTime / lifeTime;
        Vector3 scale = Vector3.Lerp(status.defaultScale, status.defaultScale * status.maxScaleMagnification, alpha);
        this.transform.localScale = scale;


        if (status.lifeTime <= 0.0f)
            Destroy(this.gameObject);
        status.lifeTime -= deltaTime;
    }

    void OnTriggerEnter(Collider col)
    {
        var player = col.gameObject.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(status.atk);
        }
    }
}
