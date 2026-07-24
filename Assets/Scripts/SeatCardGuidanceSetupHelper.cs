using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Seat Card Guidance Setup Helper")]
public sealed class SeatCardGuidanceSetupHelper : MonoBehaviour
{
    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";

    [SerializeField] private bool runOnAwake = true;
    [SerializeField] private Transform playerTransform;
    [SerializeField]
    private string[] targetSeatCardIds =
    {
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
    };

    private static bool isSettingUp;

    private void Awake()
    {
        if (Application.isPlaying && runOnAwake && !isSettingUp)
        {
            SetupGuidanceSystem();
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
        if (IsGameplayScene(scene.name))
        {
            SetupGuidanceSystem();
        }
    }

    [ContextMenu("Setup Seat Card Guidance")]
    public void SetupFromInspector()
    {
        SeatCardGuidanceManager manager = SetupGuidanceSystem();
        if (manager != null)
        {
            Debug.Log("Seat card guidance setup completed from inspector.", manager);
        }
    }

    public static SeatCardGuidanceManager SetupGuidanceSystem()
    {
        // 引导管理器只属于正式游戏场景，避免污染主菜单。
        if (!IsGameplayScene())
        {
            return null;
        }

        if (isSettingUp)
        {
            return FindFirstComponent<SeatCardGuidanceManager>();
        }

        isSettingUp = true;
        try
        {
            SeatCardGuidanceManager manager = FindFirstComponent<SeatCardGuidanceManager>();
            GameObject managerObject = manager != null ? manager.gameObject : null;

            if (managerObject == null)
            {
                managerObject = new GameObject("Seat Card Guidance Manager");
                manager = managerObject.AddComponent<SeatCardGuidanceManager>();
            }

            SeatCardGuidanceSetupHelper setupHelper = managerObject.GetComponent<SeatCardGuidanceSetupHelper>();
            if (setupHelper == null)
            {
                setupHelper = managerObject.AddComponent<SeatCardGuidanceSetupHelper>();
            }

            SeatCardInspectionTracker tracker = managerObject.GetComponent<SeatCardInspectionTracker>();
            if (tracker == null)
            {
                tracker = managerObject.AddComponent<SeatCardInspectionTracker>();
            }

            GuidanceTrailRenderer trailRenderer = managerObject.GetComponent<GuidanceTrailRenderer>();
            if (trailRenderer == null)
            {
                trailRenderer = managerObject.AddComponent<GuidanceTrailRenderer>();
            }

            SimpleObjectHighlighter highlighter = managerObject.GetComponent<SimpleObjectHighlighter>();
            if (highlighter == null)
            {
                highlighter = managerObject.AddComponent<SimpleObjectHighlighter>();
            }

            Transform player = setupHelper.playerTransform != null ? setupHelper.playerTransform : FindPlayerTransform();
            List<ObjectIdentity> targets = FindTargetIdentities(setupHelper.targetSeatCardIds);

            foreach (ObjectIdentity target in targets)
            {
                EnsureSeatCardReady(target);
            }

            tracker.ConfigureTargets(setupHelper.targetSeatCardIds);
            manager.Configure(tracker, trailRenderer, highlighter, player, targets);

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

        Debug.LogWarning("SeatCardGuidanceSetupHelper could not find a player transform.");
        return null;
    }

    private static List<ObjectIdentity> FindTargetIdentities(string[] targetIds)
    {
        List<ObjectIdentity> targets = new List<ObjectIdentity>();
        ObjectIdentity[] identities = FindAllComponents<ObjectIdentity>();

        foreach (string targetId in targetIds)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                continue;
            }

            ObjectIdentity found = null;
            foreach (ObjectIdentity identity in identities)
            {
                if (identity != null && identity.ObjectId == targetId)
                {
                    found = identity;
                    break;
                }
            }

            if (found != null)
            {
                targets.Add(found);
            }
            else
            {
                Debug.LogWarning($"SeatCardGuidanceSetupHelper could not find target object_id: {targetId}");
            }
        }

        return targets;
    }

    private static void EnsureSeatCardReady(ObjectIdentity identity)
    {
        if (identity == null)
        {
            return;
        }

        if (identity.GetComponent<InteractableObject>() == null)
        {
            identity.gameObject.AddComponent<InteractableObject>();
        }

        if (identity.GetComponent<Collider>() != null || identity.GetComponentInChildren<Collider>() != null)
        {
            return;
        }

        if (identity.GetComponent<Renderer>() != null || identity.GetComponent<MeshFilter>() != null)
        {
            identity.gameObject.AddComponent<BoxCollider>();
        }
        else
        {
            Debug.LogWarning($"Seat card guidance target {identity.ObjectId} has no collider and no local mesh for automatic BoxCollider setup.", identity);
        }
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
