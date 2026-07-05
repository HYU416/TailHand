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
    [SerializeField] private float winMoveUpOffset = 120f;
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

    void Awake()
    {
        ResolveChildReferences();
        LoadDefaultSprites();
        ApplyInitialSpriteSettings();
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
        isMashPromptVisible = false;
        isSelectionActive = false;
        hasLoadedScene = false;
        gameObject.SetActive(true);
        HideMashPrompt();
        HideSelectionButtons();

        if (winObject != null)
        {
            winObject.SetActive(true);
            winRectTransform = winObject.GetComponent<RectTransform>();

            if (winRectTransform != null)
            {
                winInitialAnchoredPosition = winRectTransform.anchoredPosition;
            }
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

        MoveWinUp();
        ShowSelectionButtons();
        currentChoice = WinMenuChoice.Next;
        UpdateSelectionVisuals();
        isSelectionActive = true;
    }

    void MoveWinUp()
    {
        if (winRectTransform == null)
        {
            return;
        }

        winRectTransform.anchoredPosition = winInitialAnchoredPosition + new Vector2(0f, winMoveUpOffset);
    }

    void ShowSelectionButtons()
    {
        if (nextGameImage != null)
        {
            nextGameImage.gameObject.SetActive(true);
        }

        if (quitGameImage != null)
        {
            quitGameImage.gameObject.SetActive(true);
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
        if (moveAction != null)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();

            if (move.y >= selectionStickDeadZone)
            {
                SetChoice(WinMenuChoice.Next);
            }
            else if (move.y <= -selectionStickDeadZone)
            {
                SetChoice(WinMenuChoice.Quit);
            }
        }

        if (catchAction != null && catchAction.WasPressedThisFrame())
        {
            ConfirmSelection();
        }
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
        SetSprite(nextGameImage, currentChoice == WinMenuChoice.Next ? nextSelectedSprite : nextUnselectedSprite);
        SetSprite(quitGameImage, currentChoice == WinMenuChoice.Quit ? quitSelectedSprite : quitUnselectedSprite);
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
            else if (nextGameImage == null && name.Contains("next"))
            {
                nextGameImage = child.GetComponent<Image>();
            }
            else if (quitGameImage == null && name.Contains("quit"))
            {
                quitGameImage = child.GetComponent<Image>();
            }
        }
    }

    void ApplyInitialSpriteSettings()
    {
        ConfigurePromptImage(keyImage);
        ConfigurePromptImage(buttonImage);
        ConfigurePromptImage(nextGameImage);
        ConfigurePromptImage(quitGameImage);
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
        nextSelectedSprite ??= LoadSprite("NEXTGAME_選択時");
        nextUnselectedSprite ??= LoadSprite("NEXTGAME_非選択時");
        quitSelectedSprite ??= LoadSprite("QUITGAME_選択時");
        quitUnselectedSprite ??= LoadSprite("QUITGAME_非選択時");
    }

    static Sprite LoadSprite(string assetName)
    {
        return Resources.Load<Sprite>(ResourceRoot + assetName);
    }
}
