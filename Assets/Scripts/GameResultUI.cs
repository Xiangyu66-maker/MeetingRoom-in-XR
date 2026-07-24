using System;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Game Result UI")]
public sealed class GameResultUI : MonoBehaviour
{
    private const string SuccessMessage = "Game Success! You escaped the meeting room.";
    private const string FailureMessage = "Game Failed! Time limit exceeded.";
    private const string ClueFoundMessage = "Clue Found! Note added to backpack.";

    [SerializeField] private bool useTextMeshProWhenAvailable = true;
    [SerializeField] private float holdSeconds = 2f;
    [SerializeField] private float fadeSeconds = 1f;

    private GameObject resultRoot;
    private CanvasGroup canvasGroup;
    private Component tmpText;
    private Text uiText;
    private string currentMessage;
    private float visibleTimer;
    private bool isShowing;

    public static GameResultUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate GameResultUI found. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
        EnsureUI();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        visibleTimer += Time.unscaledDeltaTime;
        float fadeStart = Mathf.Max(0f, holdSeconds);
        float fadeLength = Mathf.Max(0.01f, fadeSeconds);

        if (visibleTimer <= fadeStart)
        {
            SetAlpha(1f);
            return;
        }

        float fadeProgress = Mathf.Clamp01((visibleTimer - fadeStart) / fadeLength);
        SetAlpha(1f - fadeProgress);

        if (fadeProgress >= 1f)
        {
            Hide();
        }
    }

    public static GameResultUI GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameResultUI existing = FindGameResultUI();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject uiObject = new GameObject("Game Result UI");
        return uiObject.AddComponent<GameResultUI>();
    }

    public void ShowSuccess()
    {
        ShowMessage(SuccessMessage);
    }

    public void ShowFailure()
    {
        ShowMessage(FailureMessage);
    }

    public void ShowClueFound()
    {
        ShowMessage(ClueFoundMessage);
    }

    public void ShowMessage(string message)
    {
        EnsureUI();
        SetText(message);
        currentMessage = message;
        visibleTimer = 0f;
        isShowing = true;
        SetAlpha(1f);
        resultRoot.SetActive(true);
        Debug.Log($"Game result prompt shown: {message}", this);
    }

    public void Hide()
    {
        EnsureUI();
        resultRoot.SetActive(false);
        currentMessage = string.Empty;
        visibleTimer = 0f;
        isShowing = false;
        SetAlpha(0f);
    }

    private void EnsureUI()
    {
        if (resultRoot != null)
        {
            EnsureCanvasGroup();
            return;
        }

        GameObject canvasObject = new GameObject("Game Result Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        resultRoot = new GameObject("Game Result Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        resultRoot.transform.SetParent(canvasObject.transform, false);
        canvasGroup = resultRoot.AddComponent<CanvasGroup>();

        RectTransform panelRect = resultRoot.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = resultRoot.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.68f);

        GameObject textObject = new GameObject("Game Result Text", typeof(RectTransform));
        textObject.transform.SetParent(resultRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(1200f, 220f);

        if (!TryCreateTextMeshProText(textObject))
        {
            uiText = textObject.AddComponent<Text>();
            uiText.font = GetBuiltinUIFont();
            uiText.fontSize = 56;
            uiText.fontStyle = FontStyle.Bold;
            uiText.color = Color.white;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
        }
    }

    private void EnsureCanvasGroup()
    {
        if (resultRoot == null || canvasGroup != null)
        {
            return;
        }

        canvasGroup = resultRoot.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = resultRoot.AddComponent<CanvasGroup>();
        }
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }
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
        TrySetProperty(tmpText, "fontSize", 42f);
        TrySetProperty(tmpText, "color", Color.white);
        TrySetProperty(tmpText, "enableWordWrapping", true);

        Type alignmentType = Type.GetType("TMPro.TextAlignmentOptions, Unity.TextMeshPro");
        if (alignmentType != null)
        {
            TrySetProperty(tmpText, "alignment", Enum.Parse(alignmentType, "Center"));
        }

        return true;
    }

    private void SetText(string message)
    {
        if (tmpText != null)
        {
            TrySetProperty(tmpText, "text", message);
            return;
        }

        if (uiText != null)
        {
            uiText.text = message;
        }
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

    private static GameResultUI FindGameResultUI()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<GameResultUI>();
#else
        return FindObjectOfType<GameResultUI>();
#endif
    }
}
