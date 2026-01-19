using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public AudioMixer audioMixer;
    public GameObject settingspanel;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            settingspanel.SetActive(false);
        }
    }
    //test

    public void SetVolume(float volume)
    {
        Debug.Log("Volume");
        audioMixer.SetFloat("volume", volume);
    }
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }
}
