using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SafeSpaceStateMachine : MonoBehaviour
{
    [SerializeField] private BiometricReceiver biometricReceiver;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private GameObject puppiesEnvironment;

    private bool isUserOverwhelmed = false;
    [SerializeField] private float heartRateThreshold = 77f;

    void Update()
    {
        EvaluateState();
    }

    private void EvaluateState()
    {
        if (!isUserOverwhelmed)
        {
            if (biometricReceiver.heartRate > heartRateThreshold || biometricReceiver.stressLevel > 70f)
            {
                isUserOverwhelmed = true;
                TriggerIntervention();
            }
        }
        else
        {
            // Return to normal when values drop back down
            if (biometricReceiver.heartRate < heartRateThreshold && biometricReceiver.stressLevel < 50f)
            {
                isUserOverwhelmed = false;
                ResumeExperience();
            }
        }
    }

    private void TriggerIntervention()
    {
        Debug.Log("OVERWHELMED — triggering passthrough");
        arCameraManager.enabled = true;
        puppiesEnvironment.SetActive(true);
    }

    private void ResumeExperience()
    {
        Debug.Log("CALM — returning to VR");
        arCameraManager.enabled = false;
        puppiesEnvironment.SetActive(false);
    }
}