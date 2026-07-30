using TMPro;
using UnityEngine;

public class PlayerWeaponSystem : MonoBehaviour
{
    [Header("Weapon References")]
    [Tooltip("Quest头显摄像机，一般绑定CenterEyeAnchor。")]
    [SerializeField] private Camera playerCamera;

    [Tooltip("右手下的WeaponHolder，拾取前隐藏，拾取后显示。")]
    [SerializeField] private GameObject heldWeaponObject;

    [Tooltip("VR HUD中的准星对象。")]
    [SerializeField] private GameObject crosshairObject;

    [Tooltip("枪口位置，必须放在枪管出口。")]
    [SerializeField] private Transform firePoint;

    [Tooltip("带有BulletTracer和LineRenderer的弹道Prefab。")]
    [SerializeField] private BulletTracer bulletTracerPrefab;

    [Header("Cooldown UI")]
    [Tooltip("显示READY或下一次射击时间的TMP文字。")]
    [SerializeField] private TMP_Text shootingCooldownText;

    [SerializeField] private string readyText = "READY";
    [SerializeField] private string cooldownPrefix = "NEXT SHOT: ";

    [Header("Shooting Settings")]
    [SerializeField] private float shootingRange = 20f;

    [Tooltip("两次射击之间的等待时间。")]
    [SerializeField] private float shootingCooldown = 8f;

    [Tooltip("猪被击中后的眩晕时间。")]
    [SerializeField] private float pigStunDuration = 5f;

    [Tooltip("射线能够击中的Layer。")]
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Quest Input")]
    [Tooltip("使用Quest右手食指扳机开枪。")]
    [SerializeField] private bool useQuestTrigger = true;

    [Tooltip("在Unity编辑器中允许鼠标左键测试。")]
    [SerializeField] private bool allowMouseFallback = true;

    public bool HasWeapon { get; private set; }

    private float nextShootingTime;

    private void Awake()
    {
        /*
         * 如果没有手动绑定摄像机，
         * 自动寻找Camera Rig下面的Camera。
         */
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

        if (WasShootPressed())
        {
            TryShoot();
        }
    }

    private bool WasShootPressed()
    {
        bool pressed = false;

        /*
         * Quest右手食指扳机。
         */
        if (useQuestTrigger)
        {
            pressed = OVRInput.GetDown(
                OVRInput.Button.PrimaryIndexTrigger,
                OVRInput.Controller.RTouch
            );
        }

        /*
         * Unity编辑器中的鼠标备用测试。
         */
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
    /// 由WeaponPickup在玩家拾取武器时调用。
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

        Debug.Log("Quest player picked up the weapon.");
    }

    private void TryShoot()
    {
        if (Time.time < nextShootingTime)
        {
            float remaining =
                nextShootingTime - Time.time;

            Debug.Log(
                "Weapon cooling down: " +
                remaining.ToString("F1") +
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
        Transform shootingOrigin = null;

        /*
         * 正常情况下必须使用FirePoint。
         * 没绑定时才临时使用头显摄像机。
         */
        if (firePoint != null)
        {
            shootingOrigin = firePoint;
        }
        else if (playerCamera != null)
        {
            shootingOrigin = playerCamera.transform;

            Debug.LogWarning(
                "PlayerWeaponSystem: FirePoint is missing. " +
                "Using the player camera as a fallback."
            );
        }

        if (shootingOrigin == null)
        {
            Debug.LogWarning(
                "PlayerWeaponSystem: Neither FirePoint nor PlayerCamera is assigned."
            );

            return;
        }

        /*
         * Quest模式中：
         * 枪口指向哪里，射线就飞向哪里。
         */
        Ray shootingRay = new Ray(
            shootingOrigin.position,
            shootingOrigin.forward
        );

        Vector3 tracerStartPosition =
            shootingOrigin.position;

        Vector3 tracerEndPosition =
            shootingRay.origin +
            shootingRay.direction * shootingRange;

        bool hitSomething = Physics.Raycast(
            shootingRay,
            out RaycastHit hit,
            shootingRange,
            hitMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitSomething)
        {
            tracerEndPosition = hit.point;

            Debug.Log(
                "Shot hit: " +
                hit.collider.name
            );

            /*
             * 猪的碰撞体可能在子物体上，
             * 所以向父物体寻找EnemyPigStun。
             */
            EnemyPigStun pigStun =
                hit.collider.GetComponentInParent<EnemyPigStun>();

            if (pigStun != null)
            {
                pigStun.Stun(
                    pigStunDuration,
                    shootingRay.direction
                );

                Debug.Log(
                    "Pig hit. Stun duration: " +
                    pigStunDuration.ToString("F1")
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
            shootingRay.direction * shootingRange,
            Color.green,
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
                "PlayerWeaponSystem: BulletTracer prefab is not assigned."
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