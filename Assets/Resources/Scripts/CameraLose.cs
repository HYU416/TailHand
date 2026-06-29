using UnityEngine;

public class CameraLose : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private GameOver gameOver;

    [SerializeField]
    private Vector3 offset = new Vector3(0f, 2f, -4f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (!gameOver.IsStart()) return;
        targetCamera.transform.position = player.position + offset;
        targetCamera.transform.LookAt(player);
    }
}
