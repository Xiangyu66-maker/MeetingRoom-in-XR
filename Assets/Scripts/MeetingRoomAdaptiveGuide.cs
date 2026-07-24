using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("AI/Meeting Room Adaptive Guide")]
public sealed class MeetingRoomAdaptiveGuide : MonoBehaviour
{
    public enum PuzzleStage
    {
        FindWhiteboard,
        SearchSeatClues,
        UseKeypad,
        EnterPassword,
        Completed,
    }

    private enum InterventionMode
    {
        None,
        TrustUnity,
        LightInteractionHint,
        HighlightAndDirectionHint,
        OperationHint,
    }

    [Serializable]
    public sealed class GuideObjectState
    {
        public string id;
        public string name;
        public string type;
        public string role;
        public string state;
        public Vector3 position;
    }

    [Serializable]
    public sealed class VisionDiagnosis
    {
        public string[] visible_objects;
        public string likely_current_focus;
        public bool target_visible;
        public float target_confidence;
        public string visual_stage_hint;
        public string evidence;
    }

    [Header("References")]
    [SerializeField] private CameraPromptSender promptSender;
    [SerializeField] private Transform playerTransform;

    [Header("Adaptive Timing")]
    [SerializeField] private float stuckSeconds = 20f;
    [SerializeField] private bool requestAiOnStageChange = true;
    [SerializeField] private bool requestAiWhenStuck = true;
    [SerializeField] private float repeatAiRequestSeconds = 20f;

    [Header("Vision Fusion")]
    [SerializeField] private bool useVisionFusion = true;
    [SerializeField, Range(0f, 1f)] private float highVisionConfidence = 0.75f;
    [SerializeField] private float recentVisionSeconds = 12f;

    [Header("Visual Guidance")]
    [SerializeField] private bool enableHighlight = true;
    [SerializeField] private Color highlightColor = new Color(1f, 0.82f, 0.18f, 1f);
    [SerializeField] private Color operationHintColor = new Color(0.35f, 0.9f, 1f, 1f);
    [SerializeField] private float highlightPulseSpeed = 3f;
    [SerializeField] private bool showDebugOverlay = true;

    private static readonly string[] SeatCardIds =
    {
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
    };

    private static readonly string[] TrackedObjectIds =
    {
        "whiteboard_01",
        "seat_card_01",
        "seat_card_02",
        "seat_card_03",
        "seat_card_04",
        "chair_01",
        "chair_02",
        "chair_03",
        "chair_04",
        "chair_05",
        "chair_06",
        "chair_07",
        "chair_08",
        "chair_09",
        "chair_10",
        "keypad_01",
        "locked_door_01",
        "victory_exit_point",
    };

    private static MeetingRoomAdaptiveGuide activeGuide;

    private readonly Dictionary<string, ObjectIdentity> objectsById = new Dictionary<string, ObjectIdentity>();
    private readonly HashSet<string> interactedObjectIds = new HashSet<string>();
    private readonly List<Renderer> activeHighlightRenderers = new List<Renderer>();

    private MaterialPropertyBlock highlightBlock;
    private VisionDiagnosis latestVisionDiagnosis;
    private InterventionMode currentInterventionMode = InterventionMode.None;
    private string highlightedTargetId;
    private PuzzleStage currentStage = PuzzleStage.FindWhiteboard;
    private string currentTargetId = "whiteboard_01";
    private float stageStartedAt;
    private float lastAiRequestAt = -999f;
    private float lastObjectCacheAt = -999f;
    private float latestVisionReceivedAt = -999f;
    private int failedKeypadAttempts;
    private bool keypadInputStarted;
    private bool doorUnlocked;
    private string latestAiInstruction;
    private string latestVisionText;
    private string latestFusionHint;

    public PuzzleStage CurrentStage => currentStage;
    public string CurrentTargetId => currentTargetId;
    public int FailedKeypadAttempts => failedKeypadAttempts;

    private void Awake()
    {
        activeGuide = this;
        ResolveReferences();
        RebuildObjectCache();
        UpdateStage(false);
    }

    private void OnEnable()
    {
        activeGuide = this;
    }

    private void OnDisable()
    {
        if (activeGuide == this)
        {
            activeGuide = null;
        }

        ClearHighlight();
    }

    private void Update()
    {
        if (Time.time - lastObjectCacheAt > 2f)
        {
            RebuildObjectCache();
        }

        UpdateStage(true);
        ApplyVisionFusion();
        UpdateHighlight();
        RequestAiWhenPlayerIsStuck();
    }

    public static void NotifyObjectInteracted(string objectId)
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.RegisterObjectInteraction(objectId);
        }
    }

    public static void NotifyKeypadInputStarted()
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.keypadInputStarted = true;
            guide.UpdateStage(true);
        }
    }

    public static void NotifyPasswordRejected()
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.failedKeypadAttempts++;
            guide.keypadInputStarted = false;
            guide.UpdateStage(true);
            guide.RequestAiIfReady("wrong password attempt");
        }
    }

    public static void NotifyPasswordAccepted()
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.keypadInputStarted = false;
            guide.doorUnlocked = true;
            guide.UpdateStage(true);
            guide.RequestAiIfReady("password accepted");
        }
    }

    public static void NotifyDoorUnlocked()
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.doorUnlocked = true;
            guide.UpdateStage(true);
        }
    }

    public static void NotifyAiInstruction(string instruction, string visionText, VisionDiagnosis visionDiagnosis = null)
    {
        MeetingRoomAdaptiveGuide guide = FindActiveGuide();
        if (guide != null)
        {
            guide.latestAiInstruction = instruction;
            guide.latestVisionText = visionText;
            guide.latestVisionDiagnosis = visionDiagnosis;
            guide.latestVisionReceivedAt = Time.time;
            guide.ApplyVisionFusion();
        }
    }

    public string BuildAiTaskContext(string baseTask)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(baseTask);
        builder.AppendLine();
        builder.AppendLine("Known Meeting-Room escape task flow for diagnosis:");
        builder.AppendLine("1. The player is locked in the meeting room and needs the door password.");
        builder.AppendLine("2. The whiteboard_01 clue should be inspected first; it tells the player that the meeting/seat order matters.");
        builder.AppendLine("3. The player should search the chair and seat-card area for password clues, especially seat_card_01 to seat_card_04.");
        builder.AppendLine("4. After collecting enough chair/seat clues, the player should use keypad_01 beside locked_door_01.");
        builder.AppendLine("5. A correct keypad input unlocks locked_door_01 and completes the escape task.");
        builder.AppendLine();
        builder.AppendLine("Important guidance policy:");
        builder.AppendLine("- Do not reveal the final password directly.");
        builder.AppendLine("- Give adaptive hints that point to the next clue object or interaction.");
        builder.AppendLine("- Prefer gentle guidance first, then stronger object-focused guidance if the player appears stuck.");
        builder.AppendLine();
        builder.AppendLine("Fusion policy:");
        builder.AppendLine("- If Unity has confirmed interaction/completion, trust Unity.");
        builder.AppendLine("- If Unity has not confirmed progress but vision sees the target with high confidence, give a lighter interaction hint.");
        builder.AppendLine("- If the stage is stalled and vision does not see the target, use highlight or spatial guidance.");
        builder.AppendLine("- If the stage is stalled and vision keeps seeing the target, give an operation hint instead of a location hint.");
        builder.AppendLine();
        builder.AppendLine("Current Unity adaptive diagnosis:");
        builder.AppendLine($"- current_stage: {currentStage}");
        builder.AppendLine($"- recommended_target_object: {currentTargetId}");
        builder.AppendLine($"- local_hint: {BuildLocalHint()}");
        builder.AppendLine($"- fusion_hint: {latestFusionHint}");
        builder.AppendLine($"- intervention_mode: {currentInterventionMode}");
        builder.AppendLine($"- observed_or_interacted_objects: {FormatObservedObjects()}");
        builder.AppendLine($"- failed_keypad_attempts: {failedKeypadAttempts}");
        builder.AppendLine($"- time_in_current_stage_seconds: {Mathf.Max(0f, Time.time - stageStartedAt):0.0}");

        if (!string.IsNullOrWhiteSpace(latestVisionText))
        {
            builder.AppendLine($"- previous_vision_summary: {latestVisionText}");
        }

        if (latestVisionDiagnosis != null)
        {
            builder.AppendLine($"- vision_target_visible: {latestVisionDiagnosis.target_visible}");
            builder.AppendLine($"- vision_target_confidence: {latestVisionDiagnosis.target_confidence:0.00}");
            builder.AppendLine($"- vision_focus: {latestVisionDiagnosis.likely_current_focus}");
            builder.AppendLine($"- vision_stage_hint: {latestVisionDiagnosis.visual_stage_hint}");
            builder.AppendLine($"- vision_evidence: {latestVisionDiagnosis.evidence}");
        }

        return builder.ToString();
    }

    public List<GuideObjectState> GetGuideObjectsForAi()
    {
        RebuildObjectCache();

        List<GuideObjectState> states = new List<GuideObjectState>();
        foreach (string objectId in TrackedObjectIds)
        {
            if (!objectsById.TryGetValue(objectId, out ObjectIdentity identity) || identity == null)
            {
                continue;
            }

            states.Add(new GuideObjectState
            {
                id = objectId,
                name = identity.gameObject.name,
                type = string.IsNullOrWhiteSpace(identity.Category) ? InferType(objectId) : identity.Category,
                role = InferRole(objectId),
                state = BuildObjectState(objectId),
                position = identity.transform.position,
            });
        }

        return states;
    }

    private void RegisterObjectInteraction(string objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return;
        }

        interactedObjectIds.Add(objectId);

        if (objectId == "keypad_01")
        {
            keypadInputStarted = true;
        }

        UpdateStage(true);
    }

    private void UpdateStage(bool allowAiRequest)
    {
        PuzzleStage nextStage = DetermineStage();
        string nextTarget = DetermineTarget(nextStage);
        bool changed = nextStage != currentStage || nextTarget != currentTargetId;

        if (changed || stageStartedAt <= 0f)
        {
            currentStage = nextStage;
            currentTargetId = nextTarget;
            stageStartedAt = Time.time;

            if (changed)
            {
                latestFusionHint = string.Empty;
                currentInterventionMode = InterventionMode.None;
                Debug.Log($"Adaptive guide stage: {currentStage}, target: {currentTargetId}", this);
                ClearHighlight();

                if (allowAiRequest && requestAiOnStageChange)
                {
                    RequestAiIfReady("stage changed");
                }
            }
        }
    }

    private PuzzleStage DetermineStage()
    {
        if (doorUnlocked || IsDoorUnlockedInScene())
        {
            return PuzzleStage.Completed;
        }

        if (KeypadController.HasActiveInput || keypadInputStarted)
        {
            return PuzzleStage.EnterPassword;
        }

        if (!interactedObjectIds.Contains("whiteboard_01"))
        {
            return PuzzleStage.FindWhiteboard;
        }

        if (!HasObservedAllSeatCards())
        {
            return PuzzleStage.SearchSeatClues;
        }

        return PuzzleStage.UseKeypad;
    }

    private string DetermineTarget(PuzzleStage stage)
    {
        switch (stage)
        {
            case PuzzleStage.FindWhiteboard:
                return "whiteboard_01";
            case PuzzleStage.SearchSeatClues:
                return FindFirstUnobservedSeatCard();
            case PuzzleStage.UseKeypad:
            case PuzzleStage.EnterPassword:
                return "keypad_01";
            case PuzzleStage.Completed:
                return "victory_exit_point";
            default:
                return "whiteboard_01";
        }
    }

    private bool HasObservedAllSeatCards()
    {
        foreach (string seatCardId in SeatCardIds)
        {
            if (!interactedObjectIds.Contains(seatCardId))
            {
                return false;
            }
        }

        return true;
    }

    private string FindFirstUnobservedSeatCard()
    {
        foreach (string seatCardId in SeatCardIds)
        {
            if (!interactedObjectIds.Contains(seatCardId))
            {
                return seatCardId;
            }
        }

        return "keypad_01";
    }

    private bool IsDoorUnlockedInScene()
    {
        if (!objectsById.TryGetValue("locked_door_01", out ObjectIdentity identity) || identity == null)
        {
            return false;
        }

        DoorController door = identity.GetComponent<DoorController>();
        return door != null && door.isUnlocked;
    }

    private void ApplyVisionFusion()
    {
        if (!useVisionFusion)
        {
            return;
        }

        if (currentStage == PuzzleStage.Completed || HasUnityConfirmedCurrentTarget())
        {
            currentInterventionMode = InterventionMode.TrustUnity;
            latestFusionHint = "Unity has confirmed this step; use Unity progress as ground truth.";
            return;
        }

        bool stageStalled = IsStageStalled();
        bool staleVision = latestVisionDiagnosis == null || Time.time - latestVisionReceivedAt > recentVisionSeconds;
        if (staleVision)
        {
            currentInterventionMode = stageStalled ? InterventionMode.HighlightAndDirectionHint : InterventionMode.None;
            latestFusionHint = stageStalled ? "No recent vision diagnosis; keep highlighting the Unity target." : BuildStageHint();
            return;
        }

        bool highConfidenceTarget = latestVisionDiagnosis.target_visible && latestVisionDiagnosis.target_confidence >= highVisionConfidence;
        if (!stageStalled && highConfidenceTarget)
        {
            currentInterventionMode = InterventionMode.LightInteractionHint;
            latestFusionHint = $"The camera likely sees {currentTargetId}; give a light interaction hint instead of stronger guidance.";
            return;
        }

        if (stageStalled && !highConfidenceTarget)
        {
            currentInterventionMode = InterventionMode.HighlightAndDirectionHint;
            latestFusionHint = $"The player seems stuck and the camera does not clearly see {currentTargetId}; use highlight or spatial guidance.";
            return;
        }

        if (stageStalled && highConfidenceTarget)
        {
            currentInterventionMode = InterventionMode.OperationHint;
            latestFusionHint = $"The player seems stuck while looking at {currentTargetId}; give an operation hint rather than a location hint.";
            return;
        }

        currentInterventionMode = InterventionMode.None;
        latestFusionHint = BuildStageHint();
    }

    private bool HasUnityConfirmedCurrentTarget()
    {
        if (currentStage == PuzzleStage.Completed)
        {
            return true;
        }

        if (currentTargetId == "keypad_01")
        {
            return KeypadController.HasActiveInput || keypadInputStarted || doorUnlocked || IsDoorUnlockedInScene();
        }

        return interactedObjectIds.Contains(currentTargetId);
    }

    private bool IsStageStalled()
    {
        return Time.time - stageStartedAt >= stuckSeconds;
    }

    private void RequestAiWhenPlayerIsStuck()
    {
        if (!requestAiWhenStuck || currentStage == PuzzleStage.Completed)
        {
            return;
        }

        if (!IsStageStalled())
        {
            return;
        }

        if (Time.time - lastAiRequestAt < repeatAiRequestSeconds)
        {
            return;
        }

        RequestAiIfReady("stage timeout");
    }

    private void RequestAiIfReady(string reason)
    {
        ResolveReferences();

        if (promptSender == null || !isActiveAndEnabled)
        {
            return;
        }

        if (Time.time - lastAiRequestAt < 1.5f)
        {
            return;
        }

        lastAiRequestAt = Time.time;
        Debug.Log($"Requesting AI guidance because of {reason}.", this);
        promptSender.SendPrompt();
    }

    private void UpdateHighlight()
    {
        if (!ShouldHighlightTarget())
        {
            ClearHighlight();
            return;
        }

        if (highlightedTargetId != currentTargetId)
        {
            ClearHighlight();
            highlightedTargetId = currentTargetId;

            if (objectsById.TryGetValue(currentTargetId, out ObjectIdentity identity) && identity != null)
            {
                activeHighlightRenderers.AddRange(identity.GetComponentsInChildren<Renderer>(true));
            }
        }

        if (activeHighlightRenderers.Count == 0)
        {
            return;
        }

        Color baseColor = currentInterventionMode == InterventionMode.OperationHint ? operationHintColor : highlightColor;
        float pulse = 0.55f + Mathf.Sin(Time.time * highlightPulseSpeed) * 0.25f;
        Color pulseColor = baseColor * Mathf.Clamp01(pulse);
        pulseColor.a = 1f;

        if (highlightBlock == null)
        {
            highlightBlock = new MaterialPropertyBlock();
        }

        foreach (Renderer targetRenderer in activeHighlightRenderers)
        {
            if (targetRenderer == null)
            {
                continue;
            }

            targetRenderer.GetPropertyBlock(highlightBlock);
            highlightBlock.SetColor("_Color", pulseColor);
            highlightBlock.SetColor("_BaseColor", pulseColor);
            highlightBlock.SetColor("_EmissionColor", pulseColor * 1.4f);
            targetRenderer.SetPropertyBlock(highlightBlock);
        }
    }

    private bool ShouldHighlightTarget()
    {
        if (!enableHighlight || currentStage == PuzzleStage.Completed || string.IsNullOrWhiteSpace(currentTargetId))
        {
            return false;
        }

        if (!useVisionFusion)
        {
            return true;
        }

        return currentInterventionMode == InterventionMode.None ||
               currentInterventionMode == InterventionMode.HighlightAndDirectionHint ||
               currentInterventionMode == InterventionMode.OperationHint;
    }

    private void ClearHighlight()
    {
        foreach (Renderer targetRenderer in activeHighlightRenderers)
        {
            if (targetRenderer != null)
            {
                targetRenderer.SetPropertyBlock(null);
            }
        }

        activeHighlightRenderers.Clear();
        highlightedTargetId = null;
    }

    private void ResolveReferences()
    {
        if (promptSender == null)
        {
            promptSender = GetComponent<CameraPromptSender>();
        }

        if (promptSender == null)
        {
            promptSender = FindFirstObjectByType<CameraPromptSender>();
        }

        if (playerTransform == null)
        {
            Camera camera = GetComponent<Camera>();
            playerTransform = camera != null && transform.root != transform ? transform.root : transform;
        }
    }

    private void RebuildObjectCache()
    {
        lastObjectCacheAt = Time.time;
        objectsById.Clear();

        ObjectIdentity[] identities = FindObjectsByType<ObjectIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ObjectIdentity identity in identities)
        {
            if (identity == null || string.IsNullOrWhiteSpace(identity.ObjectId))
            {
                continue;
            }

            objectsById[identity.ObjectId] = identity;
        }
    }

    private string BuildObjectState(string objectId)
    {
        if (objectId == currentTargetId && currentStage != PuzzleStage.Completed)
        {
            switch (currentInterventionMode)
            {
                case InterventionMode.LightInteractionHint:
                    return "target_visible_needs_light_interaction_hint";
                case InterventionMode.HighlightAndDirectionHint:
                    return "target_not_visible_needs_highlight";
                case InterventionMode.OperationHint:
                    return "target_visible_but_player_stuck_needs_operation_hint";
                default:
                    return "next_recommended_target";
            }
        }

        if (objectId == "locked_door_01")
        {
            return doorUnlocked || IsDoorUnlockedInScene() ? "unlocked" : "locked";
        }

        if (objectId == "keypad_01")
        {
            if (KeypadController.HasActiveInput || keypadInputStarted)
            {
                return "input_active";
            }

            return failedKeypadAttempts > 0 ? "previous_password_attempt_failed" : "available";
        }

        if (interactedObjectIds.Contains(objectId))
        {
            return "observed";
        }

        if (Array.IndexOf(SeatCardIds, objectId) >= 0)
        {
            return "unobserved_password_clue";
        }

        if (objectId.StartsWith("chair_", StringComparison.Ordinal))
        {
            return currentStage == PuzzleStage.SearchSeatClues ? "chair_search_area" : "context";
        }

        return "available";
    }

    private string BuildLocalHint()
    {
        if (useVisionFusion)
        {
            switch (currentInterventionMode)
            {
                case InterventionMode.TrustUnity:
                    return "Unity has confirmed the latest interaction; continue to the next task step.";
                case InterventionMode.LightInteractionHint:
                    return $"You may already be looking at {currentTargetId}; try interacting with it.";
                case InterventionMode.HighlightAndDirectionHint:
                    return $"The target is not clearly visible; follow the highlight toward {currentTargetId}.";
                case InterventionMode.OperationHint:
                    return BuildOperationHint();
            }
        }

        return BuildStageHint();
    }

    private string BuildStageHint()
    {
        switch (currentStage)
        {
            case PuzzleStage.FindWhiteboard:
                return "Inspect the whiteboard first; it explains where the password clues come from.";
            case PuzzleStage.SearchSeatClues:
                return "Search the chair and seat-card area for the door password clues.";
            case PuzzleStage.UseKeypad:
                return "Use the keypad beside the locked door after reading the chair clues.";
            case PuzzleStage.EnterPassword:
                return failedKeypadAttempts > 0 ? "The last code was wrong; re-check the chair clues before submitting again." : "Enter the four-digit code inferred from the chair clues.";
            case PuzzleStage.Completed:
                return "The door is unlocked; move through the exit.";
            default:
                return "Look for the next useful clue object.";
        }
    }

    private string BuildOperationHint()
    {
        switch (currentStage)
        {
            case PuzzleStage.FindWhiteboard:
                return "The whiteboard is visible; get close and press E to inspect the clue.";
            case PuzzleStage.SearchSeatClues:
                return "The clue card is visible; press E on it and compare it with the meeting order.";
            case PuzzleStage.UseKeypad:
                return "The keypad is visible; interact with it and enter the code inferred from the clues.";
            case PuzzleStage.EnterPassword:
                return failedKeypadAttempts > 0 ? "The keypad is active, but the last code was wrong; re-check the seat-card order before submitting." : "The keypad is active; type the inferred four-digit code and press Enter.";
            default:
                return BuildStageHint();
        }
    }

    private string FormatObservedObjects()
    {
        if (interactedObjectIds.Count == 0)
        {
            return "none";
        }

        return string.Join(", ", interactedObjectIds);
    }

    private static string InferType(string objectId)
    {
        if (objectId == "locked_door_01")
        {
            return "puzzle_exit";
        }

        if (objectId == "keypad_01")
        {
            return "puzzle_input";
        }

        if (objectId == "whiteboard_01" || objectId.StartsWith("seat_card_", StringComparison.Ordinal))
        {
            return "puzzle_clue";
        }

        if (objectId.StartsWith("chair_", StringComparison.Ordinal))
        {
            return "furniture";
        }

        return "scene_object";
    }

    private static string InferRole(string objectId)
    {
        if (objectId == "whiteboard_01")
        {
            return "first_clue";
        }

        if (objectId.StartsWith("seat_card_", StringComparison.Ordinal))
        {
            return "password_clue";
        }

        if (objectId.StartsWith("chair_", StringComparison.Ordinal))
        {
            return "chair_search_area";
        }

        if (objectId == "keypad_01")
        {
            return "password_input";
        }

        if (objectId == "locked_door_01")
        {
            return "locked_exit";
        }

        if (objectId == "victory_exit_point")
        {
            return "completion_marker";
        }

        return "context";
    }

    private static MeetingRoomAdaptiveGuide FindActiveGuide()
    {
        if (activeGuide != null)
        {
            return activeGuide;
        }

        activeGuide = FindFirstObjectByType<MeetingRoomAdaptiveGuide>();
        return activeGuide;
    }

    private void OnGUI()
    {
        if (!showDebugOverlay || !Application.isPlaying)
        {
            return;
        }

        const float width = 640f;
        const float height = 156f;
        Rect panel = new Rect(24f, 24f, width, height);
        GUI.Box(panel, "Adaptive AI Guide");
        GUI.Label(new Rect(panel.x + 16f, panel.y + 28f, width - 32f, 22f), $"Stage: {currentStage} | Target: {currentTargetId} | Mode: {currentInterventionMode}");
        GUI.Label(new Rect(panel.x + 16f, panel.y + 52f, width - 32f, 36f), BuildLocalHint());

        if (latestVisionDiagnosis != null)
        {
            GUI.Label(new Rect(panel.x + 16f, panel.y + 86f, width - 32f, 22f), $"Vision target visible: {latestVisionDiagnosis.target_visible} ({latestVisionDiagnosis.target_confidence:0.00}) | Focus: {latestVisionDiagnosis.likely_current_focus}");
        }

        if (!string.IsNullOrWhiteSpace(latestAiInstruction))
        {
            GUI.Label(new Rect(panel.x + 16f, panel.y + 112f, width - 32f, 38f), $"AI: {latestAiInstruction}");
        }
    }
}
