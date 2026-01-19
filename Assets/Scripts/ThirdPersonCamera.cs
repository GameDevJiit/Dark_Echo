using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float mouseSensitivity = 120f;
    public float distance = 3f;
    public float height = 1.6f;

    public bool isAiming = false;
    public Vector3 fpsOffset = new Vector3(0, 1.6f, 0.5f);

    private Camera cam;
    float yaw;
    float pitch;
    float defaultPitch = 15f;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        pitch = defaultPitch;

        // Lock FOV at 60 permanently
        cam.fieldOfView = 60f;
    }

    void LateUpdate()
    {
        if (PauseMenu.GamePaused) return;
        // Toggle aiming with right mouse button
        if (Input.GetMouseButtonDown(1))
        {
            isAiming = !isAiming;

            // When switching BACK to TPP, reset camera
            if (!isAiming)
            {
                yaw = target.eulerAngles.y;
                pitch = defaultPitch;
            }
        }

        // Mouse look
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -30f, 60f);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        if (isAiming)
        {
            // FPS MODE
            transform.position = target.position + target.TransformDirection(fpsOffset);
            transform.rotation = Quaternion.Euler(pitch, target.eulerAngles.y, 0);
        }
        else
        {
            // TPP MODE
            Vector3 position = target.position - rotation * Vector3.forward * distance;
            position.y = target.position.y + height;

            transform.position = position;
            transform.rotation = rotation;
        }
    }
}