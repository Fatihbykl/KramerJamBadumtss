using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cameraTransform;

    private void Start()
    {
        cameraTransform = Camera.main.transform;
    }

    private void LateUpdate()
    {
        Vector3 positionToLook = cameraTransform.position;
        positionToLook.y = transform.position.y;

        transform.LookAt(positionToLook);
    }
}
