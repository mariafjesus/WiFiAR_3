using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;

public class HandMap : MonoBehaviour
{
    public GameObject screen; // Reference to the screen GameObject
    public Transform leftHand; // Reference to the left hand/controller Transform
    public Vector3 minPalmUpRotation; // Minimum rotation values for palm up
    public Vector3 maxPalmUpRotation; // Maximum rotation values for palm up

    public RenderTexture renderTexture; // Reference to the RenderTexture
    public RawImage rawImage; // Reference to the RawImage UI element

    void Update()
    {
        Vector3 direction = leftHand.forward;

        if (direction != null)
        {
            // Set the screen visibility based on the hand's direction
            bool isUp = direction.y >= -0.3 && direction.y <= 0.5 && direction.x < 0;
            screen.SetActive(isUp);
        }
        else
        {
            screen.SetActive(false);
        }
    }

    public void SaveImage()
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = image.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, "SignalMesh.png");
        File.WriteAllBytes(path, bytes);

        Debug.Log("Saved RenderTexture to PNG: " + path);
    }
}
