using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Simple Object Highlighter")]
public sealed class SimpleObjectHighlighter : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color emissionColor = new Color(1f, 0.6f, 0.05f, 1f);

    private readonly Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private readonly List<Material> temporaryMaterials = new List<Material>();

    public void HighlightObjects(List<GameObject> objects)
    {
        ClearHighlight();

        if (objects == null)
        {
            return;
        }

        foreach (GameObject target in objects)
        {
            if (target == null)
            {
                continue;
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || renderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                Material[] sourceMaterials = renderer.sharedMaterials;
                if (sourceMaterials == null || sourceMaterials.Length == 0)
                {
                    continue;
                }

                originalMaterials[renderer] = sourceMaterials;
                Material[] highlightedMaterials = new Material[sourceMaterials.Length];

                for (int i = 0; i < sourceMaterials.Length; i++)
                {
                    Material source = sourceMaterials[i];
                    if (source == null)
                    {
                        highlightedMaterials[i] = null;
                        continue;
                    }

                    Material copy = new Material(source)
                    {
                        name = $"{source.name}_SeatCardHighlight",
                        hideFlags = HideFlags.HideAndDontSave,
                    };

                    ApplyHighlightColor(copy);
                    temporaryMaterials.Add(copy);
                    highlightedMaterials[i] = copy;
                }

                renderer.sharedMaterials = highlightedMaterials;
            }
        }
    }

    public void ClearHighlight()
    {
        foreach (KeyValuePair<Renderer, Material[]> entry in originalMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }

        originalMaterials.Clear();

        foreach (Material material in temporaryMaterials)
        {
            if (material == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(material);
            }
            else
            {
                DestroyImmediate(material);
            }
        }

        temporaryMaterials.Clear();
    }

    private void OnDisable()
    {
        ClearHighlight();
    }

    private void ApplyHighlightColor(Material material)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", highlightColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", highlightColor);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emissionColor);
        }
    }
}
