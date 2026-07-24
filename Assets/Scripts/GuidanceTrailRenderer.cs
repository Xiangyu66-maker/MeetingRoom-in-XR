using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Guidance Trail Renderer")]
public sealed class GuidanceTrailRenderer : MonoBehaviour
{
    [SerializeField] private Color trailColor = new Color(0.15f, 0.9f, 1f, 0.9f);
    [SerializeField] private float lineWidth = 0.08f;
    [SerializeField] private float footHeightOffset = 0.05f;
    [SerializeField] private float targetHeightOffset = 0.08f;

    private readonly List<Transform> targetTransforms = new List<Transform>();
    private LineRenderer lineRenderer;
    private Transform playerTransform;
    private Material runtimeMaterial;
    private bool trailVisible;

    private void Awake()
    {
        EnsureLineRenderer();
        HideTrail();
    }

    private void Update()
    {
        if (trailVisible)
        {
            UpdateTrail();
        }
    }

    public void ShowTrail(Transform player, List<Transform> targets)
    {
        playerTransform = player;
        targetTransforms.Clear();

        if (targets != null)
        {
            foreach (Transform target in targets)
            {
                if (target != null)
                {
                    targetTransforms.Add(target);
                }
            }
        }

        if (playerTransform == null || targetTransforms.Count == 0)
        {
            Debug.LogWarning("Cannot show guidance trail because player or target transforms are missing.", this);
            return;
        }

        EnsureLineRenderer();
        trailVisible = true;
        lineRenderer.enabled = true;
        UpdateTrail();
    }

    public void HideTrail()
    {
        trailVisible = false;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    public void UpdateTrail()
    {
        if (lineRenderer == null || playerTransform == null || targetTransforms.Count == 0)
        {
            HideTrail();
            return;
        }

        Vector3 start = playerTransform.position + Vector3.up * footHeightOffset;
        Vector3 end = CalculateTargetCenter() + Vector3.up * targetHeightOffset;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    private Vector3 CalculateTargetCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        foreach (Transform target in targetTransforms)
        {
            if (target == null)
            {
                continue;
            }

            sum += target.position;
            count++;
        }

        if (count == 0)
        {
            return transform.position;
        }

        return sum / count;
    }

    private void EnsureLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.numCornerVertices = 4;
        lineRenderer.numCapVertices = 4;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.shadowCastingMode = ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        if (runtimeMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            runtimeMaterial = new Material(shader)
            {
                name = "SeatCardGuidanceTrail_Material",
                hideFlags = HideFlags.HideAndDontSave,
                color = trailColor,
            };
        }

        lineRenderer.sharedMaterial = runtimeMaterial;
        lineRenderer.startColor = trailColor;
        lineRenderer.endColor = trailColor;
    }
}
