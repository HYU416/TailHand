using UnityEngine;

public class BombEffect : MonoBehaviour
{
    [Header("エフェクトの寿命")]
    public float lifeTime = 0.6f;

    [Header("最終サイズ")]
    public float maxScale = 3.0f;

    private float timer = 0f;
    private Vector3 startScale;

    void Start()
    {
        startScale = transform.localScale;
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        timer += Time.deltaTime;

        float t = timer / lifeTime;

        float scale = Mathf.Lerp(startScale.x, maxScale, t);

        transform.localScale = new Vector3(scale, scale, scale);
    }
}