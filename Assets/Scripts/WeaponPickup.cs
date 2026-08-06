using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("World Weapon")]
    [Tooltip("Locker 03中展示的世界武器。")]
    [SerializeField] private GameObject worldWeaponObject;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;

    [Header("Quest Input")]
    [Tooltip("使用Quest右手B键拾取。")]
    [SerializeField] private bool useQuestButton = true;

    [Tooltip("Unity编辑器中允许E键测试。")]
    [SerializeField] private bool allowKeyboardFallback = true;

    private PlayerWeaponSystem nearbyPlayer;
    private readonly HashSet<Collider> nearbyPlayerColliders =
        new HashSet<Collider>();

    private bool wasPickedUp;

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (wasPickedUp ||
            nearbyPlayer == null ||
            nearbyPlayerColliders.Count == 0)
        {
            return;
        }

        if (WasInteractPressed())
        {
            PickUpWeapon();
        }
    }

    private bool WasInteractPressed()
    {
        bool pressed = false;

        /*
         * Quest右手B键。
         */
        if (useQuestButton)
        {
            pressed = OVRInput.GetDown(
                OVRInput.Button.Two,
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

    private void PickUpWeapon()
    {
        if (nearbyPlayer == null ||
            nearbyPlayer.HasWeapon)
        {
            return;
        }

        wasPickedUp = true;

        nearbyPlayer.EquipWeapon();

        if (worldWeaponObject != null)
        {
            worldWeaponObject.SetActive(false);
        }

        HidePrompt();

        Debug.Log(
            "Weapon picked up from Locker 03."
        );

        /*
         * 关闭拾取Trigger对象，
         * 防止再次拾取。
         */
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponSystem weaponSystem =
            other.GetComponentInParent<PlayerWeaponSystem>();

        if (weaponSystem == null ||
            weaponSystem.HasWeapon)
        {
            return;
        }

        /*
         * VR玩家可能有身体和双手多个Collider。
         * 使用集合记录，避免一个Collider退出后错误隐藏提示。
         */
        if (nearbyPlayer == null)
        {
            nearbyPlayer = weaponSystem;
        }

        if (weaponSystem != nearbyPlayer)
        {
            return;
        }

        nearbyPlayerColliders.Add(other);

        ShowPrompt(
            "Press B to pick up weapon"
        );

        Debug.Log(
            "Quest player entered weapon pickup range."
        );
    }

    private void OnTriggerExit(Collider other)
    {
        if (!nearbyPlayerColliders.Remove(other))
        {
            return;
        }

        if (nearbyPlayerColliders.Count > 0)
        {
            return;
        }

        nearbyPlayer = null;
        HidePrompt();
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
        nearbyPlayerColliders.Clear();
        nearbyPlayer = null;

        HidePrompt();
    }
}