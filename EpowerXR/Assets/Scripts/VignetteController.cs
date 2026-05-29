using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VignetteController : MonoBehaviour
{
    [SerializeField] private BiometricReceiver biometricReceiver;
    [SerializeField] private Volume volume;

    private Vignette vignette;

    void Start()
    {
        volume.profile.TryGet(out vignette);
    }

    void Update()
    {
        if (vignette == null) return;

        float t = Mathf.InverseLerp(60f, 120f, biometricReceiver.heartRate);
        vignette.intensity.value = Mathf.Lerp(0f, 0.8f, t);
    }
}