using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SignalMesh : MonoBehaviour
{
    Mesh mesh;

    Vector3[] vertices;
    int[] triangles;
    Color[] colors;

    public int xSize = 40;
    public int zSize = 40;
    public float vertexSpacing = 0.5f; // Spacing between vertices
    public float timeInterval = 0.5f; // Time between mesh updates
    // public float interpolationFactor = 0.5f; // Determines how quickly the mesh adapts to new heigth values
    public float interpolationDistance = 0.5f; // Radius of interpolation
    public Gradient gradient;

    public Camera mapCamera;
    public GameObject mapBackground;

    private Transform cameraRigTransform;
    private Transform centerEyeAnchorTransform;
    private WifiStrength wifiStrength;
    private float timer;

    void Start()
    {
        // Initialize WiFi signal strength fetcher
        wifiStrength = gameObject.AddComponent<WifiStrength>();

        // Get the transform of the OVRCameraRig
        GameObject cameraRig = GameObject.Find("OVRCameraRigInteraction/OVRCameraRig");
        if (cameraRig != null)
        {
            cameraRigTransform = cameraRig.transform;
            centerEyeAnchorTransform = cameraRigTransform.Find("TrackingSpace/CenterEyeAnchor");
        }

        timer = 0f;

        // Mesh
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        // Set the map camera heigth to see all the mesh
        if (mapCamera != null) {
            mapCamera.orthographicSize = xSize * vertexSpacing / 2;
        }
        // Resize background plane to fill the map
        if (mapBackground != null) {
            mapBackground.transform.localScale = new Vector3(xSize * vertexSpacing + 1, 1f, zSize * vertexSpacing + 1);
        }

        CreateShape(); // Create initial mesh
        UpdateMesh();
    }

    void CreateShape()
    {
        // Create Vertices
        vertices = new Vector3[(xSize + 1) * (zSize + 1)];

        for (int i = 0, z = 0; z <= zSize; z++)
        {
            for (int x = 0; x <= xSize; x++)
            {
                vertices[i] = new Vector3(x * vertexSpacing - xSize * vertexSpacing / 2, 0, z * vertexSpacing - zSize * vertexSpacing / 2);
                i++;
            }
        }

        // Create Triangles
        triangles = new int[xSize * zSize * 6];
        int vert = 0;
        int tris = 0;
        for (int z = 0; z < zSize; z++)
        {
            for (int x = 0; x < xSize; x++)
            {
                // Create line
                triangles[tris] = vert;
                triangles[tris + 1] = vert + xSize + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + xSize + 1;
                triangles[tris + 5] = vert + xSize + 2;

                vert++;
                tris += 6;
            }
            vert++;
        }

        // Give color to the vertices
        colors = new Color[vertices.Length];

        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear; // Initialize with the lowest value of the gradient
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;

        mesh.RecalculateNormals();
    }

    void Update()
    {
        // If centerEyeAnchorTransform is not assigned, return
        if (centerEyeAnchorTransform == null)
        {
            Debug.LogWarning("CenterEyeAnchorTransform is not assigned.");
            return;
        }

        timer += Time.deltaTime;
        if (timer >= timeInterval)
        {
            // Check if user is over a different vertex
            Vector3 userPosition = centerEyeAnchorTransform.position;
            ReadWifi(userPosition);
            timer = 0f;
        }
    }

    void ReadWifi(Vector3 userPosition)
    {
        // Get Wifi signal strength
        int signalStrength = wifiStrength.GetSignalStrength();
        Debug.Log("Signal Strength: " + signalStrength);

        // Calculate mesh height
        float height = signalStrength + 90; // Value is now between 0 and 60
        if (height < 0)
        {
            height = 0;
        }
        height /= 50; // Value is now between 0m and 1.20m

        // Get color of the vertex
        Color color = gradient.Evaluate(height / 1.20f); // Value between 0 and 1

        ApplyInterpolation(userPosition, height, color);
        UpdateMesh();
    }

    void ApplyInterpolation(Vector3 userPosition, float newHeight, Color newColor)
    {
        float minX = -xSize * vertexSpacing / 2;
        float minZ = -zSize * vertexSpacing / 2;

        float relativeX = userPosition.x - transform.position.x - minX;
        float relativeZ = userPosition.z - transform.position.z - minZ;

        int xIndex = Mathf.Clamp(Mathf.FloorToInt(relativeX / vertexSpacing), 0, xSize - 1);
        int zIndex = Mathf.Clamp(Mathf.FloorToInt(relativeZ / vertexSpacing), 0, zSize - 1);

        for (int z = -Mathf.CeilToInt(interpolationDistance / vertexSpacing); z <= Mathf.CeilToInt(interpolationDistance / vertexSpacing); z++)
        {
            for (int x = -Mathf.CeilToInt(interpolationDistance / vertexSpacing); x <= Mathf.CeilToInt(interpolationDistance / vertexSpacing); x++)
            {
                int index = (zIndex + z) * (xSize + 1) + (xIndex + x);
                if (index >= 0 && index < vertices.Length)
                {
                    float distance = Vector3.Distance(new Vector3((xIndex + x) * vertexSpacing, 0, (zIndex + z) * vertexSpacing), new Vector3(relativeX, 0, relativeZ));
                    if (distance <= interpolationDistance)
                    {
                        // Use a smoothstep function to create a smoother interpolation
                        float interpolationFactor = Mathf.SmoothStep(0, 1, 1 - (distance / interpolationDistance));

                        // Interpolation for height
                        vertices[index].y = Mathf.Lerp(vertices[index].y, newHeight, interpolationFactor);

                        // Interpolation for color
                        colors[index] = Color.Lerp(colors[index], newColor, interpolationFactor);
                    }
                }
            }
        }
    }
}