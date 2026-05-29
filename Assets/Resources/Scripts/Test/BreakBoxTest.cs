using UnityEngine;

public class BreakBoxTest : MonoBehaviour
{
    [Header("破壊後のオブジェクト")]
    [SerializeField]
    private Transform breakPrefab;
   
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Transform brokenTransform = Instantiate(breakPrefab,transform.position,transform.rotation);
            brokenTransform.localScale  =transform.localScale;
            foreach(Rigidbody rigidbody in brokenTransform.GetComponentsInChildren<Rigidbody>())
            {
                rigidbody.AddExplosionForce(1050.0f, transform.position + Vector3.up * 0.5f, 5.0f);
            }
           Destroy(gameObject);
        }
    }
}
