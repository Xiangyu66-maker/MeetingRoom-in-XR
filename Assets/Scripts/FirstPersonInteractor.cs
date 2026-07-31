using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/First Person Interactor")]
public sealed class FirstPersonInteractor : MonoBehaviour
{
    [SerializeField] private Camera interactionCamera;
    [SerializeField] private float interactionRange = 2f;
    [SerializeField] private KeyCode grabKey = KeyCode.F;
    [SerializeField] private bool showDebugPrompt = true;

    private InteractableObject currentTarget;
    private InteractableObject lastLoggedTarget;
    private ADoorController currentADoorTarget;
    private DelayedManualDoorController currentDelayedDoorTarget;
    private ComputerDoorController currentComputerDoorTarget;
    private TimedExitDoorController currentTimedDoorTarget;
    private DoorController currentDoorTarget;
    private GrabbableObject currentGrabbableTarget;
    private GrabbableObject heldObject;

    private void Awake()
    {
        ResolveCamera();
    }

    private void Update()
    {
        // Suspend world interaction while the keypad owns keyboard input.
        if (KeypadController.HasActiveInput)
        {
            currentTarget = null;
            currentADoorTarget = null;
            currentDelayedDoorTarget = null;
            currentComputerDoorTarget = null;
            currentTimedDoorTarget = null;
            currentDoorTarget = null;
            currentGrabbableTarget = null;
            return;
        }

        if (BackpackUI.Instance != null && BackpackUI.Instance.IsOpen)
        {
            currentTarget = null;
            currentADoorTarget = null;
            currentDelayedDoorTarget = null;
            currentComputerDoorTarget = null;
            currentTimedDoorTarget = null;
            currentDoorTarget = null;
            currentGrabbableTarget = null;
            return;
        }

        ResolveCamera();

        currentTarget = FindLookTarget();
        currentADoorTarget = FindADoorTarget();
        currentDelayedDoorTarget = FindDelayedDoorTarget();
        currentComputerDoorTarget = FindLookComponent<ComputerDoorController>();
        currentTimedDoorTarget = FindLookComponent<TimedExitDoorController>();
        currentDoorTarget = FindLookComponent<DoorController>();

        if (currentTarget != lastLoggedTarget)
        {
            lastLoggedTarget = currentTarget;
            if (currentTarget != null)
            {
                Debug.Log($"Looking at interactable object: {currentTarget.ObjectId}", currentTarget);
            }
        }

        if (QuestControllerInput.PrimaryActionDown)
        {
            if (currentADoorTarget != null)
            {
                currentADoorTarget.Open();
            }
            else if (currentDelayedDoorTarget != null)
            {
                currentDelayedDoorTarget.TryOpen();
            }
            else if (currentComputerDoorTarget != null)
            {
                currentComputerDoorTarget.Inspect();
            }
            else if (currentTimedDoorTarget != null)
            {
                currentTimedDoorTarget.Inspect();
            }
            else if (currentDoorTarget != null && currentDoorTarget.IsLocked)
            {
                if (currentTarget != null)
                {
                    currentTarget.Interact();
                }
                else
                {
                    currentDoorTarget.Inspect();
                }
            }
            else if (currentTarget != null)
            {
                currentTarget.Interact();
            }
        }

        if (currentTarget != null && QuestControllerInput.SecondaryActionDown)
        {
            GptVisionInteractionManager manager = GptVisionInteractionManager.Instance;
            if (manager != null)
            {
                manager.AnalyzeObject(currentTarget.gameObject, currentTarget.ObjectId, currentTarget.Description);
            }
        }

        if (heldObject == null)
        {
            currentGrabbableTarget = FindGrabbableTarget();
        }
        else
        {
            currentGrabbableTarget = null;
        }

        if (QuestControllerInput.GrabDown || Input.GetKeyDown(grabKey))
        {
            if (heldObject != null)
            {
                heldObject.Drop();
                heldObject = null;
            }
            else if (currentGrabbableTarget != null)
            {
                currentGrabbableTarget.Grab(interactionCamera.transform);
                heldObject = currentGrabbableTarget;
            }
        }
    }

    private InteractableObject FindLookTarget()
    {
        if (interactionCamera == null) return null;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<InteractableObject>();
    }

    private GrabbableObject FindGrabbableTarget()
    {
        if (interactionCamera == null) return null;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<GrabbableObject>();
    }

    private ADoorController FindADoorTarget()
    {
        if (interactionCamera == null) return null;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<ADoorController>();
    }

    private DelayedManualDoorController FindDelayedDoorTarget()
    {
        if (interactionCamera == null) return null;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<DelayedManualDoorController>();
    }

    private T FindLookComponent<T>() where T : Component
    {
        if (interactionCamera == null) return null;

        Ray ray = interactionCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            return null;

        return hit.collider.GetComponentInParent<T>();
    }

    private void ResolveCamera()
    {
        if (interactionCamera != null) return;

        interactionCamera = GetComponent<Camera>();
        if (interactionCamera != null) return;

        interactionCamera = GetComponentInChildren<Camera>();
        if (interactionCamera != null) return;

        interactionCamera = Camera.main;
        if (interactionCamera == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>();
            interactionCamera = cameras.Length > 0 ? cameras[0] : null;
        }
    }

    private void LateUpdate()
    {
        if (!showDebugPrompt
            || KeypadController.HasActiveInput
            || (BackpackUI.Instance != null && BackpackUI.Instance.IsOpen))
        {
            QuestXRUIRuntime.HideMessage(QuestXRUIRuntime.Channel.Interaction);
            return;
        }

        string prompt = GetInteractionPrompt();
        if (heldObject != null)
        {
            prompt = AppendPrompt(prompt, "Right grip: Drop object");
        }
        else if (currentGrabbableTarget != null)
        {
            prompt = AppendPrompt(prompt, "Right grip: Pick up object");
        }

        QuestXRUIRuntime.SetMessage(
            QuestXRUIRuntime.Channel.Interaction,
            prompt,
            !string.IsNullOrWhiteSpace(prompt));
    }

    private static string AppendPrompt(string current, string addition)
    {
        return string.IsNullOrWhiteSpace(current) ? addition : current + "\n" + addition;
    }

    private string GetInteractionPrompt()
    {
        if (currentADoorTarget != null)
        {
            return "A: Open selected door";
        }

        if (currentDelayedDoorTarget != null)
        {
            return currentDelayedDoorTarget.CanBeOpened
                ? "A: Open available door"
                : currentDelayedDoorTarget.IsLocked
                    ? $"A: Check door\nAvailable in {Mathf.CeilToInt(currentDelayedDoorTarget.RemainingUnlockSeconds)}s"
                    : string.Empty;
        }

        if (currentComputerDoorTarget != null)
        {
            return currentComputerDoorTarget.IsLocked ? "A: Check door" : string.Empty;
        }

        if (currentTimedDoorTarget != null)
        {
            return currentTimedDoorTarget.IsLocked ? "A: Check door" : string.Empty;
        }

        if (currentDoorTarget != null)
        {
            return currentDoorTarget.IsLocked ? "A: Check door" : string.Empty;
        }

        return currentTarget != null
            ? "A: Interact    B: Vision"
            : string.Empty;
    }

    private void OnDisable()
    {
        QuestXRUIRuntime.HideMessage(QuestXRUIRuntime.Channel.Interaction);
    }

    private void OnGUI()
    {
        if (QuestXRUIRuntime.IsXRPresentationActive) return;
        if (!showDebugPrompt) return;
        if (KeypadController.HasActiveInput) return;

        if (currentADoorTarget != null)
        {
            GUI.Label(
                new Rect((Screen.width - 300f) * 0.5f, Screen.height - 72f, 300f, 28f),
                "E: open selected Adoor");
        }
        else if (currentDelayedDoorTarget != null)
        {
            string delayedDoorPrompt = currentDelayedDoorTarget.CanBeOpened
                ? "E: open Tenminitunesdoor"
                : currentDelayedDoorTarget.IsLocked
                    ? $"E: check door | available in {Mathf.CeilToInt(currentDelayedDoorTarget.RemainingUnlockSeconds)}s"
                    : string.Empty;
            GUI.Label(
                new Rect((Screen.width - 340f) * 0.5f, Screen.height - 72f, 340f, 28f),
                delayedDoorPrompt);
        }
        else if ((currentComputerDoorTarget != null && currentComputerDoorTarget.IsLocked)
            || (currentTimedDoorTarget != null && currentTimedDoorTarget.IsLocked)
            || (currentDoorTarget != null && currentDoorTarget.IsLocked))
        {
            GUI.Label(
                new Rect((Screen.width - 300f) * 0.5f, Screen.height - 72f, 300f, 28f),
                "E: check door");
        }
        else if (currentTarget != null)
        {
            GUI.Label(new Rect((Screen.width - 300f) * 0.5f, Screen.height - 72f, 300f, 28f), "A: interact | B: VLM (E/Q on keyboard)");
        }

        if (heldObject != null)
        {
            GUI.Label(new Rect((Screen.width - 200f) * 0.5f, Screen.height - 108f, 200f, 28f), "Press F to drop");
        }
        else if (currentGrabbableTarget != null)
        {
            GUI.Label(new Rect((Screen.width - 200f) * 0.5f, Screen.height - 108f, 200f, 28f), "Press F to pick up");
        }
    }
}
