using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class SendData : MonoBehaviour
{
    public float timeInterval = 3f;
    private float timer;
    private string url = "http://" + "192.168.1.103" + ":5000/receive_data"; // Flask server URL
    public WifiStrength wifiStrength;
    public HandMap handMap;

    void Start()
    {        
        StartCoroutine(SendDataToServer());

        timer = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= timeInterval)
        {
            StartCoroutine(SendDataToServer());
            timer = 0f;
        }
    }
    
    IEnumerator SendDataToServer()
    {
        string wifi_name = wifiStrength.GetWifiName();
        string wifi_strength = wifiStrength.GetSignalStrength() + " dBm";
        string wifi_speed = "";
        string img = handMap.SaveImage();

        // Create form and add fields
        WWWForm form = new WWWForm();
        form.AddField("wifi_name", wifi_name);
        form.AddField("wifi_strength", wifi_strength);
        form.AddField("wifi_speed", wifi_speed);

        // Add the image file
        byte[] imgData = File.ReadAllBytes(img);
        form.AddBinaryData("image", imgData, "SignalMesh.png", "image/png");

        UnityWebRequest www = UnityWebRequest.Post(url, form);

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("Error: " + www.error);
        }
        else
        {
            Debug.Log("Response: " + www.downloadHandler.text);
        }
    }
}
