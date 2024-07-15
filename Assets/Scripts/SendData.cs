using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class SendData : MonoBehaviour
{
    private string url = "http://127.0.0.1:5000/receive_data"; // Flask server URL

    // Example data to send
    private Dictionary<string, string> data = new Dictionary<string, string>()
    {
        { "wifi_name", "My Wifi" },
        { "wifi_strength", "-37dBm" },
        { "wifi_speed", "120Mbps" },
        { "img_url", "https://dl3.pushbulletusercontent.com/6QIA6AZNTPRTjG5KKNDqTmQT6SYMtyOx/image.png" }
    };

    void Start()
    {
        StartCoroutine(SendDataToServer());
    }
    
    IEnumerator SendDataToServer()
    {
        string jsonData = JsonUtility.ToJson(new DataWrapper(data));

        UnityWebRequest www = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");

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

    // Wrapper class to convert Dictionary to JSON correctly
    [System.Serializable]
    private class DataWrapper
    {
        public string wifi_name;
        public string wifi_strength;
        public string wifi_speed;
        public string img_url;

        public DataWrapper(Dictionary<string, string> data)
        {
            this.wifi_name = data["wifi_name"];
            this.wifi_strength = data["wifi_strength"];
            this.wifi_speed = data["wifi_speed"];
            this.img_url = data["img_url"];
        }
    }
}
