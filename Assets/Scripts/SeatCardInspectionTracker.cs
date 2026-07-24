using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Seat Card Inspection Tracker")]
public sealed class SeatCardInspectionTracker : MonoBehaviour
{
    [SerializeField]
    private string[] targetSeatCardIds =
    {
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
    };

    private readonly HashSet<string> inspectedIds = new HashSet<string>();
    private readonly HashSet<string> targetIdSet = new HashSet<string>();

    public string[] TargetSeatCardIds => targetSeatCardIds;

    private void Awake()
    {
        RebuildTargetSet();
    }

    private void OnValidate()
    {
        if (targetSeatCardIds == null || targetSeatCardIds.Length == 0)
        {
            targetSeatCardIds = new[]
            {
                "seat_card_01",
                "seat_card_02",
                "seat_card_03",
                "seat_card_04",
            };
        }
    }

    public void ConfigureTargets(IEnumerable<string> targetIds)
    {
        List<string> cleanedIds = new List<string>();
        foreach (string targetId in targetIds)
        {
            if (!string.IsNullOrWhiteSpace(targetId) && !cleanedIds.Contains(targetId))
            {
                cleanedIds.Add(targetId);
            }
        }

        if (cleanedIds.Count > 0)
        {
            targetSeatCardIds = cleanedIds.ToArray();
        }

        RebuildTargetSet();
    }

    public bool MarkSeatCardInspected(string objectId)
    {
        RebuildTargetSet();

        if (!targetIdSet.Contains(objectId))
        {
            return false;
        }

        if (!inspectedIds.Add(objectId))
        {
            return false;
        }

        Debug.Log($"Seat card inspected and tracked: {objectId}", this);
        return true;
    }

    public bool HasInspectedAnySeatCard()
    {
        return inspectedIds.Count > 0;
    }

    public bool HasInspectedAllSeatCards()
    {
        RebuildTargetSet();

        if (targetIdSet.Count == 0)
        {
            return false;
        }

        foreach (string targetId in targetIdSet)
        {
            if (!inspectedIds.Contains(targetId))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsTargetSeatCard(string objectId)
    {
        RebuildTargetSet();
        return targetIdSet.Contains(objectId);
    }

    private void RebuildTargetSet()
    {
        targetIdSet.Clear();

        if (targetSeatCardIds == null)
        {
            return;
        }

        foreach (string targetId in targetSeatCardIds)
        {
            if (!string.IsNullOrWhiteSpace(targetId))
            {
                targetIdSet.Add(targetId);
            }
        }
    }
}
