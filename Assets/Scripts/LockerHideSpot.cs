using TMPro;
using UnityEngine;

public class LockerHideSpot : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";

    [Header("Locker Positions")]
    [Tooltip("Place this at the desired camera position inside the locker.")]
    [SerializeField] private Transform hidePoint;

    [Tooltip("Place this outside the locker.")]
    [SerializeField] private Transform exitPoint;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;

    [Header("View Inside Locker")]
    [SerializeField] private float lookSensitivity = 2f;

    [Tooltip("Maximum left/right viewing angle from the locker slit.")]
    [SerializeField] private float horizontalLookLimit = 65f;

    [Tooltip("Maximum up/down viewing angle from the locker slit.")]
    [SerializeField] private float verticalLookLimit = 35f;

    private Transform player;
    private Transform playerCamera;
    private Rigidbody playerRigidbody;

    private Behaviour playerController;
    private bool playerControllerWasEnabled;

    private bool playerInRange;
    private bool playerInsideLocker;

    private Vector3 lockedPlayerPosition;

    private RigidbodyConstraints originalConstraints;
    private bool originalUseGravity;

    private float startingYaw;
    private float startingPitch;
    private float yawOffset;
    private float pitchOffset;

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (playerInsideLocker)
        {
            // 躲藏时由这个脚本控制视角
            UpdateLockerView();

            if (QuestControllerInput.PrimaryActionDown)
            {
                ExitLocker();
            }

            return;
        }

        if (!playerInRange || player == null)
        {
            return;
        }

        if (QuestControllerInput.PrimaryActionDown)
        {
            EnterLocker();
        }
    }

    private void LateUpdate()
    {
        if (!playerInsideLocker || player == null)
        {
            return;
        }

        // 每一帧锁定玩家位置，防止WASD或物理把玩家移走
        player.position = lockedPlayerPosition;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        // 放在LateUpdate，防止其他脚本覆盖摄像机方向
        ApplyLockerView();
    }

    private void EnterLocker()
    {
        if (hidePoint == null)
        {
            Debug.LogError("LockerHideSpot: HidePoint is not assigned.");
            return;
        }

        Camera cameraComponent = player.GetComponentInChildren<Camera>(true);

        if (cameraComponent == null)
        {
            Debug.LogError("LockerHideSpot: Player camera was not found.");
            return;
        }

        playerCamera = cameraComponent.transform;
        playerRigidbody = player.GetComponent<Rigidbody>();

        FindAndDisablePlayerController();

        /*
         * HidePoint表示摄像机眼睛的位置，而不是玩家脚底的位置。
         * 这里根据摄像机在Player中的偏移，计算Player根物体应放在哪里。
         */
        Vector3 cameraLocalOffset =
            player.InverseTransformPoint(playerCamera.position);

        Quaternion desiredPlayerRotation =
            Quaternion.Euler(0f, hidePoint.eulerAngles.y, 0f);

        lockedPlayerPosition =
            hidePoint.position -
            desiredPlayerRotation * cameraLocalOffset;

        player.SetPositionAndRotation(
            lockedPlayerPosition,
            desiredPlayerRotation
        );

        if (playerRigidbody != null)
        {
            originalConstraints = playerRigidbody.constraints;
            originalUseGravity = playerRigidbody.useGravity;

            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
            playerRigidbody.useGravity = false;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        /*
         * 关键部分：
         * 刚进入时，视角完全等于HidePoint的Rotation。
         */
        startingYaw = hidePoint.eulerAngles.y;
        startingPitch = NormalizeAngle(hidePoint.eulerAngles.x);

        yawOffset = 0f;
        pitchOffset = 0f;

        playerCamera.rotation = hidePoint.rotation;

        playerInsideLocker = true;

        PlayerHideState.SetHidden(true);

        ShowPrompt("Press A to exit");

        Debug.Log("Player entered locker and is facing the slit.");
    }

    private void UpdateLockerView()
    {
        float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

        yawOffset += mouseX;
        pitchOffset -= mouseY;

        yawOffset = Mathf.Clamp(
            yawOffset,
            -horizontalLookLimit,
            horizontalLookLimit
        );

        pitchOffset = Mathf.Clamp(
            pitchOffset,
            -verticalLookLimit,
            verticalLookLimit
        );
    }

    private void ApplyLockerView()
    {
        if (playerCamera == null)
        {
            return;
        }

        float finalYaw = startingYaw + yawOffset;
        float finalPitch = startingPitch + pitchOffset;

        playerCamera.rotation = Quaternion.Euler(
            finalPitch,
            finalYaw,
            0f
        );
    }

    private void ExitLocker()
    {
        if (exitPoint == null)
        {
            Debug.LogError("LockerHideSpot: ExitPoint is not assigned.");
            return;
        }

        playerInsideLocker = false;

        PlayerHideState.SetHidden(false);

        if (playerRigidbody != null)
        {
            playerRigidbody.constraints = originalConstraints;
            playerRigidbody.useGravity = originalUseGravity;
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        player.SetPositionAndRotation(
            exitPoint.position,
            Quaternion.Euler(0f, exitPoint.eulerAngles.y, 0f)
        );

        if (playerCamera != null)
        {
            playerCamera.rotation = exitPoint.rotation;
        }

        RestorePlayerController();

        ShowPrompt("Press A to hide");

        Debug.Log("Player exited locker.");
    }

    private void FindAndDisablePlayerController()
    {
        Behaviour[] scripts = player.GetComponents<Behaviour>();

        foreach (Behaviour script in scripts)
        {
            if (script == null || script is PlayerHideState)
            {
                continue;
            }

            string scriptName = script.GetType().Name.ToLower();

            if (scriptName.Contains("firstperson") ||
                scriptName.Contains("fpscontroller") ||
                scriptName.Contains("playercontroller") ||
                scriptName.Contains("movement"))
            {
                playerController = script;
                playerControllerWasEnabled = script.enabled;
                script.enabled = false;

                Debug.Log(
                    "Temporarily disabled player controller: " +
                    script.GetType().Name
                );

                return;
            }
        }

        Debug.LogWarning(
            "LockerHideSpot: Player controller was not automatically found."
        );
    }

    private void RestorePlayerController()
    {
        if (playerController != null)
        {
            playerController.enabled = playerControllerWasEnabled;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        player = other.transform;
        playerInRange = true;

        if (!playerInsideLocker)
        {
            ShowPrompt("Press A to hide");
        }

        Debug.Log("Player entered locker range.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        // 进入柜子后可能已经离开Trigger，仍然要保留玩家引用
        if (playerInsideLocker)
        {
            return;
        }

        playerInRange = false;
        player = null;

        HidePrompt();

        Debug.Log("Player left locker range.");
    }

    private void ShowPrompt(string message)
    {
        if (promptObject != null)
        {
            promptObject.SetActive(true);
        }

        if (promptText != null)
        {
            promptText.text = message;
        }
    }

    private void HidePrompt()
    {
        if (promptObject != null)
        {
            promptObject.SetActive(false);
        }
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }
}
