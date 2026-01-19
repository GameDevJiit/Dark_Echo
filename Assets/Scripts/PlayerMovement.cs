using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 4f;
    public float rotationSpeed = 10f;
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;
    public float punchCooldown = 0.5f;

    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    public int fallDamageThreshold = 10;
    public int fallDamagePerUnit = 5;
    public int enemyDamage = 5;

    [Header("Combat Settings")]
    public float punchDamage = 10f;
    public float punchRange = 1.5f;
    public LayerMask enemyLayer;

    [Header("UI References")]
    public Slider healthSlider;
    public Image healthFill;
    public Color fullHealthColor = Color.green;
    public Color lowHealthColor = Color.red;
    public TMP_Text weaponPromptText;
    public TMP_Text ammoText;
    public TMP_Text emptyGunText;
    public TMP_Text reloadPromptText;
    public float emptyTextDuration = 2f;

    [Header("Camera Settings")]
    public Transform cameraPivot;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 80f;
    private float xRotation = 0f;
    private float yRotation = 0f;

    [Header("FPS Aim Settings")]
    public bool isInFPSMode = false;
    public Camera playerCamera;
    public Transform fpsCameraPosition;
    public float aimFOV = 60f;
    public float normalFOV = 60f;
    public GameObject crosshair;
    private Vector3 originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;
    private Transform originalCameraParent;

    [Header("Scene Settings")]
    public string deathSceneName = "DeathScene";
    public float deathAnimationDelay = 2f;

    [Header("Animation Triggers")]
    public string dieTrigger = "Die";
    public string hitTrigger = "Hit";
    public string leftPunchTrigger = "LeftPunch";
    public string rightPunchTrigger = "RightPunch";
    public string weaponAttackTrigger = "WeaponAttack";
    public string takeDamageTrigger = "TakeDamage";
    public string reloadTrigger = "Reload";

    [Header("Weapon Settings")]
    public bool hasWeapon = false;
    public GameObject weaponObject;
    public float weaponDamage = 20f;
    public float weaponRange = 50f;
    public float weaponPickupRange = 2f;

    [Header("Ammo Settings")]
    public int maxBullets = 5;
    public int currentBullets = 0;
    public float reloadTime = 1.5f;
    public float ammoPickupRange = 3f;
    private GameObject nearbyAmmo = null;

    [Header("Muzzle Flash Effects")]
    public ParticleSystem muzzleFlash;
    public Light muzzleLight;
    public float muzzleLightDuration = 0.05f;
    public float barrelForwardOffset = 0.8f;

    [Header("Aiming Settings")]
    public LayerMask shootableLayers = ~0;
    public float maxShootDistance = 100f;
    private Vector3 aimPoint;
    private bool isAimingAtEnemy = false;
    private GameObject currentAimedEnemy = null;

    [Header("Hit Effect")]
    public GameObject hitEffectPrefab;
    public float hitEffectDuration = 2f;

    [Header("Enemy Hit Settings")]
    public float enemyHealth = 100f;
    public float shotDamage = 33.34f;

    [Header("Weapon Hand Settings")]
    public Transform rightHandTransform;

    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioSource gunAudioSource;
    public AudioSource footstepsAudioSource;
    public AudioClip hitSound;
    public AudioClip deathSound;
    public AudioClip punchSound;
    public AudioClip weaponSwingSound;
    public AudioClip weaponPickupSound;
    public AudioClip gunShootSound;
    public AudioClip reloadSound;
    public AudioClip emptyGunSound;
    public AudioClip ammoPickupSound;
    public AudioClip enemyHitSound;
    public AudioClip jumpSound;
    public AudioClip landSound;
    public AudioClip[] footsteps;
    public float footstepInterval = 0.5f;
    private float footstepTimer = 0f;
    private bool wasGrounded = true;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float fallStartY;
    private bool isFalling = false;
    private bool isDead = false;
    private bool canPunch = true;
    private bool leftPunchNext = true;
    private float lastHitTime = 0f;
    private float hitCooldown = 0.5f;
    private GameObject nearbyWeapon = null;
    private GameObject equippedWeaponInstance = null;
    private bool isReloading = false;
    private Coroutine emptyTextCoroutine;
    private float muzzleLightTimer;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // Setup ALL audio sources
        SetupAudioSources();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        // Setup camera pivot if not assigned
        if (cameraPivot == null)
        {
            cameraPivot = transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                GameObject pivot = new GameObject("CameraPivot");
                pivot.transform.SetParent(transform);
                pivot.transform.localPosition = new Vector3(0, 1.7f, 0.5f);
                pivot.transform.localRotation = Quaternion.identity;
                cameraPivot = pivot.transform;
            }
        }

        // Store original camera setup
        if (playerCamera != null)
        {
            originalCameraParent = playerCamera.transform.parent;
            originalCameraLocalPos = playerCamera.transform.localPosition;
            originalCameraLocalRot = playerCamera.transform.localRotation;
        }

        currentHealth = maxHealth;
        currentBullets = 0;
        UpdateHealthUI();
        UpdateAmmoUI();

        // Create weapon holder if needed
        if (weaponObject == null && rightHandTransform != null)
        {
            weaponObject = new GameObject("WeaponHolder");
            weaponObject.transform.SetParent(rightHandTransform);
            weaponObject.transform.localPosition = Vector3.zero;
            weaponObject.transform.localRotation = Quaternion.identity;
        }

        // Initialize crosshair
        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }

        // Initialize UI
        if (weaponPromptText != null)
            weaponPromptText.gameObject.SetActive(false);

        if (emptyGunText != null)
            emptyGunText.gameObject.SetActive(false);

        if (reloadPromptText != null)
            reloadPromptText.gameObject.SetActive(false);

        // Initialize muzzle light
        if (muzzleLight != null)
        {
            muzzleLight.enabled = false;
        }

        // Set FOV to 60
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = 60f;
        }

        // Initial cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void SetupAudioSources()
    {
        // Main audio source (for general sounds)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound

        // Gun audio source (for shooting sounds)
        if (gunAudioSource == null)
        {
            gunAudioSource = gameObject.AddComponent<AudioSource>();
        }
        gunAudioSource.playOnAwake = false;
        gunAudioSource.spatialBlend = 0f; // 2D sound for gunshots
        gunAudioSource.volume = 0.7f;

        // Footsteps audio source
        if (footstepsAudioSource == null)
        {
            footstepsAudioSource = gameObject.AddComponent<AudioSource>();
        }
        footstepsAudioSource.playOnAwake = false;
        footstepsAudioSource.spatialBlend = 1f;
        footstepsAudioSource.volume = 0.3f;
        footstepsAudioSource.loop = false;
    }

    void Update()
    {
        if (isDead) return;

        // Handle camera rotation in FPS mode
        if (isInFPSMode)
        {
            HandleFPSRotation();
        }

        // Toggle FPS mode with Right Click
        if (Input.GetMouseButtonDown(1))
        {
            if (!isInFPSMode && hasWeapon && !isReloading)
            {
                EnterFPSMode();
            }
            else if (isInFPSMode)
            {
                ExitFPSMode();
            }
        }

        // Update aiming
        UpdateAiming();

        // Update crosshair position in FPS mode
        if (isInFPSMode && crosshair != null)
        {
            crosshair.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }

        // Update weapon animation
        if (!IsAttacking() && !isReloading)
        {
            animator.SetBool("HasWeapon", hasWeapon);
        }

        // Update muzzle light
        if (muzzleLightTimer > 0)
        {
            muzzleLightTimer -= Time.deltaTime;
            if (muzzleLightTimer <= 0 && muzzleLight != null)
            {
                muzzleLight.enabled = false;
            }
        }

        // Movement and physics
        bool isGrounded = controller.isGrounded;
        animator.SetBool("IsGrounded", isGrounded);

        // Footstep sounds
        HandleFootsteps(isGrounded);

        HandleFallDamage(isGrounded);

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(x, 0, z);

        // Movement direction
        if (isInFPSMode)
        {
            // FPS movement relative to camera
            Vector3 cameraForward = playerCamera.transform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            Vector3 cameraRight = playerCamera.transform.right;
            cameraRight.y = 0;
            cameraRight.Normalize();

            move = (cameraForward * z + cameraRight * x).normalized;
        }
        else
        {
            // Normal movement
            move = playerCamera.transform.TransformDirection(move);
        }

        move.y = 0f;

        // Rotate player in normal mode
        if (move.magnitude > 0.1f && !isInFPSMode)
        {
            Quaternion rot = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, rotationSpeed * Time.deltaTime);
        }

        controller.Move(move * speed * Time.deltaTime);
        animator.SetFloat("MoveX", x);
        animator.SetFloat("MoveZ", z);

        // Jumping (not in FPS mode)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && !IsAttacking() && !isReloading && !isInFPSMode)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
            PlaySound(jumpSound, audioSource);
        }

        // Check for ammo
        CheckForNearbyAmmo();

        // Reload
        if (Input.GetKeyDown(KeyCode.R) && hasWeapon && !isReloading && nearbyAmmo != null)
        {
            PickupAmmo();
        }

        // SHOOTING - Left Click
        if (Input.GetMouseButtonDown(0) && hasWeapon && !isReloading && currentBullets > 0)
        {
            ShootRaycast();
        }
        else if (Input.GetMouseButtonDown(0) && hasWeapon && !isReloading && currentBullets <= 0)
        {
            ShowEmptyGunText();
            PlaySound(emptyGunSound, gunAudioSource);
        }

        // Punching (no weapon, not in FPS)
        if (Input.GetMouseButtonDown(0) && !hasWeapon && !IsAttacking() && canPunch && !isInFPSMode)
        {
            if (leftPunchNext)
                LeftPunch();
            else
                RightPunch();

            leftPunchNext = !leftPunchNext;
            StartCoroutine(PunchCooldown());
        }

        // Pickup/Drop weapon
        if (Input.GetKeyDown(KeyCode.T) && !IsAttacking() && !isReloading)
        {
            if (!hasWeapon && nearbyWeapon != null)
            {
                PickupWeapon(nearbyWeapon);
            }
            else if (hasWeapon)
            {
                DropWeapon();
            }
        }

        CheckForNearbyWeapons();

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFootsteps(bool isGrounded)
    {
        if (!isGrounded || controller.velocity.magnitude < 0.1f)
        {
            footstepTimer = 0f;
            wasGrounded = isGrounded;
            return;
        }

        // Play landing sound
        if (!wasGrounded && isGrounded)
        {
            PlaySound(landSound, footstepsAudioSource);
        }

        // Play footsteps at intervals
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            if (footsteps != null && footsteps.Length > 0)
            {
                AudioClip footstep = footsteps[Random.Range(0, footsteps.Length)];
                PlaySound(footstep, footstepsAudioSource);
            }
            footstepTimer = footstepInterval;
        }

        wasGrounded = isGrounded;
    }

    void HandleFPSRotation()
    {
        if (!isInFPSMode || playerCamera == null) return;

        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player left/right (yaw)
        yRotation += mouseX;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

        // Rotate camera up/down (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -maxLookAngle, maxLookAngle);

        // Apply rotation to camera
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void EnterFPSMode()
    {
        if (!hasWeapon || isReloading) return;

        isInFPSMode = true;
        animator.SetBool("IsAiming", true);

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Show crosshair
        if (crosshair != null)
        {
            crosshair.SetActive(true);
            crosshair.transform.position = new Vector3(Screen.width / 2, Screen.height / 2, 0);
        }

        // Store original camera position if first time
        if (originalCameraLocalPos == Vector3.zero && playerCamera != null)
        {
            originalCameraParent = playerCamera.transform.parent;
            originalCameraLocalPos = playerCamera.transform.localPosition;
            originalCameraLocalRot = playerCamera.transform.localRotation;
        }

        // Set FOV to 60
        playerCamera.fieldOfView = 60f;

        // Move camera to gun position
        if (fpsCameraPosition != null)
        {
            playerCamera.transform.SetParent(null);
            playerCamera.transform.position = fpsCameraPosition.position;
            playerCamera.transform.rotation = fpsCameraPosition.rotation;
            playerCamera.transform.SetParent(fpsCameraPosition);
        }

        // Reset rotations
        xRotation = 0f;
        yRotation = transform.eulerAngles.y;
    }

    void ExitFPSMode()
    {
        isInFPSMode = false;
        animator.SetBool("IsAiming", false);

        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Hide crosshair
        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }

        // Return camera to original position
        if (playerCamera != null)
        {
            playerCamera.transform.SetParent(null);

            if (originalCameraParent != null)
            {
                playerCamera.transform.SetParent(originalCameraParent);
            }
            else if (cameraPivot != null)
            {
                playerCamera.transform.SetParent(cameraPivot);
            }

            playerCamera.transform.localPosition = originalCameraLocalPos;
            playerCamera.transform.localRotation = originalCameraLocalRot;
        }

        // Keep FOV at 60
        playerCamera.fieldOfView = 60f;
    }

    void UpdateAiming()
    {
        if (playerCamera == null) return;

        Ray ray;
        // ALWAYS use center of screen for aiming (both FPS and TPP)
        if (isInFPSMode)
        {
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        else
        {
            // TPP also uses center screen for crosshair detection
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }

        RaycastHit hit;

        // FIX: Increase raycast distance for better enemy detection
        if (Physics.Raycast(ray, out hit, maxShootDistance * 2f, shootableLayers))
        {
            aimPoint = hit.point;

            // FIXED: Use UniversalEnemy_NoNavMesh instead of UniversalEnemy
            UniversalEnemy enemyScript = hit.collider.GetComponentInParent<UniversalEnemy>();

            if (enemyScript != null)
            {
                float distance = Vector3.Distance(playerCamera.transform.position, hit.point);

                if (distance <= maxShootDistance)
                {
                    isAimingAtEnemy = true;
                    currentAimedEnemy = hit.collider.gameObject;

                    if (crosshair != null && isInFPSMode)
                    {
                        crosshair.GetComponent<Image>().color = Color.red;
                    }
                }
                else
                {
                    ResetAimingColors();
                }
            }
            else
            {
                ResetAimingColors();
            }
        }
        else
        {
            aimPoint = ray.origin + ray.direction * maxShootDistance;
            ResetAimingColors();
        }
    }

    void ResetAimingColors()
    {
        isAimingAtEnemy = false;
        currentAimedEnemy = null;

        if (crosshair != null && isInFPSMode)
        {
            crosshair.GetComponent<Image>().color = Color.white;
        }
    }

    void ShootRaycast()
    {
        if (!hasWeapon || currentBullets <= 0 || isReloading)
            return;

        currentBullets--;
        UpdateAmmoUI();

        animator.SetTrigger(weaponAttackTrigger);
        PlaySound(gunShootSound, gunAudioSource);
        PlayMuzzleFlash();

        Ray ray;
        // ALWAYS shoot from center screen (where crosshair is)
        if (isInFPSMode)
        {
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }
        else
        {
            // TPP also uses center screen
            ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        }

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxShootDistance, shootableLayers))
        {
            // SPAWN HIT EFFECT AT EXACT HIT POINT
            if (hitEffectPrefab != null)
            {
                GameObject hitEffect = Instantiate(
                    hitEffectPrefab,
                    hit.point,
                    Quaternion.identity
                );

                hitEffect.transform.rotation = Quaternion.LookRotation(hit.normal);
                Destroy(hitEffect, hitEffectDuration);
            }

            // FIXED: Use UniversalEnemy_NoNavMesh
            UniversalEnemy enemyScript = hit.collider.GetComponentInParent<UniversalEnemy>();
            if (enemyScript != null)
            {
                PlaySound(enemyHitSound, audioSource);
                enemyScript.TakeDamage(shotDamage);
                Debug.Log($"Hit enemy: {hit.collider.name}, Damage: {shotDamage}");
            }
        }

        StartCoroutine(PunchCooldown());
    }

    void PlayMuzzleFlash()
    {
        if (muzzleFlash != null && equippedWeaponInstance != null)
        {
            Vector3 barrelPosition = equippedWeaponInstance.transform.position +
                equippedWeaponInstance.transform.forward * barrelForwardOffset;

            muzzleFlash.transform.position = barrelPosition;
            muzzleFlash.transform.rotation = equippedWeaponInstance.transform.rotation;
            muzzleFlash.Play();
        }

        if (muzzleLight != null && equippedWeaponInstance != null)
        {
            Vector3 barrelPosition = equippedWeaponInstance.transform.position +
                equippedWeaponInstance.transform.forward * barrelForwardOffset;

            muzzleLight.transform.position = barrelPosition;
            muzzleLight.enabled = true;
            muzzleLightTimer = muzzleLightDuration;
        }
    }

    void PlaySound(AudioClip clip, AudioSource source)
    {
        if (clip != null && source != null)
        {
            source.PlayOneShot(clip);
        }
        else if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void CheckForNearbyAmmo()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, ammoPickupRange);
        nearbyAmmo = null;

        foreach (Collider c in hits)
        {
            if (c.CompareTag("Ammo") && hasWeapon)
            {
                nearbyAmmo = c.gameObject;
                break;
            }
        }

        if (reloadPromptText != null)
        {
            reloadPromptText.gameObject.SetActive(nearbyAmmo != null && hasWeapon);
            if (nearbyAmmo != null && hasWeapon)
            {
                reloadPromptText.text = "Press R to reload (pick up ammo)";
            }
        }
    }

    void PickupAmmo()
    {
        if (nearbyAmmo == null || !hasWeapon || isReloading)
            return;

        isReloading = true;
        animator.SetTrigger(reloadTrigger);
        animator.SetBool("IsReloading", true);
        PlaySound(reloadSound, audioSource);

        StartCoroutine(ReloadCoroutine(nearbyAmmo));
    }

    IEnumerator ReloadCoroutine(GameObject ammoPickup)
    {
        yield return new WaitForSeconds(reloadTime);

        currentBullets = maxBullets;
        UpdateAmmoUI();

        if (ammoPickup != null)
            Destroy(ammoPickup);
        nearbyAmmo = null;

        PlaySound(ammoPickupSound, audioSource);
        isReloading = false;
        animator.SetBool("IsReloading", false);
    }

    void ShowEmptyGunText()
    {
        if (emptyGunText != null)
        {
            emptyGunText.gameObject.SetActive(true);
            emptyGunText.text = "GUN EMPTY! Find ammo to reload";

            if (emptyTextCoroutine != null)
                StopCoroutine(emptyTextCoroutine);

            emptyTextCoroutine = StartCoroutine(HideEmptyTextAfterDelay());
        }
    }

    IEnumerator HideEmptyTextAfterDelay()
    {
        yield return new WaitForSeconds(emptyTextDuration);
        if (emptyGunText != null)
            emptyGunText.gameObject.SetActive(false);
    }

    bool IsAttacking()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsTag("Attack") ||
               stateInfo.IsName("LeftPunch") ||
               stateInfo.IsName("RightPunch") ||
               stateInfo.IsName("WeaponAttack");
    }

    void CheckForNearbyWeapons()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, weaponPickupRange);
        nearbyWeapon = null;

        foreach (Collider c in hits)
        {
            if (c.CompareTag("Weapon") && !hasWeapon)
            {
                nearbyWeapon = c.gameObject;
                break;
            }
        }

        if (weaponPromptText != null)
        {
            weaponPromptText.gameObject.SetActive(hasWeapon || nearbyWeapon != null);
            if (hasWeapon)
            {
                weaponPromptText.text = "Press T to drop weapon";
            }
            else if (nearbyWeapon != null)
            {
                weaponPromptText.text = "Press T to pick up weapon";
            }
        }
    }

    void PickupWeapon(GameObject weaponPickup)
    {
        if (hasWeapon || weaponPickup == null)
            return;

        hasWeapon = true;
        currentBullets = 0;

        equippedWeaponInstance = weaponPickup;
        animator.SetBool("HasWeapon", true);

        if (weaponObject != null)
        {
            Rigidbody rb = equippedWeaponInstance.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            Collider[] colliders = equippedWeaponInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            equippedWeaponInstance.transform.SetParent(weaponObject.transform);
            equippedWeaponInstance.transform.localPosition = Vector3.zero;
            equippedWeaponInstance.transform.localRotation = Quaternion.identity;
            equippedWeaponInstance.tag = "Untagged";
        }

        PlaySound(weaponPickupSound, audioSource);
        UpdateAmmoUI();
        nearbyWeapon = null;
    }

    void DropWeapon()
    {
        if (!hasWeapon || equippedWeaponInstance == null)
            return;

        if (isInFPSMode)
        {
            ExitFPSMode();
        }

        hasWeapon = false;
        currentBullets = 0;
        isReloading = false;
        animator.SetBool("IsReloading", false);
        animator.SetBool("HasWeapon", false);

        equippedWeaponInstance.transform.SetParent(null);

        Rigidbody rb = equippedWeaponInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.AddForce(transform.forward * 3f, ForceMode.Impulse);
        }

        Collider[] colliders = equippedWeaponInstance.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }

        equippedWeaponInstance.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 0.5f;
        equippedWeaponInstance.tag = "Weapon";

        if (muzzleFlash != null) muzzleFlash.Stop();
        if (muzzleLight != null) muzzleLight.enabled = false;

        equippedWeaponInstance = null;
        PlaySound(weaponPickupSound, audioSource);
        UpdateAmmoUI();
    }

    void LeftPunch()
    {
        animator.SetTrigger(leftPunchTrigger);
        animator.SetBool("HasWeapon", false);
        PlaySound(punchSound, audioSource);
        PerformAttack(punchDamage, punchRange);
    }

    void RightPunch()
    {
        animator.SetTrigger(rightPunchTrigger);
        animator.SetBool("HasWeapon", false);
        PlaySound(punchSound, audioSource);
        PerformAttack(punchDamage, punchRange);
    }

    void PerformAttack(float dmg, float range)
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Collider[] hits = Physics.OverlapSphere(origin + transform.forward * (range * 0.5f), range * 0.5f, enemyLayer);

        foreach (Collider e in hits)
        {
            UniversalEnemy enemyScript = e.GetComponentInParent<UniversalEnemy>();
            if (enemyScript != null)
            {
                enemyScript.TakeDamage(dmg);
                Debug.Log("Punch hit: " + e.name + " for " + dmg + " damage");
            }
        }
    }

    void UpdateAmmoUI()
    {
        if (ammoText != null)
        {
            ammoText.text = "Ammo: " + currentBullets + "/" + maxBullets;
            ammoText.gameObject.SetActive(hasWeapon);
        }
    }

    IEnumerator PunchCooldown()
    {
        canPunch = false;
        yield return new WaitForSeconds(punchCooldown);
        canPunch = true;
    }

    public void TakeDamage(int dmg, string src = "Enemy")
    {
        if (isDead) return;
        if (Time.time - lastHitTime < hitCooldown) return;

        lastHitTime = Time.time;
        currentHealth -= dmg;

        // FIX: Changed from takeDamageTrigger to hitTrigger to match your animator parameter
        animator.SetTrigger(hitTrigger); // This should trigger the "Hit" parameter
        PlaySound(hitSound, audioSource);
        UpdateHealthUI();

        if (currentHealth <= 0)
            Die();
    }

    void UpdateHealthUI()
    {
        if (healthSlider != null)
            healthSlider.value = currentHealth;
        if (healthFill != null)
            healthFill.color = Color.Lerp(lowHealthColor, fullHealthColor, (float)currentHealth / maxHealth);
    }

    void Die()
    {
        isDead = true;
        animator.SetTrigger(dieTrigger);
        PlaySound(deathSound, audioSource);
        enabled = false;

        if (crosshair != null) crosshair.SetActive(false);
        if (hasWeapon) DropWeapon();

        StartCoroutine(LoadDeathSceneAfterDelay(deathAnimationDelay));
    }

    IEnumerator LoadDeathSceneAfterDelay(float d)
    {
        yield return new WaitForSeconds(d);
        SceneManager.LoadScene(deathSceneName);
    }

    void HandleFallDamage(bool isGrounded)
    {
        if (!isGrounded && !isFalling)
        {
            fallStartY = transform.position.y;
            isFalling = true;
        }

        if (isGrounded && isFalling)
        {
            float fallDistance = fallStartY - transform.position.y;

            if (fallDistance > fallDamageThreshold)
            {
                int damage = Mathf.FloorToInt((fallDistance - fallDamageThreshold) * fallDamagePerUnit);
                TakeDamage(damage, "Fall");
            }

            isFalling = false;
        }
    }
}