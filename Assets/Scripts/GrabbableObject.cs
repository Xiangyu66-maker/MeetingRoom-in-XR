using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Grabbable Object")]
public sealed class GrabbableObject : MonoBehaviour
{
    [Header("Hold Settings")]
    [SerializeField] private Vector3 holdOffset = new Vector3(0f, -0.2f, 0.8f);
    [SerializeField] private Vector3 holdRotation = Vector3.zero;

    [Header("Placement Settings")]
    [SerializeField] private float forwardRayDistance = 3.0f;
    [SerializeField] private float downRayDistance = 5.0f;
    [SerializeField] private float extraHeightOffset = 0.0f;
    [SerializeField] private float manualVerticalOffset = 0.0f;

    [Header("Debug")]
    [SerializeField] private bool drawDebugRays = true;

    private bool isHeld;
    private Transform playerCamera;
    private Collider[] colliders;
    private Rigidbody rb;
    private InteractableObject interactable;
    private float objectHalfHeight;

    private void Awake()
    {
        colliders = GetComponents<Collider>();
        rb = GetComponent<Rigidbody>();
        interactable = GetComponent<InteractableObject>();
        CalculateObjectHeight();
    }

    private void CalculateObjectHeight()
    {
        float totalHeight = 0.4f;
        if (colliders != null && colliders.Length > 0)
        {
            Bounds combined = colliders[0].bounds;
            foreach (var c in colliders)
                combined.Encapsulate(c.bounds);
            totalHeight = combined.size.y;
        }
        else if (TryGetComponent<Renderer>(out var renderer))
        {
            totalHeight = renderer.bounds.size.y;
        }
        objectHalfHeight = totalHeight * 0.5f;
        Debug.Log($"Object half height = {objectHalfHeight}");
    }

    public void Grab(Transform cameraTransform)
    {
        if (isHeld) return;

        isHeld = true;
        playerCamera = cameraTransform;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var col in colliders)
            col.enabled = false;

        transform.SetParent(cameraTransform);
        transform.localPosition = holdOffset;
        transform.localRotation = Quaternion.Euler(holdRotation);

        if (interactable != null)
            interactable.enabled = false;

        Debug.Log($"Grabbed {gameObject.name}");
    }

    public void Drop()
    {
        if (!isHeld) return;

        transform.SetParent(null);
        bool placed = false;
        Vector3 dropPosition = transform.position;

        // 1. 向前检测
        if (playerCamera != null)
        {
            Ray forwardRay = new Ray(playerCamera.position, playerCamera.forward);
            if (drawDebugRays)
                Debug.DrawRay(forwardRay.origin, forwardRay.direction * forwardRayDistance, Color.green, 2f);

            RaycastHit[] hits = Physics.RaycastAll(forwardRay, forwardRayDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            RaycastHit? bestHit = GetFirstValidHit(hits);
            if (bestHit.HasValue)
            {
                dropPosition = bestHit.Value.point + Vector3.up * (objectHalfHeight + extraHeightOffset);
                placed = true;
                Debug.Log($"Forward placement on {bestHit.Value.collider.name} at {dropPosition}");
            }
        }

        // 2. 向下检测（如果向前失败）
        if (!placed)
        {
            Vector3 downOrigin = transform.position + Vector3.up * 0.5f;
            Ray downRay = new Ray(downOrigin, Vector3.down);
            if (drawDebugRays)
                Debug.DrawRay(downOrigin, Vector3.down * downRayDistance, Color.blue, 2f);

            RaycastHit[] hits = Physics.RaycastAll(downRay, downRayDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            RaycastHit? bestHit = GetFirstValidHit(hits);
            if (bestHit.HasValue)
            {
                dropPosition = bestHit.Value.point + Vector3.up * (objectHalfHeight + extraHeightOffset);
                placed = true;
                Debug.Log($"Downward placement on {bestHit.Value.collider.name} at {dropPosition}");
            }
        }

        // 3. 无表面，保持抓取
        if (!placed)
        {
            Debug.LogWarning("No valid surface found. Keeping object held.");
            isHeld = true;
            transform.SetParent(playerCamera);
            transform.localPosition = holdOffset;
            transform.localRotation = Quaternion.Euler(holdRotation);
            return;
        }

        // 4. 应用粗略位置
        transform.position = dropPosition;
        CalculateObjectHeight(); // 重新计算确保准确

        // 5. 微调：从中心向下检测，精确贴合
        Vector3 adjustOrigin = transform.position + Vector3.up * 0.5f;
        Ray adjustRay = new Ray(adjustOrigin, Vector3.down);
        if (drawDebugRays)
            Debug.DrawRay(adjustOrigin, Vector3.down * downRayDistance, Color.yellow, 2f);

        RaycastHit[] adjustHits = Physics.RaycastAll(adjustRay, downRayDistance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        RaycastHit? adjustHit = GetFirstValidHit(adjustHits);
        if (adjustHit.HasValue)
        {
            float newY = adjustHit.Value.point.y + objectHalfHeight + extraHeightOffset + manualVerticalOffset;
            Vector3 adjustedPos = new Vector3(transform.position.x, newY, transform.position.z);
            transform.position = adjustedPos;
            Debug.Log($"Adjusted placement to {adjustedPos} (hit {adjustHit.Value.collider.name})");
        }
        else
        {
            Debug.LogWarning("Could not find surface below object, placement may be approximate.");
        }

        // 保持竖直（仅Y轴旋转）
        Vector3 euler = transform.rotation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

        // 恢复物理与碰撞
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        foreach (var col in colliders)
            col.enabled = true;

        if (interactable != null)
            interactable.enabled = true;

        playerCamera = null;
        isHeld = false;
        Debug.Log($"Dropped {gameObject.name} at {transform.position}");

        // ---------- 触发放置事件 ----------
        GameObject surface = GetSurfaceBelow();
        // 如果 surface 为 null，尝试从物体正下方检测（更宽松）
        if (surface == null)
        {
            Ray fallbackRay = new Ray(transform.position, Vector3.down);
            if (Physics.Raycast(fallbackRay, out RaycastHit fallbackHit, 1.0f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!fallbackHit.collider.transform.IsChildOf(transform))
                    surface = fallbackHit.collider.gameObject;
            }
        }
        if (PuzzleEventManager.Instance != null)
        {
            string id = GetComponent<ObjectIdentity>()?.ObjectId ?? gameObject.name;
            PuzzleEventManager.Instance.NotifyItemDropped(id, surface);
            Debug.Log($"GrabbableObject: Triggered drop event for {id} on {(surface != null ? surface.name : "null")}");
        }
        else
        {
            Debug.LogWarning("PuzzleEventManager.Instance is null, cannot trigger drop event.");
        }
    }

    private GameObject GetSurfaceBelow()
    {
        // 从物体底部发射射线
        Vector3 origin = transform.position - Vector3.up * objectHalfHeight;
        Ray ray = new Ray(origin + Vector3.up * 0.01f, Vector3.down);
        float maxDistance = 0.5f;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.transform.IsChildOf(transform))
                return null;
            return hit.collider.gameObject;
        }
        return null;
    }


    private RaycastHit? GetFirstValidHit(RaycastHit[] hits)
    {
        if (hits.Length == 0) return null;

        HashSet<int> selfColliderIDs = new HashSet<int>();
        foreach (var c in colliders)
            selfColliderIDs.Add(c.GetInstanceID());

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (selfColliderIDs.Contains(hit.collider.GetInstanceID()))
                continue;
            if (hit.collider.transform.IsChildOf(transform))
                continue;
            return hit;
        }
        return null;
    }

    public bool IsHeld => isHeld;
}