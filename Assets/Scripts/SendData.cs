using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class SendData : MonoBehaviour
{
    public float timeInterval = 3f;
    private float timer;
    // Casa: 192.168.1.103
    // Altice: 192.168.1.64
    private string url = "http://" + "ET50002359.home" + ":5000/receive_data"; // Flask server URL
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
        // Strength values
        string wifi_strength = wifiStrength.Strength + " dBm";
        string wifi_max_strength = wifiStrength.MaxStrength + " dBm";
        string wifi_min_strength = wifiStrength.MinStrength + " dBm";
        string wifi_avg_strength = wifiStrength.AvgStrength + " dBm";
        // Speed values
        string wifi_speed = wifiStrength.Speed + " Mbps";
        string wifi_max_speed = wifiStrength.MaxSpeed + " Mbps";
        string wifi_min_speed = wifiStrength.MinSpeed + " Mbps";
        string wifi_avg_speed = wifiStrength.AvgSpeed + " Mbps";
        // Image
        string img = handMap.SaveImage();

        // Create form and add fields
        WWWForm form = new WWWForm();
        form.AddField("wifi_name", wifi_name);
        // Strength
        form.AddField("wifi_strength", wifi_strength);
        form.AddField("wifi_max_strength", wifi_max_strength);
        form.AddField("wifi_min_strength", wifi_min_strength);
        form.AddField("wifi_avg_strength", wifi_avg_strength);
        // Speed
        form.AddField("wifi_speed", wifi_speed);
        form.AddField("wifi_max_speed", wifi_max_speed);
        form.AddField("wifi_min_speed", wifi_min_speed);
        form.AddField("wifi_avg_speed", wifi_avg_speed);

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
