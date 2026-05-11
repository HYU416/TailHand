using System.Collections.Generic;
using UnityEngine;

public class BombEffect : MonoBehaviour
{
    [Header("エフェクトの寿命")]
    public float lifeTime = 0.6f;

    [Header("最終サイズ")]
    public float maxScale = 3.0f;

    private float timer = 0f;
    private Vector3 startScale;
    HashSet<Collider> hitColliders = new();

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

    private void OnDestroy()
    {
        foreach (var col in hitColliders)
        {
            if (col == null) continue;
            Debug.Log("爆弾がアイテムボックスに当たりました。アイテムボックスを破壊します。");
            Destroy(col.gameObject);

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        //爆発時にアイテムボックスに当たった場合、アイテムボックスを壊す
        if (other.gameObject.CompareTag("ItemBox"))
        {
            hitColliders.Add(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        //爆発時にアイテムボックスに当たった場合、アイテムボックスを壊す
        if (other.gameObject.CompareTag("ItemBox"))
        {
            hitColliders.Remove(other);
        }
    }
}