using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Inventory Manager")]
public sealed class InventoryManager : MonoBehaviour
{
    private readonly List<InventoryItem> items = new List<InventoryItem>();

    public static InventoryManager Instance { get; private set; }

    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate InventoryManager found. Disabling duplicate.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static InventoryManager GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        InventoryManager existing = FindInventoryManager();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject managerObject = new GameObject("Inventory Manager");
        return managerObject.AddComponent<InventoryManager>();
    }

    public bool AddItem(InventoryItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.itemId))
        {
            Debug.LogWarning("Cannot add inventory item because item or itemId is empty.", this);
            return false;
        }

        if (HasItem(item.itemId))
        {
            Debug.Log($"Item already collected: {item.itemName}", this);
            return false;
        }

        items.Add(item);
        Debug.Log($"Collected item: {item.itemName}", this);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId)
    {
        return FindItem(itemId) != null;
    }

    public List<InventoryItem> GetItems()
    {
        return new List<InventoryItem>(items);
    }

    public void MarkItemRead(string itemId)
    {
        InventoryItem item = FindItem(itemId);
        if (item == null || item.hasBeenRead)
        {
            return;
        }

        item.hasBeenRead = true;
        InventoryChanged?.Invoke();
    }

    public bool HasUnreadItems()
    {
        foreach (InventoryItem item in items)
        {
            if (item != null && !item.hasBeenRead)
            {
                return true;
            }
        }

        return false;
    }

    private InventoryItem FindItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        foreach (InventoryItem item in items)
        {
            if (item != null && item.itemId == itemId)
            {
                return item;
            }
        }

        return null;
    }

    private static InventoryManager FindInventoryManager()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<InventoryManager>();
#else
        return FindObjectOfType<InventoryManager>();
#endif
    }
}
