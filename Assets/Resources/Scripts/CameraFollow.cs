using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Transform target;
    public Vector3 offset;
    public Quaternion Quaternion;

    void LateUpdate()
    {
        transform.position = target.position + offset;

        transform.rotation = Quaternion;
    }
}
