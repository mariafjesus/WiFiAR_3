using System.Collections.Generic;
using UnityEngine;

public class RoomScanner : MonoBehaviour
{
    public Transform origin; // The transform from which to cast the ray
    public GameObject cube; // The cube prefab to instantiate
    public GameObject wallPrefab; // The wall prefab to instantiate
    public Material lineMaterial; // Material for the connecting lines
    public Material mapLineMaterial; // Material for map lines
    public float snapThreshold = 0.5f; // Threshold distance for snapping

    public OVRInput.Button placeCubeButton = OVRInput.Button.SecondaryIndexTrigger;
    public OVRInput.Button deletePreviousButton = OVRInput.Button.SecondaryHandTrigger;
    public OVRInput.Button interruptLineButton = OVRInput.Button.One; // Button to interrupt the current line

    private List<List<Vector3>> pointGroups = new List<List<Vector3>>(); // List of lists to store points for each group
    private List<Vector3> allPoints = new List<Vector3>(); // List to store all points
    private List<GameObject> placedCubes = new List<GameObject>(); // List to store placed cube instances
    private List<WallConnection> wallConnections = new List<WallConnection>(); // List to store wall connections
    private List<LineRenderer> lineRenderers = new List<LineRenderer>(); // List to store line renderers
    private List<LineRenderer> mapLineRenderers = new List<LineRenderer>(); // List to store map line renderers

    private LineRenderer currentLineRenderer; // Current line renderer
    private LineRenderer currentMapLineRenderer; // Current thick line renderer
    private List<Vector3> currentPoints; // Points for the current group

    private class WallConnection
    {
        public Vector3 Point1;
        public Vector3 Point2;
        public GameObject Wall;

        public WallConnection(Vector3 point1, Vector3 point2, GameObject wall)
        {
            Point1 = point1;
            Point2 = point2;
            Wall = wall;
        }
    }

    void Start()
    {
        CreateNewLineRenderer();
    }

    void Update()
    {
        if (OVRInput.GetDown(placeCubeButton))
        {
            RaycastHit hit;
            if (Physics.Raycast(origin.position, origin.forward, out hit) && hit.transform.name == "MapBackground")
            {
                Vector3 hitPoint = hit.point;

                // Check for snapping
                Vector3 snappedPoint = GetSnappedPoint(hitPoint);

                // Check if the new point is not repeated
                if (currentPoints.Count == 0 || snappedPoint != currentPoints[currentPoints.Count - 1])
                {
                    PlacePoint(snappedPoint);
                }
            }
        }
        else if (OVRInput.GetDown(deletePreviousButton))
        {
            RemoveLastPoint();
        }
        else if (OVRInput.GetDown(interruptLineButton))
        {
            CreateNewLineRenderer();
        }
    }

    Vector3 GetSnappedPoint(Vector3 point)
    {
        foreach (Vector3 placedPoint in allPoints)
        {
            if (Vector3.Distance(point, placedPoint) <= snapThreshold)
            {
                return placedPoint; // Snap to the closest point within the threshold
            }
        }
        return point; // Return the original point if no snapping occurs
    }

    void PlacePoint(Vector3 hitPoint)
    {
        GameObject newCube = Instantiate(cube, hitPoint, Quaternion.identity); // Create a new cube
        currentPoints.Add(hitPoint);
        allPoints.Add(hitPoint);
        placedCubes.Add(newCube);

        // Place wall if there are at least 2 points in the current group
        if (currentPoints.Count > 1)
        {
            PlaceWall(currentPoints[currentPoints.Count - 2], currentPoints[currentPoints.Count - 1]);
        }

        // Update the current line renderer with the new point
        UpdateLineRenderer(currentLineRenderer, currentPoints);

        // Update the current map line renderer with the new point
        UpdateLineRenderer(currentMapLineRenderer, currentPoints);
    }

    void PlaceWall(Vector3 startPoint, Vector3 endPoint)
    {
        Vector3 wallPosition = (startPoint + endPoint) / 2 + new Vector3(0, 0.6f, 0);
        Vector3 direction = endPoint - startPoint;
        Quaternion wallRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, 90, 0);

        GameObject newWall = Instantiate(wallPrefab, wallPosition, wallRotation);
        newWall.transform.localScale = new Vector3(direction.magnitude, 1.2f, 1); // Scale wall to fit between points

        MeshFilter mf = newWall.GetComponent<MeshFilter>();
        Mesh mesh = mf.mesh;

        Color[] colors = new Color[mesh.vertices.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.Lerp(Color.blue, Color.clear, (float)i / (colors.Length - 1));
        }

        mesh.colors = colors;

        wallConnections.Add(new WallConnection(startPoint, endPoint, newWall));
    }

    void RemoveLastPoint()
    {
        if (currentPoints.Count > 0)
        {
            // Remove the last point from the current group
            Vector3 lastPoint = currentPoints[currentPoints.Count - 1];
            currentPoints.RemoveAt(currentPoints.Count - 1);
            allPoints.Remove(lastPoint);

            // Remove the corresponding cube
            GameObject lastCube = placedCubes[placedCubes.Count - 1];
            Destroy(lastCube);
            placedCubes.RemoveAt(placedCubes.Count - 1);

            // Remove the corresponding wall
            if (currentPoints.Count > 0)
            {
                Vector3 previousPoint = currentPoints[currentPoints.Count - 1];
                for (int i = wallConnections.Count - 1; i >= 0; i--)
                {
                    if ((wallConnections[i].Point1 == lastPoint && wallConnections[i].Point2 == previousPoint) ||
                        (wallConnections[i].Point1 == previousPoint && wallConnections[i].Point2 == lastPoint))
                    {
                        Destroy(wallConnections[i].Wall);
                        wallConnections.RemoveAt(i);
                        break;
                    }
                }
            }

            // Update the current line renderer
            UpdateLineRenderer(currentLineRenderer, currentPoints);

            // Update the current map line renderer
            UpdateLineRenderer(currentMapLineRenderer, currentPoints);
        }
        else if (pointGroups.Count > 0)
        {
            // Remove the last empty group and its corresponding line renderers
            pointGroups.RemoveAt(pointGroups.Count - 1);
            LineRenderer lastLineRenderer = lineRenderers[lineRenderers.Count - 1];
            LineRenderer lastMapLineRenderer = mapLineRenderers[mapLineRenderers.Count - 1];
            lineRenderers.RemoveAt(lineRenderers.Count - 1);
            mapLineRenderers.RemoveAt(mapLineRenderers.Count - 1);
            Destroy(lastLineRenderer.gameObject);
            Destroy(lastMapLineRenderer.gameObject);

            if (pointGroups.Count > 0)
            {
                // Set the current points to the last non-empty group
                currentPoints = pointGroups[pointGroups.Count - 1];
                currentLineRenderer = lineRenderers[lineRenderers.Count - 1];
                currentMapLineRenderer = mapLineRenderers[mapLineRenderers.Count - 1];
            }
            else
            {
                CreateNewLineRenderer();
            }
        }
    }

    void CreateNewLineRenderer()
    {
        currentLineRenderer = new GameObject("LineRenderer").AddComponent<LineRenderer>();
        currentLineRenderer.material = lineMaterial;
        currentLineRenderer.startWidth = 0.02f;
        currentLineRenderer.endWidth = 0.02f;
        currentLineRenderer.positionCount = 0;
        lineRenderers.Add(currentLineRenderer);

        currentMapLineRenderer = new GameObject("ThickLineRenderer").AddComponent<LineRenderer>();
        currentMapLineRenderer.material = mapLineMaterial;
        currentMapLineRenderer.startWidth = 0.5f;
        currentMapLineRenderer.endWidth = 0.5f;
        currentMapLineRenderer.positionCount = 0;
        mapLineRenderers.Add(currentMapLineRenderer);

        currentMapLineRenderer.gameObject.layer = LayerMask.NameToLayer("Rooms");

        // Start a new group of points
        currentPoints = new List<Vector3>();
        pointGroups.Add(currentPoints);
    }

    void UpdateLineRenderer(LineRenderer lineRenderer, List<Vector3> points)
    {
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
    }
}
