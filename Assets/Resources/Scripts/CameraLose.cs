using System.Collections;
using UnityEngine;

public class CameraLose : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform player;
    [SerializeField] private GameOver gameOver;

    [Header("カメラ開始位置")]
    [SerializeField] private Vector3 startOffset = new Vector3(0f, 0.6f, -2.5f);
    [Header("カメラ終了位置")]
    [SerializeField] private Vector3 endOffset = new Vector3(0f, 2f, -4f);
    [Header("見る位置")]
    [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.2f, 0f);

    [SerializeField] private float duration = 1.5f;

    private bool isPlaying = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (!gameOver.IsStart()) return;
        if (isPlaying)
        {
            StartCoroutine(CameraMove());
        }
    }

    private IEnumerator CameraMove()
    {
        float timer = 0f;

        Vector3 startPos = player.position + player.TransformDirection(startOffset);
        Vector3 endPos = player.position + player.TransformDirection(endOffset);

        while(timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            t = Mathf.SmoothStep(0f,1f,t);

            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t); 
            targetCamera.transform.position = currentPos;

            Vector3 lookPos = player.position + player.TransformDirection(lookOffset);
            Vector3 direction = lookPos - targetCamera.transform.position;  

            if (direction != Vector3.zero)
            {
                targetCamera.transform.rotation = Quaternion.LookRotation(direction);
            }

            yield return null;  
        }

        targetCamera.transform.position = endPos;

        Vector3 finalLookPos = player.position + player.TransformDirection(lookOffset);
        targetCamera.transform.rotation = Quaternion.LookRotation(finalLookPos - targetCamera.transform.position);

        isPlaying = false;
    }
}
