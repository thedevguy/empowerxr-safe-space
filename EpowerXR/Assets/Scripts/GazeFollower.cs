using UnityEngine;

public class GazeFollower : MonoBehaviour
{
    [Header("Gaze")]
    [SerializeField] private Camera xrCamera;
    [SerializeField] private float distance = 2f;
    [SerializeField] private float lerpSpeed = 3f;

    [Header("Scale Oscillation")]
    [SerializeField] private float scaleFrequency = 1f;
    [SerializeField] private float scaleAmplitude = 0.1f;
    private Vector3 baseScale;

    [Header("Hover Oscillation")]
    [SerializeField] private float hoverFrequency = 0.8f;
    [SerializeField] private float hoverAmplitude = 0.05f;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        // Gaze follow
        Vector3 targetPosition = xrCamera.transform.position +
                                 xrCamera.transform.forward * distance;

        // Hover offset
        float hoverOffset = Mathf.Sin(Time.time * hoverFrequency * Mathf.PI * 2f)
                            * hoverAmplitude;
        targetPosition.y += hoverOffset;

        transform.position = Vector3.Lerp(transform.position, targetPosition,
                                          Time.deltaTime * lerpSpeed);

        // Scale oscillation
        float scaleOffset = Mathf.Sin(Time.time * scaleFrequency * Mathf.PI * 2f)
                            * scaleAmplitude;
        transform.localScale = baseScale + Vector3.one * scaleOffset;
    }
}