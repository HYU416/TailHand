using SimplestarGame;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class BreakBoxTest : MonoBehaviour
{
    [SerializeField]
     private Transform camereTf;
    [Header("”š”­—Í")]
    [SerializeField] float explosionForce = 300f;
    [Header("”š”­”¼Œa")]
    [SerializeField] float explosionRadius = 1f;
    
    private void Update()
    {
        Vector3 objPos = transform.position;
        Vector3 camPos = camereTf.position;
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (Physics.Raycast(camPos, objPos - camPos, out hit))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    var frag = hit.collider.GetComponent<VoronoiFragmenter>();
                    if (frag)
                    {
                        float scale = 1.0f;
                         frag.Fragment(hit);
                        StartCoroutine(this.CoExplodeObjects(hit, scale));
                    }
                }
            }

        }
    }

    IEnumerator CoExplodeObjects(RaycastHit hit, float scale)
    {
        yield return new WaitForFixedUpdate();
        Collider[] colliders = Physics.OverlapSphere(hit.point, this.explosionRadius);
        foreach (var item in colliders)
        {
            if (item.TryGetComponent(out Rigidbody rigidbody))
            {
                rigidbody.isKinematic = false;
                rigidbody.AddExplosionForce(this.explosionForce * scale, hit.point + hit.normal * 0.1f, this.explosionRadius * scale);
            }
        }
    }
}
