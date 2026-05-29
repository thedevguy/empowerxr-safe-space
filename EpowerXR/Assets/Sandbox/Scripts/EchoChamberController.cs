using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoChamberController : MonoBehaviour
{
    // In Chamber3 audis are added and the volumes raised and the ceiling and walls are coming nearer.


    [System.Serializable]
    public class MovingWall
    {
        public Transform wall;
        public Vector3 targetPosition;
        [HideInInspector] public Vector3 startPosition;
    }

    [Header("Wall Closing")]
    public MovingWall[] movingWalls;
    public float closeDuration = 20f;

    [Header("Audio")]
    public AudioClip[] harshSounds;
    public int audioSourceCount = 5;
    public float maxVolume = 1.0f;
    public float volumeRampDuration = 20f;

    private List<AudioSource> audioSources = new();
    private bool isActive;


    public void Activate()
    {
        gameObject.SetActive(true);
        isActive = true;

        // Startpositionen einmalig merken
        foreach (var w in movingWalls)
            w.startPosition = w.wall.position;

        SpawnAudioSources();
        StartCoroutine(CloseWalls());
        StartCoroutine(RampVolume());
    }


    void SpawnAudioSources()
    {
        for (int i = 0; i < audioSourceCount; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Random.insideUnitSphere * 2.5f;
            var src = go.AddComponent<AudioSource>();
            src.clip = harshSounds[i % harshSounds.Length];
            src.loop = true;
            src.spatialBlend = 1f;
            src.volume = 0f;
            src.Play();
            audioSources.Add(src);
        }
    }

    IEnumerator CloseWalls()
    {
        float elapsed = 0;

        while (elapsed < closeDuration && isActive)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / closeDuration);

            foreach (var w in movingWalls)
                w.wall.position = Vector3.Lerp(w.startPosition, w.targetPosition, t);

            yield return null;
        }

        if (isActive)
            foreach (var w in movingWalls)
                w.wall.position = w.targetPosition;
    }



    IEnumerator RampVolume()
    {
        float elapsed = 0;
        while (elapsed < volumeRampDuration && isActive)
        {
            elapsed += Time.deltaTime;
            float vol = Mathf.Lerp(0, maxVolume, elapsed / volumeRampDuration);
            foreach (var src in audioSources) src.volume = vol;
            yield return null;
        }
    }



    public void Deactivate()
    {
        StopAllCoroutines();
        isActive = false;

        // Destroy audi
        foreach (var src in audioSources) Destroy(src.gameObject);
        audioSources.Clear();

        // Reset walls
        foreach (var w in movingWalls)
            if (w.wall != null) w.wall.position = w.startPosition;

        gameObject.SetActive(false);
    }

}
