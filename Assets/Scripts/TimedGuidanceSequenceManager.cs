using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Timed Guidance Sequence Manager")]
public sealed class TimedGuidanceSequenceManager : MonoBehaviour
{
    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";

    [System.Serializable]
    private sealed class TimedGuidanceStage
    {
        public float triggerTimeSeconds;
        public string objectId;
        public string label;
        public bool hasTriggered;
        public bool wasInteracted;
    }

    [SerializeField] private Transform playerTransform;
    [SerializeField] private GuidanceTrailRenderer trailRenderer;
    [SerializeField] private SimpleObjectHighlighter objectHighlighter;
    [SerializeField] private TimedGuidanceStage[] stages;

    private readonly List<Transform> activeTargetTransforms = new List<Transform>();
    private readonly List<GameObject> activeTargetObjects = new List<GameObject>();
    private GameStateManager gameStateManager;
    private float sequenceStartTime;
    private int activeStageIndex = -1;
    private static bool isSettingUp;

    private void Awake()
    {
        InitializeStagesIfNeeded();
        EnsureLocalReferences();
        sequenceStartTime = Time.time;
    }

    private void OnEnable()
    {
        InteractableObject.Interacted += HandleObjectInteracted;
    }

    private void OnDisable()
    {
        InteractableObject.Interacted -= HandleObjectInteracted;
        HideActiveGuidance();
    }

    private void Update()
    {
        if (IsGameOver())
        {
            HideActiveGuidance();
            return;
        }

        EnsureLocalReferences();

        float elapsed = Time.time - sequenceStartTime;
        for (int i = 0; i < stages.Length; i++)
        {
            TimedGuidanceStage stage = stages[i];
            if (stage == null || stage.hasTriggered)
            {
                continue;
            }

            if (elapsed < stage.triggerTimeSeconds)
            {
                continue;
            }

            stage.hasTriggered = true;
            if (!stage.wasInteracted)
            {
                ShowStageGuidance(i);
            }
        }
    }

    public void Configure(Transform player, GuidanceTrailRenderer guidanceTrail, SimpleObjectHighlighter highlighter)
    {
        playerTransform = player;
        trailRenderer = guidanceTrail;
        objectHighlighter = highlighter;
        InitializeStagesIfNeeded();
        sequenceStartTime = Time.time;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (IsGameplayScene(scene.name))
        {
            SetupGuidanceSystem();
        }
    }

    [ContextMenu("Setup Timed Guidance")]
    public void SetupFromInspector()
    {
        SetupGuidanceSystem();
    }

    public static TimedGuidanceSequenceManager SetupGuidanceSystem()
    {
        // 计时提示只在正式游戏场景中运行。
        if (!IsGameplayScene())
        {
            return null;
        }

        if (isSettingUp)
        {
            return FindFirstComponent<TimedGuidanceSequenceManager>();
        }

        isSettingUp = true;
        try
        {
            TimedGuidanceSequenceManager manager = FindFirstComponent<TimedGuidanceSequenceManager>();
            GameObject managerObject = manager != null ? manager.gameObject : null;

            if (managerObject == null)
            {
                managerObject = new GameObject("Timed Clue Guidance Manager");
                manager = managerObject.AddComponent<TimedGuidanceSequenceManager>();
            }

            GuidanceTrailRenderer guidanceTrail = managerObject.GetComponent<GuidanceTrailRenderer>();
            if (guidanceTrail == null)
            {
                guidanceTrail = managerObject.AddComponent<GuidanceTrailRenderer>();
            }

            SimpleObjectHighlighter highlighter = managerObject.GetComponent<SimpleObjectHighlighter>();
            if (highlighter == null)
            {
                highlighter = managerObject.AddComponent<SimpleObjectHighlighter>();
            }

            manager.InitializeStagesIfNeeded();
            Transform player = manager.playerTransform != null ? manager.playerTransform : FindPlayerTransform();
            manager.Configure(player, guidanceTrail, highlighter);

            foreach (TimedGuidanceStage stage in manager.stages)
            {
                if (stage == null)
                {
                    continue;
                }

                EnsureTargetReady(stage.objectId);
            }

            Debug.Log("Timed guidance sequence setup completed.", manager);
            return manager;
        }
        finally
        {
            isSettingUp = false;
        }
    }

    private static bool IsGameplayScene()
    {
        return IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return string.Equals(sceneName, GameplaySceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private void HandleObjectInteracted(string objectId, InteractableObject source)
    {
        if (string.IsNullOrWhiteSpace(objectId) || stages == null)
        {
            return;
        }

        for (int i = 0; i < stages.Length; i++)
        {
            TimedGuidanceStage stage = stages[i];
            if (stage == null || stage.objectId != objectId || stage.wasInteracted)
            {
                continue;
            }

            stage.wasInteracted = true;
            if (activeStageIndex == i)
            {
                HideActiveGuidance();
            }

            Debug.Log($"Timed guidance target interacted: {objectId}", source);
        }
    }

    private void ShowStageGuidance(int stageIndex)
    {
        if (stageIndex < 0 || stageIndex >= stages.Length)
        {
            return;
        }

        TimedGuidanceStage stage = stages[stageIndex];
        if (stage == null || stage.wasInteracted)
        {
            return;
        }

        EnsureLocalReferences();
        ObjectIdentity targetIdentity = FindObjectIdentity(stage.objectId);
        if (targetIdentity == null)
        {
            Debug.LogWarning($"Timed guidance could not find target object_id: {stage.objectId}", this);
            return;
        }

        HideActiveGuidance();

        activeTargetTransforms.Clear();
        activeTargetObjects.Clear();
        activeTargetTransforms.Add(targetIdentity.transform);
        activeTargetObjects.Add(targetIdentity.gameObject);
        activeStageIndex = stageIndex;

        if (trailRenderer != null)
        {
            trailRenderer.ShowTrail(playerTransform, activeTargetTransforms);
        }

        if (objectHighlighter != null)
        {
            objectHighlighter.HighlightObjects(activeTargetObjects);
        }

        Debug.Log($"Timed guidance triggered at {stage.triggerTimeSeconds:0}s: {stage.label} ({stage.objectId}).", this);
    }

    private void HideActiveGuidance()
    {
        if (trailRenderer != null)
        {
            trailRenderer.HideTrail();
        }

        if (objectHighlighter != null)
        {
            objectHighlighter.ClearHighlight();
        }

        activeTargetTransforms.Clear();
        activeTargetObjects.Clear();
        activeStageIndex = -1;
    }

    private void EnsureLocalReferences()
    {
        if (trailRenderer == null)
        {
            trailRenderer = GetComponent<GuidanceTrailRenderer>();
        }

        if (objectHighlighter == null)
        {
            objectHighlighter = GetComponent<SimpleObjectHighlighter>();
        }

        if (playerTransform == null)
        {
            playerTransform = FindPlayerTransform();
        }

        if (gameStateManager == null)
        {
            gameStateManager = FindFirstComponent<GameStateManager>();
        }
    }

    private bool IsGameOver()
    {
        EnsureLocalReferences();
        return gameStateManager != null && gameStateManager.IsGameOver();
    }

    private void InitializeStagesIfNeeded()
    {
        if (stages != null && stages.Length > 0)
        {
            return;
        }

        stages = new[]
        {
            CreateStage(120f, "desktop_computer_01", "desktop computer"),
            CreateStage(180f, "cabinet_01", "cabinet"),
            CreateStage(240f, "sticky_note_01", "sticky note paper"),
        };
    }

    private static TimedGuidanceStage CreateStage(float triggerTimeSeconds, string objectId, string label)
    {
        return new TimedGuidanceStage
        {
            triggerTimeSeconds = triggerTimeSeconds,
            objectId = objectId,
            label = label,
        };
    }

    private static void EnsureTargetReady(string objectId)
    {
        ObjectIdentity identity = FindObjectIdentity(objectId);
        if (identity == null)
        {
            Debug.LogWarning($"Timed guidance setup could not find target object_id: {objectId}");
            return;
        }

        if (identity.GetComponent<InteractableObject>() == null)
        {
            identity.gameObject.AddComponent<InteractableObject>();
            Debug.Log($"Added InteractableObject to timed guidance target {objectId}.", identity);
        }

        if (identity.GetComponent<Collider>() == null && identity.GetComponentInChildren<Collider>() == null)
        {
            identity.gameObject.AddComponent<BoxCollider>();
            Debug.Log($"Added BoxCollider to timed guidance target {objectId}.", identity);
        }
    }

    private static ObjectIdentity FindObjectIdentity(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return null;
        }

        ObjectIdentity[] identities = FindAllComponents<ObjectIdentity>();
        foreach (ObjectIdentity identity in identities)
        {
            if (identity != null && identity.ObjectId == objectId)
            {
                return identity;
            }
        }

        return null;
    }

    private static Transform FindPlayerTransform()
    {
        try
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                return taggedPlayer.transform;
            }
        }
        catch (UnityException)
        {
            // The scene may not define a Player tag yet; fall back to the camera root below.
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform.root;
        }

        Camera[] cameras = FindAllComponents<Camera>();
        if (cameras.Length > 0 && cameras[0] != null)
        {
            return cameras[0].transform.root;
        }

        Debug.LogWarning("TimedGuidanceSequenceManager could not find a player transform.");
        return null;
    }

    private static T FindFirstComponent<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<T>(true);
#endif
    }

    private static T[] FindAllComponents<T>() where T : Object
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<T>(true);
#endif
    }
}
