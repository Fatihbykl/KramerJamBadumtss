using UnityEngine;

namespace ClockworkGearslinger.Player
{
    /// <summary>
    /// Handles First-Person mouse look and cursor locking.
    /// Rotates the player body horizontally and the camera root vertically.
    /// </summary>
    public class PlayerLook : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Create an Empty GameObject as a child of the Player (at eye level) and drag it here.")]
        [SerializeField] private Transform cameraRoot;

        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookUpAngle = -80f;
        [SerializeField] private float maxLookDownAngle = 80f;

        private float xRotation = 0f;

        private void Start()
        {
            // Lock the cursor to the center of the screen and hide it
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // 1. Get raw mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // 2. Calculate vertical rotation (tilt up/down)
            xRotation -= mouseY;
            // Clamp the rotation so the player can't break their neck looking backwards
            xRotation = Mathf.Clamp(xRotation, maxLookUpAngle, maxLookDownAngle);

            // 3. Apply vertical rotation to the Camera Root
            if (cameraRoot != null)
            {
                cameraRoot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }

            // 4. Apply horizontal rotation to the entire Player GameObject (spin left/right)
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}
