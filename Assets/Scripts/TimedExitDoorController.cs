using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Timed Exit Door Controller")]
public sealed class TimedExitDoorController : MonoBehaviour
{
    private const string GameplayScenePath =
        "Assets/Scenes/ConferenceRoom_before_blockout_sync.unity";
    private const string FirstTimedDoorName = "danger";
    private const string SecondTimedDoorName = "danger2";

    [SerializeField] private float unlockDelaySeconds = 60f;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;

    private GameStateManager gameStateManager;
    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private float fallbackStartTime;
    private bool isOpening;
    private bool hasOpened;

    public bool IsLocked =>
        !isOpening && !hasOpened && GetElapsedGameTime() < unlockDelaySeconds;

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

        int configuredCount = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            configuredCount += ConfigureTimedDoor(root.transform, FirstTimedDoorName);
            configuredCount += ConfigureTimedDoor(root.transform, SecondTimedDoorName);
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

    private static int ConfigureTimedDoor(Transform root, string doorName)
    {
        if (string.Equals(
                root.name,
                doorName,
                System.StringComparison.OrdinalIgnoreCase))
        {
            if (root.GetComponent<TimedExitDoorController>() == null)
            {
                root.gameObject.AddComponent<TimedExitDoorController>();
            }

            return 1;
        }

        foreach (Transform child in root)
        {
            int configuredCount = ConfigureTimedDoor(child, doorName);
            if (configuredCount > 0)
            {
                return configuredCount;
            }
        }

        return 0;
    }

    public void Inspect()
    {
        if (IsLocked)
        {
            GameResultUI.GetOrCreate()?.ShowDoorLocked();
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

    private static GameStateManager[] FindAllGameStateManagers()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<GameStateManager>(FindObjectsSortMode.None);
#else
        return FindObjectsOfType<GameStateManager>();
#endif
    }
}
