using UnityEngine;
using TMPro;

public class BiometricDebugUI : MonoBehaviour
{
    [SerializeField] private BiometricReceiver biometricReceiver;
    [SerializeField] private TextMeshProUGUI debugText;

    void Update()
    {
        debugText.text = $"HR: {biometricReceiver.heartRate} bpm\n" +
                         $"Resp: {biometricReceiver.respiration}\n" +
                         $"Skin Temp: {biometricReceiver.skinTemp} °C\n" +
                         $"SpO2: {biometricReceiver.spo2}%\n" +
                         $"Stress: {biometricReceiver.stressLevel}\n" +
                         $"Sweat: {biometricReceiver.sweatLoss}";
    }
}