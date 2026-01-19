using UnityEngine;
using TMPro;

public class PasswordDoor : MonoBehaviour
{
    [Header("Password Settings")]
    public string doorPassword = "A12";

    [Header("References")]
    public GameObject doorMesh;
    public Collider doorBlocker;
    public TMP_Text promptText;

    [Header("Audio")]
    public AudioClip successSound;  // Play when correct password
    public AudioClip errorSound;    // Play when wrong password

    private bool unlocked = false;

    void Start()
    {
        if (promptText)
            promptText.gameObject.SetActive(false);
    }

    public bool TryPassword(string input)
    {
        if (unlocked) return true;

        if (input.ToUpper() == doorPassword.ToUpper())
        {
            // Play success sound IMMEDIATELY
            PlaySound(successSound);

            UnlockDoor();
            return true;
        }
        else
        {
            if (promptText)
                promptText.text = "Wrong Password";

            // Play error sound
            PlaySound(errorSound);

            return false;
        }
    }

    void PlaySound(AudioClip clip)
    {
        if (clip == null) return;

        // This ALWAYS plays the sound, regardless of door state
        AudioSource.PlayClipAtPoint(clip, GetSoundPosition(), 1f);
        Debug.Log("Playing: " + clip.name);
    }

    Vector3 GetSoundPosition()
    {
        // Try to play at camera, otherwise at door position
        Camera cam = Camera.main;
        return cam != null ? cam.transform.position : transform.position;
    }

    void UnlockDoor()
    {
        unlocked = true;

        if (doorMesh) doorMesh.SetActive(false);
        if (doorBlocker) doorBlocker.enabled = false;

        if (promptText)
        {
            promptText.text = "Correct Password";
            Invoke(nameof(HidePrompt), 1.5f);
        }
    }

    void HidePrompt()
    {
        if (promptText)
            promptText.gameObject.SetActive(false);
    }
}