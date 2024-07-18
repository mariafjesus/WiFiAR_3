using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using Meta.XR.Locomotion.Teleporter;

public class HandMap : MonoBehaviour
{
    public GameObject screen; // Reference to the screen GameObject
    public GameObject handMap;
    public Transform leftHand; // Reference to the left hand/controller Transform

    public RenderTexture mapRenderTexture; // Reference to the RenderTexture
    public RenderTexture roomsRenderTexture; // Reference to the RenderTexture
    public RawImage rawImage; // Reference to the RawImage UI element

    void Start() {
        rawImage.enabled = false;
    }

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
                handMap.transform.rotation = leftHand.rotation * Quaternion.Euler(75, 90, 0);
                handMap.transform.position = leftHand.position + /*X*/ leftHand.right * 0.1f + /*Y*/leftHand.up * 0.05f + /*Z*/ leftHand.forward * -0.18f;
            }
            else
            {
                // Using Controllers
                handMap.transform.rotation = leftHand.rotation * Quaternion.Euler(90, 0, 0);
                handMap.transform.position = leftHand.position + leftHand.right * 0.18f;
            }

            screen.SetActive(isUp);
        }
        else
        {
            screen.SetActive(false);
        }
    }

    public string SaveImage(RenderTexture renderTexture, string filename)
    {
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = image.EncodeToPNG();
        string path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(path, bytes);

        Debug.Log("Saved RenderTexture to PNG: " + path);

        // Return the file path
        return path;
    }

    public string SaveMapImage() {
        return SaveImage(mapRenderTexture, "SignalMesh.png");
    }

    public string SaveRoomsImage() {
        return SaveImage(roomsRenderTexture, "RoomLayout.png");
    }

    public void HideWalls() {
        rawImage.enabled = false;
    }

    public void ShowWalls() {
        rawImage.enabled = true;
    }
}
