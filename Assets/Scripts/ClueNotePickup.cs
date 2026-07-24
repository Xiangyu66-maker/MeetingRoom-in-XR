using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Clue Note Pickup")]
public sealed class ClueNotePickup : MonoBehaviour
{
    [SerializeField] private string itemId = "note_first_digit";
    [SerializeField] private string itemName = "Cabinet Note";
    [SerializeField] private string itemType = "note";
    [TextArea]
    [SerializeField] private string content = "The first digit of the password is 3.";

    public void Configure(string newItemId, string newItemName, string newItemType, string newContent)
    {
        itemId = newItemId;
        itemName = newItemName;
        itemType = newItemType;
        content = newContent;
    }

    public bool TryCollect()
    {
        InventoryManager inventory = InventoryManager.GetOrCreate();
        if (inventory == null)
        {
            Debug.LogWarning("Could not collect clue note because no InventoryManager is available.", this);
            return false;
        }

        InventoryItem item = new InventoryItem(itemId, itemName, itemType, content, false);
        bool added = inventory.AddItem(item);
        if (added)
        {
            GameResultUI notificationUI = GameResultUI.GetOrCreate();
            if (notificationUI != null)
            {
                notificationUI.ShowClueFound();
            }
        }

        return added;
    }
}
