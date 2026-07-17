using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
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
    [SerializeField] private Image[] navigate;
    [SerializeField] private Sprite[] navigateASprite;
    [SerializeField] private Sprite[] navigateBSprite;
    [SerializeField] private PlayerInput playerInput;


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
    private Dictionary<RectTransform, Vector2> buttonDefaultPositions =
    new Dictionary<RectTransform, Vector2>();
    private bool isStageDecide = false;
    private int previousTitleIndex = 0;
    private int previousStageIndex = 0;

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

    private bool useGamepadPrompt = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //NavigationPanel.SetActive(false);
       // stageSelectPanel.transform.position = new Vector3(uiAnimationEndPos, stageSelectPanel.transform.position.y, transform.position.z);
        animator.Play("Take 001");
        // カメラのポジション
        Vector3 center = player.position + Vector3.up * 1.5f;
        titleStartOffset = titleCamera.transform.position - center;
        // UIのデフォルトポジション
        defoultShowPos = TitlePanel.transform.position.x;
        defaultTitleNamePos = titleAnimationTargets[0].transform.position.x;

        Canvas.ForceUpdateCanvases();
        GridLayoutGroup grid = stageSelectPanel.GetComponent<GridLayoutGroup>();
        if (grid != null) grid.enabled = false;
        foreach (Image panel in stagePanels)
        {
            RectTransform rect = panel.rectTransform;
            buttonDefaultPositions[rect] = rect.anchoredPosition;
        }
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
        foreach(Image panel in titlePanels)
        {
            RectTransform rect = panel.rectTransform;
            buttonDefaultPositions[rect] = rect.anchoredPosition;
        }

        // MoveDecideButtonSelection(defaultPositions, currentIndex);

        useGamepadPrompt = true;  

        for (int i = 0; i < navigate.Length; i++)
        {
            navigate[i].sprite = useGamepadPrompt
                ? navigateBSprite[i]
                : navigateASprite[i];
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
        //IsUsingGamepad(playerInput);
        CheckLastUsedDevice();

        for(int i = 0;i < navigate.Length;i++)
        {
            navigate[i].sprite = useGamepadPrompt ? navigateASprite[i] : navigateBSprite[i];
        }

        // ステージの回転
        stage.transform.Rotate(0,stageRotateSpeed * Time.deltaTime, 0);
        //item.transform.Rotate(0,itemRotateSpeed * Time.deltaTime, 0);

        if (isCameraMoving) return;

        if (Time.time > nextMoveTime && !isStageDecide)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            // 現在のパネル数を取得
            int panelCount = currentState == MenuState.Title? titlePanels.Length: stagePanels.Length;
            int minIndex = 0;
            int maxIndex = panelCount - 1;

            if (Input.GetKeyDown(KeyCode.RightArrow) || h > 0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex + 1, minIndex, maxIndex);
                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow) || h < -0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex - 1, minIndex, maxIndex);

                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }
            if (Input.GetKeyDown(KeyCode.UpArrow) || v > 0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex - columnCount, minIndex, maxIndex);

                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || v < -0.5f)
            {
                int nextIndex = Mathf.Clamp(currentIndex + columnCount, minIndex, maxIndex);
                if (nextIndex != currentIndex)
                {
                    currentIndex = nextIndex;
                    nextMoveTime = Time.time + moveCooldown;
                    MySoundManeger.Play(gameObject, SEList.SE_SELECT);
                }
            }
        }

        // ステージ一覧が表示されたら
        if (currentState == MenuState.Title)
        {
            currentIndex = Mathf.Clamp(currentIndex, 0, titlePanels.Length - 1);
            MoveDecideButtonSelection(titlePanels, ref previousTitleIndex, currentIndex);
            for (int i = 0; i < titlePanels.Length; i++)
            {
                bool isSelected = i == currentIndex;
                titlePanels[i].sprite = isSelected ? titleFrontSprite[i] : titleNormalSprite[i];
            }
            // StartButtonでStage一覧表示
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit"))
            {
                MySoundManeger.Play(gameObject, SEList.SE_ENTER);
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
            currentIndex = Mathf.Clamp(currentIndex, 0, stagePanels.Length - 1);
            if (!isStageDecide)
            {
                MoveDecideButtonSelection(stagePanels, ref previousStageIndex, currentIndex);
            }
            if ((Input.GetKeyDown(KeyCode.Return) || Input.GetButtonDown("Submit")) && !isStageDecide)
            {
                MySoundManeger.Play(gameObject, SEList.SE_ENTER);
                // 最後尾のみExitの処理
                if (currentIndex == stagePanels.Count() - 1 && !isCameraMoving)
                {
                    StartCoroutine(BackToTitle());
                }
                else
                {
                    isStageDecide = true;
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
    
    private IEnumerator BackToTitle()
    {
        isStageDecide = true;

        StartCoroutine(UIAnimation(stageSelectAnimationTargets,false));

        StartCoroutine(UIAnimation(titleAnimationTargets, true));

        yield return StartCoroutine(CameraTurn(false));

        currentIndex = 0;
        previousStageIndex = -1;
        currentState = MenuState.Title;

        isStageDecide = false;
    }
    private void MoveDecideButtonSelection(Image[] panels,ref int index,int currentIndex)
    {
        if (panels == null || panels.Length == 0) return;
        if(currentIndex < 0 || currentIndex >= panels.Length) return;

        // 前の選択を戻す
        if(index >= 0 && index < panels.Length) 
        {
            RectTransform prev = panels[index].rectTransform;
            if(buttonDefaultPositions.TryGetValue(prev,out Vector2 previousDefaultPos))
            {
                prev.anchoredPosition = previousDefaultPos;
            }
        }
        // 今の選択を動かす
        RectTransform current = panels[currentIndex].rectTransform;
        if (buttonDefaultPositions.TryGetValue(current, out Vector2 currentDefaultPos))
        {
            current.anchoredPosition = currentDefaultPos + Vector2.right * -50.0f;
        }

        index = currentIndex;
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

    void RefreshInputDeviceMode()
    {
        useGamepadPrompt = IsUsingGamepad(playerInput);
    }
    static bool IsUsingGamepad(PlayerInput input)
    {
        if (input == null)
        {
            return Gamepad.current != null && Keyboard.current == null;
        }

        string scheme = input.currentControlScheme;

        if (!string.IsNullOrEmpty(scheme))
        {
            if (scheme.Contains("Gamepad"))
            {
                return true;
            }

            if (scheme.Contains("Keyboard") || scheme.Contains("Mouse"))
            {
                return false;
            }
        }

        foreach (InputDevice device in input.devices)
        {
            if (device is Gamepad)
            {
                return true;
            }
        }

        return Gamepad.current != null && Keyboard.current == null;
    }

    void CheckLastUsedDevice()
    {
        // キーボードが押されたらKeyboard表示
        if (Keyboard.current != null &&
            Keyboard.current.anyKey.wasPressedThisFrame)
        {
            useGamepadPrompt = false;
        }

        if (Gamepad.current == null)
        {
            return;
        }

        Gamepad gamepad = Gamepad.current;

        // ゲームパッドのボタンが押されたらGamePad表示
        if (gamepad.buttonSouth.wasPressedThisFrame ||
            gamepad.buttonNorth.wasPressedThisFrame ||
            gamepad.buttonEast.wasPressedThisFrame ||
            gamepad.buttonWest.wasPressedThisFrame ||
            gamepad.startButton.wasPressedThisFrame ||
            gamepad.selectButton.wasPressedThisFrame ||
            gamepad.leftShoulder.wasPressedThisFrame ||
            gamepad.rightShoulder.wasPressedThisFrame ||
            gamepad.dpad.up.wasPressedThisFrame ||
            gamepad.dpad.down.wasPressedThisFrame ||
            gamepad.dpad.left.wasPressedThisFrame ||
            gamepad.dpad.right.wasPressedThisFrame)
        {
            useGamepadPrompt = true;
            return;
        }

        // スティックを動かしたらGamePad表示
        if (gamepad.leftStick.ReadValue().sqrMagnitude > 0.25f ||
            gamepad.rightStick.ReadValue().sqrMagnitude > 0.25f ||
            gamepad.leftTrigger.ReadValue() > 0.2f ||
            gamepad.rightTrigger.ReadValue() > 0.2f)
        {
            useGamepadPrompt = true;
        }
    }
}

