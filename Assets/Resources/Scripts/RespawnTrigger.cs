using UnityEngine;

public class RespawnTrigger : MonoBehaviour
{
    [Header("リスポーン地点")]
    [SerializeField] private Transform respawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        // Playerタグだけ反応
        if (!other.CompareTag("Player"))
            return;

        Rigidbody rb = other.attachedRigidbody;

        // 位置移動
        other.transform.position = respawnPoint.position;
        other.transform.rotation = respawnPoint.rotation;

        // Rigidbody速度リセット
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}