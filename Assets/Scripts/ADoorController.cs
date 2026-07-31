using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/A Door Controller")]
public sealed class ADoorController : MonoBehaviour
{
    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";
    private const string DoorObjectName = "Adoor";
    private static readonly string[] LegacyDoorObjectNames =
    {
        "Locked Exit Door (5)",
        "Locked Exit Door (2)",
    };

    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;
    [SerializeField] private float openHoldSeconds = 5f;

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private bool isOpening;
    private bool isClosing;
    private bool isOpen;
    private float closeAtTime;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
    }

    private void Start()
    {
        RemoveLegacyDoorLogic();
        EnsureNavigationObstacle();
    }

    private void Update()
    {
        AnimateDoor();
    }

    public void Open()
    {
        if (isOpen)
        {
            closeAtTime = Time.time + openHoldSeconds;
            return;
        }

        isOpening = true;
        isClosing = false;
        Debug.Log("Selected Adoor is opening.", this);
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
        SetupADoors(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        SetupADoors(scene);
    }

    private static void SetupADoors(Scene scene)
    {
        if (!scene.IsValid()
            || !string.Equals(
                scene.name,
                GameplaySceneName,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        List<GameObject> doors = FindSceneObjectsByName(scene, DoorObjectName);
        if (doors.Count == 0)
        {
            foreach (string legacyName in LegacyDoorObjectNames)
            {
                GameObject legacyDoor = FindFirstSceneObjectByName(scene, legacyName);
                if (legacyDoor != null)
                {
                    doors.Add(legacyDoor);
                }
            }
        }

        foreach (GameObject door in doors)
        {
            door.name = DoorObjectName;
            if (door.GetComponent<ADoorController>() == null)
            {
                door.AddComponent<ADoorController>();
            }
        }

        if (doors.Count != 2)
        {
            Debug.LogWarning($"Expected 2 Adoor objects, but configured {doors.Count}.");
        }
    }

    private void AnimateDoor()
    {
        if (isOpening)
        {
            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                openLocalPosition,
                openSpeed * Time.deltaTime);

            if ((transform.localPosition - openLocalPosition).sqrMagnitude
                <= 0.0001f)
            {
                transform.localPosition = openLocalPosition;
                isOpening = false;
                isOpen = true;
                closeAtTime = Time.time + openHoldSeconds;
                Debug.Log(
                    $"Adoor opened. It will close after {openHoldSeconds:0.#} seconds.",
                    this);
            }

            return;
        }

        if (isOpen && Time.time >= closeAtTime)
        {
            isOpen = false;
            isClosing = true;
            Debug.Log("Adoor is closing automatically.", this);
        }

        if (!isClosing)
        {
            return;
        }

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            closedLocalPosition,
            openSpeed * Time.deltaTime);

        if ((transform.localPosition - closedLocalPosition).sqrMagnitude
            <= 0.0001f)
        {
            transform.localPosition = closedLocalPosition;
            isClosing = false;
            Debug.Log("Adoor closed.", this);
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

    private void EnsureNavigationObstacle()
    {
        NavMeshObstacle obstacle = GetComponent<NavMeshObstacle>();
        if (obstacle == null)
        {
            obstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null)
        {
            obstacle.center = boxCollider.center;
            obstacle.size = boxCollider.size;
        }
    }

    private static List<GameObject> FindSceneObjectsByName(
        Scene scene,
        string objectName)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectChildrenByName(root.transform, objectName, matches);
        }

        return matches;
    }

    private static GameObject FindFirstSceneObjectByName(
        Scene scene,
        string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GameObject match = FindFirstChildByName(root.transform, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void CollectChildrenByName(
        Transform root,
        string objectName,
        List<GameObject> matches)
    {
        if (string.Equals(root.name, objectName, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(root.gameObject);
        }

        foreach (Transform child in root)
        {
            CollectChildrenByName(child, objectName, matches);
        }
    }

    private static GameObject FindFirstChildByName(
        Transform root,
        string objectName)
    {
        if (string.Equals(root.name, objectName, StringComparison.OrdinalIgnoreCase))
        {
            return root.gameObject;
        }

        foreach (Transform child in root)
        {
            GameObject match = FindFirstChildByName(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
