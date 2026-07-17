using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Canvas 直下の UI マネージャ用。
/// 連打プロンプト、WIN 表示、クリア後の NEXT / QUIT 選択を管理します。
/// </summary>
public class LastAttackUIManager : MonoBehaviour
{
    enum WinMenuChoice
    {
        Next,
        Quit,
    }

    const string ResourceRoot = "Last_Attack/UI_Last_attack/";

    [Header("子 UI")]
    [SerializeField] private Image keyImage;
    [SerializeField] private Image buttonImage;
    [SerializeField] private GameObject winObject;
    [SerializeField] private Image nextGameImage;
    [SerializeField] private Image quitGameImage;

    [Header("入力")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField, Min(0.1f)] private float selectionStickDeadZone = 0.5f;

    [Header("Keyboard スプライト")]
    [SerializeField] private Sprite keyReleasedSprite;
    [SerializeField] private Sprite keyPressedSprite;

    [Header("Gamepad スプライト")]
    [SerializeField] private Sprite buttonReleasedSprite;
    [SerializeField] private Sprite buttonPressedSprite;

    [Header("NEXT / QUIT スプライト")]
    [SerializeField] private Sprite nextSelectedSprite;
    [SerializeField] private Sprite nextUnselectedSprite;
    [SerializeField] private Sprite quitSelectedSprite;
    [SerializeField] private Sprite quitUnselectedSprite;

    [Header("WIN 表示後")]
    [SerializeField, Min(0f)] private float delayBeforeShowSelection = 2f;
    [Tooltip("WIN 表示位置（UImanager 基準）")]
    [SerializeField] private Vector2 winDisplayPosition = new Vector2(0f, -60f);
    [Tooltip("WIN の表示倍率（均一スケール）")]
    [SerializeField, Min(0.1f)] private float winDisplayScale = 1.45f;
    [Tooltip("選択 UI 表示前に WIN を上へ移動する量")]
    [SerializeField] private float winMoveUpOffset = 50f;
    [Tooltip("WIN が上へスライドする秒数")]
    [SerializeField, Min(0.01f)] private float winMoveDuration = 0.45f;
    [Tooltip("NEXT / QUIT の表示幅（縦横比は維持）")]
    [SerializeField, Min(1f)] private float menuButtonTargetWidth = 350f;
    [SerializeField] private Vector2 nextMenuPosition = new Vector2(0f, -44f);
    [SerializeField] private Vector2 quitMenuPosition = new Vector2(0f, -164f);
    [SerializeField] private string nextSceneName = "BossStage_Big_G";
    [SerializeField] private string quitSceneName = "TitleScene";

    InputAction catchAction;
    InputAction moveAction;
    RectTransform winRectTransform;
    Vector2 winInitialAnchoredPosition;
    Coroutine winSequenceRoutine;

    bool isMashPromptVisible;
    bool isSelectionActive;
    bool hasLoadedScene;
    bool useGamepadPrompt;
    WinMenuChoice currentChoice = WinMenuChoice.Next;
    float lastSelectionMoveY;

    void Awake()
    {
        ResolveChildReferences();
        LoadDefaultSprites();
        ApplyInitialSpriteSettings();
        PrepareMenuImageLayouts();
        HideSelectionButtons();
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        BindInputActions();

        if (playerInput != null)
        {
            playerInput.onControlsChanged += OnControlsChanged;
        }
    }

    void OnDisable()
    {
        if (playerInput != null)
        {
            playerInput.onControlsChanged -= OnControlsChanged;
        }

        catchAction = null;
        moveAction = null;
    }

    void Update()
    {
        if (isMashPromptVisible)
        {
            RefreshInputDeviceMode();
            ApplyPromptVisibility();

            bool isPressed = catchAction != null && catchAction.IsPressed();
            UpdateActivePromptSprite(isPressed);
            return;
        }

        if (!isSelectionActive || hasLoadedScene)
        {
            return;
        }

        UpdateSelectionInput();
    }

    /// <summary>QTE 連打中に KEY / bottun プロンプトを表示します。</summary>
    public void ShowMashPrompt()
    {
        StopWinSequence();

        ResolvePlayerInput();
        BindInputActions();
        RefreshInputDeviceMode();

        isMashPromptVisible = true;
        isSelectionActive = false;
        hasLoadedScene = false;
        gameObject.SetActive(true);

        if (winObject != null)
        {
            winObject.SetActive(false);
        }

        HideSelectionButtons();
        ApplyPromptVisibility();
        UpdateActivePromptSprite(false);
    }

    /// <summary>KEY / bottun だけ非表示にします（投げモーション開始時）。</summary>
    public void HideMashPrompt()
    {
        isMashPromptVisible = false;

        if (keyImage != null)
        {
            keyImage.gameObject.SetActive(false);
        }

        if (buttonImage != null)
        {
            buttonImage.gameObject.SetActive(false);
        }
    }

    /// <summary>ボスの頭が爆発したとき WIN を表示し、選択 UI へ進みます。</summary>
    public void ShowWin()
    {
        ResolveChildReferences();
        ReloadMenuSprites();

        isMashPromptVisible = false;
        isSelectionActive = false;
        hasLoadedScene = false;
        gameObject.SetActive(true);
        HideMashPrompt();
        HideSelectionButtons();

        ResolvePlayerInput();
        BindInputActions();
        lastSelectionMoveY = 0f;

        if (winObject != null)
        {
            winObject.SetActive(true);
            winRectTransform = winObject.GetComponent<RectTransform>();
            ConfigureWinDisplay();
        }

        StopWinSequence();
        winSequenceRoutine = StartCoroutine(WinSequenceRoutine());
    }

    /// <summary>UI 全体を非表示にします。</summary>
    public void HideAll()
    {
        StopWinSequence();
        isMashPromptVisible = false;
        isSelectionActive = false;
        HideMashPrompt();
        HideSelectionButtons();

        if (winObject != null)
        {
            winObject.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    IEnumerator WinSequenceRoutine()
    {
        if (delayBeforeShowSelection > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeShowSelection);
        }

        yield return MoveWinUpRoutine();
        ShowSelectionButtons();
        currentChoice = WinMenuChoice.Next;
        UpdateSelectionVisuals();
        isSelectionActive = true;
    }

    IEnumerator MoveWinUpRoutine()
    {
        if (winRectTransform == null || Mathf.Approximately(winMoveUpOffset, 0f))
        {
            yield break;
        }

        Vector2 from = winInitialAnchoredPosition;
        Vector2 to = winInitialAnchoredPosition + new Vector2(0f, winMoveUpOffset);
        float duration = Mathf.Max(0.01f, winMoveDuration);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);
            float eased = t * t * (3f - 2f * t);
            winRectTransform.anchoredPosition = Vector2.Lerp(from, to, eased);
            yield return null;
        }

        winRectTransform.anchoredPosition = to;
    }

    void ConfigureWinDisplay()
    {
        if (winRectTransform == null)
        {
            return;
        }

        Image winImage = winObject != null ? winObject.GetComponent<Image>() : null;

        if (winImage != null)
        {
            winImage.preserveAspect = true;
            winImage.raycastTarget = false;
        }

        winRectTransform.localScale = Vector3.one;
        winRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        winRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        winRectTransform.pivot = new Vector2(0.5f, 0.5f);
        winRectTransform.anchoredPosition = winDisplayPosition;

        if (winImage != null && winImage.sprite != null)
        {
            winImage.SetNativeSize();
        }

        winRectTransform.localScale = Vector3.one * winDisplayScale;
        winInitialAnchoredPosition = winDisplayPosition;
    }

    void ShowSelectionButtons()
    {
        if (nextGameImage != null)
        {
            nextGameImage.gameObject.SetActive(true);
            ConfigureMenuImageLayout(nextGameImage, nextMenuPosition);
        }

        if (quitGameImage != null)
        {
            quitGameImage.gameObject.SetActive(true);
            ConfigureMenuImageLayout(quitGameImage, quitMenuPosition);
        }
    }

    void HideSelectionButtons()
    {
        if (nextGameImage != null)
        {
            nextGameImage.gameObject.SetActive(false);
        }

        if (quitGameImage != null)
        {
            quitGameImage.gameObject.SetActive(false);
        }
    }

    void UpdateSelectionInput()
    {
        if (!TryMoveSelectionFromKeyboard())
        {
            TryMoveSelectionFromGamepad();
        }

        if (catchAction != null && catchAction.WasPressedThisFrame())
        {
            ConfirmSelection();
        }
    }

    bool TryMoveSelectionFromKeyboard()
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            SetChoice(WinMenuChoice.Next);
            MySoundManeger.Play(gameObject, SEList.SE_SELECT);
            return true;
        }

        if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            SetChoice(WinMenuChoice.Quit);
            MySoundManeger.Play(gameObject, SEList.SE_SELECT);
            return true;
        }

        return false;
    }

    bool TryMoveSelectionFromGamepad()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.up.wasPressedThisFrame
                || Gamepad.current.leftStick.up.wasPressedThisFrame)
            {
                SetChoice(WinMenuChoice.Next);
                return true;
            }

            if (Gamepad.current.dpad.down.wasPressedThisFrame
                || Gamepad.current.leftStick.down.wasPressedThisFrame)
            {
                SetChoice(WinMenuChoice.Quit);
                return true;
            }
        }

        if (moveAction == null)
        {
            return false;
        }

        float moveY = moveAction.ReadValue<Vector2>().y;
        bool moved = false;

        if (moveY >= selectionStickDeadZone && lastSelectionMoveY < selectionStickDeadZone)
        {
            SetChoice(WinMenuChoice.Next);
            moved = true;
        }
        else if (moveY <= -selectionStickDeadZone && lastSelectionMoveY > -selectionStickDeadZone)
        {
            SetChoice(WinMenuChoice.Quit);
            moved = true;
        }

        lastSelectionMoveY = moveY;
        return moved;
    }

    void SetChoice(WinMenuChoice choice)
    {
        if (currentChoice == choice)
        {
            return;
        }

        currentChoice = choice;
        UpdateSelectionVisuals();
    }

    void UpdateSelectionVisuals()
    {
        UpdateMenuButtonSprite(
            nextGameImage,
            nextMenuPosition,
            currentChoice == WinMenuChoice.Next ? nextSelectedSprite : nextUnselectedSprite
        );
        UpdateMenuButtonSprite(
            quitGameImage,
            quitMenuPosition,
            currentChoice == WinMenuChoice.Quit ? quitSelectedSprite : quitUnselectedSprite
        );
    }

    void UpdateMenuButtonSprite(Image image, Vector2 anchoredPosition, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
        ConfigureMenuImageLayout(image, anchoredPosition);
    }

    void ConfigureMenuImageLayout(Image image, Vector2 anchoredPosition)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;

        if (image.sprite == null)
        {
            return;
        }

        image.SetNativeSize();

        if (rect.sizeDelta.x <= 0.01f || menuButtonTargetWidth <= 0f)
        {
            return;
        }

        float uniformScale = menuButtonTargetWidth / rect.sizeDelta.x;
        rect.localScale = Vector3.one * uniformScale;
    }

    void ConfirmSelection()
    {
        if (hasLoadedScene)
        {
            return;
        }

        hasLoadedScene = true;
        isSelectionActive = false;

        string sceneName = currentChoice == WinMenuChoice.Next
            ? nextSceneName
            : quitSceneName;

        SceneLoader.Load(sceneName);
    }

    void StopWinSequence()
    {
        if (winSequenceRoutine == null)
        {
            return;
        }

        StopCoroutine(winSequenceRoutine);
        winSequenceRoutine = null;
    }

    void OnControlsChanged(PlayerInput input)
    {
        RefreshInputDeviceMode();
        ApplyPromptVisibility();
    }

    void ResolveChildReferences()
    {
        foreach (Transform child in transform)
        {
            string name = child.name.ToLowerInvariant();

            if (keyImage == null && name == "key")
            {
                keyImage = child.GetComponent<Image>();
            }
            else if (buttonImage == null && (name == "bottun" || name == "button"))
            {
                buttonImage = child.GetComponent<Image>();
            }
            else if (winObject == null && name.Contains("win"))
            {
                winObject = child.gameObject;
            }
        }

        ResolveMenuImages();
    }

    void ResolveMenuImages()
    {
        Transform nextTransform = transform.Find("NEXTGAME");
        Transform quitTransform = transform.Find("QUITGAME");

        if (nextTransform != null)
        {
            nextGameImage = nextTransform.GetComponent<Image>();
        }

        if (quitTransform != null)
        {
            quitGameImage = quitTransform.GetComponent<Image>();
        }
    }

    void ApplyInitialSpriteSettings()
    {
        ConfigurePromptImage(keyImage);
        ConfigurePromptImage(buttonImage);
        ConfigurePromptImage(nextGameImage);
        ConfigurePromptImage(quitGameImage);
    }

    void PrepareMenuImageLayouts()
    {
        if (nextGameImage != null)
        {
            ConfigureMenuImageLayout(nextGameImage, nextMenuPosition);
        }

        if (quitGameImage != null)
        {
            ConfigureMenuImageLayout(quitGameImage, quitMenuPosition);
        }
    }

    static void ConfigurePromptImage(Image image)
    {
        if (image == null)
        {
            return;
        }

        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    void ResolvePlayerInput()
    {
        if (playerInput != null)
        {
            return;
        }

        playerInput = FindObjectOfType<PlayerInput>();
    }

    void BindInputActions()
    {
        if (playerInput == null || playerInput.actions == null)
        {
            catchAction = null;
            moveAction = null;
            return;
        }

        catchAction = playerInput.actions.FindAction("Catch", false);
        moveAction = playerInput.actions.FindAction("Move", false);

        if (catchAction != null && !catchAction.enabled)
        {
            catchAction.Enable();
        }

        if (moveAction != null && !moveAction.enabled)
        {
            moveAction.Enable();
        }
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

    void ApplyPromptVisibility()
    {
        if (keyImage != null)
        {
            keyImage.gameObject.SetActive(!useGamepadPrompt);
        }

        if (buttonImage != null)
        {
            buttonImage.gameObject.SetActive(useGamepadPrompt);
        }
    }

    void UpdateActivePromptSprite(bool isPressed)
    {
        if (useGamepadPrompt)
        {
            SetSprite(buttonImage, isPressed ? buttonPressedSprite : buttonReleasedSprite);
            return;
        }

        SetSprite(keyImage, isPressed ? keyPressedSprite : keyReleasedSprite);
    }

    static void SetSprite(Image image, Sprite sprite)
    {
        if (image == null || sprite == null)
        {
            return;
        }

        image.sprite = sprite;
    }

    void LoadDefaultSprites()
    {
        keyReleasedSprite ??= LoadSprite("Last_attack_key_1");
        keyPressedSprite ??= LoadSprite("Last_attack_key_2");
        buttonReleasedSprite ??= LoadSprite("Last_attack_bottun_1");
        buttonPressedSprite ??= LoadSprite("Last_attack_bottun_2");
        ReloadMenuSprites();
    }

    void ReloadMenuSprites()
    {
        nextSelectedSprite = LoadSprite("NEXTGAME_選択時");
        nextUnselectedSprite = LoadSprite("NEXTGAME_非選択時");
        quitSelectedSprite = LoadSprite("QUITGAME_選択時");
        quitUnselectedSprite = LoadSprite("QUITGAME_非選択時");
    }

    static Sprite LoadSprite(string assetName)
    {
        return Resources.Load<Sprite>(ResourceRoot + assetName);
    }
}
