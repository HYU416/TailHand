using UnityEngine;

public class UVScrool : MonoBehaviour
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private float speedX = 0.2f;
    [SerializeField] private float speedY = 0.0f;
    [SerializeField] private string textureName;

    private Material material;
    private Vector2 offset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = targetRenderer.material;
    }

    // Update is called once per frame
    void Update()
    {
        offset.x += speedX * Time.deltaTime;
        offset.y += speedY * Time.deltaTime;

        material.SetTextureOffset(textureName, offset);
    }
}
