using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SafeSpaceStateMachine : MonoBehaviour
{
    [SerializeField] private BiometricReceiver biometricReceiver;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private GameObject puppiesEnvironment;
    public enum InterventionMode { ARPassthrough, SwapObjects }
    [SerializeField] private InterventionMode interventionMode = InterventionMode.ARPassthrough;
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

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
        Debug.Log("OVERWHELMED - triggering intervention");
        if (interventionMode == InterventionMode.ARPassthrough)
        {
            arCameraManager.enabled = true;
            puppiesEnvironment.SetActive(true);
        }
        else if (interventionMode == InterventionMode.SwapObjects)
        {
            if (objectsToActivate != null)
            {
                foreach (var obj in objectsToActivate)
                    if (obj != null) obj.SetActive(true);
            }
            if (objectsToDeactivate != null)
            {
                foreach (var obj in objectsToDeactivate)
                    if (obj != null) obj.SetActive(false);
            }
        }
    }

    private void ResumeExperience()
    {
        Debug.Log("CALM - returning to VR");
        if (interventionMode == InterventionMode.ARPassthrough)
        {
            arCameraManager.enabled = false;
            puppiesEnvironment.SetActive(false);
        }
        else if (interventionMode == InterventionMode.SwapObjects)
        {
            if (objectsToActivate != null)
            {
                foreach (var obj in objectsToActivate)
                    if (obj != null) obj.SetActive(false);
            }
            if (objectsToDeactivate != null)
            {
                foreach (var obj in objectsToDeactivate)
                    if (obj != null) obj.SetActive(true);
            }
        }
    }
}