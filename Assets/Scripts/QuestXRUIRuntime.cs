using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Converts the meeting-room's desktop-style canvases into a head-locked,
/// world-space HUD and enables Quest Touch controller ray input.
/// Desktop play mode is left unchanged when no XR display is running.
/// </summary>
[DefaultExecutionOrder(-10000)]
[DisallowMultipleComponent]
public sealed class QuestXRUIRuntime : MonoBehaviour
{
    public enum Channel
    {
        Interaction,
        Context,
        Keypad,
        Vision,
        AdaptiveGuide
    }

    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";
    private const string MenuSceneName = "Menu";
    private const string StatusCanvasName = "Quest XR Status Canvas";
    private const float CanvasRefreshInterval = 0.5f;

    private static readonly List<XRDisplaySubsystem> DisplaySubsystems = new();

    private readonly Dictionary<Channel, TextMeshProUGUI> channelTexts = new();
    private readonly HashSet<int> configuredCanvases = new();

    private static QuestXRUIRuntime instance;

    private Transform headTransform;
    private Transform rightControllerTransform;
    private Camera xrCamera;
    private EventSystem eventSystem;
    private QuestOVRInputModule ovrInputModule;
    private LineRenderer controllerRay;
    private Material controllerRayMaterial;
    private float nextCanvasRefreshTime;
    private bool xrPresentationActive;

    public static bool IsXRPresentationActive =>
        instance != null && instance.xrPresentationActive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapCurrentScene()
    {
        EnsureForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        EnsureForScene(scene);
    }

    private static void EnsureForScene(Scene scene)
    {
        if (!IsSupportedScene(scene.name))
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        QuestXRUIRuntime existing = FindFirstObjectByType<QuestXRUIRuntime>();
#else
        QuestXRUIRuntime existing = FindObjectOfType<QuestXRUIRuntime>();
#endif
        if (existing == null)
        {
            GameObject runtimeObject = new GameObject("Quest XR UI Runtime");
            existing = runtimeObject.AddComponent<QuestXRUIRuntime>();
        }

        instance = existing;
    }

    private static bool IsSupportedScene(string sceneName)
    {
        return string.Equals(sceneName, GameplaySceneName, System.StringComparison.OrdinalIgnoreCase)
            || string.Equals(sceneName, MenuSceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        bool shouldPresentInXR = IsXRDisplayRunning();
        if (!shouldPresentInXR)
        {
            xrPresentationActive = false;
            SetControllerRayVisible(false);
            return;
        }

        xrPresentationActive = true;
        ResolveRigReferences();
        if (headTransform == null)
        {
            return;
        }

        EnsureQuestInput();
        EnsureStatusCanvas();

        if (Time.unscaledTime >= nextCanvasRefreshTime)
        {
            nextCanvasRefreshTime = Time.unscaledTime + CanvasRefreshInterval;
            ConfigureSceneCanvases();
        }

        SetControllerRayVisible(HasVisibleInteractiveCanvas());
    }

    private static bool IsXRDisplayRunning()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return true;
#else
        DisplaySubsystems.Clear();
        SubsystemManager.GetSubsystems(DisplaySubsystems);
        foreach (XRDisplaySubsystem display in DisplaySubsystems)
        {
            if (display != null && display.running)
            {
                return true;
            }
        }

        return false;
#endif
    }

    private void ResolveRigReferences()
    {
        if (headTransform != null && rightControllerTransform != null && xrCamera != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        OVRCameraRig rig = FindFirstObjectByType<OVRCameraRig>();
#else
        OVRCameraRig rig = FindObjectOfType<OVRCameraRig>();
#endif
        if (rig != null)
        {
            headTransform = rig.centerEyeAnchor;
            rightControllerTransform = rig.rightControllerAnchor;
            xrCamera = headTransform != null ? headTransform.GetComponent<Camera>() : null;
        }

        if (headTransform == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                xrCamera = mainCamera;
                headTransform = mainCamera.transform;
            }
        }

        if (rightControllerTransform == null)
        {
            GameObject controller = GameObject.Find("RightControllerAnchor");
            rightControllerTransform = controller != null ? controller.transform : headTransform;
        }
    }

    private void EnsureQuestInput()
    {
        if (eventSystem == null)
        {
#if UNITY_2023_1_OR_NEWER
            eventSystem = FindFirstObjectByType<EventSystem>();
#else
            eventSystem = FindObjectOfType<EventSystem>();
#endif
        }

        if (eventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("Quest XR EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (ovrInputModule == null)
        {
            ovrInputModule = eventSystem.GetComponent<QuestOVRInputModule>();
            if (ovrInputModule == null)
            {
                OVRInputModule existingModule = eventSystem.GetComponent<OVRInputModule>();
                if (existingModule != null)
                {
                    existingModule.enabled = false;
                }

                ovrInputModule = eventSystem.gameObject.AddComponent<QuestOVRInputModule>();
            }
        }

        ovrInputModule.rayTransform =
            rightControllerTransform != null ? rightControllerTransform : headTransform;
        ovrInputModule.joyPadClickButton = OVRInput.Button.One;
        ovrInputModule.gazeClickKey = KeyCode.Space;
        ovrInputModule.enabled = true;

        StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            standalone.enabled = false;
        }

        EventSystem.current = eventSystem;
        EnsureControllerRay();
    }

    private void EnsureControllerRay()
    {
        if (controllerRay != null || rightControllerTransform == null)
        {
            return;
        }

        GameObject rayObject = new GameObject("Quest UI Controller Ray");
        rayObject.transform.SetParent(rightControllerTransform, false);
        rayObject.transform.localPosition = Vector3.zero;
        rayObject.transform.localRotation = Quaternion.identity;

        controllerRay = rayObject.AddComponent<LineRenderer>();
        controllerRay.useWorldSpace = false;
        controllerRay.positionCount = 2;
        controllerRay.SetPosition(0, Vector3.zero);
        controllerRay.SetPosition(1, Vector3.forward * 6f);
        controllerRay.startWidth = 0.003f;
        controllerRay.endWidth = 0.0015f;
        controllerRay.startColor = new Color(0.18f, 0.82f, 1f, 0.9f);
        controllerRay.endColor = new Color(0.18f, 0.82f, 1f, 0.18f);
        controllerRay.numCapVertices = 4;
        controllerRay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        controllerRay.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            controllerRayMaterial = new Material(shader)
            {
                color = Color.white
            };
            controllerRay.material = controllerRayMaterial;
        }

        controllerRay.enabled = false;
    }

    private void EnsureStatusCanvas()
    {
        if (channelTexts.Count > 0 || headTransform == null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            StatusCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 15000;
        canvas.overrideSorting = true;
        canvas.worldCamera = xrCamera;

        RectTransform rect = canvasObject.GetComponent<RectTransform>();
        ConfigureHeadLockedRect(rect, 1.35f, 0.001f);
        configuredCanvases.Add(canvas.GetInstanceID());

        CreateChannel(
            canvas.transform,
            Channel.AdaptiveGuide,
            "Adaptive Guide",
            new Vector2(0f, 1f),
            new Vector2(80f, -70f),
            new Vector2(760f, 220f),
            TextAlignmentOptions.TopLeft,
            22f);

        CreateChannel(
            canvas.transform,
            Channel.Keypad,
            "Quest Keypad Status",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -90f),
            new Vector2(700f, 150f),
            TextAlignmentOptions.Center,
            32f);

        CreateChannel(
            canvas.transform,
            Channel.Vision,
            "Vision Feedback",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 210f),
            new Vector2(1240f, 150f),
            TextAlignmentOptions.Center,
            24f);

        CreateChannel(
            canvas.transform,
            Channel.Context,
            "Context Prompt",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 128f),
            new Vector2(900f, 58f),
            TextAlignmentOptions.Center,
            28f);

        CreateChannel(
            canvas.transform,
            Channel.Interaction,
            "Interaction Prompt",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 62f),
            new Vector2(1100f, 58f),
            TextAlignmentOptions.Center,
            28f);
    }

    private void CreateChannel(
        Transform parent,
        Channel channel,
        string objectName,
        Vector2 anchor,
        Vector2 anchoredPosition,
        Vector2 size,
        TextAlignmentOptions alignment,
        float fontSize)
    {
        GameObject panelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        panelObject.transform.SetParent(parent, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = anchor;
        panelRect.anchorMax = anchor;
        panelRect.pivot = new Vector2(
            Mathf.Approximately(anchor.x, 0f) ? 0f : 0.5f,
            Mathf.Approximately(anchor.y, 0f) ? 0f : 1f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = size;

        Image background = panelObject.GetComponent<Image>();
        background.color = new Color(0.025f, 0.035f, 0.05f, 0.78f);
        background.raycastTarget = false;

        GameObject textObject = new GameObject(
            objectName + " Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 10f);
        textRect.offsetMax = new Vector2(-18f, -10f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        text.text = string.Empty;

        channelTexts[channel] = text;
        panelObject.SetActive(false);
    }

    private void ConfigureSceneCanvases()
    {
#if UNITY_2023_1_OR_NEWER
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
#else
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
#endif
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || configuredCanvases.Contains(canvas.GetInstanceID()))
            {
                continue;
            }

            ConfigureCanvas(canvas);
        }
    }

    private void ConfigureCanvas(Canvas canvas)
    {
        if (headTransform == null)
        {
            return;
        }

        RectTransform rect = canvas.transform as RectTransform;
        if (rect == null)
        {
            return;
        }

        float distance = GetCanvasDistance(canvas.name);
        float scale = GetCanvasScale(canvas.name);

        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.worldCamera = xrCamera;
        ConfigureHeadLockedRect(rect, distance, scale);

        if (CanvasHasSelectable(canvas))
        {
            GraphicRaycaster[] graphicRaycasters = canvas.GetComponents<GraphicRaycaster>();
            foreach (GraphicRaycaster raycaster in graphicRaycasters)
            {
                if (raycaster != null && raycaster.GetType() == typeof(GraphicRaycaster))
                {
                    raycaster.enabled = false;
                }
            }

            OVRRaycaster ovrRaycaster = canvas.GetComponent<OVRRaycaster>();
            if (ovrRaycaster == null)
            {
                ovrRaycaster = canvas.gameObject.AddComponent<OVRRaycaster>();
            }

            ovrRaycaster.sortOrder = canvas.sortingOrder;
            ovrRaycaster.enabled = true;
        }

        configuredCanvases.Add(canvas.GetInstanceID());
        Debug.Log($"Quest XR UI configured: {canvas.name}", canvas);
    }

    private void ConfigureHeadLockedRect(RectTransform rect, float distance, float scale)
    {
        rect.SetParent(headTransform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1920f, 1080f);
        rect.localPosition = new Vector3(0f, 0f, distance);
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one * scale;
    }

    private static float GetCanvasDistance(string canvasName)
    {
        if (canvasName == "Backpack Canvas")
        {
            return 1.15f;
        }

        if (canvasName == "Game Timer Canvas")
        {
            return 1.3f;
        }

        return 1.35f;
    }

    private static float GetCanvasScale(string canvasName)
    {
        if (canvasName == "Backpack Canvas")
        {
            return 0.0014f;
        }

        return 0.001f;
    }

    private static bool CanvasHasSelectable(Canvas canvas)
    {
        return canvas.GetComponentsInChildren<Selectable>(true).Length > 0;
    }

    public static void ConfigureCanvasForXR(Canvas canvas)
    {
        if (!IsXRPresentationActive || canvas == null)
        {
            return;
        }

        instance.ResolveRigReferences();
        instance.EnsureQuestInput();
        instance.ConfigureCanvas(canvas);
    }

    private bool HasVisibleInteractiveCanvas()
    {
        if (headTransform == null)
        {
            return false;
        }

#if UNITY_2023_1_OR_NEWER
        Canvas[] canvases = FindObjectsByType<Canvas>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
#else
        Canvas[] canvases = FindObjectsOfType<Canvas>();
#endif
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null
                && canvas.isActiveAndEnabled
                && canvas.GetComponent<OVRRaycaster>() != null
                && HasVisibleSelectable(canvas))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasVisibleSelectable(Canvas canvas)
    {
        Selectable[] selectables = canvas.GetComponentsInChildren<Selectable>(false);
        foreach (Selectable selectable in selectables)
        {
            if (selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable())
            {
                return true;
            }
        }

        return false;
    }

    private void SetControllerRayVisible(bool visible)
    {
        if (controllerRay != null)
        {
            controllerRay.enabled = visible;
        }
    }

    public static void SetMessage(Channel channel, string message, bool visible = true)
    {
        if (!IsXRPresentationActive)
        {
            return;
        }

        instance.SetMessageInternal(channel, message, visible);
    }

    public static void HideMessage(Channel channel)
    {
        SetMessage(channel, string.Empty, false);
    }

    private void SetMessageInternal(Channel channel, string message, bool visible)
    {
        EnsureStatusCanvas();
        if (!channelTexts.TryGetValue(channel, out TextMeshProUGUI text) || text == null)
        {
            return;
        }

        bool shouldShow = visible && !string.IsNullOrWhiteSpace(message);
        text.text = shouldShow ? message : string.Empty;
        text.transform.parent.gameObject.SetActive(shouldShow);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        if (controllerRayMaterial != null)
        {
            Destroy(controllerRayMaterial);
        }
    }
}
