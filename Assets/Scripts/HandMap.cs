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
        Vector3 palmNormal = leftHand.up;

        if (palmNormal != null)
        {
            // Set the screen visibility based on the hand's direction
            bool isUp = Vector3.Dot(palmNormal, Vector3.up) > 0.5f;

            // Check if the user is using hands or controllers
            if (OVRInput.GetActiveController() == OVRInput.Controller.Hands)
            {
                // Using hands
                screen.transform.rotation = leftHand.rotation * Quaternion.Euler(75, 90, 0);
                screen.transform.position = leftHand.position + /*X*/ leftHand.right * 0.1f + /*Y*/leftHand.up * 0.05f + /*Z*/ leftHand.forward * -0.18f;
            }
            else
            {
                // Using Controllers
                screen.transform.rotation = leftHand.rotation * Quaternion.Euler(90, 0, 0);
                screen.transform.position = leftHand.position + leftHand.right * 0.18f;
            }

            screen.SetActive(isUp);
        }
        else
        {
            screen.SetActive(false);
        }
    }

    public string SaveImage()
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

        // Return the file path
        return path;
    }
}
