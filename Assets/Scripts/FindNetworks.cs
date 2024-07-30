using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class FindNetworks : MonoBehaviour
{
    public float distanceInterval = 10f; // Distance between scans
    public GameObject locationMarker;
    public GameObject scanIcon;

    private Transform cameraRigTransform;
    private Transform centerEyeAnchorTransform;
    private Vector3 lastPosition;
    private AndroidJavaObject wifiManager;
    private AndroidJavaObject context;
    private List<ScanData> scanDataList = new List<ScanData>();

    private Dictionary<string, GameObject> locationMarkers = new Dictionary<string, GameObject>();
    private const int maxScans = 4;
    private int totalScans = 0;
    private float timer = 0f;

    // Scan data structure
    private struct ScanData
    {
        public Vector3 position;
        public Dictionary<string, int> networks; // SSID -> RSSI
    }

    private struct NetworkData
    {
        public Vector2 position;
        public float distance;
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
        timer += Time.deltaTime;
        if (Vector3.Distance(centerEyeAnchorTransform.position, lastPosition) >= distanceInterval)
        {
            if (!(totalScans % 4 == 0 && totalScans != 0 && timer <= 120)) // Stop scans if the two minutes haven't passed
            {
                lastPosition = centerEyeAnchorTransform.position;
                //PlaceLocationMarker("2", new Vector2(lastPosition.x, lastPosition.z));
                StartCoroutine(ScanNetworks());
                timer = 0;
            }            
        }
    }

    IEnumerator ScanNetworks() {
        //Instantiate(scanIcon, lastPosition, Quaternion.identity);
        totalScans++;
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
            //scanDataList.Clear(); // Clear the list for new scans
            yield return null;
        }
        yield return null;
    }

    void CalculateCenter()
    {
        // Assuming all scans detect the same networks
        Dictionary<string, List<Vector2>> networkPositions = new Dictionary<string, List<Vector2>>();
        Dictionary<string, List<NetworkData>> newList = new Dictionary<string, List<NetworkData>>();

        foreach (var scanData in scanDataList)
        {
            foreach (var network in scanData.networks)
            {
                if (!networkPositions.ContainsKey(network.Key))
                {
                    networkPositions[network.Key] = new List<Vector2>();
                    newList[network.Key] = new List<NetworkData>();
                }

                // Convert RSSI to distance
                float distance = RssiToDistance(network.Value);
                Vector2 position2D = new Vector2(scanData.position.x, scanData.position.z);
                networkPositions[network.Key].Add(position2D + (Vector2.one * distance));

                NetworkData newPos = new NetworkData();
                newPos.position = position2D;
                newPos.distance = RssiToDistance(network.Value);
                newList[network.Key].Add(newPos);
            }
        }

        /*foreach (var network in networkPositions)
        {
            if (network.Value.Count > 3) // Must have at least 4 points
            {
                Vector2 center = WeightedCentroid(network.Value);
                Debug.Log($"Calculated center for network {network.Key}: {center}");
                
                PlaceLocationMarker(network.Key, center);
            }
        }*/

        foreach (var network in newList)
        {
            if (network.Value.Count > 3) // Must have at least 4 points
            {
                Vector2 center = FindCenter(network.Value);
                Debug.Log($"Calculated center for network {network.Key}: {center}");
                
                PlaceLocationMarker(network.Key, center);
            }
            
        }

        /*foreach (var network in newList)
        {
            int size = network.Value.Count;
            Vector2 center = Trilaterate(network.Value[size-1].position, network.Value[size-1].distance, network.Value[size-2].position, network.Value[size-2].distance, network.Value[size-3].position, network.Value[size-3].distance);

            if (center != Vector2.zero)
            {
                PlaceLocationMarker(network.Key, center);
            }
        }*/
    }

    float RssiToDistance(int rssi)
    {
        // Using the log-distance path loss model
        // RSSI = - (10 * n * log10(d) + A)
        // d = 10 ^ ((A - RSSI) / (10 * n))

        float A = -15f; // RSSI at 1 meter, you may need to adjust this
        float n = 5f; // Path-loss exponent, you may need to adjust this

        float distance = Mathf.Pow(10, (A - rssi) / (10 * n));
        return distance;
    }

    Vector3 WeightedCentroid(List<Vector2> positions)
    {
        // Simple averaging for trilateration
        Vector2 sum = Vector2.zero;
        foreach (var position in positions)
        {
            sum += position;
        }

        return sum / positions.Count;
    }

    /*Vector3 FindCenter(List<NetworkData> data)
    {
        float totalX = 0;
        float totalY = 0;
        float total = 0;
        foreach (var d in data) {
            float n = d.distance + 100f;
            if (n < 0) {
                n = 0;
            }
            total += n;
            totalX += d.position.x * n;
            totalY += d.position.y * n;
        }
        return new Vector3(totalX/total, 0, totalY/total);
    }*/

    Vector2 FindCenter(List<NetworkData> data)
    {
        int count = data.Count;
        float[] distances = new float[count];
        Vector2[] positions = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            distances[i] = data[i].distance;
            positions[i] = data[i].position;
        }

        float x = 0, y = 0;
        float totalWeight = 0;

        for (int i = 0; i < count; i++)
        {
            float weight = 1.0f / distances[i];
            x += positions[i].x * weight;
            y += positions[i].y * weight;
            totalWeight += weight;
        }

        return new Vector2(x / totalWeight, y / totalWeight);
    }

    // Uses last 3 values to calculate center of Network
    Vector3 Trilaterate(Vector2 p1, float d1, Vector2 p2, float d2, Vector2 p3, float d3) {
        float A = 2 * (p2.x - p1.x);
        float B = 2 * (p2.y - p1.y);
        float D = 2 * (p3.x - p1.x);
        float E = 2 * (p3.y - p1.y);
        
        float C = d1 * d1 - d2 * d2 - p1.x * p1.x - p1.y * p1.y + p2.x * p2.x + p2.y * p2.y;
        float F = d1 * d1 - d3 * d3 - p1.x * p1.x - p1.y * p1.y + p3.x * p3.x + p3.y * p3.y;

        float denominator = 2 * (A * E - B * D);

        if (denominator < 1e-6)
        {
            Debug.Log("The provided points are collinear or too close to being collinear.");
            return Vector2.zero;
        }

        float x = (C * E - F * B) / denominator;
        float y = (A * F - D * C) / denominator;

        return new Vector2 (x, y);
    }

    void PlaceLocationMarker(string name, Vector2 position) {
        if (locationMarkers.ContainsKey(name)) {
            // Update the position of the marker
            locationMarkers[name].transform.position = new Vector3(position.x, 0, position.y);
        } else {
            GameObject marker = Instantiate(locationMarker, new Vector3(position.x, 0, position.y), Quaternion.identity);

            // Change text
            TextMeshProUGUI textMeshPro = marker.GetComponentInChildren<TextMeshProUGUI>();

            if (textMeshPro != null)
            {
                textMeshPro.text = name;
            }

            locationMarkers.Add(name, marker);
        }
        
    }
}
