using UnityEngine;

public class BillboardText : MonoBehaviour
{
    private GameObject mainCamera;

    void Start()
    {
        mainCamera = GameObject.Find("CenterEyeAnchor");
    }
    
    void LateUpdate()
    {
        // Make the text face the camera
        transform.LookAt(mainCamera.transform.position);

        // Only rotate around y axis
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
    }
}
