using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EchoChamberController : MonoBehaviour
{
    // In Chamber3 audis are added and the volumes raised and the ceiling and walls are coming nearer.


    [Header("Audio")]
    public AudioClip[] harshSounds;
    public int audioSourceCount = 5;
    public float maxVolume = 1.0f;
    public float volumeRampDuration = 20f;

    private List<AudioSource> audioSources = new();
    private bool isActive;


    private void Start()
    {
        SpawnAudioSources();
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

        // Destroy audi
        foreach (var src in audioSources) Destroy(src.gameObject);
        audioSources.Clear();

    }

}
