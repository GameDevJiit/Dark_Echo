using UnityEngine;
using System.Collections;

public class UniversalEnemy : MonoBehaviour
{
    [Header("🎯 ENEMY TYPE SETTINGS")]
    [Tooltip("Name this enemy type (Zombie, Fast Zombie, Boss, etc.)")]
    public string enemyType = "Zombie";
    [Tooltip("Bullets needed to kill this enemy")]
    [Range(1, 20)] public int bulletsToKill = 3;
    [Tooltip("Visual feedback when hit?")]
    public bool flashOnHit = true;

    [Header("🎭 ANIMATION SETUP")]
    [Tooltip("Drag your model's Animator here")]
    public Animator enemyAnimator;
    [Tooltip("Speed multiplier for animation")]
    [Range(0.5f, 3f)] public float animSpeedMultiplier = 1f;

    [Header("👣 MOVEMENT & CHASE")]
    [Tooltip("How far enemy can see player")]
    [Range(5, 50)] public float detectionRange = 15f;
    [Tooltip("How close before attacking")]
    [Range(0.5f, 5)] public float attackRange = 2f;
    [Tooltip("Run speed (chases player)")]
    [Range(1, 10)] public float runSpeed = 3.5f;
    [Tooltip("Walk speed (patrolling)")]
    [Range(0.1f, 5)] public float walkSpeed = 1.5f;
    [Tooltip("How fast enemy rotates")]
    [Range(1, 20)] public float rotationSpeed = 5f;
    [Tooltip("Gravity force")]
    public float gravity = -9.81f;

    [Header("👁️ VISION SETTINGS")]
    [Tooltip("Field of view angle (degrees)")]
    [Range(30, 180)] public float visionAngle = 90f;
    [Tooltip("What blocks vision (walls, doors)")]
    public LayerMask visionBlockingLayers = -1;

    [Header("⚔️ ATTACK SETTINGS")]
    [Tooltip("Damage per attack to player")]
    [Range(5, 50)] public int attackDamage = 10;
    [Tooltip("Time between attacks")]
    [Range(0.5f, 5)] public float attackCooldown = 2f;
    [Tooltip("Delay before damage is applied")]
    [Range(0, 1)] public float attackDelay = 0.5f;

    [Header("🚪 DOOR SYSTEM")]
    [Tooltip("Door that locks this enemy in room")]
    public GameObject roomDoor;

    [Header("🎭 ANIMATOR PARAMETERS")]
    [Tooltip("Float for movement speed (0=Idle, 0.5=Walk, 1=Run)")]
    public string speedParam = "Speed";
    [Tooltip("Trigger for attack animation")]
    public string attackParam = "Attack";
    [Tooltip("Trigger for death animation")]
    public string dieParam = "Die";
    [Tooltip("Trigger for hit reaction")]
    public string hitParam = "Hit";

    [Header("🔊 AUDIO")]
    public AudioClip attackSound;
    public AudioClip deathSound;
    public AudioClip hurtSound;
    public AudioClip chaseSound;
    [Range(0, 1)] public float volume = 0.7f;

    // Components
    private CharacterController controller;
    private AudioSource audioSource;
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private Color[] originalColors;

    // State
    private Transform player;
    private float currentHealth;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool canAttack = true;
    private float lastAttackTime;
    private Vector3 velocity;
    private bool isDoorDestroyed = false;

    // Movement
    private Vector3 moveDirection;
    private float currentSpeed;
    private bool playerVisible = false;

    void Start()
    {
        InitializeComponents();
        SetupEnemy();
        FindPlayer();

        // Start checking door status
        if (roomDoor != null)
        {
            StartCoroutine(CheckDoorStatus());
        }
    }

    void InitializeComponents()
    {
        // Get or add Animator
        if (enemyAnimator == null)
            enemyAnimator = GetComponentInChildren<Animator>();

        if (enemyAnimator == null)
            Debug.LogError($"No Animator found on {enemyType}! Add one to the model.");

        // Get or add CharacterController
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0, 1f, 0);
            controller.slopeLimit = 45f;
            controller.stepOffset = 0.3f;
            controller.minMoveDistance = 0.001f;
        }

        // Get audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.volume = volume;
        audioSource.spatialBlend = 1f;

        // Get renderers for hit flash
        renderers = GetComponentsInChildren<Renderer>();
        StoreOriginalMaterials();
    }

    void StoreOriginalMaterials()
    {
        if (renderers != null && renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    originalMaterials[i] = renderers[i].material;
                    if (originalMaterials[i] != null)
                        originalColors[i] = originalMaterials[i].color;
                }
            }
        }
    }

    void SetupEnemy()
    {
        // Calculate health based on bullets needed
        currentHealth = bulletsToKill * 34f;
        Debug.Log($"{enemyType} initialized: {bulletsToKill} bullets to kill");

        // Set animation speed
        if (enemyAnimator != null)
            enemyAnimator.speed = animSpeedMultiplier;
    }

    IEnumerator CheckDoorStatus()
    {
        while (!isDead)
        {
            // Check if door exists and is active
            if (roomDoor == null || !roomDoor.activeInHierarchy)
            {
                isDoorDestroyed = true;
                Debug.Log($"{enemyType}: Door is destroyed! Can now chase player.");
                break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
        else
            Debug.LogWarning("Player not found! Make sure player has 'Player' tag.");
    }

    void Update()
    {
        if (isDead || player == null) return;

        // Handle gravity
        HandleGravity();

        // Check if door is still locked
        if (!isDoorDestroyed && roomDoor != null && roomDoor.activeInHierarchy)
        {
            // Door locked - just idle animation
            HandleIdle();
            ApplyMovement();
            UpdateAnimations();
            return;
        }

        // Door destroyed - check for player
        playerVisible = IsPlayerVisible();

        if (isAttacking)
        {
            HandleAttack();
        }
        else if (playerVisible)
        {
            float distance = Vector3.Distance(transform.position, player.position);

            if (distance <= attackRange)
            {
                StartAttack();
            }
            else
            {
                ChasePlayer();
            }
        }
        else
        {
            // Player not visible - idle or walk around
            HandleIdle();
        }

        ApplyMovement();
        UpdateAnimations();
    }

    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }
    }

    void ApplyMovement()
    {
        Vector3 totalMove = moveDirection * currentSpeed * Time.deltaTime;
        totalMove.y = velocity.y * Time.deltaTime;

        if (totalMove.magnitude > 0)
        {
            controller.Move(totalMove);
        }
    }

    bool IsPlayerVisible()
    {
        if (player == null || !isDoorDestroyed) return false;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionRange) return false;

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        float angle = Vector3.Angle(transform.forward, directionToPlayer);

        if (angle > visionAngle * 0.5f) return false;

        RaycastHit hit;
        Vector3 eyePosition = transform.position + Vector3.up * 1.5f;

        if (Physics.Raycast(eyePosition, directionToPlayer, out hit, detectionRange, visionBlockingLayers))
        {
            return hit.transform == player || hit.transform.IsChildOf(player);
        }

        return false;
    }

    void HandleIdle()
    {
        // Just stand still with idle animation
        moveDirection = Vector3.zero;
        currentSpeed = 0f;

        // Optional: Add occasional small movements
        if (Random.value < 0.01f)
        {
            moveDirection = Random.insideUnitSphere;
            moveDirection.y = 0;
            moveDirection.Normalize();
            currentSpeed = walkSpeed * 0.3f;
        }
    }

    void ChasePlayer()
    {
        if (player == null) return;

        // Run towards player
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        moveDirection = directionToPlayer;
        currentSpeed = runSpeed;

        // Rotate to face player
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // Play chase sound occasionally
        if (chaseSound != null && Random.value < 0.01f)
            PlaySound(chaseSound);
    }

    void StartAttack()
    {
        if (canAttack && Time.time > lastAttackTime + attackCooldown)
        {
            isAttacking = true;
            canAttack = false;
            StartCoroutine(PerformAttack());
        }
        else
        {
            // Face player while waiting for attack cooldown
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 2f * Time.deltaTime);
            }
        }
    }

    void HandleAttack()
    {
        // Stop moving while attacking
        moveDirection = Vector3.zero;
        currentSpeed = 0f;

        // Face player during attack
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * 3f * Time.deltaTime);
            }
        }
    }

    IEnumerator PerformAttack()
    {
        // Trigger attack animation
        if (enemyAnimator != null && !string.IsNullOrEmpty(attackParam))
            enemyAnimator.SetTrigger(attackParam);

        // Play attack sound
        if (attackSound != null)
            PlaySound(attackSound);

        yield return new WaitForSeconds(attackDelay);

        ApplyDamageToPlayer();

        lastAttackTime = Time.time;
        yield return new WaitForSeconds(0.5f); // Brief pause after attack
        isAttacking = false;
        canAttack = true;
    }

    void ApplyDamageToPlayer()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > attackRange * 1.5f) return;

        PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.TakeDamage(attackDamage, enemyType);
            Debug.Log($"{enemyType} attacked player for {attackDamage} damage!");
        }
    }

    void UpdateAnimations()
    {
        if (enemyAnimator == null) return;

        // Set speed parameter based on movement
        float speedValue = 0f; // Default: Idle

        if (isAttacking)
        {
            speedValue = 0f; // Stop moving during attack
        }
        else if (playerVisible && isDoorDestroyed)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > attackRange)
            {
                speedValue = 1f; // Run when chasing
            }
            else
            {
                speedValue = 0f; // Stop when in attack range
            }
        }
        else if (currentSpeed > walkSpeed * 0.5f)
        {
            speedValue = 0.5f; // Walk
        }

        enemyAnimator.SetFloat(speedParam, speedValue);
    }

    // PUBLIC METHOD - Call this from PlayerShooting script
    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (hurtSound != null)
            PlaySound(hurtSound);

        if (flashOnHit)
            StartCoroutine(HitFlash());

        if (enemyAnimator != null && !string.IsNullOrEmpty(hitParam))
            enemyAnimator.SetTrigger(hitParam);

        if (currentHealth <= 0)
        {
            Die();
        }

        Debug.Log($"{enemyType} took {damage} damage. Health: {currentHealth}/{bulletsToKill * 34f}");
    }

    IEnumerator HitFlash()
    {
        if (renderers == null) yield break;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material.color = Color.white;
            }
        }

        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material.color = originalColors[i];
            }
        }
    }

    void Die()
    {
        isDead = true;

        // Play death animation
        if (enemyAnimator != null && !string.IsNullOrEmpty(dieParam))
            enemyAnimator.SetTrigger(dieParam);

        if (deathSound != null)
            PlaySound(deathSound);

        // Stop all movement
        moveDirection = Vector3.zero;
        currentSpeed = 0f;
        velocity = Vector3.zero;

        // Disable controller and colliders
        if (controller != null)
            controller.enabled = false;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }

        // Destroy game object after death animation (3 seconds)
        Destroy(gameObject, 3f);

        Debug.Log($"{enemyType} died!");
    }

    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Vector3 leftBoundary = Quaternion.Euler(0, -visionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, visionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Gizmos.DrawRay(transform.position + Vector3.up, leftBoundary);
        Gizmos.DrawRay(transform.position + Vector3.up, rightBoundary);
        Gizmos.DrawLine(transform.position + Vector3.up + leftBoundary, transform.position + Vector3.up + rightBoundary);
    }
}