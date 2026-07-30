using UnityEngine;

public class PlayerHideState : MonoBehaviour
{
    public static bool IsHidden { get; private set; }

    public static void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        Debug.Log("Player hidden state: " + IsHidden);
    }

    public void SetHideState(bool hidden)
    {
        SetHidden(hidden);
    }

    private void OnDisable()
    {
        IsHidden = false;
    }
}