using System;
using System.Reflection;
using TMPro;
using UnityEngine;

public class PlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapon References")]
    [Tooltip("Quest头显摄像机，一般绑定CenterEyeAnchor。")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("右手下面的WeaponHolder。")]
    [SerializeField] private GameObject heldWeaponObject;

    [Tooltip("枪口位置，必须放在枪管出口。")]
    [SerializeField] private Transform firePoint;

    [Tooltip("弹道Prefab。")]
    [SerializeField] private BulletTracer bulletTracerPrefab;

    [Header("Cooldown UI")]
    [SerializeField] private TMP_Text shootingCooldownText;

    [SerializeField] private string readyText = "READY";
    [SerializeField] private string cooldownPrefix = "NEXT SHOT: ";

    [Header("Shooting Settings")]
    [SerializeField] private float shootingRange = 20f;

    [Tooltip("两次射击之间的冷却时间。")]
    [SerializeField] private float shootingCooldown = 8f;

    [Tooltip("猪被击中后的眩晕时间。")]
    [SerializeField] private float pigStunDuration = 5f;

    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Quest Input")]
    [Tooltip("Quest右手食指扳机开枪。")]
    [SerializeField] private bool useQuestTrigger = true;

    [Tooltip("Unity编辑器中允许鼠标左键测试。")]
    [SerializeField] private bool allowMouseFallback = true;

    public bool HasWeapon { get; private set; }

    /// <summary>
    /// The muzzle used by both the shot and the XR aiming ray.
    /// </summary>
    public Transform FirePoint => firePoint;

    /// <summary>
    /// Keeps menu input on the controller while the gun is hidden.
    /// </summary>
    public bool IsWeaponVisualActive =>
        HasWeapon &&
        heldWeaponObject != null &&
        heldWeaponObject.activeInHierarchy &&
        firePoint != null;

    private float nextShootingTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera =
                GetComponentInChildren<Camera>(true);
        }

        HasWeapon = false;
        nextShootingTime = 0f;

        if (heldWeaponObject != null)
        {
            heldWeaponObject.SetActive(false);
        }

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

        if (WasShootPressed())
        {
            TryShoot();
        }
    }

    private bool WasShootPressed()
    {
        bool pressed = false;

        if (useQuestTrigger)
        {
            pressed = OVRInput.GetDown(
                OVRInput.Button.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch
            );
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (allowMouseFallback &&
            Input.GetMouseButtonDown(0))
        {
            pressed = true;
        }
#endif

        return pressed;
    }

    /// <summary>
    /// QuestWeaponAttacher会调用此方法，
    /// 自动设置手持武器与FirePoint。
    /// </summary>
    public void ConfigureHeldWeapon(
        GameObject newHeldWeaponObject,
        Transform newFirePoint)
    {
        heldWeaponObject = newHeldWeaponObject;

        if (newFirePoint != null)
        {
            firePoint = newFirePoint;
        }

        if (heldWeaponObject != null)
        {
            bool shouldShow =
                HasWeapon &&
                !PlayerHideState.IsHidden;

            heldWeaponObject.SetActive(shouldShow);
        }

        Debug.Log(
            "Held weapon configured: " +
            (
                heldWeaponObject != null
                    ? heldWeaponObject.name
                    : "None"
            )
        );

        Debug.Log(
            "FirePoint configured: " +
            (
                firePoint != null
                    ? firePoint.name
                    : "None"
            )
        );
    }

    /// <summary>
    /// WeaponPickup拾取武器时调用。
    /// </summary>
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

        Debug.Log(
            "Quest player picked up the weapon."
        );
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
        Transform shootingOrigin = firePoint;

        if (shootingOrigin == null &&
            playerCamera != null)
        {
            shootingOrigin =
                playerCamera.transform;

            Debug.LogWarning(
                "FirePoint is missing. " +
                "Using the camera as a fallback."
            );
        }

        if (shootingOrigin == null)
        {
            Debug.LogError(
                "PlayerWeaponSystem: " +
                "FirePoint and PlayerCamera are both missing."
            );

            return;
        }

        Ray shootingRay = new Ray(
            shootingOrigin.position,
            shootingOrigin.forward
        );

        Vector3 tracerStartPosition =
            shootingOrigin.position;

        Vector3 tracerEndPosition =
            shootingRay.origin +
            shootingRay.direction *
            shootingRange;

        bool hitSomething = TryGetFirstShotHit(
            shootingRay,
            shootingRange,
            out RaycastHit hit
        );

        if (hitSomething)
        {
            tracerEndPosition = hit.point;

            Debug.Log(
                "Shot hit: " +
                hit.collider.name
            );

            EnemyPigStun pigStun =
                hit.collider
                    .GetComponentInParent<EnemyPigStun>();

            if (pigStun != null)
            {
                ApplyPigHit(
                    pigStun,
                    shootingRay.direction,
                    shootingOrigin.position
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
            shootingRay.origin,
            shootingRay.direction *
            shootingRange,
            Color.green,
            2f
        );
    }

    /// <summary>
    /// Finds the first obstruction outside the player's rig and held gun.
    /// Hand/controller colliders therefore cannot consume a shot at its origin.
    /// </summary>
    public bool TryGetFirstShotHit(
        Ray ray,
        float maxDistance,
        out RaycastHit firstHit)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            maxDistance,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        Array.Sort(
            hits,
            (left, right) =>
                left.distance.CompareTo(right.distance)
        );

        foreach (RaycastHit candidate in hits)
        {
            if (OwnsCollider(candidate.collider))
            {
                continue;
            }

            firstHit = candidate;
            return true;
        }

        firstHit = default;
        return false;
    }

    public bool OwnsCollider(Collider candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Transform candidateTransform = candidate.transform;

        if (candidateTransform.IsChildOf(transform))
        {
            return true;
        }

        return
            heldWeaponObject != null &&
            candidateTransform.IsChildOf(
                heldWeaponObject.transform
            );
    }

    /// <summary>
    /// 同时兼容两种EnemyPigStun版本：
    ///
    /// Stun(float duration, Vector3 direction)
    /// ApplyHit(Vector3 attackerPosition)
    /// </summary>
    private void ApplyPigHit(
        EnemyPigStun pigStun,
        Vector3 shootingDirection,
        Vector3 attackerPosition)
    {
        Type pigStunType =
            pigStun.GetType();

        MethodInfo stunMethod =
            pigStunType.GetMethod(
                "Stun",
                new Type[]
                {
                    typeof(float),
                    typeof(Vector3)
                }
            );

        if (stunMethod != null)
        {
            stunMethod.Invoke(
                pigStun,
                new object[]
                {
                    pigStunDuration,
                    shootingDirection
                }
            );

            Debug.Log(
                "Pig hit using Stun()."
            );

            return;
        }

        MethodInfo applyHitMethod =
            pigStunType.GetMethod(
                "ApplyHit",
                new Type[]
                {
                    typeof(Vector3)
                }
            );

        if (applyHitMethod != null)
        {
            applyHitMethod.Invoke(
                pigStun,
                new object[]
                {
                    attackerPosition
                }
            );

            Debug.Log(
                "Pig hit using ApplyHit()."
            );

            return;
        }

        Debug.LogWarning(
            "EnemyPigStun does not contain " +
            "Stun(float, Vector3) or " +
            "ApplyHit(Vector3)."
        );
    }

    private void SpawnBulletTracer(
        Vector3 startPosition,
        Vector3 endPosition)
    {
        if (bulletTracerPrefab == null)
        {
            Debug.LogWarning(
                "BulletTracer prefab is not assigned."
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
            shootingCooldownText.text =
                readyText;
        }
    }

    private void UpdateWeaponVisuals(bool visible)
    {
        if (heldWeaponObject != null &&
            heldWeaponObject.activeSelf != visible)
        {
            heldWeaponObject.SetActive(visible);
        }

    }

    private void SetCooldownTextVisible(bool visible)
    {
        if (shootingCooldownText == null)
        {
            return;
        }

        if (shootingCooldownText.gameObject.activeSelf !=
            visible)
        {
            shootingCooldownText.gameObject.SetActive(
                visible
            );
        }
    }

    private void OnDisable()
    {
        SetCooldownTextVisible(false);
    }
}
