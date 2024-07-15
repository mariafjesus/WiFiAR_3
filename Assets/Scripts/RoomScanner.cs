using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomScanner : MonoBehaviour
{
    public Transform origin; // The transform from which to cast the ray
    public GameObject cube; // The cube prefab to instantiate
    public Material lineMaterial; // Material for the connecting lines
    public float snapThreshold = 0.5f; // Threshold distance for snapping

    private List<Vector3> points = new List<Vector3>(); // List to store points
    private List<GameObject> placedCubes = new List<GameObject>(); // List to store placed cube instances
    private LineRenderer lineRenderer; // LineRenderer to draw connecting lines

    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.02f;
        lineRenderer.positionCount = 0;
    }

    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger))
        {
            RaycastHit hit;
            if (Physics.Raycast(origin.position, origin.forward, out hit) && hit.transform.name == "Plane")
            {
                Vector3 hitPoint = hit.point;

                // Check for snapping
                Vector3 snappedPoint = GetSnappedPoint(hitPoint);

                // Check if the new point is not repeated
                if (points.Count == 0 || snappedPoint != points[points.Count - 1])
                {
                    PlacePoint(snappedPoint);
                }
            }
        } else if (OVRInput.GetDown(OVRInput.Button.SecondaryHandTrigger))
        {
            RemoveLastPoint();
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
        //Instantiate(cube, hitPoint, Quaternion.identity);
        GameObject newCube = Instantiate(cube, hitPoint, Quaternion.identity); // Create a new cube
        points.Add(hitPoint);
        placedCubes.Add(newCube);

        // Update the line renderer with the new point
        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
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

            // Update the line renderer
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
            Debug.Log("Removed the last point");
        }
    }
}