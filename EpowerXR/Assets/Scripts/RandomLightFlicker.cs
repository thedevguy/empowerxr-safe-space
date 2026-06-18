using UnityEngine;

public class RandomLightFlicker : MonoBehaviour
{
    [SerializeField] private Light[] lights;
    [SerializeField] private float minOnDuration = 0.5f;
    [SerializeField] private float maxOnDuration = 2f;
    [SerializeField] private float minOffDuration = 0.3f;
    [SerializeField] private float maxOffDuration = 1.5f;
    [SerializeField] private float overallFrequency = 1f;
    [SerializeField] private float overallDelay = 0f;
    [SerializeField] private float overallDuration = 10f;
    [SerializeField] private float minMoveAmplitude = 0.2f;
    [SerializeField] private float maxMoveAmplitude = 1f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private float intensityMultiplier = 1f;
    [SerializeField] private float rangeMultiplier = 1f;
    [SerializeField] private AudioSource soundSource1;
    [SerializeField] private AudioSource soundSource2;
    [SerializeField] private AudioSource soundSource3;
    [SerializeField] private float soundPlayDelay = 5f;
    [SerializeField] private Rigidbody rigidbodyTarget;
    [SerializeField] private GameObject activateOnDrop;

    private float[] lightTimers;
    private bool[] lightStates;
    private float elapsedTime;
    private bool rigidbodyActivated = false;
    private bool dropActivated = false;
    private float[] initialIntensities;
    private float[] initialRanges;
    private Vector3[] initialPositions;
    private Vector3[] moveAxes;
    private float[] moveAmplitudes;
    private float[] movePhases;
    private bool[] soundPlayed = new bool[3];

    void Start()
    {
        if (lights.Length == 0)
        {
            Debug.LogWarning("RandomLightFlicker: No lights assigned!");
            return;
        }

        overallFrequency = Mathf.Clamp01(overallFrequency);
        elapsedTime = 0f;
        soundPlayed[0] = false;
        soundPlayed[1] = false;
        soundPlayed[2] = false;
        rigidbodyActivated = false;

        if (rigidbodyTarget != null)
        {
            rigidbodyTarget.isKinematic = true;
        }

        lightTimers = new float[lights.Length];
        lightStates = new bool[lights.Length];
        initialPositions = new Vector3[lights.Length];
        moveAxes = new Vector3[lights.Length];
        moveAmplitudes = new float[lights.Length];
        movePhases = new float[lights.Length];
        initialIntensities = new float[lights.Length];
        initialRanges = new float[lights.Length];

        // Initialize all lights, timers, and oscillation data
        for (int i = 0; i < lights.Length; i++)
        {
            lightStates[i] = lights[i].enabled;
            lightTimers[i] = lightStates[i] ? Random.Range(minOnDuration, maxOnDuration) : Random.Range(minOffDuration, maxOffDuration);
            initialPositions[i] = lights[i].transform.localPosition;
            initialIntensities[i] = lights[i].intensity;
            initialRanges[i] = lights[i].range;
            bool horizontal = Random.value > 0.5f;
            moveAxes[i] = horizontal ? lights[i].transform.right : lights[i].transform.forward;
            moveAmplitudes[i] = Random.Range(minMoveAmplitude, maxMoveAmplitude);
            movePhases[i] = Random.Range(0f, Mathf.PI * 2f);
        }
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;

        if (overallDuration > 0f && elapsedTime >= overallDelay)
        {
            float t = (elapsedTime - overallDelay) / overallDuration;
            overallFrequency = Mathf.Lerp(1f, 0.1f, Mathf.Clamp01(t));

            if (!soundPlayed[0] && soundSource1 != null)
            {
                soundSource1.Play();
                soundPlayed[0] = true;
            }
            if (!soundPlayed[1] && soundSource2 != null)
            {
                soundSource2.Play();
                soundPlayed[1] = true;
            }
            if (!soundPlayed[2] && soundSource3 != null)
            {
                soundSource3.Play();
                soundPlayed[2] = true;
            }

            if (!dropActivated && overallFrequency < 1f && activateOnDrop != null)
            {
                activateOnDrop.SetActive(true);
                dropActivated = true;
            }

            if (!rigidbodyActivated && overallFrequency <= 0.1f && rigidbodyTarget != null)
            {
                rigidbodyTarget.isKinematic = false;
                rigidbodyActivated = true;
            }
        }
        else
        {
            overallFrequency = 1f;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            lightTimers[i] -= Time.deltaTime;

            if (lightTimers[i] <= 0f)
            {
                // Toggle the light state
                lightStates[i] = !lightStates[i];
                lights[i].enabled = lightStates[i];

                // Set new timer based on current state
                if (lightStates[i])
                {
                    lightTimers[i] = Random.Range(minOnDuration, maxOnDuration) * overallFrequency;
                }
                else
                {
                    lightTimers[i] = Random.Range(minOffDuration, maxOffDuration) * overallFrequency;
                }
            }

            float moveOffset = Mathf.Sin(Time.time * movementSpeed + movePhases[i]) * moveAmplitudes[i];
            lights[i].transform.localPosition = initialPositions[i] + moveAxes[i] * moveOffset;

            lights[i].intensity = initialIntensities[i] + (1 - overallFrequency) * intensityMultiplier;
            lights[i].range = initialRanges[i] + (1 - overallFrequency) * rangeMultiplier;
        }
    }
}
