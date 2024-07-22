using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class WrapTextAroundCylinder : MonoBehaviour
{
    public Transform cylinder;  // Reference to the cylinder
    public float radius = 1.0f; // Radius of the cylinder

    private TextMeshPro tmpText;
    private Mesh mesh;
    private Vector3[] vertices;

    void Start()
    {
        tmpText = GetComponent<TextMeshPro>();
        mesh = tmpText.mesh;
        vertices = mesh.vertices;
    }

    void Update()
    {
        if (cylinder != null)
        {
            WrapText();
        }
    }

    void WrapText()
    {
        mesh = tmpText.mesh;
        vertices = mesh.vertices;

        float circumference = 2 * Mathf.PI * radius;
        float angleIncrement = 360f / tmpText.textInfo.characterCount;

        for (int i = 0; i < tmpText.textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = tmpText.textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            int vertexIndex = charInfo.vertexIndex;
            float angle = i * angleIncrement * Mathf.Deg2Rad;

            for (int j = 0; j < 4; j++)
            {
                Vector3 original = vertices[vertexIndex + j];
                float x = original.x;
                float z = Mathf.Sin(angle) * radius;
                float y = original.y;
                float newZ = Mathf.Cos(angle) * radius;

                vertices[vertexIndex + j] = new Vector3(x, y, newZ);
                angle += angleIncrement * Mathf.Deg2Rad / 4;
            }
        }

        mesh.vertices = vertices;
        tmpText.canvasRenderer.SetMesh(mesh);
    }
}