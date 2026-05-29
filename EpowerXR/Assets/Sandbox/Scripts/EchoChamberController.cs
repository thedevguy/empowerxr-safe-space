using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoChamberController : MonoBehaviour
{
    [Header("Wall Closing")]
    public Transform[] movingWalls;         // Linke, rechte, vordere, hintere Wand
    public float[] wallTargetPositions;     // Ziel-X oder Z je Wand
    public float wallSpeed = 0.3f;

    [Header("Audio")]
    public AudioClip[] harshSounds;         // Alarm, Verkehr, Dissonanz
    public int audioSourceCount = 5;
    public float maxVolume = 1.0f;
    public float volumeRampDuration = 20f;

    private List<AudioSource> audioSources = new();
    private bool isActive;

    public void Activate()
    {
        gameObject.SetActive(true);
        isActive = true;
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
            // Zufällige Position um den Spieler (Radius 1-3m)
            go.transform.localPosition = Random.insideUnitSphere * 2.5f;
            var src = go.AddComponent<AudioSource>();
            src.clip = harshSounds[i % harshSounds.Length];
            src.loop = true;
            src.spatialBlend = 1f;           // Voller 3D-Sound
            src.volume = 0f;
            src.Play();
            audioSources.Add(src);
        }
    }

    IEnumerator CloseWalls()
    {
        // Wände bewegen sich über die gesamte Laufzeit
        float elapsed = 0;
        Vector3[] startPositions = System.Array.ConvertAll(movingWalls, w => w.position);

        while (elapsed < volumeRampDuration && isActive)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / volumeRampDuration;
            for (int i = 0; i < movingWalls.Length; i++)
            {
                var pos = movingWalls[i].position;
                // Nur die relevante Achse bewegen (X oder Z je Wand)
                pos.x = Mathf.Lerp(startPositions[i].x, wallTargetPositions[i], t);
                movingWalls[i].position = pos;
            }
            yield return null;
        }
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
        isActive = false;
        foreach (var src in audioSources) Destroy(src.gameObject);
        audioSources.Clear();
        gameObject.SetActive(false);
    }
}
