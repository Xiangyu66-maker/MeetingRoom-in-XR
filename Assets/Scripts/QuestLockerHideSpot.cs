using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestLockerHideSpot : MonoBehaviour
{
    [Header("Quest Player")]
    [Tooltip("绑定[BuildingBlock] Camera Rig根物体。")]
    [SerializeField] private Transform xrRigRoot;

    [Tooltip("绑定TrackingSpace下面的CenterEyeAnchor。")]
    [SerializeField] private Transform centerEye;

    [Tooltip("绑定OVRComprehensiveInteractionRig下面的Locomotor对象。")]
    [SerializeField] private GameObject locomotorObject;

    [Header("Locker Points")]
    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform exitPoint;

    [Tooltip("进入柜子时让玩家初始朝向HidePoint的前方。")]
    [SerializeField] private bool alignViewToHidePoint = true;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;

    [Header("Quest Input")]
    [SerializeField] private bool useQuestButton = true;
    [SerializeField] private bool allowKeyboardFallback = true;

    private readonly HashSet<Collider> nearbyPlayerColliders =
        new HashSet<Collider>();

    private bool isHidden;

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
        {
            return;
        }

        if (!WasInteractPressed())
        {
            return;
        }

        if (isHidden)
        {
            ExitLocker();
        }
        else if (nearbyPlayerColliders.Count > 0)
        {
            EnterLocker();
        }
    }

    private bool WasInteractPressed()
    {
        bool pressed = false;

        /*
         * Quest右手A键。
         */
        if (useQuestButton)
        {
            pressed = OVRInput.GetDown(
                OVRInput.Button.One,
                OVRInput.Controller.RTouch
            );
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowKeyboardFallback &&
            Input.GetKeyDown(KeyCode.E))
        {
            pressed = true;
        }
#endif

        return pressed;
    }

    private void EnterLocker()
    {
        if (xrRigRoot == null ||
            centerEye == null ||
            hidePoint == null)
        {
            Debug.LogWarning(
                "QuestLockerHideSpot: XR Rig, Center Eye or Hide Point is missing."
            );

            return;
        }

        isHidden = true;

        /*
         * 先告诉猪，玩家已经躲藏。
         */
        PlayerHideState.SetHidden(true);

        /*
         * 禁用Quest移动，但Camera Rig和头显追踪继续运行，
         * 所以玩家仍然可以在柜子中转头。
         */
        SetLocomotionEnabled(false);

        MoveEyeToPoint(
            hidePoint,
            alignViewToHidePoint
        );

        ShowPrompt(
            "Press A to exit locker"
        );

        Debug.Log(
            "Quest player entered the locker."
        );
    }

    private void ExitLocker()
    {
        if (xrRigRoot == null ||
            centerEye == null)
        {
            return;
        }

        if (exitPoint != null)
        {
            MoveEyeToPoint(
                exitPoint,
                false
            );
        }

        PlayerHideState.SetHidden(false);

        SetLocomotionEnabled(true);

        isHidden = false;

        if (nearbyPlayerColliders.Count > 0)
        {
            ShowPrompt(
                "Press A to hide"
            );
        }
        else
        {
            HidePrompt();
        }

        Debug.Log(
            "Quest player exited the locker."
        );
    }

    private void MoveEyeToPoint(
        Transform targetPoint,
        bool alignView)
    {
        if (targetPoint == null ||
            xrRigRoot == null ||
            centerEye == null)
        {
            return;
        }

        /*
         * 不直接修改CenterEyeAnchor。
         * CenterEyeAnchor受Quest头显追踪控制。
         *
         * 如需调整初始朝向，
         * 围绕当前眼睛位置旋转整个Camera Rig。
         */
        if (alignView)
        {
            AlignRigYawToTarget(
                targetPoint
            );
        }

        /*
         * 计算当前眼睛到目标点需要的世界坐标偏移，
         * 然后移动整个Camera Rig。
         */
        Vector3 requiredOffset =
            targetPoint.position -
            centerEye.position;

        xrRigRoot.position +=
            requiredOffset;
    }

    private void AlignRigYawToTarget(
        Transform targetPoint)
    {
        Vector3 currentForward =
            centerEye.forward;

        Vector3 targetForward =
            targetPoint.forward;

        currentForward.y = 0f;
        targetForward.y = 0f;

        if (currentForward.sqrMagnitude < 0.001f ||
            targetForward.sqrMagnitude < 0.001f)
        {
            return;
        }

        currentForward.Normalize();
        targetForward.Normalize();

        float yawAngle =
            Vector3.SignedAngle(
                currentForward,
                targetForward,
                Vector3.up
            );

        /*
         * 围绕眼睛位置旋转Rig，
         * 避免旋转时眼睛位置发生较大偏移。
         */
        xrRigRoot.RotateAround(
            centerEye.position,
            Vector3.up,
            yawAngle
        );
    }

    private void SetLocomotionEnabled(
        bool enabledState)
    {
        if (locomotorObject != null &&
            locomotorObject.activeSelf != enabledState)
        {
            locomotorObject.SetActive(
                enabledState
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHideState playerState =
            other.GetComponentInParent<PlayerHideState>();

        if (playerState == null)
        {
            return;
        }

        nearbyPlayerColliders.Add(other);

        if (!isHidden)
        {
            ShowPrompt(
                "Press A to hide"
            );
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!nearbyPlayerColliders.Remove(other))
        {
            return;
        }

        if (!isHidden &&
            nearbyPlayerColliders.Count == 0)
        {
            HidePrompt();
        }
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

    private void OnDisable()
    {
        SetLocomotionEnabled(true);

        if (isHidden)
        {
            PlayerHideState.SetHidden(false);
        }

        isHidden = false;
        nearbyPlayerColliders.Clear();

        HidePrompt();
    }
}