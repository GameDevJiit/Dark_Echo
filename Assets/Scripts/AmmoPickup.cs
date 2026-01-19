using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [Header("Ammo Settings")]
    public int ammoAmount = 5; // How many bullets this pickup gives
    public float rotationSpeed = 50f;

    [Header("Visual Effects")]
    public GameObject pickupEffect;
    public AudioClip pickupSound;

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ensure the object has proper physics components
        SetupPhysics();
    }

    void SetupPhysics()
    {
        // Add Rigidbody if not present
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Keep it in place but still solid
            rb.useGravity = false;
        }

        // Ensure collider is NOT a trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = false; // Make it a solid collider
        }
        else
        {
            // Add a collider if none exists
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(1f, 1f, 1f); // Adjust size as needed
        }
    }

    void Update()
    {
        // Rotate the ammo pickup (visual effect only)
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Now this will be called when the player physically collides with it
        if (collision.gameObject.CompareTag("Player"))
        {
            // Optional: Play a sound or show particle effect
            if (pickupSound != null)
                audioSource.PlayOneShot(pickupSound);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}