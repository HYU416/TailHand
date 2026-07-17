using System.Collections;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameOver : MonoBehaviour
{
    [SerializeField] int StageNumber = 1;
    [SerializeField] private Image fadeOutPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject buttonPanel;
    [SerializeField] private CanvasGroup loseCanvasGroup;
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField] private Image[] buttonPanels;
    [SerializeField] private Animator animator;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject losePlayer;
    [SerializeField] private Sprite[] loseNormalSprite;
    [SerializeField] private Sprite[] loseFrontSprite;

    [SerializeField] private float loseFadeStartTime = 1.0f;
    [SerializeField] private float loseFadeDuration = 1.0f;
    [SerializeField] private float buttonFadeStartTime = 1.0f;
    [SerializeField] private float buttonFadeDuration = 1.0f;

    private bool isFadeStarted = false;
    private bool loseFadeStarted = false;
    private bool isAlphaMax = false;
    private int currentIndex = 0;
    private float nextMoveTime = 0f;
    private float moveCooldown = 0.2f;

    private enum ShowState
    {
        LosePanel,
        ButtonPanel,

        None,
    }

    private ShowState currentShowState = ShowState.None;
    bool isStart;

    void Start()
    {
        loseCanvasGroup.alpha = 0f;
        buttonCanvasGroup.alpha = 0f;

        // フェード
        Color fadeColor = fadeOutPanel.color;
        fadeColor.a = 0f;
        fadeOutPanel.color = fadeColor;

        losePlayer.SetActive(false);
    }

    void Update()
    {
        if (!isStart) return;

        if (Time.time > nextMoveTime)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.UpArrow) || v > 0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex - 1, 0, buttonPanels.Length - 1);
                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || v < -0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex + 1, 0, buttonPanels.Length - 1);
                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }

            if (Input.GetButtonDown("Submit"))
            {
                MySoundManeger.Play(gameObject, SEList.SE_ENTER);
                if (currentIndex == 0)
                {
                    if (StageNumber == 1) sceneLoader.LoadScene("GameScene");
                    if (StageNumber == 2) sceneLoader.LoadScene("BossStage_Big_G");
                }
                if (currentIndex == 1)
                {
                    QuitGame();
                }
            }
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, buttonPanels.Length - 1);

        for (int i = 0; i < buttonPanels.Length; i++)
        {
            buttonPanels[i].sprite = i == currentIndex ? loseFrontSprite[i] : loseNormalSprite[i];
        }

        // アニメーション
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        if (!isFadeStarted &&
            stateInfo.normalizedTime >= loseFadeStartTime && !animator.IsInTransition(0))
        {
            if (!isFadeStarted)
            {
                StartCoroutine(BlackOut());
            }
            isFadeStarted = true;
        }

        // Lose表示
        if (currentShowState == ShowState.LosePanel && !loseFadeStarted)
        {
            StartCoroutine(FadeIn(loseCanvasGroup));
            loseFadeStarted = true;
            if (loseCanvasGroup.alpha < 1.0f && !isAlphaMax)
            {
                isAlphaMax = true;
                currentShowState = ShowState.ButtonPanel;
            }
        }

        if (currentShowState == ShowState.ButtonPanel)
        {
            StartCoroutine(FadeIn(buttonCanvasGroup));
            buttonPanel.SetActive(true);
        }
    }
    IEnumerator BlackOut()
    {
        float timer = 0f;
        while (timer < loseFadeDuration)
        {
            timer += Time.deltaTime;
            float t = timer / loseFadeDuration;

            Color c = fadeOutPanel.color;
            c.a = Mathf.Lerp(0f, 0.7f, t);
            fadeOutPanel.color = c;

            yield return null;
        }
        Color end = fadeOutPanel.color;
        end.a = 0.7f;
        fadeOutPanel.color = end;
        currentShowState = ShowState.LosePanel;
    }    

    IEnumerator FadeIn(CanvasGroup canvas)
    {
        float timer = 0f;

        while (timer < buttonFadeDuration)
        {
            timer += Time.deltaTime;
            canvas.alpha = Mathf.Lerp(0f, 1f, timer / buttonFadeDuration);
            yield return null;
        }

        canvas.alpha = 1f;
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
    public void StartLose()
    {
        if(!isStart)
        {
            player.SetActive(false);
            losePlayer.SetActive(true);
            losePlayer.transform.position = player.transform.position ;
        }

        isStart = true;

        Time.timeScale = 1f;
        animator.Play("Take 001", 0, 0f);

        string sceneName = SceneManager.GetActiveScene().name;
        //シーンによってBGMを変える
        if (sceneName == "GameScene")
        {
            GameObject mainCamera  = Camera.main.gameObject;
            MySoundManeger.Stop(mainCamera);
        }
        else if(sceneName == "BossStage_Big_G")
        {
            GameObject mainCamera  = Camera.main.gameObject;
            MySoundManeger.Stop(mainCamera);
        }
        //ゲームオーバーのジングルとBGMを再生
        var intro = MySoundManeger.Play(gameObject, BGMList.BGM_GAMEOVER_JINGLE);
        //intro.time += 70.0f;
        var loop = MySoundManeger.Play(gameObject, BGMList.BGM_GAMEOVER);
        loop.Stop();
        loop.PlayScheduled(AudioSettings.dspTime + intro.clip.length - intro.time);
        //SceneManager.LoadScene("LoseScene");
    }

    public bool IsStart()
    {
        return isStart;
    }
}