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
        transform.LookAt(-cameraTransform.position);
    }
}
