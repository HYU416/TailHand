using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class TItleScene : MonoBehaviour
{
    [SerializeField] private GameObject stageSelectPanel;
    [SerializeField] private GameObject TitlePanel;
    [SerializeField] private GameObject NavigationPanel;
    [SerializeField] private SceneLoader sceneLoader;
    [SerializeField] private Animator animator;
    [SerializeField] private Sprite[] stageSelcetNormalSprite;
    [SerializeField] private Sprite[] stageSelectFrontSprite;
     [SerializeField] private Sprite[] titleNormalSprite;
    [SerializeField] private Sprite[] titleFrontSprite;

    [Header("タイトルのPanel")]
    [SerializeField] private Image[] titlePanels;
    [Header("1行に並べるステージの数")]
    [SerializeField] private int columnCount = 3;
    [Header("コントローラー入力のクールダウン")]
    [SerializeField] private float moveCooldown = 0.2f;
    [Header("各ステージのPanel")]
    [SerializeField] private Image[] stagePanels;
    [Header("各ステージのシーンの名前")]
    [SerializeField] private string[] stageSceneNames;

    private int currentIndex = 0;
    private float nextMoveTime = 0f;
    private Dictionary<RectTransform, Vector2> defaultPositions =
    new Dictionary<RectTransform, Vector2>();

    [Header("カメラワークの設定")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera titleCamera;
    [SerializeField] private float cameraMoveDuration = 2.0f;
    [SerializeField] private float endAngleOffset = 180f;
    [SerializeField] private float lookRightOffset = 5f;
    [SerializeField] private float lookHeight = 3f;
    private bool isCameraMoving = false;
    private Vector3 titleStartOffset;
    private float cameraAngle = 0f;

    [Header("UIアニメーションの設定")]
    [SerializeField] private Transform[] titleAnimationTargets;
    [SerializeField] private Transform[] stageSelectAnimationTargets;
    [SerializeField] private float uiAnimationDuration;
    [SerializeField] private float uiAnimationEndPos = 0f;
    private float defoultShowPos = 0f;
    private float defaultTitleNamePos = 0f;

    [Header("フェードアウト")]
    [SerializeField] private Image whiteFadeImage;
    [SerializeField] private float fadeDuration = 1f;

    [Header("Playerの走るスピード")]
    [SerializeField] private float runSpeed = 1f;

    [Header("Stageの回転スピード")]
    [SerializeField] private GameObject stage;
    [SerializeField] private GameObject item;
    [SerializeField] private float stageRotateSpeed = 10.0f;
    [SerializeField] private float itemRotateSpeed = 10.0f;

    public enum MenuState
    {
        Title,
        StageSelect,
    }
    private MenuState currentState = MenuState.Title;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //NavigationPanel.SetActive(false);
       // stageSelectPanel.transform.position = new Vector3(uiAnimationEndPos, stageSelectPanel.transform.position.y, transform.position.z);
        animator.Play("Run");
        // カメラのポジション
        Vector3 center = player.position + Vector3.up * 1.5f;
        titleStartOffset = titleCamera.transform.position - center;
        // UIのデフォルトポジション
        defoultShowPos = TitlePanel.transform.position.x;
        defaultTitleNamePos = titleAnimationTargets[0].transform.position.x;

        Canvas.ForceUpdateCanvases();
        GridLayoutGroup grid = stageSelectPanel.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.enabled = false;
        foreach (Transform target in stageSelectAnimationTargets)
        {
            RectTransform rect = target as RectTransform;
            if (rect == null) continue;

            Vector2 showPos = rect.anchoredPosition;
            defaultPositions[rect] = showPos;

            Vector2 hidePos = showPos;
            hidePos.x += uiHideOffsetX;

            rect.anchoredPosition = hidePos;
        }
        // フェード
        Color c = whiteFadeImage.color;
        c.a = 0f;
        whiteFadeImage.color = c;

        var intro = MySoundManeger.Play(gameObject, BGMList.BGM_TITLE);
        var loop = MySoundManeger.Play(gameObject, BGMList.BGM_TITLE_LOOP);
        loop.Stop();
        loop.PlayScheduled(AudioSettings.dspTime + intro.clip.length - intro.time);     
    }

    // Update is called once per frame
    void Update()
    {
        // ステージの回転
        stage.transform.Rotate(0,stageRotateSpeed * Time.deltaTime, 0);
        //item.transform.Rotate(0,itemRotateSpeed * Time.deltaTime, 0);

        if (isCameraMoving) return;

        if (Time.time > nextMoveTime)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (Input.GetKeyDown(KeyCode.RightArrow) || h > 0.5f)
            {
                currentIndex += 1;
                nextMoveTime = Time.time + moveCooldown;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || h < -0.5f)
            {
                currentIndex -= 1;
                nextMoveTime = Time.time + moveCooldown;
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || v > 0.5f)
            {
                currentIndex -= columnCount;
                nextMoveTime = Time.time + moveCooldown;
            }
            if (Input.GetKeyDown(KeyCode.DownArrow) || v < -0.5f)
            {
                currentIndex += columnCount;
                nextMoveTime = Time.time + moveCooldown;
            }
        }

        // ステージ一覧が表示されたら
        if (currentState == MenuState.Title)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, titlePanels.Length - 1);

            for (int i = 0; i < titlePanels.Length; i++)
            {
                titlePanels[i].sprite = i == currentIndex ? titleFrontSprite[i] : titleNormalSprite[i];
            }
            // StartButtonでStage一覧表示
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                if (currentIndex == 0 && !isCameraMoving)
                {
                    StartCoroutine(UIAnimation(titleAnimationTargets, false)); 
 
                    StartCoroutine(UIAnimation(stageSelectAnimationTargets, true));
                    
                    StartCoroutine(CameraTurn(true));
                }
                else
                {
                    OnGameExitButton();
                }
            }
        }

        // Titleへ遷移
        if(currentState == MenuState.StageSelect)
        {
            currentIndex = Mathf.Clamp(currentIndex, 1, stagePanels.Length - 1);

            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                // 最後尾のみExitの処理
                if (currentIndex == stagePanels.Count() - 1 && !isCameraMoving)
                {
                    currentState = MenuState.Title;
                    StartCoroutine(UIAnimation(titleAnimationTargets, true));

                    StartCoroutine(UIAnimation(stageSelectAnimationTargets, false));

                    StartCoroutine(CameraTurn(false));

                    //NavigationPanel.SetActive(false);
                }
                else
                {
                    StartCoroutine(LoadStageWithWhiteOut());
                }
            }

            for (int i = 0; i < stagePanels.Length; i++)
            {
                stagePanels[i].sprite = i == currentIndex ? stageSelectFrontSprite[i] : stageSelcetNormalSprite[i];
            }
        } 
    }

    public void OnClickStart()
    {
        currentIndex = 0;
        //NavigationPanel.SetActive(true);
        currentState = MenuState.StageSelect;
    }

    public void OnGameExitButton()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }

    // カメラワーク
    IEnumerator CameraTurn(bool openStage)
    {
        isCameraMoving = true;

        Vector3 center = player.position + Vector3.up * 1.5f;

        float startAngle = cameraAngle;
        float targetAngle = openStage ? endAngleOffset : 0f;

        float timer = 0f;

        while (timer < cameraMoveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / cameraMoveDuration);
            float angle = Mathf.Lerp(startAngle, targetAngle, t);

            Vector3 rotatedOffset = Quaternion.Euler(0f, angle, 0f) * titleStartOffset;
            titleCamera.transform.position = center + rotatedOffset;

            Vector3 lookTarget =
            player.position
            + Vector3.up * lookHeight
            + titleCamera.transform.right * lookRightOffset;

            titleCamera.transform.LookAt(lookTarget);
            yield return null;
        }

        cameraAngle = targetAngle;

        if (openStage)
        {
            OnClickStart();
        }
        else
        {
            currentIndex = 0;
        }

        isCameraMoving = false;
    }

    // UIアニメーション
    IEnumerator UIAnimation(Transform[] parent, bool isShow, float delay = 0.1f)
    {
        GridLayoutGroup grid = stageSelectPanel.GetComponent<GridLayoutGroup>();

        if (grid != null)
        {
           // grid.enabled = false;
        }

        for (int i = 0; i < parent.Length; i++)
        {
            StartCoroutine(MoveUI(parent[i], isShow));
            yield return new WaitForSeconds(delay);
        }

        yield return new WaitForSeconds(uiAnimationDuration);

        if (grid != null && isShow)
        {
           // grid.enabled = true;
        }
    }

    [SerializeField] private float uiHideOffsetX = -1200f;

    IEnumerator MoveUI(Transform target, bool isShow)
    {
        RectTransform rect = target as RectTransform;
        if (rect == null) yield break;

        float timer = 0f;

        Vector2 showPos = rect.anchoredPosition;

        // 初回だけ元の位置を保存したい場合は本当はDictionary推奨
        if (!defaultPositions.ContainsKey(rect))
        {
            defaultPositions[rect] = rect.anchoredPosition;
        }

        showPos = defaultPositions[rect];

        Vector2 hidePos = showPos;
        hidePos.x += uiHideOffsetX;

        Vector2 startPos = rect.anchoredPosition;
        Vector2 endPos = isShow ? showPos : hidePos;

        while (timer < uiAnimationDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.SmoothStep(0f, 1f, timer / uiAnimationDuration);
            rect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);

            yield return null;
        }

        rect.anchoredPosition = endPos;
    }
    IEnumerator WhiteOut()
    {
        float timer = 0f;
        while(timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float t= timer / fadeDuration;

            Color c = whiteFadeImage.color;
            c.a = Mathf.Lerp(0f, 1f, t);
            whiteFadeImage.color = c;

            yield return null;
        }
        Color end = whiteFadeImage.color;
        end.a = 1f;
        whiteFadeImage.color = end;
    }

    IEnumerator LoadStageWithWhiteOut()
    {
        StartCoroutine(WhiteOut());
        StartCoroutine(UIAnimation(stageSelectAnimationTargets, false));
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            player.transform.position += player.forward * runSpeed * Time.deltaTime;

            yield return null;
        }
        sceneLoader.LoadScene(stageSceneNames[currentIndex]);
    }
}
