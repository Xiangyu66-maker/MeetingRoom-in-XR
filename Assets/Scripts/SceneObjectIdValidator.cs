using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Conference Room/Scene Object ID Validator")]
public sealed class SceneObjectIdValidator : MonoBehaviour
{
    private static readonly string[] RequiredObjectIds =
    {
        "locked_door_01",
        "keypad_01",
        "whiteboard_01",
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
        "remote_01",
        "screen_01",
        "meeting_table_01",
        "cabinet_01",
        "player_start",
        "victory_exit_point",
    };

    [ContextMenu("Validate Puzzle Object IDs")]
    public void ValidatePuzzleObjectIds()
    {
        HashSet<string> foundIds = new HashSet<string>();

#if UNITY_2020_1_OR_NEWER
        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>(true);
#else
        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
#endif

        foreach (ObjectIdentity identity in identities)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.ObjectId))
            {
                continue;
            }

            foundIds.Add(identity.ObjectId);
        }

        bool hasMissingId = false;
        foreach (string requiredId in RequiredObjectIds)
        {
            if (foundIds.Contains(requiredId))
            {
                continue;
            }

            hasMissingId = true;
            Debug.LogWarning($"Missing required puzzle object_id: {requiredId}", this);
        }

        if (!hasMissingId)
        {
            Debug.Log("All required meeting-room puzzle object IDs are present.", this);
        }
    }

    private void Start()
    {
        ValidatePuzzleObjectIds();
    }
}
