using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;

public class SendData : MonoBehaviour
{
    public float timeInterval = 5f;
    private float timer;
    // Casa: 192.168.1.103
    // Altice: 192.168.1.64 ET5002359.home
    private string url = "http://" + "192.168.1.103" + ":5000/receive_data"; // Flask server URL
    public WifiStrength wifiStrength;
    public HandMap handMap;
    public SignalMesh signalMesh;

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
        int wifi_strength = wifiStrength.Strength;
        int wifi_max_strength = wifiStrength.MaxStrength;
        int wifi_min_strength = wifiStrength.MinStrength;
        string wifi_avg_strength = wifiStrength.AvgStrength + "";
        // Speed values
        int wifi_speed = (int)wifiStrength.Speed;
        int wifi_max_speed = (int)wifiStrength.MaxSpeed;
        int wifi_min_speed = (int)wifiStrength.MinSpeed;
        string wifi_avg_speed = wifiStrength.AvgSpeed + "";
        // Image
        string map_img = handMap.SaveMapImage();
        string rooms_img = handMap.SaveRoomsImage();
        // Matrix
        string strength_matrix = signalMesh.GetSignalStrengthJson();
        string speed_matrix = signalMesh.GetSignalSpeedJson();

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
        byte[] mapImgData = File.ReadAllBytes(map_img);
        form.AddBinaryData("map_image", mapImgData, "SignalMesh.png", "image/png");

        byte[] roomsImgData = File.ReadAllBytes(rooms_img);
        form.AddBinaryData("rooms_image", roomsImgData, "RoomsLayout.png", "image/png");

        // Add matrix
        form.AddField("wifi_strength_matrix", strength_matrix);
        form.AddField("wifi_speed_matrix", speed_matrix);

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
