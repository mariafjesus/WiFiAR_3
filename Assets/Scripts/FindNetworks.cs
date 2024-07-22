using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FindNetworks : MonoBehaviour
{
    public float distanceInterval = 10f; // Distance between scans
    public GameObject cube;
    public GameObject locationMarker;

    private Transform cameraRigTransform;
    private Transform centerEyeAnchorTransform;
    private Vector3 lastPosition;
    private AndroidJavaObject wifiManager;
    private AndroidJavaObject context;
    private List<ScanData> scanDataList = new List<ScanData>();
    private const int maxScans = 4;

    // Scan data structure
    private struct ScanData
    {
        public Vector3 position;
        public Dictionary<string, int> networks; // SSID -> RSSI
    }

    void Start()
    {
        // Get the transform of the OVRCameraRig
        GameObject cameraRig = GameObject.Find("OVRCameraRig");
        if (cameraRig != null)
        {
            cameraRigTransform = cameraRig.transform;
            centerEyeAnchorTransform = cameraRigTransform.Find("TrackingSpace/CenterEyeAnchor");

            if (centerEyeAnchorTransform != null)
            {
                lastPosition = centerEyeAnchorTransform.position;
                Debug.Log("CenterEyeAnchor found, initial position: " + lastPosition);
            }
            else
            {
                Debug.LogError("CenterEyeAnchor not found in the OVRCameraRig!");
            }
        }
        else
        {
            Debug.LogError("OVRCameraRig not found in the scene!");
        }

        // Initialize wifiManager
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            wifiManager = context.Call<AndroidJavaObject>("getSystemService", "wifi");
        }

        StartCoroutine(ScanNetworks());
    }

    // Update is called once per frame
    void Update()
    {
        // Check if the user moved 
        if (Vector3.Distance(centerEyeAnchorTransform.position, lastPosition) >= distanceInterval)
        {
            lastPosition = centerEyeAnchorTransform.position;
            StartCoroutine(ScanNetworks());
        }
    }

    IEnumerator ScanNetworks() {
        if (wifiManager == null) yield return null;

        bool success = wifiManager.Call<bool>("startScan");
        if (!success) yield return null; // Couldn't start scan

        AndroidJavaObject wifiList = wifiManager.Call<AndroidJavaObject>("getScanResults");
        int size = wifiList.Call<int>("size");

        Dictionary<string, int> networks = new Dictionary<string, int>();
        for (int i = 0; i < size; i++)
        {
            AndroidJavaObject scanResult = wifiList.Call<AndroidJavaObject>("get", i);
            string ssid = scanResult.Get<string>("SSID");
            int rssi = scanResult.Get<int>("level");
            if (!string.IsNullOrEmpty(ssid) && !networks.ContainsKey(ssid))
            {
                networks[ssid] = rssi;
            }
        }

        scanDataList.Add(new ScanData { position = centerEyeAnchorTransform.position, networks = networks });

        if (scanDataList.Count >= maxScans)
        {
            CalculateCenter();
            scanDataList.Clear(); // Clear the list for new scans
            yield return null;
        }
        yield return null;
    }

    void CalculateCenter()
    {
        // Assuming all scans detect the same networks
        Dictionary<string, List<Vector2>> networkPositions = new Dictionary<string, List<Vector2>>();

        foreach (var scanData in scanDataList)
        {
            foreach (var network in scanData.networks)
            {
                if (!networkPositions.ContainsKey(network.Key))
                {
                    networkPositions[network.Key] = new List<Vector2>();
                }

                // Convert RSSI to distance
                float distance = RssiToDistance(network.Value);
                Vector2 position2D = new Vector2(scanData.position.x, scanData.position.z);
                networkPositions[network.Key].Add(position2D + (Vector2.one * distance));
            }
        }

        foreach (var network in networkPositions)
        {
            Vector2 center = Trilaterate(network.Value);
            Debug.Log($"Calculated center for network {network.Key}: {center}");
            //Instantiate(cube, center, Quaternion.identity);
            PlaceLocationMarker(network.Key, center);
        }
    }

    float RssiToDistance(int rssi)
    {
        // Example path loss model conversion
        int txPower = -59; // Typical value for WiFi APs
        return Mathf.Pow(10, (txPower - rssi) / (10 * 2)); // Path loss exponent n=2 (free space)
    }

    Vector3 Trilaterate(List<Vector2> positions)
    {
        // Simple averaging for trilateration
        Vector2 sum = Vector2.zero;
        foreach (var position in positions)
        {
            sum += position;
        }

        return sum / positions.Count;
    }

    void PlaceLocationMarker(string name, Vector2 position) {
        GameObject marker = Instantiate(locationMarker, new Vector3(position.x, 0, position.y), Quaternion.identity);

        // Change text
        TextMeshProUGUI textMeshPro = marker.GetComponentInChildren<TextMeshProUGUI>();

        if (textMeshPro != null)
        {
            textMeshPro.text = name;
        }
    }
}
