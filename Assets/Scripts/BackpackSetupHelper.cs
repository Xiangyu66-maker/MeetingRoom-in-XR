using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Backpack Setup Helper")]
public sealed class BackpackSetupHelper : MonoBehaviour
{
    [SerializeField] private bool setupOnAwake = true;

    private const string GameplaySceneName = "ConferenceRoom_before_blockout_sync";
    private const string CabinetObjectId = "cabinet_01";
    private const string DesktopComputerObjectId = "desktop_computer_01";
    private const string DesktopComputerNoteItemId = "note_desktop_password_hint";
    private const string DesktopComputerNoteItemName = "Computer Hint";
    private const string DesktopComputerNoteItemType = "note";
    private const string DesktopComputerNoteContent = "Hint: the password is <color=#FF0000>?O??</color>";
    private const string SeatOrderNoteItemId = "note_seat_order";
    private const string SeatOrderNoteItemName = "Seat Order Note";
    private const string SeatOrderNoteItemType = "note";
    private const string SeatOrderNoteContent =
        "Meeting seat order:\nSeat 1 = circle\nSeat 2 = star\nSeat 3 = triangle\nSeat 4 = square";
    private static readonly string[] SeatCardObjectIds =
    {
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
    };

    private void Awake()
    {
        if (setupOnAwake)
        {
            SetupBackpackSystem();
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
            SetupBackpackSystem();
        }
    }

    [ContextMenu("Setup Backpack System")]
    public static void SetupBackpackSystem()
    {
        // 背包仅在正式会议室场景中生成，避免出现在 Menu。
        if (!IsGameplayScene())
        {
            return;
        }

        GameObject root = FindOrCreateSystemRoot();

        InventoryManager inventoryManager = root.GetComponent<InventoryManager>();
        if (inventoryManager == null)
        {
            inventoryManager = root.AddComponent<InventoryManager>();
        }

        BackpackUI backpackUI = root.GetComponent<BackpackUI>();
        if (backpackUI == null)
        {
            backpackUI = root.AddComponent<BackpackUI>();
        }

        backpackUI.Configure(inventoryManager);

        BackpackInputController inputController = root.GetComponent<BackpackInputController>();
        if (inputController == null)
        {
            inputController = root.AddComponent<BackpackInputController>();
        }

        inputController.Configure(backpackUI);
        EnsureCabinetPickup();
        EnsureSeatOrderPickup();
        EnsureDesktopComputerPickup();
        Debug.Log($"Backpack system initialized in scene: {SceneManager.GetActiveScene().name}.", root);
    }

    private static bool IsGameplayScene()
    {
        return IsGameplayScene(SceneManager.GetActiveScene().name);
    }

    private static bool IsGameplayScene(string sceneName)
    {
        return string.Equals(sceneName, GameplaySceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureCabinetPickup()
    {
        GameObject cabinet = FindCabinetObject();
        if (cabinet == null)
        {
            Debug.LogWarning("Backpack setup could not find cabinet_01 / Openable Cabinet for clue note pickup.");
            return;
        }

        if (cabinet.GetComponent<ClueNotePickup>() == null)
        {
            cabinet.AddComponent<ClueNotePickup>();
            Debug.Log("Added ClueNotePickup to cabinet_01.", cabinet);
        }

        if (cabinet.GetComponent<InteractableObject>() == null)
        {
            cabinet.AddComponent<InteractableObject>();
            Debug.Log("Added InteractableObject to cabinet_01 for backpack note pickup.", cabinet);
        }

        if (cabinet.GetComponent<Collider>() == null && cabinet.GetComponentInChildren<Collider>() == null)
        {
            cabinet.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to cabinet_01 for backpack note pickup.", cabinet);
        }
    }

    private static void EnsureSeatOrderPickup()
    {
        foreach (string seatCardObjectId in SeatCardObjectIds)
        {
            GameObject seatCard = FindObjectById(seatCardObjectId);
            if (seatCard == null)
            {
                Debug.LogWarning($"Backpack setup could not find {seatCardObjectId} for seat order clue pickup.");
                continue;
            }

            ClueNotePickup pickup = seatCard.GetComponent<ClueNotePickup>();
            if (pickup == null)
            {
                pickup = seatCard.AddComponent<ClueNotePickup>();
                Debug.Log($"Added ClueNotePickup to {seatCardObjectId}.", seatCard);
            }

            pickup.Configure(SeatOrderNoteItemId, SeatOrderNoteItemName, SeatOrderNoteItemType, SeatOrderNoteContent);

            if (seatCard.GetComponent<InteractableObject>() == null)
            {
                seatCard.AddComponent<InteractableObject>();
                Debug.Log($"Added InteractableObject to {seatCardObjectId} for seat order note pickup.", seatCard);
            }

            if (seatCard.GetComponent<Collider>() == null && seatCard.GetComponentInChildren<Collider>() == null)
            {
                seatCard.AddComponent<BoxCollider>();
                Debug.Log($"Added BoxCollider to {seatCardObjectId} for seat order note pickup.", seatCard);
            }
        }
    }

    private static void EnsureDesktopComputerPickup()
    {
        GameObject desktopComputer = FindObjectById(DesktopComputerObjectId);
        if (desktopComputer == null)
        {
            Debug.LogWarning("Backpack setup could not find desktop_computer_01 for computer hint pickup.");
            return;
        }

        ClueNotePickup pickup = desktopComputer.GetComponent<ClueNotePickup>();
        if (pickup == null)
        {
            pickup = desktopComputer.AddComponent<ClueNotePickup>();
            Debug.Log("Added ClueNotePickup to desktop_computer_01.", desktopComputer);
        }

        pickup.Configure(
            DesktopComputerNoteItemId,
            DesktopComputerNoteItemName,
            DesktopComputerNoteItemType,
            DesktopComputerNoteContent);

        if (desktopComputer.GetComponent<InteractableObject>() == null)
        {
            desktopComputer.AddComponent<InteractableObject>();
            Debug.Log("Added InteractableObject to desktop_computer_01 for computer hint pickup.", desktopComputer);
        }

        if (desktopComputer.GetComponent<Collider>() == null && desktopComputer.GetComponentInChildren<Collider>() == null)
        {
            desktopComputer.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider to desktop_computer_01 for computer hint pickup.", desktopComputer);
        }
    }

    private static GameObject FindCabinetObject()
    {
        GameObject cabinetById = FindObjectById(CabinetObjectId);
        if (cabinetById != null)
        {
            return cabinetById;
        }

        return FindSceneObjectByName("Openable Cabinet");
    }

    private static GameObject FindObjectById(string objectId)
    {
        ObjectIdentity[] identities = FindObjectIdentities();
        foreach (ObjectIdentity identity in identities)
        {
            if (identity != null && identity.ObjectId == objectId)
            {
                return identity.gameObject;
            }
        }

        return null;
    }

    private static ObjectIdentity[] FindObjectIdentities()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<ObjectIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<ObjectIdentity>(true);
#endif
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        foreach (GameObject root in roots)
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
        if (root.name == objectName)
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

    private static GameObject FindOrCreateSystemRoot()
    {
        GameObject existing = FindSceneObjectByName("Backpack System");
        if (existing != null)
        {
            return existing;
        }

        return new GameObject("Backpack System");
    }
}
