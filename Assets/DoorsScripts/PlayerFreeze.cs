using UnityEngine;

public class PlayerFreeze : MonoBehaviour
{
    public static PlayerFreeze Instance;

    private MonoBehaviour[] movementScripts;

    void Awake()
    {
        Instance = this;

        movementScripts = new MonoBehaviour[]
        {
            GetComponent<PlayerMovement>(),
            GetComponent<ThirdPersonCamera>()
        };
    }

    public void Freeze()
    {
        Debug.Log("Freezing player");
        foreach (var script in movementScripts)
        {
            if (script != null)
            {
                Debug.Log($"Disabling {script.GetType().Name}");
                script.enabled = false;
            }
        }
    }

    public void Unfreeze()
    {
        Debug.Log("Unfreezing player");
        foreach (var script in movementScripts)
        {
            if (script != null)
            {
                Debug.Log($"Enabling {script.GetType().Name}");
                script.enabled = true;
            }
        }
    }
}