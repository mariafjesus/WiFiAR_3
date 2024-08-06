using System.Collections;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using System.Threading.Tasks;

public class HandMap : MonoBehaviour
{
    public GameObject screen; // Reference to the canvas
    public GameObject handMap;
    public Transform leftHand;
    public GameObject mapCursor;
    public SignalMesh signalMesh;
    public WifiStrength wifiStrength;
    public GameObject confirmResetDialog;

    public RenderTexture mapRenderTexture;
    public RenderTexture roomsRenderTexture;
    public RawImage rawImage; // Reference to the RawImage with the walls image

    private int confirmedReset = 0;

    void Start() {
        rawImage.enabled = false;
        confirmResetDialog.SetActive(false);
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
                handMap.transform.position = leftHand.position + /*X*/ leftHand.right * 0.1f + /*Y*/ leftHand.up * 0.05f + /*Z*/ leftHand.forward * -0.18f;
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

    public async Task<string> SaveImageAsync(RenderTexture renderTexture, string filename, bool hide_cursor)
    {
        // Hide map cursor for picture
        if (hide_cursor)
        {
            mapCursor.SetActive(false);
            await Task.Yield(); // Wait for the next frame
            await Task.Yield();
        }

        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;

        Texture2D image = new Texture2D(renderTexture.width, renderTexture.height);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();

        RenderTexture.active = currentRT;

        byte[] bytes = await Task.Run(() => image.EncodeToPNG());
        string path = Path.Combine(Application.persistentDataPath, filename);

        await Task.Run(() => File.WriteAllBytes(path, bytes));

        Destroy(image); // Free memory

        if (hide_cursor)
        {
            mapCursor.SetActive(true);
        }

        // Return the file path
        return path;
    }

    public async Task<string> SaveMapImageAsync() {
        return await SaveImageAsync(mapRenderTexture, "SignalMesh.png", true);
    }
    public async Task<string> SaveRoomsImageAsync() {
        return await SaveImageAsync(roomsRenderTexture, "RoomLayout.png", false);
    }

    public void HideWalls() {
        rawImage.enabled = false;
    }

    public void ShowWalls() {
        rawImage.enabled = true;
    }

    public void ResetScanWrapper() {
        StartCoroutine(ResetScan());
    }
    public IEnumerator ResetScan()
    {
        confirmedReset = 0;
        // Show confirmation dialog
        confirmResetDialog.SetActive(true);
        
        yield return new WaitUntil(ConfirmedReset); // Wait until confirmedReset is updated

        if (confirmedReset == 1)
        {
            signalMesh.ResetSignalMesh();
            wifiStrength.ResetWifiStats();
        }

        confirmResetDialog.SetActive(false);

        yield return null;
    }

    public void ResetYes() {
        confirmedReset = 1;
    }

    public void ResetNo() {
        confirmedReset = -1;
    }

    bool ConfirmedReset() {
        return confirmedReset != 0;
    }
}
