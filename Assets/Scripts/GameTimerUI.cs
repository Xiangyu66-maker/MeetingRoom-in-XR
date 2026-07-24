using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Game Timer UI")]
public sealed class GameTimerUI : MonoBehaviour
{
    [SerializeField] private GameStateManager manager;
    [SerializeField] private bool useTopPlacement = true;
    [SerializeField] private float lowTimeThresholdSeconds = 60f;
    [SerializeField] private bool useTextMeshProWhenAvailable = true;
    [SerializeField] private float barWidth = 900f;
    [SerializeField] private float barHeight = 34f;
    [SerializeField] private float edgeOffset = 54f;
    [SerializeField] private bool showImmediateModeFallback;

    private GameObject canvasObject;
    private RectTransform fillRect;
    private Image fillImage;
    private Image backgroundImage;
    private Component tmpText;
    private Text uiText;
    private bool finalVisualLocked;

    private void Awake()
    {
        ResolveManager();
        EnsureUI();
        UpdateTimerVisuals();
    }

    private void Update()
    {
        ResolveManager();
        if (manager == null)
        {
            return;
        }

        if (finalVisualLocked)
        {
            return;
        }

        UpdateTimerVisuals();

        if (manager.IsGameOver())
        {
            finalVisualLocked = true;
        }
    }

    private void OnGUI()
    {
        if (!showImmediateModeFallback || IsCanvasUIReady())
        {
            return;
        }

        ResolveManager();

        float remaining = GetRemainingSeconds();
        float limit = manager != null ? Mathf.Max(0.01f, manager.timeLimitSeconds) : 600f;
        float fillValue = Mathf.Clamp01(remaining / limit);
        bool lowTime = remaining <= lowTimeThresholdSeconds;

        float width = Mathf.Min(Screen.width * 0.56f, 900f);
        float height = Mathf.Clamp(Screen.height * 0.032f, 22f, 34f);
        float x = (Screen.width - width) * 0.5f;
        float y = useTopPlacement ? 24f : Screen.height - height - 24f;

        int previousDepth = GUI.depth;
        GUI.depth = -850;

        Color previousColor = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.68f);
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, width + 4f, height + 4f), Texture2D.whiteTexture);

        GUI.color = lowTime ? new Color(1f, 0.22f, 0.12f, 0.96f) : new Color(0.15f, 0.86f, 0.38f, 0.96f);
        GUI.DrawTexture(new Rect(x, y, width * fillValue, height), Texture2D.whiteTexture);

        GUI.color = new Color(1f, 1f, 1f, 0.9f);
        GUI.DrawTexture(new Rect(x, y, width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + height - 1f, width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, 1f, height), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + width - 1f, y, 1f, height), Texture2D.whiteTexture);

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.022f), 18, 28),
            fontStyle = FontStyle.Bold
        };
        labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(x, y - 1f, width, height), $"Time Left: {FormatTime(remaining)}", labelStyle);

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private bool IsCanvasUIReady()
    {
        return canvasObject != null
            && canvasObject.activeInHierarchy
            && fillImage != null
            && fillImage.isActiveAndEnabled;
    }

    private void EnsureUI()
    {
        if (fillImage != null)
        {
            return;
        }

        canvasObject = new GameObject("Game Timer Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4800;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject timerRoot = new GameObject("Timer Root", typeof(RectTransform));
        timerRoot.transform.SetParent(canvasObject.transform, false);

        RectTransform rootRect = timerRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = useTopPlacement ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rootRect.anchorMax = rootRect.anchorMin;
        rootRect.pivot = useTopPlacement ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
        rootRect.anchoredPosition = useTopPlacement ? new Vector2(0f, -edgeOffset) : new Vector2(0f, edgeOffset);
        rootRect.sizeDelta = new Vector2(barWidth, 62f);

        GameObject background = new GameObject("Timer Bar Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        background.transform.SetParent(timerRoot.transform, false);

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 0f);
        backgroundRect.anchorMax = new Vector2(0.5f, 0f);
        backgroundRect.pivot = new Vector2(0.5f, 0f);
        backgroundRect.anchoredPosition = new Vector2(0f, 4f);
        backgroundRect.sizeDelta = new Vector2(barWidth, barHeight);

        backgroundImage = background.GetComponent<Image>();
        backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);

        Outline backgroundOutline = background.AddComponent<Outline>();
        backgroundOutline.effectColor = new Color(1f, 1f, 1f, 0.22f);
        backgroundOutline.effectDistance = new Vector2(1.5f, -1.5f);

        GameObject fill = new GameObject("Timer Bar Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fill.transform.SetParent(background.transform, false);

        fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.22f, 0.82f, 0.42f, 0.95f);

        GameObject textObject = new GameObject("Timer Text", typeof(RectTransform));
        textObject.transform.SetParent(timerRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 1f);
        textRect.anchorMax = new Vector2(0.5f, 1f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, -2f);
        textRect.sizeDelta = new Vector2(barWidth, 30f);

        if (!TryCreateTextMeshProText(textObject))
        {
            uiText = textObject.AddComponent<Text>();
            uiText.font = GetBuiltinUIFont();
            uiText.fontSize = 26;
            uiText.color = Color.white;
            uiText.fontStyle = FontStyle.Bold;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Debug.Log("GameTimerUI created countdown progress bar for player view.", this);
    }

    private static Font GetBuiltinUIFont()
    {
        try
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch (ArgumentException)
        {
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }

    private void UpdateTimerVisuals()
    {
        if (fillImage == null || fillRect == null)
        {
            return;
        }

        float remaining = GetRemainingSeconds();
        float limit = manager != null ? Mathf.Max(0.01f, manager.timeLimitSeconds) : 600f;
        float fillValue = Mathf.Clamp01(remaining / limit);

        fillRect.anchorMax = new Vector2(fillValue, 1f);

        bool lowTime = remaining <= lowTimeThresholdSeconds;
        Color timerColor = lowTime ? new Color(1f, 0.25f, 0.18f, 0.95f) : new Color(0.22f, 0.82f, 0.42f, 0.95f);
        if (fillImage != null)
        {
            fillImage.color = timerColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = lowTime ? new Color(0.22f, 0f, 0f, 0.62f) : new Color(0f, 0f, 0f, 0.55f);
        }

        string label = $"Time Left: {FormatTime(remaining)}";
        if (tmpText != null)
        {
            TrySetProperty(tmpText, "text", label);
            TrySetProperty(tmpText, "color", lowTime ? new Color(1f, 0.85f, 0.72f, 1f) : Color.white);
        }
        else if (uiText != null)
        {
            uiText.text = label;
            uiText.color = lowTime ? new Color(1f, 0.85f, 0.72f, 1f) : Color.white;
        }
    }

    private float GetRemainingSeconds()
    {
        if (manager == null)
        {
            return 600f;
        }

        return manager.GetRemainingTime();
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        int minutes = totalSeconds / 60;
        int remainingSeconds = totalSeconds % 60;
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    private bool TryCreateTextMeshProText(GameObject textObject)
    {
        if (!useTextMeshProWhenAvailable)
        {
            return false;
        }

        Type tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType == null)
        {
            return false;
        }

        tmpText = textObject.AddComponent(tmpType);
        TrySetProperty(tmpText, "fontSize", 26f);
        TrySetProperty(tmpText, "color", Color.white);
        TrySetProperty(tmpText, "enableWordWrapping", false);

        Type alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignmentType != null)
        {
            TrySetProperty(tmpText, "alignment", Enum.Parse(alignmentType, "Center"));
        }

        return true;
    }

    private static void TrySetProperty(Component component, string propertyName, object value)
    {
        if (component == null)
        {
            return;
        }

        var property = component.GetType().GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(component, value, null);
        }
    }

    private void ResolveManager()
    {
        if (manager != null)
        {
            return;
        }

        manager = GetComponent<GameStateManager>();
        if (manager == null)
        {
#if UNITY_2023_1_OR_NEWER
            manager = FindFirstObjectByType<GameStateManager>();
#else
            manager = FindObjectOfType<GameStateManager>();
#endif
        }
    }
}
