using UnityEngine;

using TMPro;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Keypad Controller")]
public sealed class KeypadController : MonoBehaviour
{
    [SerializeField] private string correctPassword = "3142";
    [SerializeField] private DoorController doorController;
    [SerializeField] private bool showDebugInputOverlay = true;

    private string currentInput = string.Empty;
    private bool inputModeActive;
    private GameObject questKeypadCanvas;
    private TextMeshProUGUI questDisplayText;
    private static KeypadController activeKeypad;

    public static bool HasActiveInput => activeKeypad != null && activeKeypad.inputModeActive;

    private void Awake()
    {
        ResolveDoorController();
    }

    private void Update()
    {
        if (!inputModeActive)
        {
            SetQuestKeypadVisible(false);
            return;
        }

        if (QuestXRUIRuntime.IsXRPresentationActive)
        {
            EnsureQuestKeypadUI();
            SetQuestKeypadVisible(true);
        }

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + digit);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + digit);
            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                AppendDigit(digit);
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateQuestDisplay();
            Debug.Log($"Keypad input: {DisplayInput()}", this);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitInput();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInputMode();
        }
    }

    public void BeginInputMode()
    {
        if (inputModeActive)
        {
            return;
        }

        activeKeypad = this;
        inputModeActive = true;
        currentInput = string.Empty;
        EnsureQuestKeypadUI();
        SetQuestKeypadVisible(true);
        UpdateQuestDisplay();
        QuestXRUIRuntime.SetMessage(
            QuestXRUIRuntime.Channel.Keypad,
            "Point with the right controller, then press A to select.");
        Debug.Log("Keypad input mode started. Type 0-9, Backspace to delete, Enter to submit, Escape to cancel.", this);
        MeetingRoomAdaptiveGuide.NotifyKeypadInputStarted();
    }

    public void ConfigureDoor(DoorController door)
    {
        if (doorController == null)
        {
            doorController = door;
        }
    }

    private void AppendDigit(int digit)
    {
        if (currentInput.Length >= correctPassword.Length)
        {
            return;
        }

        currentInput += digit.ToString();
        UpdateQuestDisplay();
        Debug.Log($"Keypad input: {DisplayInput()}", this);
    }

    private void SubmitInput()
    {
        if (currentInput == correctPassword)
        {
            Debug.Log("Correct password. Door unlocked.", this);
            MeetingRoomAdaptiveGuide.NotifyPasswordAccepted();
            ResolveDoorController();
            if (doorController != null)
            {
                doorController.UnlockDoor();
            }
            else
            {
                Debug.LogWarning("Correct password entered, but no DoorController was assigned.", this);
            }

            inputModeActive = false;
            activeKeypad = null;
            SetQuestKeypadVisible(false);
            QuestXRUIRuntime.HideMessage(QuestXRUIRuntime.Channel.Keypad);
            return;
        }

        Debug.Log("Wrong password. Try again.", this);
        MeetingRoomAdaptiveGuide.NotifyPasswordRejected();
        currentInput = string.Empty;
        UpdateQuestDisplay();
    }

    private void CancelInputMode()
    {
        inputModeActive = false;
        activeKeypad = null;
        currentInput = string.Empty;
        SetQuestKeypadVisible(false);
        QuestXRUIRuntime.HideMessage(QuestXRUIRuntime.Channel.Keypad);
        Debug.Log("Keypad input mode cancelled.", this);
    }

    private void ResolveDoorController()
    {
        if (doorController != null)
        {
            return;
        }

        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
        foreach (ObjectIdentity identity in identities)
        {
            if (identity != null && identity.ObjectId == "locked_door_01")
            {
                doorController = identity.GetComponent<DoorController>();
                return;
            }
        }
    }

    private string DisplayInput()
    {
        if (currentInput.Length == 0)
        {
            return "_ _ _ _";
        }

        char[] display = { '_', '_', '_', '_' };
        for (int i = 0; i < currentInput.Length && i < display.Length; i++)
        {
            display[i] = currentInput[i];
        }

        return $"{display[0]} {display[1]} {display[2]} {display[3]}";
    }

    private void EnsureQuestKeypadUI()
    {
        if (!QuestXRUIRuntime.IsXRPresentationActive || questKeypadCanvas != null)
        {
            return;
        }

        questKeypadCanvas = new GameObject(
            "Quest Keypad Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        questKeypadCanvas.transform.SetParent(transform, false);

        Canvas canvas = questKeypadCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 18000;

        CanvasScaler scaler = questKeypadCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image backdrop = CreateImage(
            "Keypad Backdrop",
            questKeypadCanvas.transform,
            new Color(0f, 0f, 0f, 0.58f));
        StretchToParent(backdrop.rectTransform);

        Image panel = CreateImage(
            "Keypad Panel",
            backdrop.transform,
            new Color(0.035f, 0.055f, 0.075f, 0.98f));
        SetRect(panel.rectTransform, Vector2.zero, new Vector2(660f, 880f));

        TextMeshProUGUI title = CreateText(
            "Keypad Title",
            panel.transform,
            "DOOR KEYPAD",
            46f,
            FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 370f), new Vector2(580f, 70f));

        questDisplayText = CreateText(
            "Keypad Display",
            panel.transform,
            DisplayInput(),
            58f,
            FontStyles.Bold);
        SetRect(questDisplayText.rectTransform, new Vector2(0f, 285f), new Vector2(560f, 88f));

        for (int digit = 1; digit <= 9; digit++)
        {
            int capturedDigit = digit;
            int row = (digit - 1) / 3;
            int column = (digit - 1) % 3;
            Vector2 position = new Vector2((column - 1) * 160f, 175f - row * 115f);
            Button button = CreateButton(
                $"Digit {digit}",
                panel.transform,
                digit.ToString(),
                position,
                new Vector2(132f, 88f));
            button.onClick.AddListener(() => AppendDigit(capturedDigit));
        }

        Button zeroButton = CreateButton(
            "Digit 0",
            panel.transform,
            "0",
            new Vector2(0f, -170f),
            new Vector2(132f, 88f));
        zeroButton.onClick.AddListener(() => AppendDigit(0));

        Button backButton = CreateButton(
            "Backspace",
            panel.transform,
            "BACK",
            new Vector2(-160f, -285f),
            new Vector2(180f, 76f));
        backButton.onClick.AddListener(DeleteLastDigit);

        Button submitButton = CreateButton(
            "Submit",
            panel.transform,
            "OK",
            new Vector2(50f, -285f),
            new Vector2(180f, 76f));
        submitButton.onClick.AddListener(SubmitInput);

        Button cancelButton = CreateButton(
            "Cancel",
            panel.transform,
            "CANCEL",
            new Vector2(0f, -380f),
            new Vector2(260f, 68f));
        cancelButton.onClick.AddListener(CancelInputMode);

        QuestXRUIRuntime.ConfigureCanvasForXR(canvas);
    }

    private void DeleteLastDigit()
    {
        if (currentInput.Length == 0)
        {
            return;
        }

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdateQuestDisplay();
        Debug.Log($"Keypad input: {DisplayInput()}", this);
    }

    private void UpdateQuestDisplay()
    {
        if (questDisplayText != null)
        {
            questDisplayText.text = DisplayInput();
        }
    }

    private void SetQuestKeypadVisible(bool visible)
    {
        if (questKeypadCanvas != null && questKeypadCanvas.activeSelf != visible)
        {
            questKeypadCanvas.SetActive(visible);
        }
    }

    private static Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size)
    {
        GameObject buttonObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetRect(rect, position, size);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.55f, 0.74f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.82f, 0.96f, 1f, 1f);
        colors.pressedColor = new Color(0.58f, 0.82f, 0.95f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        TextMeshProUGUI text = CreateText(
            "Label",
            rect,
            label,
            label.Length > 2 ? 24f : 38f,
            FontStyles.Bold);
        StretchToParent(text.rectTransform);
        return button;
    }

    private static TextMeshProUGUI CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        FontStyles style)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDisable()
    {
        if (activeKeypad == this)
        {
            activeKeypad = null;
        }

        inputModeActive = false;
        SetQuestKeypadVisible(false);
        QuestXRUIRuntime.HideMessage(QuestXRUIRuntime.Channel.Keypad);
    }

    private void OnGUI()
    {
        if (QuestXRUIRuntime.IsXRPresentationActive)
        {
            return;
        }

        if (!showDebugInputOverlay || !inputModeActive)
        {
            return;
        }

        // TODO: Replace this debug overlay with a proper UI Text/TextMeshProUGUI keypad display.
        GUI.Box(new Rect((Screen.width - 320f) * 0.5f, 72f, 320f, 86f), "Keypad");
        GUI.Label(new Rect((Screen.width - 260f) * 0.5f, 108f, 260f, 24f), DisplayInput());
        GUI.Label(new Rect((Screen.width - 260f) * 0.5f, 132f, 260f, 24f), "Enter submits, Backspace deletes, Esc cancels");
    }
}
