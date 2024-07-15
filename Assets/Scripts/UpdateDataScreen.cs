using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class UpdateDataScreen : MonoBehaviour
{
    public TextMeshProUGUI wifiStrengthText; // Reference to the Text
    public float textTimeInterval = 0.5f; // Time between updates

    public TextMeshProUGUI wifiSpeedText; // Reference to the Text
    public float speedTextTimeInterval = 2f; // Time between updates

    public TMP_Dropdown wifiDropdown;

    public float dropdownTimeInterval = 1f;
    public TMP_Text text;

    private float textTimer;
    private float speedTextTimer;
    private float dropdownTimer;
    public WifiStrength wifiStrength;

    void Start() {
        if (wifiStrengthText == null)
        {
            Debug.LogError("UI Text reference is not set.");
        }

        UpdateSignalStrength();
    }

    void Update() {
        textTimer += Time.deltaTime; // Only update after a given time interval
        if (textTimer >= textTimeInterval)
        {
            UpdateSignalStrength();
            textTimer = 0f;
        }

        speedTextTimer += Time.deltaTime; // Only update after a given time interval
        if (speedTextTimer >= speedTextTimeInterval)
        {
            StartCoroutine(wifiStrength.GetSpeed());
            speedTextTimer = 0f;
        }

        dropdownTimer += Time.deltaTime; // Only update after a given time interval
        if (dropdownTimer >= dropdownTimeInterval)
        {
            UpdateDropdown();
            dropdownTimer = 0f;
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

    public void UpdateDropdown()
    {
        wifiDropdown.options.Clear();
        List<string> networks = wifiStrength.GetPreviouslyConnectedNetworks();
        foreach (string n in networks)
        {
            wifiDropdown.options.Add (new TMP_Dropdown.OptionData() {text=n});
        }
    }
}
