using TMPro;
using UnityEngine;

public class PlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapon References")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("The held weapon or WeaponHolder object.")]
    [SerializeField] private GameObject heldWeaponObject;

    [SerializeField] private GameObject crosshairObject;

    [Tooltip("Position at the end of the gun barrel.")]
    [SerializeField] private Transform firePoint;

    [Tooltip("Prefab containing BulletTracer and LineRenderer.")]
    [SerializeField] private BulletTracer bulletTracerPrefab;

    [Header("Cooldown UI")]
    [Tooltip("Text that displays time until the next shot.")]
    [SerializeField] private TMP_Text shootingCooldownText;

    [SerializeField] private string readyText = "READY";

    [SerializeField] private string cooldownPrefix = "NEXT SHOT: ";

    [Header("Shooting Settings")]
    [SerializeField] private float shootingRange = 20f;

    [Tooltip("Time between two shots.")]
    [SerializeField] private float shootingCooldown = 3f;

    [Tooltip("How long the pig remains stunned.")]
    [SerializeField] private float pigStunDuration = 3f;

    [SerializeField] private LayerMask hitMask = ~0;

    public bool HasWeapon { get; private set; }

    private float nextShootingTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        HasWeapon = false;
        nextShootingTime = 0f;

        if (heldWeaponObject != null)
        {
            heldWeaponObject.SetActive(false);
        }

        SetCrosshairVisible(false);
        SetCooldownTextVisible(false);
    }

    private void Update()
    {
        bool canUseWeapon =
            HasWeapon &&
            !PlayerHideState.IsHidden &&
            Time.timeScale > 0f;

        UpdateWeaponVisuals(canUseWeapon);
        UpdateCooldownUI(canUseWeapon);

        if (!canUseWeapon)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryShoot();
        }
    }

    public void EquipWeapon()
    {
        if (HasWeapon)
        {
            return;
        }

        HasWeapon = true;
        nextShootingTime = Time.time;

        UpdateWeaponVisuals(true);
        UpdateCooldownUI(true);

        Debug.Log("Player picked up the weapon.");
    }

    private void TryShoot()
    {
        if (Time.time < nextShootingTime)
        {
            float remainingTime =
                nextShootingTime - Time.time;

            Debug.Log(
                "Weapon cooling down: " +
                remainingTime.ToString("F1") +
                " seconds."
            );

            return;
        }

        nextShootingTime =
            Time.time + shootingCooldown;

        Shoot();
        UpdateCooldownUI(true);
    }

    private void Shoot()
    {
        if (playerCamera == null)
        {
            Debug.LogWarning(
                "PlayerWeaponSystem: Player Camera is not assigned."
            );

            return;
        }

        Ray aimingRay =
            playerCamera.ViewportPointToRay(
                new Vector3(0.5f, 0.5f, 0f)
            );

        Vector3 tracerStartPosition =
            firePoint != null
                ? firePoint.position
                : aimingRay.origin;

        Vector3 tracerEndPosition =
            aimingRay.origin +
            aimingRay.direction * shootingRange;

        bool hitSomething = Physics.Raycast(
            aimingRay,
            out RaycastHit hit,
            shootingRange,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            tracerEndPosition = hit.point;

            Debug.Log("Shot hit: " + hit.collider.name);

            EnemyPigStun pigStun =
                hit.collider.GetComponentInParent<EnemyPigStun>();

            if (pigStun != null)
            {
                pigStun.Stun(
                    pigStunDuration,
                    aimingRay.direction
                );

                Debug.Log(
                    "Pig was hit and stunned for " +
                    pigStunDuration +
                    " seconds."
                );
            }
        }
        else
        {
            Debug.Log("Shot missed.");
        }

        SpawnBulletTracer(
            tracerStartPosition,
            tracerEndPosition
        );

        Debug.DrawRay(
            aimingRay.origin,
            aimingRay.direction * shootingRange,
            Color.red,
            1f
        );
    }

    private void SpawnBulletTracer(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        if (bulletTracerPrefab == null)
        {
            Debug.LogWarning(
                "PlayerWeaponSystem: Bullet Tracer Prefab is not assigned."
            );

            return;
        }

        BulletTracer tracer = Instantiate(
            bulletTracerPrefab,
            startPosition,
            Quaternion.identity
        );

        tracer.Play(
            startPosition,
            endPosition
        );
    }

    private void UpdateCooldownUI(bool visible)
    {
        if (shootingCooldownText == null)
        {
            return;
        }

        SetCooldownTextVisible(visible);

        if (!visible)
        {
            return;
        }

        float remainingTime =
            nextShootingTime - Time.time;

        if (remainingTime > 0f)
        {
            shootingCooldownText.text =
                cooldownPrefix +
                remainingTime.ToString("F1") +
                "s";
        }
        else
        {
            shootingCooldownText.text = readyText;
        }
    }

    private void UpdateWeaponVisuals(bool visible)
    {
        if (heldWeaponObject != null &&
            heldWeaponObject.activeSelf != visible)
        {
            heldWeaponObject.SetActive(visible);
        }

        SetCrosshairVisible(visible);
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (crosshairObject != null &&
            crosshairObject.activeSelf != visible)
        {
            crosshairObject.SetActive(visible);
        }
    }

    private void SetCooldownTextVisible(bool visible)
    {
        if (shootingCooldownText != null &&
            shootingCooldownText.gameObject.activeSelf != visible)
        {
            shootingCooldownText.gameObject.SetActive(visible);
        }
    }

    private void OnDisable()
    {
        SetCrosshairVisible(false);
        SetCooldownTextVisible(false);
    }
}