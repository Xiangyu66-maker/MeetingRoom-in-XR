using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Openable Cabinet")]
public sealed class OpenableCabinet : MonoBehaviour
{
    [SerializeField] private string doorChildName = "Cabinet Sliding Door";
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 0f, -0.72f);
    [SerializeField] private float interactionDistance = 2.4f;
    [SerializeField] private float openSpeed = 6f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool startOpen;

    private Transform door;
    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private bool targetOpen;
    private bool canInteract;
    private Camera cachedCamera;

    public bool IsOpen => targetOpen;

    private void Awake()
    {
        door = transform.Find(doorChildName);
        if (door == null)
        {
            door = FindDoorByName(transform);
        }

        if (door == null)
        {
            enabled = false;
            Debug.LogWarning($"{nameof(OpenableCabinet)} on {name} could not find a cabinet door child.");
            return;
        }

        closedLocalPosition = door.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
        targetOpen = startOpen;
        door.localPosition = targetOpen ? openLocalPosition : closedLocalPosition;
    }

    private void Update()
    {
        bool backpackOpen = IsBackpackOpen();
        canInteract = !backpackOpen && IsPlayerAimingAtCabinet();

        if (!backpackOpen && canInteract && Input.GetKeyDown(interactKey))
        {
            Toggle();
        }

        Vector3 targetPosition = targetOpen ? openLocalPosition : closedLocalPosition;
        float t = 1f - Mathf.Exp(-openSpeed * Time.deltaTime);
        door.localPosition = Vector3.Lerp(door.localPosition, targetPosition, t);

        if ((door.localPosition - targetPosition).sqrMagnitude < 0.0001f)
        {
            door.localPosition = targetPosition;
        }
    }

    public void Toggle()
    {
        NotifyGuidanceInteraction();
        targetOpen = !targetOpen;
        if (targetOpen)
        {
            TryCollectAttachedClueNote();
        }
    }

    private void TryCollectAttachedClueNote()
    {
        ClueNotePickup pickup = GetComponent<ClueNotePickup>();
        if (pickup == null)
        {
            pickup = GetComponentInParent<ClueNotePickup>();
        }

        if (pickup == null)
        {
            pickup = GetComponentInChildren<ClueNotePickup>();
        }

        if (pickup != null)
        {
            pickup.TryCollect();
        }
    }

    private bool IsPlayerAimingAtCabinet()
    {
        Camera playerCamera = GetPlayerCamera();
        if (playerCamera == null)
        {
            return false;
        }

        float distance = Vector3.Distance(playerCamera.transform.position, transform.position);
        if (distance > interactionDistance)
        {
            return false;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance))
        {
            return distance <= interactionDistance * 0.65f;
        }

        Transform hitTransform = hit.transform;
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private Camera GetPlayerCamera()
    {
        if (cachedCamera != null)
        {
            return cachedCamera;
        }

        cachedCamera = Camera.main;
        if (cachedCamera != null)
        {
            return cachedCamera;
        }

        Camera[] cameras = FindObjectsOfType<Camera>();
        cachedCamera = cameras.Length > 0 ? cameras[0] : null;
        return cachedCamera;
    }

    private static Transform FindDoorByName(Transform root)
    {
        foreach (Transform child in root)
        {
            if (child.name.ToLowerInvariant().Contains("door"))
            {
                return child;
            }

            Transform nested = FindDoorByName(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static bool IsBackpackOpen()
    {
        BackpackUI backpack = BackpackUI.Instance;
        return backpack != null && backpack.IsOpen;
    }

    private void NotifyGuidanceInteraction()
    {
        ObjectIdentity identity = GetComponent<ObjectIdentity>();
        string objectId = identity != null && !string.IsNullOrWhiteSpace(identity.ObjectId)
            ? identity.ObjectId
            : gameObject.name;

        InteractableObject.NotifyExternalInteraction(objectId, GetComponent<InteractableObject>());
    }

    private void OnGUI()
    {
        if (!canInteract)
        {
            return;
        }

        string label = targetOpen ? "Press E to close cabinet" : "Press E to pull cabinet open";
        GUI.Label(new Rect((Screen.width - 260f) * 0.5f, Screen.height - 86f, 260f, 32f), label);
    }
}
