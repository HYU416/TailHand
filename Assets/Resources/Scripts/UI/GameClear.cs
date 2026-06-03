using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClear : MonoBehaviour
{
    [SerializeField]
    [Header("勝利演出に使う時間")]
    float winRenderTime = 10f;

    [SerializeField]
    [Header("勝利時のテキスト")]
    [Tooltip("GameClearの文字")] GameObject winTextObject;

    bool isStart = false;
    float timer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        winTextObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!isStart) return;
        timer += Time.unscaledDeltaTime;

        if (timer > winRenderTime)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void StartWin()
    {
        winTextObject.SetActive(true);
        isStart = true;
        Time.timeScale = 0;
    }
}
