using UnityEngine;

public class Footstepsound : MonoBehaviour
{
    public AudioSource footstepSource;
    public CharacterController controller;

    void Update()
    {
        bool isMovingForward = Input.GetKey(KeyCode.W);
        bool isGrounded = controller.isGrounded;

        if (isMovingForward && isGrounded)
        {
            if (!footstepSource.isPlaying)
                footstepSource.Play();
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }
    }
}
