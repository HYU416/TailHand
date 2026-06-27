using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    [SerializeField]
    [Header("敗北演出に使う時間")]
    float loseRenderTime = 10f;

    [SerializeField]
    [Header("敗北時のテキスト")]
    GameObject loseTextObject;

    [SerializeField]
    [Header("カウントダウン表示")]
    TextMeshProUGUI countdownText;

    [SerializeField]
    [Header("リトライ表示用テキスト")]
    GameObject RetryText;

    bool isStart;
    float timer = 0f;

    void Start()
    {
        loseTextObject.SetActive(false);
        RetryText.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isStart) return;

        timer += Time.unscaledDeltaTime;

        // 残り時間
        float remain = loseRenderTime - timer;
        countdownText.text = Mathf.CeilToInt(remain).ToString();

        if (timer > loseRenderTime)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void StartLose()
    {
        loseTextObject.SetActive(true);
        RetryText.SetActive(true);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        timer = 0f;
        isStart = true;

        Time.timeScale = 1f;
        SceneManager.LoadScene("LoseScene");
    }
}