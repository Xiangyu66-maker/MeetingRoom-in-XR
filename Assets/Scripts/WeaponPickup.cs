using TMPro;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("World Weapon")]
    [SerializeField] private GameObject worldWeaponObject;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptObject;
    [SerializeField] private TMP_Text promptText;

    private PlayerWeaponSystem nearbyPlayer;
    private bool playerInRange;
    private bool wasPickedUp;

    private void Start()
    {
        HidePrompt();
    }

    private void Update()
    {
        if (wasPickedUp ||
            !playerInRange ||
            nearbyPlayer == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            PickUpWeapon();
        }
    }

    private void PickUpWeapon()
    {
        wasPickedUp = true;

        nearbyPlayer.EquipWeapon();

        if (worldWeaponObject != null)
        {
            worldWeaponObject.SetActive(false);
        }

        HidePrompt();

        Debug.Log("Weapon picked up from Locker 03.");

        // 关闭拾取区域，防止重复按E
        gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeaponSystem weaponSystem =
            other.GetComponentInParent<PlayerWeaponSystem>();

        if (weaponSystem == null || weaponSystem.HasWeapon)
        {
            return;
        }

        nearbyPlayer = weaponSystem;
        playerInRange = true;

        ShowPrompt("Press E to pick up weapon");

        Debug.Log("Player entered weapon pickup range.");
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerWeaponSystem weaponSystem =
            other.GetComponentInParent<PlayerWeaponSystem>();

        if (weaponSystem == null ||
            weaponSystem != nearbyPlayer)
        {
            return;
        }

        playerInRange = false;
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
        HidePrompt();
    }
}