using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Networking;

public class WifiStrength : MonoBehaviour
{
    private AndroidJavaObject wifiManager;
    private AndroidJavaObject context;

    public UpdateDataScreen updateDataScreen;
    public SignalMesh signalMesh;

    public int ScanNumber { get; private set; } // Number of scans made

    // Strength
    public int Strength { get; private set; }
    public int MaxStrength { get; private set; }
    public int MinStrength { get; private set; }
    public float AvgStrength { get; private set; }
    private int totalStrength = 0;
    private int totalStrengthCount = 0;

    // Speed
    public float Speed { get; private set; }
    public float MaxSpeed { get; private set; }
    public float MinSpeed { get; private set; }
    public float AvgSpeed { get; private set; }
    private float totalSpeed = 0;
    private int totalSpeedCount = 0;
    
    void Start()
    {
        ScanNumber = 1;

        // Initialize Strength and Speed values
        ResetWifiStats();
        GetSignalStrength();
        StartCoroutine(GetSpeed());

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
        }

        // Request necessary permissions
        RequestPermissions();
    }

    public int GetSignalStrength()
    {
        if (wifiManager == null) return -99; // Default to a very low signal strength if not available

        AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
        Debug.Log("Wifi Signal: " + wifiInfo.Call<int>("getRssi"));
        int strength = wifiInfo.Call<int>("getRssi");
        updateStrength(strength);

        return strength;
    }

    public string GetWifiName()
    {
        if (wifiManager == null) return "N/A"; // Default to "N/A" if not available

        AndroidJavaObject wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");
        string wifiName = wifiInfo.Call<string>("getSSID");

        if (wifiName.StartsWith("\"") && wifiName.EndsWith("\""))
        {
            wifiName = wifiName.Substring(1, wifiName.Length - 2);
        }
        return wifiName;
    }

    public IEnumerator GetSpeed()
    {
        string uri = "https://lisboa.speedtest.net.zon.pt.prod.hosts.ooklaserver.net:8080/download?nocache=1e75a1a4-1969-489b-8fa7-f7cdd9e2f599&size=25000000&guid=06ff9720-e905-44e7-b589-ecdcfd4154e4";
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            float startTime = Time.realtimeSinceStartup;
            yield return webRequest.SendWebRequest();
            float endTime = Time.realtimeSinceStartup;

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                float timeTaken = endTime - startTime;
                float dataSize = 25 * 8; // size in megabits
                float speed = dataSize / timeTaken; // Speed in Mbps
                speed = MathF.Round(speed);
                updateSpeed(speed);
                updateDataScreen.UpdateSignalSpeed(speed);
            }
            else
            {
                updateSpeed(0);
                updateDataScreen.UpdateSignalSpeed(0);
            }
        }
    }

    void updateStrength(int s)
    {
        Strength = s;
        // Max
        if (Strength > MaxStrength) {
            MaxStrength = Strength;
        }
        // Min
        if (Strength < MinStrength) {
            MinStrength = Strength;
        }
        // Average
        totalStrength += Strength;
        totalStrengthCount++;
        AvgStrength = MathF.Round(totalStrength / totalStrengthCount * 100f)/100f; // Round with 2 decimal places
    }

    void updateSpeed(float s)
    {
        // Add to matrix
        signalMesh.addSpeed((int)s);

        Speed = s;
        // Max
        if (Speed > MaxSpeed) {
            MaxSpeed = Speed;
        }
        // Min
        if (Speed < MinSpeed || MinSpeed == -1) {
            MinSpeed = Speed;
        }
        // Average
        totalSpeed += Speed;
        totalSpeedCount++;
        AvgSpeed = MathF.Round(totalSpeed / totalSpeedCount * 100f)/100f; // Round with 2 decimal places
    }

    public void ResetWifiStats()
    {
        ScanNumber++;

        // Strength
        MaxStrength = -99;
        MinStrength = 0;
        AvgStrength = 0;
        totalStrength = 0;
        totalStrengthCount = 0;

        // Speed
        MaxSpeed = 0;
        MinSpeed = -1;
        AvgSpeed = 0;
        totalSpeed = 0;
        totalSpeedCount = 0;
    }

    void RequestPermissions()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.CoarseLocation))
        {
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }

        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
        }
    }
}
