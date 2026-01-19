using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    private Light hospitalLight;
    public AudioSource flickerSFX;
    public float minIntensity = 0f;
    public float maxIntensity = 1.2f;
    [Range(0.01f, 0.2f)] public float flickerSpeed = 0.07f;

    void Start()
    {
        hospitalLight = GetComponent<Light>();
        if(flickerSFX != null && !flickerSFX.isPlaying)
        {
            flickerSFX.Play();
        }
    }

    void Update()
    {
        if (Random.value > 0.9f)
        {
            hospitalLight.intensity = Random.Range(minIntensity, maxIntensity);
            if (flickerSFX != null)
            {
                flickerSFX.volume = hospitalLight.intensity / maxIntensity;
            }
        }
        else
        {
            hospitalLight.intensity = Mathf.Lerp(hospitalLight.intensity, maxIntensity, flickerSpeed);
        }
    }
}