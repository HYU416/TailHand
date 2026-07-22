using SimplestarGame;
using UnityEngine;
using System.Collections;

public class BreakObject : MonoBehaviour
{
    [SerializeField]
    private Transform camereTf;
    [Header("”š”­—Í")]
    [SerializeField] float explosionForce = 300f;
    [Header("”š”­”¼Œa")]
    [SerializeField] float explosionRadius = 1f;
    
    public void OnBreak()
    {
        Collider col = GetComponent<Collider>();

        if (col == null)
        {
            Debug.LogWarning($"{name}: Collider ‚ª‚ ‚è‚Ü‚¹‚ñ");
            return;
        }

        Bounds bounds = col.bounds;

        // Collider‚ÌŠmŽÀ‚ÉŠO‘¤‚©‚ç‰ºŒü‚«‚ÉRay‚ð”ò‚Î‚·
        Vector3 startPos = bounds.center + Vector3.up * (bounds.extents.y + 0.5f);
        Vector3 direction = Vector3.down;
        float distance = bounds.extents.y * 2f + 1.0f;

        Ray ray = new Ray(startPos, direction);
        RaycastHit hit;

        Debug.DrawRay(startPos, direction * distance, Color.red, 3f);

        if (col.Raycast(ray, out hit, distance))
        {
            VoronoiFragmenter frag = GetComponent<VoronoiFragmenter>();

            if (frag != null)
            {
                float scale = 1.0f;
                frag.Fragment(hit);
                StartCoroutine(CoExplodeObjects(hit, scale));
                Debug.Log($"{name}: Fragmentation and explosion applied at {hit.point}");
            }
            else
            {
                Debug.LogWarning($"{name}: VoronoiFragmenter ‚ª‚ ‚è‚Ü‚¹‚ñ");
            }
        }
        else
        {
            Debug.LogWarning($"{name}: Collider.Raycast ‚ª“–‚½‚è‚Ü‚¹‚ñ‚Å‚µ‚½");
        }
    }

    IEnumerator CoExplodeObjects(RaycastHit hit, float scale)
    {
        yield return new WaitForFixedUpdate();

        Collider[] colliders = Physics.OverlapSphere(
            hit.point,
            explosionRadius,
            LayerMask.GetMask("BreakObject_Fragment")
        );

        foreach (Collider item in colliders)
        {
            Rigidbody rigidbody = item.attachedRigidbody;

            if (rigidbody == null)
            {
                continue;
            }

            rigidbody.isKinematic = false;
            rigidbody.AddExplosionForce(
                explosionForce * scale,
                hit.point + hit.normal * 0.1f,
                explosionRadius * scale
            );
        }
    }
}
