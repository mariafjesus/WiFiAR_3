using UnityEngine;

public class MapCursor : MonoBehaviour
{
    public Transform playerTransform; // Reference to the player's transform (the centerEyeAnchor or any object that represents player's position)
    private Vector3 initialArrowPosition;

    void Start()
    {
        // Initialize the arrow's initial position relative to the player
        if (playerTransform != null)
        {
            initialArrowPosition = transform.position - playerTransform.position;
        }
    }

    void LateUpdate()
    {
        if (playerTransform != null)
        {
            // Update the arrow's position to follow the player's 2D translation
            Vector3 newPosition = playerTransform.position + initialArrowPosition;
            newPosition.y = transform.position.y; // Keep the arrow at the same height
            transform.position = newPosition;

            transform.rotation = Quaternion.Euler(90, playerTransform.transform.localEulerAngles.y, 0);
        }
    }
}
