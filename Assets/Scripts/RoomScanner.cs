using System.Collections.Generic;
using UnityEngine;

public class RoomScanner : MonoBehaviour
{
    public Transform origin; // The transform from which to cast the ray
    public GameObject cube; // The cube prefab to instantiate
    public Material lineMaterial; // Material for the connecting lines
    public float snapThreshold = 0.5f; // Threshold distance for snapping
    public OVRInput.Button placeCubeButton = OVRInput.Button.SecondaryIndexTrigger;
    public OVRInput.Button deletePreviousButton = OVRInput.Button.SecondaryHandTrigger;
    public OVRInput.Button interruptLineButton = OVRInput.Button.One; // Button to interrupt the current line

    private List<Vector3> points = new List<Vector3>(); // List to store points
    private List<GameObject> placedCubes = new List<GameObject>(); // List to store placed cube instances
    private List<LineRenderer> lineRenderers = new List<LineRenderer>(); // List to store line renderers

    private LineRenderer currentLineRenderer; // Current line renderer

    void Start()
    {
        CreateNewLineRenderer();
    }

    void Update()
    {
        if (OVRInput.GetDown(placeCubeButton))
        {
            Debug.Log("Res: Trigger point");
            RaycastHit hit;
            if (Physics.Raycast(origin.position, origin.forward, out hit) && hit.transform.name == "Plane")
            {
                Debug.Log("Res: Hit plane");
                Vector3 hitPoint = hit.point;

                // Check for snapping
                Vector3 snappedPoint = GetSnappedPoint(hitPoint);

                // Check if the new point is not repeated
                if (points.Count == 0 || snappedPoint != points[points.Count - 1])
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
        foreach (Vector3 placedPoint in points)
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
        points.Add(hitPoint);
        placedCubes.Add(newCube);

        // Update the current line renderer with the new point
        currentLineRenderer.positionCount = points.Count;
        currentLineRenderer.SetPositions(points.ToArray());
    }

    void RemoveLastPoint()
    {
        // Undo action
        if (points.Count > 0)
        {
            // Remove the last point and the corresponding cube
            points.RemoveAt(points.Count - 1);
            GameObject lastCube = placedCubes[placedCubes.Count - 1];
            Destroy(lastCube);
            placedCubes.RemoveAt(placedCubes.Count - 1);

            // Update the current line renderer
            currentLineRenderer.positionCount = points.Count;
            currentLineRenderer.SetPositions(points.ToArray());
            Debug.Log("Removed the last point");
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

        // Clear points list to start a new line segment
        points.Clear();
    }
}