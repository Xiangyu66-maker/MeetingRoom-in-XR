using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[ExecuteAlways]
[AddComponentMenu("Conference Room/Interaction Setup Helper")]
public sealed class InteractionSetupHelper : MonoBehaviour
{
    [SerializeField] private bool runOnAwake = true;
    [SerializeField] private bool runInEditMode = true;
    [SerializeField] private bool addFirstPersonInteractorToCamera = true;

    private static readonly HashSet<string> ImportantObjectIds = new HashSet<string>
    {
        "locked_door_01",
        "keypad_01",
        "whiteboard_01",
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
        "remote_01",
        "screen_01",
        "meeting_table_01",
        "cabinet_01",
        "document_01",
        "document_02",
        "desktop_computer_01",
        "plant_01",
        "plant_02",
        "plant_03",
        "plant_04",
        "cup_01",
        "cup_02",
    };

    private static readonly HashSet<string> GrabbableObjectIds = new HashSet<string>
    {
        "remote_01",
        "document_01",
        "document_02",
        "plant_01",
        "plant_02",
        "plant_03",
        "plant_04",
        "cup_01",
        "cup_02",

    };

    private static readonly Dictionary<string, Metadata> DefaultMetadata = new Dictionary<string, Metadata>
    {
        { "locked_door_01", new Metadata("puzzle_exit", "Locked meeting room exit. Inspecting it should establish that the player needs a keypad password.") },
        { "keypad_01", new Metadata("puzzle_input", "Door keypad. Interacting with it starts four-digit password input.") },
        { "whiteboard_01", new Metadata("puzzle_clue", "Whiteboard clue: symbols plus the instruction that meeting order decides the exit.") },
        { "seat_card_01", new Metadata("puzzle_clue", "Seat card clue: Seat 1 is circle.") },
        { "seat_card_02", new Metadata("puzzle_clue", "Seat card clue: Seat 2 is star.") },
        { "seat_card_03", new Metadata("puzzle_clue", "Seat card clue: Seat 3 is triangle.") },
        { "seat_card_04", new Metadata("puzzle_clue", "Seat card clue: Seat 4 is square.") },
        { "remote_01", new Metadata("puzzle_tool", "Presentation remote. For this first loop it logs remote use; later it should activate the screen clue.") },
        { "screen_01", new Metadata("puzzle_clue", "Projection screen where the symbol-number mapping appears after remote activation.") },
        { "meeting_table_01", new Metadata("furniture", "Main meeting table containing the seat-card clues.") },
        { "cabinet_01", new Metadata("furniture", "Openable cabinet for optional exploration.") },
        { "document_01", new Metadata("distractor", "Environmental document distractor on the meeting table.") },
        { "document_02", new Metadata("distractor", "Environmental document distractor on the meeting table.") },
        { "desktop_computer_01", new Metadata("distractor", "Desktop computer context object, not the main puzzle target.") },
        { "plant_01", new Metadata("distractor", "Decorative plant.") },
        { "plant_02", new Metadata("distractor", "Decorative plant.") },
        { "plant_03", new Metadata("distractor", "Decorative plant near the entry side.") },
        { "plant_04", new Metadata("distractor", "Small table plant distractor.") },
    };

    // TextMesh深度文字材质缓存
    private static readonly Dictionary<Font, Material> DepthTextMaterials = new Dictionary<Font, Material>();

    private void Awake()
    {
        if (Application.isPlaying && runOnAwake)
        {
            SetupSceneInteractions();
        }
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && runInEditMode)
        {
            SetupSceneInteractions();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplyTextDepthMaterialsAfterSceneLoad()
    {
        ApplyDepthTestedTextMaterials();
    }

    [ContextMenu("Setup Scene Interactions")]
    public void SetupSceneInteractions()
    {
        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
        Dictionary<string, ObjectIdentity> byId = new Dictionary<string, ObjectIdentity>();
        foreach (ObjectIdentity identity in identities)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.ObjectId))
                continue;

            byId[identity.ObjectId] = identity;
            ApplyMetadata(identity);
            EnsureInteractable(identity);
            EnsureColliderWhenSafe(identity);
            EnsureGrabbable(identity);
        }

        DoorController door = EnsureDoorController(byId);
        EnsureKeypadController(byId, door);
        ApplyDepthTestedTextMaterials();

        if (addFirstPersonInteractorToCamera)
        {
            EnsureCameraGuidanceComponents();
        }

        Debug.Log($"Interaction setup complete. Found {byId.Count} ObjectIdentity components.", this);
    }

    private static void ApplyMetadata(ObjectIdentity identity)
    {
        if (DefaultMetadata.TryGetValue(identity.ObjectId, out Metadata metadata))
        {
            identity.SetMetadataIfEmpty(metadata.Category, metadata.Description);
        }
    }

    private static void EnsureInteractable(ObjectIdentity identity)
    {
        if (!ImportantObjectIds.Contains(identity.ObjectId))
            return;

        if (identity.GetComponent<InteractableObject>() == null)
        {
            identity.gameObject.AddComponent<InteractableObject>();
            Debug.Log($"Added InteractableObject to {identity.ObjectId}.", identity);
        }
    }

    private static void EnsureColliderWhenSafe(ObjectIdentity identity)
    {
        if (!ImportantObjectIds.Contains(identity.ObjectId))
            return;

        if (identity.GetComponent<Collider>() != null || identity.GetComponentInChildren<Collider>() != null)
            return;

        if (identity.GetComponent<Renderer>() == null && identity.GetComponent<MeshFilter>() == null)
        {
            Debug.LogWarning($"No collider added to {identity.ObjectId}; object has no local renderer or mesh. Add a collider manually if this should be directly raycastable.", identity);
            return;
        }

        identity.gameObject.AddComponent<BoxCollider>();
        Debug.Log($"Added BoxCollider to {identity.ObjectId}.", identity);
    }

    // 确保可拾取物体挂载 GrabbableObject
    private static void EnsureGrabbable(ObjectIdentity identity)
    {
        if (!GrabbableObjectIds.Contains(identity.ObjectId))
            return;

        if (identity.GetComponent<GrabbableObject>() == null)
        {
            identity.gameObject.AddComponent<GrabbableObject>();
            Debug.Log($"Added GrabbableObject to {identity.ObjectId}.", identity);
        }
    }

    private static DoorController EnsureDoorController(Dictionary<string, ObjectIdentity> byId)
    {
        if (!byId.TryGetValue("locked_door_01", out ObjectIdentity doorIdentity))
        {
            Debug.LogWarning("Could not find locked_door_01 for DoorController setup.");
            return null;
        }

        DoorController door = doorIdentity.GetComponent<DoorController>();
        if (door == null)
        {
            door = doorIdentity.gameObject.AddComponent<DoorController>();
            Debug.Log("Added DoorController to locked_door_01.", doorIdentity);
        }
        return door;
    }

    private static void EnsureKeypadController(Dictionary<string, ObjectIdentity> byId, DoorController door)
    {
        if (!byId.TryGetValue("keypad_01", out ObjectIdentity keypadIdentity))
        {
            Debug.LogWarning("Could not find keypad_01 for KeypadController setup.");
            return;
        }

        KeypadController keypad = keypadIdentity.GetComponent<KeypadController>();
        if (keypad == null)
        {
            keypad = keypadIdentity.gameObject.AddComponent<KeypadController>();
            Debug.Log("Added KeypadController to keypad_01.", keypadIdentity);
        }
        keypad.ConfigureDoor(door);
    }

    #region TextMesh 深度透明材质处理
    private static void ApplyDepthTestedTextMaterials()
    {
        Shader textShader = Shader.Find("ConferenceRoom/World Text Depth");
        if (textShader == null)
        {
            Debug.LogWarning("World Text Depth shader not found. Scene TextMesh labels may render through objects.");
            return;
        }

        int updatedCount = 0;
        TextMesh[] textMeshes = FindSceneTextMeshes();
        foreach (TextMesh textMesh in textMeshes)
        {
            if (textMesh == null || textMesh.font == null)
                continue;

            MeshRenderer renderer = textMesh.GetComponent<MeshRenderer>();
            if (renderer == null)
                continue;

            textMesh.font.RequestCharactersInTexture(textMesh.text, textMesh.fontSize, textMesh.fontStyle);
            Material material = GetDepthTextMaterial(textMesh.font, textShader);
            if (material == null)
                continue;

            renderer.sharedMaterial = material;
            updatedCount++;
        }

        if (updatedCount > 0)
        {
            Debug.Log($"Applied depth-tested text material to {updatedCount} TextMesh labels.");
        }
    }

    private static TextMesh[] FindSceneTextMeshes()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<TextMesh>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        return FindObjectsOfType<TextMesh>(true);
#endif
    }

    private static Material GetDepthTextMaterial(Font font, Shader shader)
    {
        Texture fontTexture = font.material != null ? font.material.mainTexture : null;
        if (DepthTextMaterials.TryGetValue(font, out Material material) && material != null && material.shader == shader)
        {
            if (fontTexture != null && material.mainTexture != fontTexture)
            {
                material.mainTexture = fontTexture;
            }
            return material;
        }

        material = new Material(shader)
        {
            name = $"{font.name}_WorldTextDepth",
            hideFlags = HideFlags.HideAndDontSave,
            mainTexture = fontTexture,
            color = Color.white,
            renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest,
        };
        material.SetFloat("_Cutoff", 0.1f);
        DepthTextMaterials[font] = material;
        return material;
    }
    #endregion

    private static void EnsureCameraGuidanceComponents()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            targetCamera = cameras.Length > 0 ? cameras[0] : null;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("No camera found for guidance setup.");
            return;
        }

        if (targetCamera.GetComponent<FirstPersonInteractor>() == null)
        {
            targetCamera.gameObject.AddComponent<FirstPersonInteractor>();
            Debug.Log($"Added FirstPersonInteractor to camera {targetCamera.name}.", targetCamera);
        }

        if (targetCamera.GetComponent<CameraPromptSender>() == null)
        {
            targetCamera.gameObject.AddComponent<CameraPromptSender>();
            Debug.Log($"Added CameraPromptSender to camera {targetCamera.name}.", targetCamera);
        }

        if (targetCamera.GetComponent<MeetingRoomAdaptiveGuide>() == null)
        {
            targetCamera.gameObject.AddComponent<MeetingRoomAdaptiveGuide>();
            Debug.Log($"Added MeetingRoomAdaptiveGuide to camera {targetCamera.name}.", targetCamera);
        }
    }

    private struct Metadata
    {
        public Metadata(string category, string description)
        {
            Category = category;
            Description = description;
        }
        public string Category { get; }
        public string Description { get; }
    }
}
