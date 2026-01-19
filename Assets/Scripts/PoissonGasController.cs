using UnityEngine;

public class RoomGasController : MonoBehaviour
{
    [Header("⚠️ GAS SETTINGS")]
    public float damagePerSecond = 15f;
    public float damageInterval = 0.5f;

    [Header("🌫️ VISUAL SETTINGS")]
    public Material gasMaterial;
    public float gasHeight = 10f;
    public Color gasColor = new Color(0, 0.5f, 0, 0.3f);

    [Header("🔊 AUDIO")]
    public AudioClip gasHissSound;
    public AudioClip coughSound;

    private BoxCollider gasArea;
    private GameObject gasVisual;
    private AudioSource audioSource;
    private bool playerInside = false;
    private float nextDamageTime = 0f;
    private Color originalFogColor; // FIXED: Changed from Vector3 to Color
    private float originalFogDensity;

    void Start()
    {
        gasArea = GetComponent<BoxCollider>();
        if (gasArea == null)
        {
            gasArea = gameObject.AddComponent<BoxCollider>();
            gasArea.isTrigger = true;
        }

        // Create gas visual that fills the box
        CreateRoomGasVisual();

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // Store fog settings
        originalFogColor = RenderSettings.fogColor; // Now Color matches Color
        originalFogDensity = RenderSettings.fogDensity;
    }

    void CreateRoomGasVisual()
    {
        // Create a cube that matches the room size
        gasVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gasVisual.name = "RoomGasVisual";
        gasVisual.transform.parent = transform;
        gasVisual.transform.localPosition = Vector3.zero;

        // Match collider size
        gasVisual.transform.localScale = gasArea.size;

        // Remove collider from visual
        Destroy(gasVisual.GetComponent<Collider>());

        // Create or assign material
        if (gasMaterial != null)
        {
            gasVisual.GetComponent<Renderer>().material = gasMaterial;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = gasColor;
            mat.SetFloat("_Mode", 3); // Transparent
            gasVisual.GetComponent<Renderer>().material = mat;
        }

        // Make it look like fog
        Renderer renderer = gasVisual.GetComponent<Renderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    void Update()
    {
        if (playerInside && Time.time >= nextDamageTime)
        {
            ApplyDamage();
            nextDamageTime = Time.time + damageInterval;
        }
    }

    void ApplyDamage()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            PlayerMovement playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                int damage = Mathf.RoundToInt(damagePerSecond * damageInterval);
                playerMovement.TakeDamage(damage, "Room Gas");

                // Occasional cough
                if (coughSound != null && Random.value > 0.7f)
                {
                    AudioSource.PlayClipAtPoint(coughSound, player.transform.position, 0.5f);
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = true;
            nextDamageTime = Time.time;

            // Start effects
            if (gasHissSound != null)
                audioSource.PlayOneShot(gasHissSound);

            // Add fog effect
            RenderSettings.fog = true;
            RenderSettings.fogColor = gasColor;
            RenderSettings.fogDensity = 0.05f;

            Debug.Log("Player entered room gas!");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;

            // Stop audio
            audioSource.Stop();

            // Restore fog
            RenderSettings.fogColor = originalFogColor;
            RenderSettings.fogDensity = originalFogDensity;

            Debug.Log("Player exited room gas!");
        }
    }

    // Visualize in editor
    void OnDrawGizmos()
    {
        if (gasArea != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position + gasArea.center, gasArea.size);
        }
    }

    // Helper method to check if player is inside
    public bool IsPlayerInside()
    {
        return playerInside;
    }
}