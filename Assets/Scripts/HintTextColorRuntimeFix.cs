using System.Collections.Generic;
using TMPro;
using UnityEngine;

public static class HintTextColorRuntimeFix
{
    private const string RedColorTag = "<color=#FF0000>";
    private static readonly int FaceColorShaderId = Shader.PropertyToID("_FaceColor");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyHintKeywordColors()
    {
        var materialCache = new Dictionary<Material, Material>();
        var textObjects = Object.FindObjectsOfType<TextMeshPro>(true);

        foreach (var textObject in textObjects)
        {
            if (textObject == null || string.IsNullOrEmpty(textObject.text))
            {
                continue;
            }

            if (!textObject.text.Contains(RedColorTag))
            {
                continue;
            }

            textObject.richText = true;
            textObject.color = Color.black;

            var sourceMaterial = textObject.fontSharedMaterial;
            if (sourceMaterial != null)
            {
                if (!materialCache.TryGetValue(sourceMaterial, out var richTextMaterial))
                {
                    richTextMaterial = new Material(sourceMaterial)
                    {
                        name = $"{sourceMaterial.name} Rich Keyword Runtime"
                    };
                    richTextMaterial.SetColor(FaceColorShaderId, Color.white);
                    materialCache[sourceMaterial] = richTextMaterial;
                }

                textObject.fontSharedMaterial = richTextMaterial;
            }

            textObject.ForceMeshUpdate();
        }
    }
}
