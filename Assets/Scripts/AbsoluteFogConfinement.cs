using UnityEngine;

public class TriggerFog : MonoBehaviour
{
    [Header("Fog Settings")]
    public ParticleSystem fogSystem;
    public float fadeInTime = 3f;
    public float fadeOutTime = 3f;
    public float maxEmissionRate = 50f;

    [Header("Trigger Settings")]
    public string playerTag = "Player";
    public bool oneTimeTrigger = false;

    private bool isActive = false;
    private bool hasTriggered = false;
    private float currentEmission = 0f;

    void Start()
    {
        if (fogSystem == null)
            fogSystem = GetComponent<ParticleSystem>();

        // Start with fog disabled
        var emission = fogSystem.emission;
        emission.rateOverTime = 0f;
    }

    void Update()
    {
        var emission = fogSystem.emission;

        if (isActive && currentEmission < maxEmissionRate)
        {
            currentEmission += Time.deltaTime * (maxEmissionRate / fadeInTime);
            emission.rateOverTime = Mathf.Min(currentEmission, maxEmissionRate);
        }
        else if (!isActive && currentEmission > 0f)
        {
            currentEmission -= Time.deltaTime * (maxEmissionRate / fadeOutTime);
            emission.rateOverTime = Mathf.Max(currentEmission, 0f);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (oneTimeTrigger && hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            isActive = true;
            hasTriggered = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isActive = false;
        }
    }
}