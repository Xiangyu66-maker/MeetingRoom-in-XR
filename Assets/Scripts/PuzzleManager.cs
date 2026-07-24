using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance { get; private set; }

    [Header("√’Ã‚≈‰÷√")]
    [SerializeField] private string[] requiredItemsForDoor = { "document_01", "remote_01" };
    [SerializeField] private string remoteId = "remote_01";
    [SerializeField] private string screenId = "screen_01";
    [SerializeField] private string doorId = "locked_door_01";

    [Header("±≠◊”≈‰÷√")]
    [SerializeField] private string cupIdPrefix = "cup_";

    [Header("◊˘“Œ≈‰÷√")]
    [SerializeField] private string seatIdPrefix = "chair_";

    [Header("± º«±æœﬂÀ˜")]
    [SerializeField] private string notebookId = "document_01";
    [SerializeField] private string deskTargetId = "computer_desk_01";

    private HashSet<string> collectedItems = new HashSet<string>();
    private bool doorUnlocked = false;
    private bool screenActivated = false;
    private bool cupClueTriggered = false;
    private bool notebookClueTriggered = false;

    private DoorController doorController;
    private GameObject screenObject;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreateAndSubscribe()
    {
        if (Instance == null)
        {
            PuzzleManager existing = FindObjectOfType<PuzzleManager>();
            if (existing != null)
            {
                Instance = existing;
                Instance.SubscribeEvents();
                Debug.Log("PuzzleManager found in scene and subscribed.");
                return;
            }

            GameObject go = new GameObject("PuzzleManager");
            Instance = go.AddComponent<PuzzleManager>();
            DontDestroyOnLoad(go);
            Instance.SubscribeEvents();
            Debug.Log("PuzzleManager auto-created and subscribed.");
        }
        else
        {
            Instance.SubscribeEvents();
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            SubscribeEvents();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    private void SubscribeEvents()
    {
        if (PuzzleEventManager.Instance != null)
        {
            PuzzleEventManager.Instance.OnItemGrabbed -= OnItemGrabbedHandler;
            PuzzleEventManager.Instance.OnItemDropped -= OnItemDroppedHandler;
            PuzzleEventManager.Instance.OnItemGrabbed += OnItemGrabbedHandler;
            PuzzleEventManager.Instance.OnItemDropped += OnItemDroppedHandler;
            Debug.Log("PuzzleManager subscribed to events.");
        }
        else
        {
            Debug.LogWarning("PuzzleEventManager.Instance is null, will retry later.");
            Invoke(nameof(SubscribeEvents), 0.5f);
        }
    }

    private void Start()
    {
        doorController = FindDoor();
        screenObject = FindScreen();
    }

    private void OnDestroy()
    {
        if (PuzzleEventManager.Instance != null)
        {
            PuzzleEventManager.Instance.OnItemGrabbed -= OnItemGrabbedHandler;
            PuzzleEventManager.Instance.OnItemDropped -= OnItemDroppedHandler;
        }
        if (Instance == this) Instance = null;
    }

    private void OnItemGrabbedHandler(string objectId)
    {
        Debug.Log($"Pickup event: {objectId}");
        collectedItems.Add(objectId);
        CheckDoorCollection();
    }

    private void OnItemDroppedHandler(string objectId, GameObject surface)
    {
        if (surface == null) return;
        string surfaceId = GetRootObjectId(surface);
        Debug.Log($"Drop event: {objectId} on '{surface.name}', rootId='{surfaceId}'");

        // ≤Ë±≠∑≈»Œ“‚◊˘“Œ
        if (!cupClueTriggered && objectId.StartsWith("cup_") && surfaceId != null && surfaceId.StartsWith(seatIdPrefix))
        {
            TriggerCupOnSeatClue();
        }

        // ± º«±æ∑≈µÁƒ‘◊¿
        if (!notebookClueTriggered && objectId == notebookId && surfaceId == deskTargetId)
        {
            TriggerNotebookOnDeskClue();
        }

        // “£øÿ∆˜∑≈∆¡ƒª
        if (objectId == remoteId && surfaceId == screenId)
        {
            ActivateScreen();
        }
    }

    private void CheckDoorCollection()
    {
        if (doorUnlocked) return;

        bool allCollected = true;
        foreach (string id in requiredItemsForDoor)
        {
            if (!collectedItems.Contains(id))
            {
                allCollected = false;
                break;
            }
        }

        if (allCollected)
        {
            UnlockDoor();
        }
    }

    private void UnlockDoor()
    {
        if (doorController != null)
        {
            doorController.UnlockDoor();
            doorUnlocked = true;
            Debug.Log("Door unlocked! (by collecting items)");
        }
        else
        {
            Debug.LogWarning("DoorController not found, cannot unlock.");
        }
    }

    private void ActivateScreen()
    {
        if (screenActivated) return;

        if (screenObject != null)
        {
            Renderer r = screenObject.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = Color.green;
                Debug.Log("Screen activated (green)");
            }
        }
        else
        {
            Debug.LogWarning("Screen object not found, cannot activate.");
        }
        screenActivated = true;
    }

    private void TriggerCupOnSeatClue()
    {
        cupClueTriggered = true;

        GameResultUI ui = GameResultUI.GetOrCreate();
        if (ui != null)
            ui.ShowMessage("You pick up the cup and find a note underneath: The first digit is 3!");

        InventoryManager inv = InventoryManager.GetOrCreate();
        if (inv != null)
        {
            InventoryItem clue = new InventoryItem("cup_clue_note", "Note under cup", "note", "The first digit is 3.");
            inv.AddItem(clue);
        }

        Debug.Log("Cup-on-seat clue triggered!");
    }

    private void TriggerNotebookOnDeskClue()
    {
        notebookClueTriggered = true;

        GameResultUI ui = GameResultUI.GetOrCreate();
        if (ui != null)
            ui.ShowMessage("You find a sticky note on the computer desk: The second digit is 1!");

        InventoryManager inv = InventoryManager.GetOrCreate();
        if (inv != null)
        {
            InventoryItem clue = new InventoryItem("notebook_clue_note", "Sticky Note", "note", "The second digit is 1.");
            inv.AddItem(clue);
        }

        Debug.Log("Notebook-on-desk clue triggered!");
    }

    // ---------- ∏®÷˙ ----------
    private string GetRootObjectId(GameObject obj)
    {
        if (obj == null) return null;

        ObjectIdentity id = obj.GetComponent<ObjectIdentity>();
        if (id != null && !string.IsNullOrWhiteSpace(id.ObjectId))
            return id.ObjectId;

        Transform current = obj.transform.parent;
        while (current != null)
        {
            id = current.GetComponent<ObjectIdentity>();
            if (id != null && !string.IsNullOrWhiteSpace(id.ObjectId))
                return id.ObjectId;
            current = current.parent;
        }
        return null;
    }

    private DoorController FindDoor()
    {
        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
        foreach (var identity in identities)
        {
            if (identity.ObjectId == doorId)
            {
                DoorController door = identity.GetComponent<DoorController>();
                if (door != null) return door;
            }
        }
        return FindObjectOfType<DoorController>();
    }

    private GameObject FindScreen()
    {
        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
        foreach (var identity in identities)
        {
            if (identity.ObjectId == screenId)
                return identity.gameObject;
        }
        return GameObject.Find(screenId);
    }
}