using System;

[Serializable]
public sealed class InventoryItem
{
    public string itemId;
    public string itemName;
    public string itemType;
    public string content;
    public bool hasBeenRead;

    public InventoryItem()
    {
    }

    public InventoryItem(string itemId, string itemName, string itemType, string content, bool hasBeenRead = false)
    {
        this.itemId = itemId;
        this.itemName = itemName;
        this.itemType = itemType;
        this.content = content;
        this.hasBeenRead = hasBeenRead;
    }
}
