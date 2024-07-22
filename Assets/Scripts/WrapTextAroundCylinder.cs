using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshProUGUI))]
public class WrapTextAroundCylinderUI : MonoBehaviour
{
    public Transform cylinder;  // Reference to the cylinder
    public float radius = 1.0f; // Radius of the cylinder

    public TextMeshProUGUI tmpText;

    void Start()
    {
        WrapText();
    }

    void WrapText()
    {
        TMP_TextInfo textInfo = tmpText.textInfo;
        Vector3[] vertices = textInfo.meshInfo[0].vertices;

        float circumference = 2 * Mathf.PI * radius;
        float angleIncrement = 360f / textInfo.characterCount;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
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

        tmpText.canvasRenderer.SetMesh(tmpText.mesh);
    }
}
