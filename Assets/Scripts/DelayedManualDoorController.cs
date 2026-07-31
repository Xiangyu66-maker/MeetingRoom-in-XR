using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Delayed Manual Door Controller")]
public sealed class DelayedManualDoorController : MonoBehaviour
{
    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";
    private const string DoorObjectName = "Tenminitunesdoor";

    [SerializeField] private float unlockDelaySeconds = 60f;
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;

    private GameStateManager gameStateManager;
    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private float fallbackStartTime;
    private bool isOpening;
    private bool isOpen;

    public bool CanOpen => RemainingUnlockSeconds <= 0f;
    public bool IsLocked => !isOpening && !isOpen && !CanOpen;
    public bool CanBeOpened => !isOpening && !isOpen && CanOpen;

    public float RemainingUnlockSeconds =>
        Mathf.Max(0f, unlockDelaySeconds - GetElapsedGameTime());

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
        fallbackStartTime = Time.unscaledTime;
        ResolveGameStateManager();
    }

    private void Start()
    {
        RemoveLegacyDoorLogic();
    }

    private void Update()
    {
        if (!isOpening)
        {
            return;
        }

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            openLocalPosition,
            openSpeed * Time.deltaTime);

        if ((transform.localPosition - openLocalPosition).sqrMagnitude
            > 0.0001f)
        {
            return;
        }

        transform.localPosition = openLocalPosition;
        isOpening = false;
        isOpen = true;
        Debug.Log("Tenminitunesdoor opened and will remain open.", this);
    }

    public bool TryOpen()
    {
        if (isOpen || isOpening)
        {
            return false;
        }

        float remainingSeconds = RemainingUnlockSeconds;
        if (remainingSeconds > 0f)
        {
            GameResultUI.GetOrCreate()?.ShowDoorLocked();
            Debug.Log(
                $"Tenminitunesdoor cannot open for another {Mathf.CeilToInt(remainingSeconds)} seconds.",
                this);
            return false;
        }

        isOpening = true;
        Debug.Log(
            "Tenminitunesdoor is now available. Quest A pressed; opening door.",
            this);
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedHandler()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SetupInitialScene()
    {
        SetupDoor(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        SetupDoor(scene);
    }

    private static void SetupDoor(Scene scene)
    {
        if (!scene.IsValid()
            || !string.Equals(
                scene.name,
                GameplaySceneName,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        GameObject door = FindSceneObjectByName(scene, DoorObjectName);
        if (door == null)
        {
            Debug.LogWarning($"Could not find {DoorObjectName} for delayed A-button setup.");
            return;
        }

        if (door.GetComponent<DelayedManualDoorController>() == null)
        {
            door.AddComponent<DelayedManualDoorController>();
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

    private void RemoveLegacyDoorLogic()
    {
        RemoveComponent<DoorController>();
        RemoveComponent<TimedExitDoorController>();
        RemoveComponent<InteractableObject>();

        ObjectIdentity identity = GetComponent<ObjectIdentity>();
        if (identity != null
            && !string.IsNullOrWhiteSpace(identity.ObjectId)
            && identity.ObjectId.StartsWith(
                "timed_exit_door_",
                StringComparison.OrdinalIgnoreCase))
        {
            Destroy(identity);
        }
    }

    private void RemoveComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component != null)
        {
            Destroy(component);
        }
    }

    private static GameObject FindSceneObjectByName(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject match = FindChildByName(root.transform, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static GameObject FindChildByName(Transform root, string objectName)
    {
        if (string.Equals(root.name, objectName, StringComparison.OrdinalIgnoreCase))
        {
            return root.gameObject;
        }

        foreach (Transform child in root)
        {
            GameObject match = FindChildByName(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
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
