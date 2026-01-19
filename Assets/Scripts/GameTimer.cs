using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Collections;

public class GameTimer : MonoBehaviour
{
    [Header("Timer Settings")]
    public float initialTimeInMinutes = 5f; // Set to 5 minutes by default
    public bool timerRunning = true;

    [Header("UI References")]
    public TMP_Text timerText; // Optional: Assign a UI Text to display the timer

    [Header("Death Scene Settings")]
    public string deathSceneName = "DeathScene"; // Scene to load when timer expires
    public float deathDelay = 2f; // Delay before loading death scene

    [Header("Audio")]
    public AudioClip timeAddedSound;
    public AudioClip timerExpiredSound;
    public AudioClip warningSound;
    public float warningTime = 60f; // Start warning at 60 seconds left
    public float warningSoundInterval = 1f; // How often to play warning sound

    // Private variables
    private float currentTimeInSeconds;
    private AudioSource audioSource;
    private bool warningPlaying = false;
    private bool timerExpired = false;
    private float lastWarningSoundTime = 0f;

    // Events for other scripts to listen to
    public event Action OnTimerExpired;
    public event Action<float> OnTimeAdded;
    public event Action<float> OnTimerTick; // For UI updates

    void Start()
    {
        // Initialize timer
        currentTimeInSeconds = initialTimeInMinutes * 60f;

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        Debug.Log("Game Timer initialized: " + FormatTime(currentTimeInSeconds));
    }

    void Update()
    {
        if (!timerRunning || timerExpired) return;

        // Decrease timer
        currentTimeInSeconds -= Time.deltaTime;

        // Trigger tick event for UI updates
        OnTimerTick?.Invoke(currentTimeInSeconds);

        // Update UI if assigned
        UpdateTimerDisplay();

        // Handle continuous warning sound
        HandleWarningSound();

        // Check if timer expired
        if (currentTimeInSeconds <= 0f)
        {
            TimerExpired();
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            timerText.text = FormatTime(currentTimeInSeconds);

            // Change color when time is low
            if (currentTimeInSeconds <= warningTime)
            {
                timerText.color = Color.red;
            }
            else if (currentTimeInSeconds <= warningTime * 2)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    void HandleWarningSound()
    {
        // Check if we should be playing warning sound
        bool shouldPlayWarning = currentTimeInSeconds <= warningTime && currentTimeInSeconds > 0f;

        if (shouldPlayWarning)
        {
            // If warning just started, play immediately
            if (!warningPlaying)
            {
                PlayWarningSound();
                warningPlaying = true;
                lastWarningSoundTime = Time.time;
                Debug.Log("Warning sound started - Time is running out!");
            }
            else if (Time.time - lastWarningSoundTime >= warningSoundInterval)
            {
                // Play warning sound at intervals
                PlayWarningSound();
                lastWarningSoundTime = Time.time;
            }
        }
        else if (warningPlaying)
        {
            // Stop warning sound if time went above warning threshold
            warningPlaying = false;
            Debug.Log("Warning sound stopped - Time above " + warningTime + " seconds");
        }
    }

    void PlayWarningSound()
    {
        if (warningSound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(warningSound);
        }
    }

    void TimerExpired()
    {
        if (timerExpired) return;

        timerExpired = true;
        timerRunning = false;
        currentTimeInSeconds = 0f;

        // Stop any warning sound
        warningPlaying = false;
        audioSource.Stop();

        // Play expired sound
        if (timerExpiredSound != null)
        {
            audioSource.PlayOneShot(timerExpiredSound);
        }

        // Trigger event
        OnTimerExpired?.Invoke();

        Debug.Log("Timer expired! Loading death scene...");

        // Load death scene
        StartCoroutine(LoadDeathScene());
    }

    IEnumerator LoadDeathScene()
    {
        // Wait for delay
        yield return new WaitForSeconds(deathDelay);

        // Load death scene
        if (!string.IsNullOrEmpty(deathSceneName))
        {
            SceneManager.LoadScene(deathSceneName);
        }
        else
        {
            Debug.LogError("Death scene name not set in GameTimer!");
        }
    }

    // Public method to add time to the timer
    public void AddTime(float minutesToAdd)
    {
        if (timerExpired) return;

        float secondsToAdd = minutesToAdd * 60f;
        currentTimeInSeconds += secondsToAdd;

        // Play sound
        if (timeAddedSound != null)
        {
            audioSource.PlayOneShot(timeAddedSound);
        }

        // Trigger event
        OnTimeAdded?.Invoke(secondsToAdd);

        Debug.Log("Added " + minutesToAdd + " minutes to timer. New time: " + FormatTime(currentTimeInSeconds));

        // Stop warning sound if enough time was added
        if (currentTimeInSeconds > warningTime && warningPlaying)
        {
            warningPlaying = false;
            audioSource.Stop();
            Debug.Log("Warning sound stopped - Added time pushed timer above " + warningTime + " seconds");
        }
    }

    // Public method to get current time
    public float GetCurrentTime()
    {
        return currentTimeInSeconds;
    }

    // Public method to get formatted time string
    public string GetFormattedTime()
    {
        return FormatTime(currentTimeInSeconds);
    }

    // Helper method to format time as MM:SS
    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // Public method to pause/resume timer
    public void SetTimerRunning(bool running)
    {
        timerRunning = running;

        // Stop warning sound if timer is paused
        if (!running && warningPlaying)
        {
            warningPlaying = false;
            audioSource.Stop();
        }
    }

    // Public method to reset timer
    public void ResetTimer()
    {
        currentTimeInSeconds = initialTimeInMinutes * 60f;
        timerExpired = false;
        warningPlaying = false;
        timerRunning = true;
        audioSource.Stop();
        Debug.Log("Timer reset to: " + FormatTime(currentTimeInSeconds));
    }

    // Public method to change initial time
    public void SetInitialTime(float minutes)
    {
        initialTimeInMinutes = minutes;
        ResetTimer();
    }

    // Method to stop warning sound manually
    public void StopWarningSound()
    {
        if (warningPlaying)
        {
            warningPlaying = false;
            audioSource.Stop();
        }
    }

    // Check if warning sound is currently playing
    public bool IsWarningPlaying()
    {
        return warningPlaying;
    }
}