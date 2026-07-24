using System;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Interactable Object")]
public sealed class InteractableObject : MonoBehaviour
{
    public static event Action<string, InteractableObject> Interacted;

    [SerializeField] private ObjectIdentity identity;
    [TextArea]
    [SerializeField] private string fallbackDescription;

    public ObjectIdentity Identity
    {
        get
        {
            if (identity == null)
            {
                identity = GetComponent<ObjectIdentity>();
            }

            return identity;
        }
    }

    public string ObjectId => Identity != null ? Identity.ObjectId : gameObject.name;
    public string Description => GetDescription();

    public static void NotifyExternalInteraction(string objectId, InteractableObject source = null)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return;
        }

        Interacted?.Invoke(objectId, source);
    }

    public void Interact()
    {
        string objectId = ObjectId;
        string description = GetDescription();
        Debug.Log($"Interacted with: {objectId} | {description}", this);
        NotifyExternalInteraction(objectId, this);
        if (GptVisionInteractionManager.Instance != null)
        {
            GptVisionInteractionManager.Instance.AnalyzeObject(gameObject, objectId, description);
        }
        MeetingRoomAdaptiveGuide.NotifyObjectInteracted(objectId);

        switch (objectId)
        {
            case "keypad_01":
                KeypadController keypad = GetComponent<KeypadController>();
                if (keypad == null)
                {
                    keypad = GetComponentInParent<KeypadController>();
                }

                if (keypad != null)
                {
                    keypad.BeginInputMode();
                }
                else
                {
                    Debug.LogWarning("keypad_01 was interacted with, but no KeypadController was found.", this);
                }
                break;

            case "remote_01":
                // Trigger hook: screen activated.
                Debug.Log("Remote used. TODO: connect this to screen clue activation.", this);
                break;

            case "whiteboard_01":
                // Trigger hook: whiteboard observed.
                Debug.Log("Whiteboard clue observed.", this);
                break;

            case "seat_card_01":
            case "seat_card_02":
            case "seat_card_03":
            case "seat_card_04":
                // Trigger hook: seat cards observed.
                Debug.Log($"Seat card observed: {objectId}", this);
                NotifySeatCardGuidanceSystem(objectId);
                break;

            case "locked_door_01":
                // Trigger hook: door inspected.
                Debug.Log("Locked door inspected.", this);
                break;

            case "cabinet_01":
                Debug.Log("Cabinet interacted. Checking for clue note pickup.", this);
                break;
        }

        TryCollectAttachedClueNote();
    }

    private string GetDescription()
    {
        ObjectIdentity objectIdentity = Identity;
        if (objectIdentity != null && !string.IsNullOrWhiteSpace(objectIdentity.Description))
        {
            return objectIdentity.Description;
        }

        if (!string.IsNullOrWhiteSpace(fallbackDescription))
        {
            return fallbackDescription;
        }

        return GetDefaultDescription(ObjectId);
    }

    private static string GetDefaultDescription(string objectId)
    {
        switch (objectId)
        {
            case "locked_door_01":
                return "Locked meeting room exit.";
            case "keypad_01":
                return "Door keypad for entering the four-digit password.";
            case "whiteboard_01":
                return "Whiteboard clue with symbol order instruction.";
            case "seat_card_01":
                return "Seat card clue: Seat 1 is circle.";
            case "seat_card_02":
                return "Seat card clue: Seat 2 is star.";
            case "seat_card_03":
                return "Seat card clue: Seat 3 is triangle.";
            case "seat_card_04":
                return "Seat card clue: Seat 4 is square.";
            case "remote_01":
                return "Presentation remote for the screen clue.";
            case "screen_01":
                return "Projection screen clue location.";
            case "meeting_table_01":
                return "Main meeting table.";
            case "cabinet_01":
                return "Openable cabinet.";
            case "document_01":
            case "document_02":
                return "Meeting room document distractor.";
            case "desktop_computer_01":
                return "Desktop computer context object.";
            case "plant_01":
            case "plant_02":
            case "plant_03":
            case "plant_04":
                return "Decorative plant.";
            default:
                return string.IsNullOrWhiteSpace(objectId) ? "Interactable object." : objectId;
        }
    }

    private void Reset()
    {
        identity = GetComponent<ObjectIdentity>();
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

    private static void NotifySeatCardGuidanceSystem(string objectId)
    {
        SeatCardGuidanceManager manager = FindSeatCardGuidanceManager();
        SeatCardInspectionTracker tracker = FindSeatCardInspectionTracker();

        if (manager == null || tracker == null)
        {
            manager = SeatCardGuidanceSetupHelper.SetupGuidanceSystem();
            tracker = FindSeatCardInspectionTracker();
        }

        if (tracker != null)
        {
            tracker.MarkSeatCardInspected(objectId);
        }

        if (manager != null)
        {
            manager.NotifySeatCardInspected(objectId);
        }
    }

    private static SeatCardGuidanceManager FindSeatCardGuidanceManager()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<SeatCardGuidanceManager>();
#else
        return FindObjectOfType<SeatCardGuidanceManager>();
#endif
    }

    private static SeatCardInspectionTracker FindSeatCardInspectionTracker()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<SeatCardInspectionTracker>();
#else
        return FindObjectOfType<SeatCardInspectionTracker>();
#endif
    }
}

