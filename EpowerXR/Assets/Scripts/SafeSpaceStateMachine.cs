using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SafeSpaceStateMachine : MonoBehaviour
{
    [SerializeField] private BiometricReceiver biometricReceiver;
    [SerializeField] private ARCameraManager arCameraManager;
    [SerializeField] private GameObject puppiesEnvironment;
    public enum InterventionMode { ARPassthrough, SwapObjects }
    [SerializeField] private InterventionMode interventionMode = InterventionMode.ARPassthrough;
    public enum TriggerMode { HeartRateThreshold, TimerCountdown }
    [SerializeField] private TriggerMode triggerMode = TriggerMode.HeartRateThreshold;
    [SerializeField] private GameObject[] objectsToActivate;
    [SerializeField] private GameObject[] objectsToDeactivate;

    private bool isUserOverwhelmed = false;
    [SerializeField] private float heartRateThreshold = 77f;
    [SerializeField] private float countdownDuration = 30f;
    private float countdownTimer = 0f;
    private Material originalSkybox;

    private void Awake()
    {
        originalSkybox = RenderSettings.skybox;
    }

    void Update()
    {
        EvaluateState();
    }

    private void EvaluateState()
    {
        if (!isUserOverwhelmed)
        {
            bool shouldTrigger = false;

            if (triggerMode == TriggerMode.HeartRateThreshold)
            {
                shouldTrigger = (biometricReceiver.heartRate > heartRateThreshold || biometricReceiver.stressLevel > 70f);
            }
            else if (triggerMode == TriggerMode.TimerCountdown)
            {
                countdownTimer += Time.deltaTime;
                shouldTrigger = (countdownTimer >= countdownDuration);
            }

            if (shouldTrigger)
            {
                isUserOverwhelmed = true;
                TriggerIntervention();
            }
        }
        else
        {
            bool shouldResume = false;

            if (triggerMode == TriggerMode.HeartRateThreshold)
            {
                shouldResume = (biometricReceiver.heartRate < heartRateThreshold && biometricReceiver.stressLevel < 50f);
            }
            else if (triggerMode == TriggerMode.TimerCountdown)
            {
                shouldResume = (countdownTimer <= 0f);
                if (!shouldResume)
                    countdownTimer -= Time.deltaTime;
            }

            if (shouldResume)
            {
                isUserOverwhelmed = false;
                ResumeExperience();
                if (triggerMode == TriggerMode.TimerCountdown)
                    countdownTimer = 0f;
            }
        }
    }

    private void TriggerIntervention()
    {
        Debug.Log("OVERWHELMED - triggering intervention");
        RenderSettings.skybox = null;

        if (interventionMode == InterventionMode.ARPassthrough)
        {
            arCameraManager.enabled = true;
            puppiesEnvironment.SetActive(true);
        }
        else if (interventionMode == InterventionMode.SwapObjects)
        {
            arCameraManager.enabled = true;
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
        RenderSettings.skybox = originalSkybox;

        if (interventionMode == InterventionMode.ARPassthrough)
        {
            arCameraManager.enabled = false;
            puppiesEnvironment.SetActive(false);
        }
        else if (interventionMode == InterventionMode.SwapObjects)
        {
            arCameraManager.enabled = false;
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