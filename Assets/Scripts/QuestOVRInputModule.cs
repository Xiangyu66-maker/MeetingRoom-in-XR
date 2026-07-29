using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Uses the physical A button on the right Touch controller for Quest UI clicks.
/// OVRInputModule's logical Button.One can also match X on the left controller.
/// </summary>
public sealed class QuestOVRInputModule : OVRInputModule
{
    protected override PointerEventData.FramePressState GetGazeButtonState()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        bool pressed =
            Input.GetKeyDown(gazeClickKey) || OVRInput.GetDown(OVRInput.RawButton.A);
        bool released =
            Input.GetKeyUp(gazeClickKey) || OVRInput.GetUp(OVRInput.RawButton.A);
#else
        bool pressed = OVRInput.GetDown(OVRInput.RawButton.A);
        bool released = OVRInput.GetUp(OVRInput.RawButton.A);
#endif

        if (pressed && released)
        {
            return PointerEventData.FramePressState.PressedAndReleased;
        }

        if (pressed)
        {
            return PointerEventData.FramePressState.Pressed;
        }

        if (released)
        {
            return PointerEventData.FramePressState.Released;
        }

        return PointerEventData.FramePressState.NotChanged;
    }
}
