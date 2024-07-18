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
        // Initialize Strength values
        MaxStrength = -99;
        MinStrength = 0;
        GetSignalStrength();

        // Initialize Speed values
        Speed = 0;
        MaxSpeed = 0;
        MinSpeed = -1;
        StartCoroutine(GetSpeed());

        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
        }

        // Request necessary permissions
        RequestPermissions();

        // ScanForNetworks();
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
    public string ScanForNetworks()
    {

        if (wifiManager == null) return "Network 0";

        bool success = wifiManager.Call<bool>("startScan");
        AndroidJavaObject scanResults = wifiManager.Call<AndroidJavaObject>("getScanResults");
        int size = scanResults.Call<int>("size");

        string n = "";
        for (int i = 0; i < size; i++)
        {
            AndroidJavaObject scanResult = scanResults.Call<AndroidJavaObject>("get", i);
            string ssid = scanResult.Get<string>("SSID");
            int rssi = scanResult.Get<int>("level");

            Debug.Log($"Network: {ssid}, Signal Strength: {rssi} dBm");
            n += "Network: " + ssid + ", Signal Strength: " + rssi + " dBm";
            
        }
        return success + " " + n;
    }

    public List<string> GetAvailableNetworks()
    {
        List<string> networks = new List<string>();
        if (wifiManager == null) return networks;

        bool success = wifiManager.Call<bool>("startScan");
        AndroidJavaObject wifiList = wifiManager.Call<AndroidJavaObject>("getScanResults");

        int size = wifiList.Call<int>("size");
        networks.Add(""+success);
        for (int i = 0; i < size; i++)
        {
            AndroidJavaObject scanResult = wifiList.Call<AndroidJavaObject>("get", i);
            string ssid = scanResult.Get<string>("SSID");
            int rssi = scanResult.Get<int>("level");
            if (!string.IsNullOrEmpty(ssid) && !networks.Contains(ssid))
            {
                networks.Add(rssi + " " + ssid);
            }
        }
        return networks;
    }

    public List<string> GetPreviouslyConnectedNetworks()
    {
        
        List<string> networkList = new List<string>();
        if (wifiManager == null)
        {
            networkList.Add("null");
            return networkList;
        }

        AndroidJavaObject configuredNetworks = wifiManager.Call<AndroidJavaObject>("getConfiguredNetworks");
        int networkCount = configuredNetworks.Call<int>("size");

        for (int i = 0; i < networkCount; i++)
        {
            AndroidJavaObject network = configuredNetworks.Call<AndroidJavaObject>("get", i);
            string ssid = network.Get<string>("SSID");

            if (ssid.StartsWith("\"") && ssid.EndsWith("\""))
            {
                ssid = ssid.Substring(1, ssid.Length - 2);
            }

            networkList.Add(ssid);
        }
        networkList.Add("Empty");

        return networkList;
    }

    public bool ConnectToNetwork(string ssid)
    {
        if (wifiManager == null) return false;

        AndroidJavaObject configuredNetworks = wifiManager.Call<AndroidJavaObject>("getConfiguredNetworks");
        int networkCount = configuredNetworks.Call<int>("size");

        for (int i = 0; i < networkCount; i++)
        {
            AndroidJavaObject network = configuredNetworks.Call<AndroidJavaObject>("get", i);
            string networkSSID = network.Get<string>("SSID");

            if (networkSSID.Equals("\"" + ssid + "\""))
            {
                int networkId = network.Get<int>("networkId");
                bool result = wifiManager.Call<bool>("enableNetwork", networkId, true);
                return result;
            }
        }

        return false;
    }

    public IEnumerator GetSpeed() {
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

    void updateStrength(int s) {
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
        AvgStrength = totalStrength / totalStrengthCount;
    }

    void updateSpeed(float s) {
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
        AvgSpeed = totalSpeed / totalSpeedCount;
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
