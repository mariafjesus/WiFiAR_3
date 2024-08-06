using System;
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

    IEnumerator ScanNetworks()
    {
        totalScans++;

        if (wifiManager == null) yield return null;

        bool success = wifiManager.Call<bool>("startScan");
        if (!success) yield return null; // Couldn't start scan

        // Place scan icon
        Instantiate(scanIcon, new Vector3(lastPosition.x, 0, lastPosition.z), Quaternion.Euler(90, 0, 0));

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
        }
        yield return null;
    }

    void CalculateCenter()
    {
        Dictionary<string, List<NetworkData>> networksList = new Dictionary<string, List<NetworkData>>();

        foreach (var scanData in scanDataList)
        {
            foreach (var network in scanData.networks)
            {
                if (!networksList.ContainsKey(network.Key))
                {
                    networksList[network.Key] = new List<NetworkData>();
                }

                NetworkData newPos = new NetworkData();
                newPos.position = new Vector2(scanData.position.x, scanData.position.z);
                newPos.distance = RssiToDistance(network.Value);

                networksList[network.Key].Add(newPos);
            }
        }

        foreach (var network in networksList)
        {
            if (network.Value.Count > 3) // Must have at least 4 points
            {
                Vector2 center = FindCenter(network.Value);
                Debug.Log($"Calculated center for network {network.Key}: {center}");
                
                PlaceLocationMarker(network.Key, center);
            }
            
        }
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

    // Calculate center using trilateration
    Vector2 FindCenterTrilateration(List<NetworkData> data)
    {
        if (data.Count < 3) return Vector2.zero; // Need at least 3 points for trilateration
        
        // Use last 3 points
        Vector2 p1 = data[data.Count - 3].position;
        Vector2 p2 = data[data.Count - 2].position;
        Vector2 p3 = data[data.Count - 1].position;

        float r1 = data[data.Count - 3].distance;
        float r2 = data[data.Count - 2].distance;
        float r3 = data[data.Count - 1].distance;

        // Find the points of intersection
        Vector2 intersection12a, intersection12b,
                intersection23a, intersection23b,
                intersection31a, intersection31b;

        if (FindCircleCircleIntersections(p1, p2, r1, r2, out intersection12a, out intersection12b) == 0) {
            Debug.Log("Failed at T1");
            return Vector2.zero;
        }
        if (FindCircleCircleIntersections(p1, p3, r1, r3, out intersection23a, out intersection23b) == 0) {
            Debug.Log("Failed at T2");
            return Vector2.zero;
        }
        if (FindCircleCircleIntersections(p3, p2, r3, r2, out intersection31a, out intersection31b) == 0) {
            Debug.Log("Failed at T3");
            return Vector2.zero;
        }

        // Find the points that make up the target area.
        Vector2[] triangle = new Vector2[3];
        if (Distance(intersection12a, p3) <
                Distance(intersection12b, p3))
            triangle[0] = intersection12a;
        else
            triangle[0] = intersection12b;
        if (Distance(intersection23a, p1) <
                Distance(intersection23b, p1))
            triangle[1] = intersection23a;
        else
            triangle[1] = intersection23b;
        if (Distance(intersection31a, p2) <
                Distance(intersection31b, p2))
            triangle[2] = intersection31a;
        else
            triangle[2] = intersection31b;

        return FindTriangleCentroid(triangle[0], triangle[1], triangle[2]);
    }

    // Return the triangle's center
    private Vector2 FindTriangleCentroid(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return new Vector2(
            (p1.x + p2.x + p3.x) / 3f,
            (p1.y + p2.y + p3.y) / 3f);
    }

    private double Distance(Vector2 p1, Vector2 p2)
    {
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        double dist = Math.Sqrt(dx*dx + dy*dy);
        return dist;
    }

    int FindCircleCircleIntersections(Vector2 p1, Vector2 p2, float r1, float r2, out Vector2 intersection1, out Vector2 intersection2)
    {
        // Find the distance between the centers
        float dx = p1.x - p2.x;
        float dy = p1.y - p2.y;
        double dist = Math.Sqrt(dx*dx + dy*dy);

        // Find number of solutions
        if (dist > r1 + r2)
        {
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if (dist < Math.Abs(r1 - r2))
        {
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else if ((dist == 0) && (r1 == r2))
        {
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 0;
        }
        else {
            // Find a and h.
            double a = (r1 * r1 - r2 * r2 + dist * dist) / (2 * dist);
            double h = Math.Sqrt(r1 * r1 - a * a);

            // Find P2.
            double cx2 = p1.x + a * (p2.x - p1.x) / dist;
            double cy2 = p1.y + a * (p2.y - p1.y) / dist;

            // Get the points P3.
            intersection1 = new Vector2(
                (float)(cx2 + h * (p2.y - p1.y) / dist),
                (float)(cy2 - h * (p1.x - p1.x) / dist));
            intersection2 = new Vector2(
                (float)(cx2 - h * (p2.y - p1.y) / dist),
                (float)(cy2 + h * (p2.x - p1.x) / dist));

            // See if we have 1 or 2 solutions.
            if (dist == r1 + r2) return 1;
            intersection1 = new Vector2(float.NaN, float.NaN);
            intersection2 = new Vector2(float.NaN, float.NaN);
            return 2;
        }
    }

    void PlaceLocationMarker(string name, Vector2 position)
    {
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
