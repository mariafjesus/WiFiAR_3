using UnityEngine;
using TMPro;

public class UpdateDataScreen : MonoBehaviour
{
    // Wifi Name
    public TextMeshProUGUI wifiNameText;
    public float nameTimeInterval = 60f;
    
    // Wifi Strength
    public TextMeshProUGUI wifiStrengthText;
    public float textTimeInterval = 0.5f;

    // Wifi Speed
    public TextMeshProUGUI wifiSpeedText;
    public float speedTextTimeInterval = 2f;

    public TextMeshProUGUI FPSText; // FPS text label

    private float nameTimer;
    private float textTimer;
    private float speedTextTimer;
    public WifiStrength wifiStrength;
    int fps = 0;

    void Start()
    {
        if (wifiStrengthText == null)
        {
            Debug.LogError("UI Text reference is not set.");
        }

        UpdateSignalStrength();
        UpdateWifiName();
    }

    void Update()
    {
        // Only update after a given time interval
        textTimer += Time.deltaTime;
        if (textTimer >= textTimeInterval)
        {
            UpdateSignalStrength();
            UpdateFPS();
            textTimer = 0f;
        }

        speedTextTimer += Time.deltaTime;
        if (speedTextTimer >= speedTextTimeInterval)
        {
            StartCoroutine(wifiStrength.GetSpeed());
            speedTextTimer = 0f;
        }

        nameTimer += Time.deltaTime;
        if (nameTimer >= nameTimeInterval)
        {
            UpdateWifiName();
            nameTimer = 0f;
        }

        fps = (int)(1f / Time.unscaledDeltaTime); // Calculate FPS
    }

    public void UpdateWifiName()
    {
        if (wifiNameText != null)
        {
            string name = wifiStrength.GetWifiName();
            wifiNameText.text = name;
        }
    }

    public void UpdateSignalStrength()
    {
        if (wifiStrengthText != null)
        {
            int signalStrength = wifiStrength.GetSignalStrength();
            wifiStrengthText.text = signalStrength + " dBm";
        }
    }

    public void UpdateSignalSpeed(float speed)
    {
        if (wifiSpeedText != null)
        {
            wifiSpeedText.text = speed + " Mbps";
        }
    }

    public void UpdateFPS()
    {
        FPSText.text = fps + " FPS";
    }
}
