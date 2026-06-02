using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [SerializeField]
    [Header("”s–k‰‰o‚ÉŽg‚¤ŽžŠÔ")]
    float loseRenderTime = 10f;

    [SerializeField] [Header("”s–kŽž‚ÌƒeƒLƒXƒg")]
    [Tooltip("GameOver‚Ì•¶Žš")] GameObject loseTextObject;

    bool isStart;
    float timer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        loseTextObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStart) return;

        timer += Time.unscaledDeltaTime;

        if(timer > loseRenderTime)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void StartLose()
    {
        loseTextObject.SetActive(true);
        isStart = true;

        Time.timeScale = 0f;
    }
}
