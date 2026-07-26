using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Meeting Room/Main Menu Controller")]
public sealed class MainMenuController : MonoBehaviour
{
    private const string MenuSceneName = "Menu";
    private const string GameSceneName = "ConferenceRoom_before_blockout_sync";

    private GameObject menuCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        // 支持关闭 Domain Reload 的编辑器设置，先移除再注册以避免重复回调。
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!string.Equals(scene.name, MenuSceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // 从暂停菜单返回时，保证主菜单不会继承暂停状态。
        Time.timeScale = 1f;

        ResetMenuState();

#if UNITY_2023_1_OR_NEWER
        MainMenuController controller = FindFirstObjectByType<MainMenuController>();
#else
        MainMenuController controller = FindObjectOfType<MainMenuController>();
#endif
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("Main Menu Controller");
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        controller.EnsureMenuUI();
    }

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name == MenuSceneName)
        {
            ResetMenuState();
            EnsureMenuUI();
        }
    }

    private IEnumerator Start()
    {
        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            yield break;
        }

        // The gameplay EventSystem can disappear after sceneLoaded callbacks finish.
        yield return null;
        ResetMenuState();
        EnsureMenuUI();
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        Debug.Log($"Start Game: loading {GameSceneName} scene.", this);
        SceneManager.LoadScene(GameSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        // 编辑器中只记录日志，避免停止或退出 Unity Editor。
        Debug.Log("Quit Game", this);
#else
        Application.Quit();
#endif
    }

    private void EnsureMenuUI()
    {
        // Repair input even when the menu Canvas already exists.
        EnsureEventSystem();

        if (menuCanvas != null)
        {
            return;
        }

        Transform existing = transform.Find("Main Menu Canvas");
        if (existing != null)
        {
            menuCanvas = existing.gameObject;
            return;
        }

        menuCanvas = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        menuCanvas.transform.SetParent(transform, false);

        Canvas canvas = menuCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = menuCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        Image background = CreateImage("Menu Background", menuCanvas.transform, new Color(0.035f, 0.045f, 0.05f, 1f));
        StretchToParent(background.rectTransform);

        RectTransform content = CreateRect("Menu Content", background.transform);
        content.anchorMin = new Vector2(0.5f, 0.5f);
        content.anchorMax = new Vector2(0.5f, 0.5f);
        content.pivot = new Vector2(0.5f, 0.5f);
        content.anchoredPosition = new Vector2(0f, 20f);
        content.sizeDelta = new Vector2(760f, 500f);

        TextMeshProUGUI title = CreateText("Game Title", content, "Meeting Room Escape", 72f, FontStyles.Bold);
        SetRect(title.rectTransform, new Vector2(0f, 126f), new Vector2(720f, 120f));

        Button startButton = CreateButton("Start Game Button", content, "Start Game", new Vector2(0f, 12f));
        startButton.onClick.AddListener(StartGame);

        Button quitButton = CreateButton("Quit Game Button", content, "Quit Game", new Vector2(0f, -76f));
        quitButton.onClick.AddListener(QuitGame);
    }

    private static void ResetMenuState()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Input.ResetInputAxes();
    }

    private static void EnsureEventSystem()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        EventSystem eventSystem = null;

#if UNITY_2023_1_OR_NEWER
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
#else
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
#endif

        foreach (EventSystem candidate in eventSystems)
        {
            if (candidate != null && candidate.gameObject.scene == activeScene)
            {
                eventSystem = candidate;
                break;
            }
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
        else
        {
            eventSystem.enabled = true;
            StandaloneInputModule inputModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (inputModule == null)
            {
                inputModule = eventSystem.gameObject.AddComponent<StandaloneInputModule>();
            }

            inputModule.enabled = true;
        }

        EventSystem.current = eventSystem;
        eventSystem.SetSelectedGameObject(null);
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

        TextMeshProUGUI text = CreateText("Label", rect, label, 30f, FontStyles.Bold);
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
