using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Conference Room/Door Controller")]
public sealed class DoorController : MonoBehaviour
{
    [SerializeField] private Vector3 openOffset = new Vector3(0f, 2.4f, 0f);
    [SerializeField] private float openSpeed = 2.5f;

    public bool isUnlocked;

    private Vector3 closedLocalPosition;
    private Vector3 openLocalPosition;
    private bool isOpening;
    private bool loggedVictory;
    private bool notifiedGameState;

    private void Awake()
    {
        closedLocalPosition = transform.localPosition;
        openLocalPosition = closedLocalPosition + openOffset;
    }

    private void Update()
    {
        if (!isOpening)
        {
            return;
        }

        transform.localPosition = Vector3.MoveTowards(transform.localPosition, openLocalPosition, openSpeed * Time.deltaTime);
        if ((transform.localPosition - openLocalPosition).sqrMagnitude <= 0.0001f)
        {
            transform.localPosition = openLocalPosition;
            isOpening = false;

            if (!loggedVictory)
            {
                loggedVictory = true;
                Debug.Log("Door opened. Victory!", this);
            }
        }
    }

    public void UnlockDoor()
    {
        if (isUnlocked)
        {
            Debug.Log("Door is already unlocked.", this);
            return;
        }

        isUnlocked = true;
        isOpening = true;
        Debug.Log("Door unlocked. Opening door.", this);

        if (!notifiedGameState)
        {
            notifiedGameState = true;
            FindObjectOfType<GameStateManager>()?.NotifyDoorUnlocked();
        }

        MeetingRoomAdaptiveGuide.NotifyDoorUnlocked();
    }
}

