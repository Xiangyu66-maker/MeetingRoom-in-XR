using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-100)]
[AddComponentMenu("Meeting Room/Pause Menu Controller")]
public sealed class PauseMenuController : MonoBehaviour
{
    private const string MenuSceneName = "Menu";
    private const string GameSceneName = "ConferenceRoom_before_blockout_sync";

    private GameObject pauseCanvas;
    private GameObject pausePanel;
    private bool isPaused;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!IsGameplayScene(scene.name))
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        PauseMenuController controller = FindFirstObjectByType<PauseMenuController>();
#else
        PauseMenuController controller = FindObjectOfType<PauseMenuController>();
#endif
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("Pause Menu Controller");
            controller = controllerObject.AddComponent<PauseMenuController>();
        }

        controller.EnsurePauseUI();
    }

    private void Awake()
    {
        if (!IsGameplayScene())
        {
            enabled = false;
            return;
        }

        // 每次进入正式游戏场景都从正常速度开始。
        Time.timeScale = 1f;
        EnsurePauseUI();
        SetPausePanelVisible(false);
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
        {
            return;
        }

        // 输入密码时，Esc 先交给键盘控制器取消输入，避免同时弹出暂停菜单。
        if (!isPaused && KeypadController.HasActiveInput)
        {
            return;
        }

        if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (isPaused)
        {
            return;
        }

        EnsurePauseUI();
        previousCursorLockMode = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        isPaused = true;
        SetPausePanelVisible(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Game paused.", this);
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            Time.timeScale = 1f;
            SetPausePanelVisible(false);
            return;
        }

        isPaused = false;
        SetPausePanelVisible(false);
        Time.timeScale = 1f;
        Cursor.lockState = previousCursorLockMode;
        Cursor.visible = previousCursorVisible;
        Debug.Log("Game resumed.", this);
    }

    public void BackToMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("Back to Menu.", this);
        SceneManager.LoadScene(MenuSceneName);
    }

    public void QuitGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

#if UNITY_EDITOR
        // 编辑器中只记录日志，打包后才真正退出程序。
        Debug.Log("Quit Game", this);
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // 防止暂停时切换或停止场景后，下一次运行仍保持 timeScale = 0。
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
    }

    private static bool IsGameplayScene()
    {
        return IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return string.Equals(sceneName, GameSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void EnsurePauseUI()
    {
        if (pauseCanvas != null && pausePanel != null)
        {
            return;
        }

        EnsureEventSystem();

        pauseCanvas = new GameObject("Pause Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        pauseCanvas.transform.SetParent(transform, false);

        Canvas canvas = pauseCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10000;

        CanvasScaler scaler = pauseCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image overlay = CreateImage("Pause Overlay", pauseCanvas.transform, new Color(0f, 0f, 0f, 0.72f));
        StretchToParent(overlay.rectTransform);
        pausePanel = overlay.gameObject;

        RectTransform content = CreateRect("Pause Content", overlay.transform);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = new Vector2(560f, 520f);

        TextMeshProUGUI title = CreateText("Pause Title", content, "Paused", 64f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 170f), new Vector2(520f, 90f));

        Button resumeButton = CreateButton("Resume Button", content, "Resume", new Vector2(0f, 58f));
        resumeButton.onClick.AddListener(ResumeGame);

        Button menuButton = CreateButton("Back to Menu Button", content, "Back to Menu", new Vector2(0f, -30f));
        menuButton.onClick.AddListener(BackToMenu);

        Button quitButton = CreateButton("Quit Game Button", content, "Quit Game", new Vector2(0f, -118f));
        quitButton.onClick.AddListener(QuitGame);
    }

    private void SetPausePanelVisible(bool visible)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(visible);
        }
    }

    private static void EnsureEventSystem()
    {
#if UNITY_2023_1_OR_NEWER
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
#else
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
#endif
        if (eventSystem == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private static Button CreateButton(string objectName, Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        SetRect(rect, position, new Vector2(360f, 68f));

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.55f, 0.37f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 1f, 0.93f, 1f);
        colors.pressedColor = new Color(0.72f, 0.88f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.65f);
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", rect, label, 29f, FontStyles.Bold);
        StretchToParent(text.rectTransform);
        return button;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, string value, float fontSize, FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateRect(string objectName, Transform parent)
    {
        GameObject rectObject = new GameObject(objectName, typeof(RectTransform));
        rectObject.transform.SetParent(parent, false);
        return rectObject.GetComponent<RectTransform>();
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
}
