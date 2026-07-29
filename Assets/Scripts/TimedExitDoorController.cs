using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Timed Exit Door Controller")]
public sealed class TimedExitDoorController : MonoBehaviour
{
    private const string GameplayScenePath =
        "Assets/Scenes/ConferenceRoom_before_blockout_sync.unity";
    private const string TimedDoorIdPrefix = "timed_exit_door_";

    [SerializeField] private float unlockDelaySeconds = 60f;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;

    private GameStateManager gameStateManager;
    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private float fallbackStartTime;
    private bool isOpening;
    private bool hasOpened;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
        fallbackStartTime = Time.unscaledTime;
        ResolveGameStateManager();
    }

    private void Update()
    {
        if (!isOpening && !hasOpened && GetElapsedGameTime() >= unlockDelaySeconds)
        {
            isOpening = true;
            Debug.Log(
                $"Timed exit door unlocked after {unlockDelaySeconds:0} seconds. Opening door.",
                this);
        }

        if (!isOpening)
        {
            return;
        }

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            openLocalPosition,
            openSpeed * Time.deltaTime);

        if ((transform.localPosition - openLocalPosition).sqrMagnitude <= 0.0001f)
        {
            transform.localPosition = openLocalPosition;
            isOpening = false;
            hasOpened = true;
            Debug.Log(
                $"Timed exit door opened after {unlockDelaySeconds:0} seconds.",
                this);
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        string normalizedPath = scene.path.Replace('\\', '/');
        if (!string.Equals(
                normalizedPath,
                GameplayScenePath,
                System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        ObjectIdentity[] identities = FindAllObjectIdentities();
        int configuredCount = 0;

        foreach (ObjectIdentity identity in identities)
        {
            if (identity == null
                || identity.gameObject.scene != scene
                || string.IsNullOrWhiteSpace(identity.ObjectId)
                || !identity.ObjectId.StartsWith(
                    TimedDoorIdPrefix,
                    System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (identity.GetComponent<TimedExitDoorController>() == null)
            {
                identity.gameObject.AddComponent<TimedExitDoorController>();
            }

            configuredCount++;
        }

        if (configuredCount != 2)
        {
            Debug.LogWarning(
                $"Expected 2 timed exit doors, but configured {configuredCount}.");
        }
        else
        {
            Debug.Log("Configured 2 timed exit doors to open after 60 seconds.");
        }
    }

    private float GetElapsedGameTime()
    {
        ResolveGameStateManager();
        if (gameStateManager != null)
        {
            return gameStateManager.GetElapsedTime();
        }

        return Mathf.Max(0f, Time.unscaledTime - fallbackStartTime);
    }

    private void ResolveGameStateManager()
    {
        if (gameStateManager != null)
        {
            return;
        }

        Scene owningScene = gameObject.scene;
        GameStateManager[] managers = FindAllGameStateManagers();
        foreach (GameStateManager manager in managers)
        {
            if (manager != null && manager.gameObject.scene == owningScene)
            {
                gameStateManager = manager;
                return;
            }
        }
    }

    private static ObjectIdentity[] FindAllObjectIdentities()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<ObjectIdentity>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<ObjectIdentity>();
#endif
    }

    private static GameStateManager[] FindAllGameStateManagers()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<GameStateManager>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<GameStateManager>();
#endif
    }
}
