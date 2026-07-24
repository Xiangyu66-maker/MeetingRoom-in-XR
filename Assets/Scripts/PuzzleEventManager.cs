using System;
using UnityEngine;

public class PuzzleEventManager : MonoBehaviour
{
    public static PuzzleEventManager Instance { get; private set; }

    public event Action<string> OnItemGrabbed;
    public event Action<string, GameObject> OnItemDropped;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("PuzzleEventManager");
            Instance = go.AddComponent<PuzzleEventManager>();
            DontDestroyOnLoad(go);
            Debug.Log("PuzzleEventManager auto-created.");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void NotifyItemGrabbed(string objectId)
    {
        OnItemGrabbed?.Invoke(objectId);
        Debug.Log($"PuzzleEvent: Item grabbed [{objectId}]");
    }

    public void NotifyItemDropped(string objectId, GameObject surface)
    {
        OnItemDropped?.Invoke(objectId, surface);
        Debug.Log($"PuzzleEvent: Item dropped [{objectId}] on [{surface?.name ?? "null"}]");
    }
}