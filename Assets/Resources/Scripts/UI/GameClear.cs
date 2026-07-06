using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameClear : MonoBehaviour
{
    [SerializeField]
    [Header("勝利演出に使う時間")]
    float winRenderTime = 10f;

    [SerializeField]
    [Header("勝利時のテキスト")]
    [Tooltip("GameClearの文字")]
    GameObject winTextObject;

    [SerializeField]
    [Header("カウントダウン表示用テキスト")]
    TextMeshProUGUI countdownText;

    [SerializeField]
    [Header("リトライ表示用テキスト")]
    GameObject RetryText;

    bool isStart = false;
    float timer = 0f;

    void Start()
    {
        winTextObject.SetActive(false);
        RetryText.SetActive(false);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!isStart) return;

        timer += Time.unscaledDeltaTime;

        float remainTime = winRenderTime - timer;
        countdownText.text = Mathf.CeilToInt(remainTime).ToString();

        if (timer > winRenderTime)
        {
            //countdownText.text = "0";

            Time.timeScale = 1f;
            //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            SceneManager.LoadScene("TitleScene");
        }
    }

    public void StartWin()
    {
        winTextObject.SetActive(true);
        RetryText.SetActive(true);

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        timer = 0f;
        isStart = true;

        Time.timeScale = 0f;
    }
}