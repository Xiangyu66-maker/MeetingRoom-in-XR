using UnityEngine;

/// <summary>
/// Central Quest Touch controller mapping used by the meeting-room interactions.
/// Keyboard fallbacks are kept for testing in the Unity Editor.
/// </summary>
public static class QuestControllerInput
{
    public static bool PrimaryActionDown =>
        Input.GetKeyDown(KeyCode.E) ||
        OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch); // A

    public static bool SecondaryActionDown =>
        Input.GetKeyDown(KeyCode.Q) ||
        OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch); // B

    public static bool BackpackDown =>
        Input.GetKeyDown(KeyCode.Tab) ||
        OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch); // X

    public static bool MenuDown =>
        Input.GetKeyDown(KeyCode.Escape) ||
        OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.LTouch); // Y

    public static bool GrabDown =>
        Input.GetKeyDown(KeyCode.F) ||
        OVRInput.GetDown(
            OVRInput.Button.PrimaryHandTrigger,
            OVRInput.Controller.RTouch); // Right grip
}
