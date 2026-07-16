using UnityEngine;

public class Boss_2SceneManager : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject animePlayer;
    [SerializeField] private CameraFollow cameraFollow;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player.SetActive(false);
        animePlayer.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (cameraFollow != null)
        {
            if (cameraFollow.Gamestart)
            {
                player.SetActive(true);
                animePlayer.SetActive(false);
            }
        }
    }
}
