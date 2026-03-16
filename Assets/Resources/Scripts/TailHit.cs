using UnityEngine;

public class TailHit : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;
        if (enemy.Catch) return;
        if (other.CompareTag("Enemy"))
        {
            Destroy(other.gameObject);
        }
    }
}