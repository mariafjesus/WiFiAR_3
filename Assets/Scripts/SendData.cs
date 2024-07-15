using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Text;

public class SendData : MonoBehaviour
{
    private string url = "http://127.0.0.1:5000/receive_data";

    // Example data to send
    private Dictionary<string, string> data = new Dictionary<string, string>()
    {
        { "name", "John Doe" },
        { "score", "100" }
    };

    void Start()
    {
        StartCoroutine(SendDataToServer());
    }
    
    IEnumerator SendDataToServer()
    {
        string jsonData = JsonUtility.ToJson(data);

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
}
