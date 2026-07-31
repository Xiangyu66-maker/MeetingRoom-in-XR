using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Computer Door Controller")]
public sealed class ComputerDoorController : MonoBehaviour
{
    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";
    private const string DoorObjectName = "Computerdoor";
    private const string ComputerObjectId = "desktop_computer_01";
    private const string ComputerClueItemId = "note_desktop_password_hint";

    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private bool isOpening;
    private bool isOpen;

    public bool IsLocked => !isOpening && !isOpen;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
    }

    private void OnEnable()
    {
        ClueNotePickup.Collected -= HandleClueCollected;
        ClueNotePickup.Collected += HandleClueCollected;
    }

    private void Start()
    {
        RemoveLegacyDoorLogic();
    }

    private void OnDisable()
    {
        ClueNotePickup.Collected -= HandleClueCollected;
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

        if ((transform.localPosition - openLocalPosition).sqrMagnitude > 0.0001f)
        {
            return;
        }

        transform.localPosition = openLocalPosition;
        isOpening = false;
        isOpen = true;
        Debug.Log("Computer door opened after the desktop computer clue was collected.", this);
    }

    public void Open()
    {
        if (isOpen || isOpening)
        {
            return;
        }

        isOpening = true;
        Debug.Log("Desktop computer clue collected. Opening Computerdoor.", this);
    }

    public void Inspect()
    {
        if (IsLocked)
        {
            GameResultUI.GetOrCreate()?.ShowDoorLocked();
        }
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
        SetupComputerDoor(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        SetupComputerDoor(scene);
    }

    private static void SetupComputerDoor(Scene scene)
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
            Debug.LogWarning($"Computer door setup could not find {DoorObjectName}.");
            return;
        }

        door.name = DoorObjectName;
        if (door.GetComponent<ComputerDoorController>() == null)
        {
            door.AddComponent<ComputerDoorController>();
        }
    }

    private void HandleClueCollected(string itemId, ClueNotePickup pickup)
    {
        if (!string.Equals(itemId, ComputerClueItemId, StringComparison.Ordinal))
        {
            return;
        }

        ObjectIdentity identity = pickup != null
            ? pickup.GetComponent<ObjectIdentity>()
            : null;
        if (identity == null && pickup != null)
        {
            identity = pickup.GetComponentInParent<ObjectIdentity>();
        }

        if (identity != null
            && !string.Equals(
                identity.ObjectId,
                ComputerObjectId,
                StringComparison.Ordinal))
        {
            return;
        }

        Open();
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
}
