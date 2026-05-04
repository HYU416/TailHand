using UnityEngine;

public class TestP : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        const float spe = 0.06f;
        if (Input.GetKey(KeyCode.LeftArrow))
            transform.Translate(Vector3.left * spe);
        if (Input.GetKey(KeyCode.RightArrow))
            transform.Translate(Vector3.right * spe);
        if (Input.GetKey(KeyCode.UpArrow))
            transform.Translate(Vector3.forward * spe);
        if (Input.GetKey(KeyCode.DownArrow))
            transform.Translate(Vector3.back * spe);
        float rotInput = 0f;

        if (Input.GetKey(KeyCode.A))
            rotInput -= 1f;
        if (Input.GetKey(KeyCode.D))
            rotInput += 1f;

        if (rotInput != 0)
        {
            // Vector3.up (0, 1, 0) ‚ðŽ²‚É‚µ‚Ä‰ñ“]
            transform.Rotate(Vector3.up * rotInput * 1000.0f * Time.deltaTime);
        }
    }
}
