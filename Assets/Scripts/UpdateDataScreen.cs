using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class UpdateDataScreen : MonoBehaviour
{
    public TextMeshProUGUI wifiNameText; // Reference to the Text
    public float nameTimeInterval = 60f; // Time between updates
    
    public TextMeshProUGUI wifiStrengthText; // Reference to the Text
    public float textTimeInterval = 0.5f; // Time between updates

    public TextMeshProUGUI wifiSpeedText; // Reference to the Text
    public float speedTextTimeInterval = 2f; // Time between updates

    public TMP_Text text;

    public TextMeshProUGUI FPSText; // Reference to the Text

    private float nameTimer;
    private float textTimer;
    private float speedTextTimer;
    public WifiStrength wifiStrength;
    int fps = 0;

    void Start() {
        if (wifiStrengthText == null)
        {
            Debug.LogError("UI Text reference is not set.");
        }

        UpdateSignalStrength();
        UpdateWifiName();
    }

    void Update() {
        textTimer += Time.deltaTime; // Only update after a given time interval
        if (textTimer >= textTimeInterval)
        {
            UpdateSignalStrength();
            UpdateFPS();
            textTimer = 0f;
        }

        speedTextTimer += Time.deltaTime; // Only update after a given time interval
        if (speedTextTimer >= speedTextTimeInterval)
        {
            StartCoroutine(wifiStrength.GetSpeed());
            speedTextTimer = 0f;
        }

        nameTimer += Time.deltaTime; // Only update after a given time interval
        if (nameTimer >= nameTimeInterval)
        {
            UpdateWifiName();
            nameTimer = 0f;
        }

        fps = (int)(1f / Time.unscaledDeltaTime);
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
            //string signalStrength = wifiStrength.ScanForNetworks();
            //wifiStrengthText.text = signalStrength;
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
