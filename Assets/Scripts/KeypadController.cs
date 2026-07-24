using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Keypad Controller")]
public sealed class KeypadController : MonoBehaviour
{
    [SerializeField] private string correctPassword = "3142";
    [SerializeField] private DoorController doorController;
    [SerializeField] private bool showDebugInputOverlay = true;

    private string currentInput = string.Empty;
    private bool inputModeActive;
    private static KeypadController activeKeypad;

    public static bool HasActiveInput => activeKeypad != null && activeKeypad.inputModeActive;

    private void Awake()
    {
        ResolveDoorController();
    }

    private void Update()
    {
        if (!inputModeActive)
        {
            return;
        }

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + digit);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + digit);
            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                AppendDigit(digit);
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            Debug.Log($"Keypad input: {DisplayInput()}", this);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitInput();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelInputMode();
        }
    }

    public void BeginInputMode()
    {
        if (inputModeActive)
        {
            return;
        }

        activeKeypad = this;
        inputModeActive = true;
        currentInput = string.Empty;
        Debug.Log("Keypad input mode started. Type 0-9, Backspace to delete, Enter to submit, Escape to cancel.", this);
        MeetingRoomAdaptiveGuide.NotifyKeypadInputStarted();
    }

    public void ConfigureDoor(DoorController door)
    {
        if (doorController == null)
        {
            doorController = door;
        }
    }

    private void AppendDigit(int digit)
    {
        if (currentInput.Length >= correctPassword.Length)
        {
            return;
        }

        currentInput += digit.ToString();
        Debug.Log($"Keypad input: {DisplayInput()}", this);
    }

    private void SubmitInput()
    {
        if (currentInput == correctPassword)
        {
            Debug.Log("Correct password. Door unlocked.", this);
            MeetingRoomAdaptiveGuide.NotifyPasswordAccepted();
            ResolveDoorController();
            if (doorController != null)
            {
                doorController.UnlockDoor();
            }
            else
            {
                Debug.LogWarning("Correct password entered, but no DoorController was assigned.", this);
            }

            inputModeActive = false;
            activeKeypad = null;
            return;
        }

        Debug.Log("Wrong password. Try again.", this);
        MeetingRoomAdaptiveGuide.NotifyPasswordRejected();
        currentInput = string.Empty;
    }

    private void CancelInputMode()
    {
        inputModeActive = false;
        activeKeypad = null;
        currentInput = string.Empty;
        Debug.Log("Keypad input mode cancelled.", this);
    }

    private void ResolveDoorController()
    {
        if (doorController != null)
        {
            return;
        }

        ObjectIdentity[] identities = FindObjectsOfType<ObjectIdentity>();
        foreach (ObjectIdentity identity in identities)
        {
            if (identity != null && identity.ObjectId == "locked_door_01")
            {
                doorController = identity.GetComponent<DoorController>();
                return;
            }
        }
    }

    private string DisplayInput()
    {
        if (currentInput.Length == 0)
        {
            return "_ _ _ _";
        }

        char[] display = { '_', '_', '_', '_' };
        for (int i = 0; i < currentInput.Length && i < display.Length; i++)
        {
            display[i] = currentInput[i];
        }

        return $"{display[0]} {display[1]} {display[2]} {display[3]}";
    }

    private void OnGUI()
    {
        if (!showDebugInputOverlay || !inputModeActive)
        {
            return;
        }

        // TODO: Replace this debug overlay with a proper UI Text/TextMeshProUGUI keypad display.
        GUI.Box(new Rect((Screen.width - 320f) * 0.5f, 72f, 320f, 86f), "Keypad");
        GUI.Label(new Rect((Screen.width - 260f) * 0.5f, 108f, 260f, 24f), DisplayInput());
        GUI.Label(new Rect((Screen.width - 260f) * 0.5f, 132f, 260f, 24f), "Enter submits, Backspace deletes, Esc cancels");
    }
}

