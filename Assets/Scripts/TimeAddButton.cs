using UnityEngine;
using TMPro;

public class TimeAddButton : MonoBehaviour
{
    [Header("Button Settings")]
    public float timeToAddInMinutes = 5f; // How much time to add when pressed
    public bool canBeUsedOnce = true; // If true, button works only once

    [Header("References")]
    public GameTimer gameTimer; // Assign in Inspector or it will find automatically

    [Header("Physical Button Settings")]
    public float pressDistance = 0.2f; // How far the button moves when pressed
    public float pressSpeed = 5f; // Speed of button press animation
    public float interactionRange = 3f; // How close player needs to be to press with P

    [Header("UI Settings")]
    public TMP_Text interactionText; // Text to show "Press P to add time"
    public string pressMessage = "Press P to add time (+5 min)";
    public string usedMessage = "Time already added";
    public string disabledMessage = "Button disabled";

    [Header("Visual Feedback")]
    public GameObject pressEffect; // Optional particle effect
    public AudioClip buttonPressSound;
    public Material pressedMaterial; // Optional material change when pressed
    public Material disabledMaterial; // Material after being used
    public Light buttonLight; // Optional light on the button

    [Header("Button State Colors")]
    public Color readyColor = Color.green;
    public Color pressedColor = Color.red;
    public Color disabledColor = Color.gray;

    private AudioSource audioSource;
    private Vector3 initialPosition;
    private Vector3 pressedPosition;
    private bool isPressed = false;
    private bool isAnimating = false;
    private bool isInRange = false;
    private bool hasBeenUsed = false;
    private bool isDisabled = false;
    private Material originalMaterial;
    private Renderer buttonRenderer;
    private Transform playerTransform;

    void Start()
    {
        // Store initial position
        initialPosition = transform.position;
        pressedPosition = initialPosition - transform.up * pressDistance;

        // Find GameTimer if not assigned
        if (gameTimer == null)
        {
            gameTimer = FindObjectOfType<GameTimer>();
            if (gameTimer == null)
            {
                Debug.LogError("No GameTimer found in scene!");
                return;
            }
        }

        // Find player
        FindPlayer();

        // Get renderer for material changes
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
        {
            originalMaterial = buttonRenderer.material;
        }

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Setup button light
        if (buttonLight != null)
        {
            buttonLight.color = readyColor;
        }

        // Hide interaction text initially
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }

        Debug.Log("Physical TimeAddButton initialized: Adds " + timeToAddInMinutes + " minutes");
    }

    void Update()
    {
        // Check if player is in range
        CheckPlayerDistance();

        // Handle P key press
        if (isInRange && Input.GetKeyDown(KeyCode.P) && !hasBeenUsed && !isDisabled)
        {
            StartButtonPress();
        }

        // Handle button press animation
        if (isAnimating)
        {
            if (isPressed)
            {
                // Move button down
                transform.position = Vector3.Lerp(transform.position, pressedPosition, pressSpeed * Time.deltaTime);

                if (Vector3.Distance(transform.position, pressedPosition) < 0.01f)
                {
                    // Button fully pressed
                    OnButtonFullyPressed();
                }
            }
        }
        else if (hasBeenUsed && canBeUsedOnce)
        {
            // Keep button in pressed position if used only once
            transform.position = pressedPosition;
        }
    }

    void CheckPlayerDistance()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = isInRange;
        isInRange = distance <= interactionRange;

        // Update UI text when entering/exiting range
        if (isInRange && !wasInRange && interactionText != null)
        {
            if (hasBeenUsed && canBeUsedOnce)
            {
                interactionText.text = usedMessage;
            }
            else if (isDisabled)
            {
                interactionText.text = disabledMessage;
            }
            else
            {
                interactionText.text = pressMessage;
            }
            interactionText.gameObject.SetActive(true);
        }
        else if (!isInRange && wasInRange && interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    void FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            // Try to find PlayerMovement script
            PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
            if (playerMovement != null)
            {
                playerTransform = playerMovement.transform;
            }
        }
    }

    void StartButtonPress()
    {
        if (hasBeenUsed && canBeUsedOnce) return;

        isPressed = true;
        isAnimating = true;

        // Update UI text
        if (interactionText != null && isInRange)
        {
            interactionText.text = "Adding time...";
        }

        // Change visual feedback
        if (pressedMaterial != null && buttonRenderer != null)
        {
            buttonRenderer.material = pressedMaterial;
        }

        if (buttonLight != null)
        {
            buttonLight.color = pressedColor;
        }

        Debug.Log("Physical button pressed with P key");
    }

    void OnButtonFullyPressed()
    {
        // Mark as used
        hasBeenUsed = true;
        isAnimating = false;

        // Add time to timer
        if (gameTimer != null)
        {
            gameTimer.AddTime(timeToAddInMinutes);
        }

        // Play sound
        if (buttonPressSound != null)
        {
            audioSource.PlayOneShot(buttonPressSound);
        }

        // Show particle effect
        if (pressEffect != null)
        {
            Instantiate(pressEffect, transform.position, Quaternion.identity);
        }

        Debug.Log("Button used! Added " + timeToAddInMinutes + " minutes to timer");

        // Apply permanent pressed state if can be used only once
        if (canBeUsedOnce)
        {
            ApplyDisabledState();
        }

        // Update UI text
        if (interactionText != null && isInRange)
        {
            if (canBeUsedOnce)
            {
                interactionText.text = usedMessage;
            }
            else
            {
                interactionText.text = pressMessage;
            }
        }
    }

    void ApplyDisabledState()
    {
        // Apply disabled material if specified
        if (disabledMaterial != null && buttonRenderer != null)
        {
            buttonRenderer.material = disabledMaterial;
        }

        // Set light to disabled color
        if (buttonLight != null)
        {
            buttonLight.color = disabledColor;
        }

        // Disable further interactions
        isDisabled = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // Still support physical collision if needed
        if (!hasBeenUsed && !isDisabled && IsPlayerCollider(other))
        {
            StartButtonPress();
        }
    }

    bool IsPlayerCollider(Collider collider)
    {
        // Check if collider has Player tag
        if (collider.CompareTag("Player"))
        {
            return true;
        }

        // Check for CharacterController (player movement script)
        if (collider.GetComponent<CharacterController>() != null)
        {
            return true;
        }

        // Check for PlayerMovement script
        if (collider.GetComponent<PlayerMovement>() != null)
        {
            return true;
        }

        return false;
    }

    // Public method to change how much time to add
    public void SetTimeToAdd(float minutes)
    {
        timeToAddInMinutes = minutes;
        pressMessage = "Press P to add time (+" + minutes + " min)";
        Debug.Log("Button now adds " + timeToAddInMinutes + " minutes");
    }

    // Method to manually press button (for testing or other scripts)
    public void PressButton()
    {
        if (!hasBeenUsed && !isDisabled)
        {
            StartButtonPress();
        }
    }

    // Method to reset button (for game restart or debugging)
    public void ResetButton()
    {
        hasBeenUsed = false;
        isDisabled = false;
        isPressed = false;
        isAnimating = false;
        transform.position = initialPosition;

        // Reset material
        if (buttonRenderer != null && originalMaterial != null)
        {
            buttonRenderer.material = originalMaterial;
        }

        // Reset light
        if (buttonLight != null)
        {
            buttonLight.color = readyColor;
        }

        Debug.Log("Button reset and ready to use");
    }

    // Method to disable button
    public void DisableButton()
    {
        isDisabled = true;
        ApplyDisabledState();
        Debug.Log("Button disabled");
    }

    // Method to enable button (if it was disabled but not used)
    public void EnableButton()
    {
        if (!hasBeenUsed)
        {
            isDisabled = false;

            // Reset material
            if (buttonRenderer != null && originalMaterial != null)
            {
                buttonRenderer.material = originalMaterial;
            }

            // Reset light
            if (buttonLight != null)
            {
                buttonLight.color = readyColor;
            }

            Debug.Log("Button enabled");
        }
    }

    // Check if button has been used
    public bool HasBeenUsed()
    {
        return hasBeenUsed;
    }

    // Draw gizmo to show interaction range
    void OnDrawGizmosSelected()
    {
        // Show press distance
        Gizmos.color = hasBeenUsed ? Color.gray : Color.green;
        Gizmos.DrawLine(transform.position, transform.position - transform.up * pressDistance);
        Gizmos.DrawWireSphere(transform.position - transform.up * pressDistance, 0.1f);

        // Show interaction range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}