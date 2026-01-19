using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class PauseMenu : MonoBehaviour
{
   public static bool GamePaused= false;
   public GameObject pausepanel;
   public GameObject settingspanel;
   public Camera mainCamera;
   public float normalFOV = 60f;
   public float pausedFOV = 80f;
   public float transitionSpeed = 5f;
   public PostProcessVolume blurVolume;

   void Start()
{
    if (blurVolume != null) 
    {
        blurVolume.weight = 0f;
    }
    
    Time.timeScale = 1f;
    GamePaused = false;
    
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
}

   void Update() {
        float targetFOV= GamePaused? pausedFOV: normalFOV;
        mainCamera.fieldOfView= Mathf.Lerp(mainCamera.fieldOfView, targetFOV, Time.unscaledDeltaTime* transitionSpeed);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if(GamePaused== false)
            {
                Pause();
            }
        }
   }
    public void Resume()
    {
        Debug.Log("resume");
        pausepanel.SetActive(false);
        Time.timeScale= 1f;
        GamePaused= false;

        if(blurVolume != null) blurVolume.weight = 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Pause()
    {
        pausepanel.SetActive(true);
        Time.timeScale= 0.0001f;
        GamePaused= true;

        if(blurVolume != null) blurVolume.weight = 1f;

        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true;
    }
    public void settings()
    {
        Debug.Log("settings");
        settingspanel.SetActive(true);
    }
}
