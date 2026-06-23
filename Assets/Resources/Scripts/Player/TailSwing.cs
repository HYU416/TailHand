using UnityEngine;

public class TailSwing : MonoBehaviour
{
    [SerializeField] Transform parent;
    [SerializeField] float swingSpeed;
    Rigidbody[] rbs;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rbs = GetComponentsInChildren<Rigidbody>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 tailVel = -parent.forward * Time.deltaTime * swingSpeed;
        Debug.Log(tailVel);

        foreach (var rb in rbs) 
        {
            rb.AddForce(tailVel, ForceMode.Acceleration);
        }
    }
}
