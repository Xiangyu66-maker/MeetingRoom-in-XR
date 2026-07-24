using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("Conference Room/Victory Exit Trigger")]
public sealed class VictoryExitTrigger : MonoBehaviour
{
    [SerializeField] private GameStateManager manager;

    private void Reset()
    {
        EnsureTriggerCollider();
    }

    private void Awake()
    {
        EnsureTriggerCollider();
        ResolveManager();
    }

    private void OnValidate()
    {
        EnsureTriggerCollider();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsLikelyPlayer(other))
        {
            return;
        }

        ResolveManager();
        if (manager == null)
        {
            Debug.LogWarning("Victory exit reached, but no GameStateManager was found.", this);
            return;
        }

        manager.TryTriggerSuccess();
    }

    private void ResolveManager()
    {
        if (manager == null)
        {
            manager = FindObjectOfType<GameStateManager>();
        }
    }

    private void EnsureTriggerCollider()
    {
        BoxCollider triggerCollider = GetComponent<BoxCollider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private static bool IsLikelyPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (other.CompareTag("Player"))
        {
            return true;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Transform otherRoot = other.transform.root;
            Transform cameraRoot = mainCamera.transform.root;
            if (otherRoot == cameraRoot || other.transform == mainCamera.transform || mainCamera.transform.IsChildOf(other.transform))
            {
                return true;
            }
        }

        // Fallback for controller prefabs that are not tagged as Player yet.
        string rootName = other.transform.root.name.ToLowerInvariant();
        return rootName.Contains("player") || rootName.Contains("fps") || rootName.Contains("firstperson");
    }
}
