using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcFlashController : MonoBehaviour
{
    // In Chamber2 are spawned fliockering and growing primitive shapes that orbit around the player. After the strobedealy a strobe effect is added.

    [Header("Lighting")]
    public Light DirectLight;
    public Color ambientColor;
    public float strobeDelay;
    public float flashInterval = 0.1f;

    [Header("Shape Settings")]
    public GameObject[] primitivePrefabs;   // Sphere, Cube, etc.
    public int shapeCount = 12;
    public float orbitRadius = 3f;
    public float orbitSpeed = 180f;
    public float emissionIntensity = 2.5f;
    public float spawnInterval = 0.8f;

    [Header("Shape Growth")]
    public float startScale = 0.3f;
    public float endScale = 1.5f;
    public float growthDuration = 20f;

    [Header("Colors")]
    public Color[] neonColors;


    private List<GameObject> spawnedShapes = new();
    private Coroutine flashRoutine;


    public void Activate()
    {
        ambientColor = RenderSettings.ambientSkyColor;
        gameObject.SetActive(true);

        StartCoroutine(SpawnAndOrbit());
        StartCoroutine(GrowShapes());
        flashRoutine = StartCoroutine(FlashLoop());
        StartCoroutine(Strobe());
    }


    IEnumerator SpawnAndOrbit()
    {
        for (int i = 0; i < shapeCount; i++)
        {
            var go = Instantiate(primitivePrefabs[i % primitivePrefabs.Length], transform);
            go.AddComponent<OrbitBehavior>().Init(transform, orbitRadius, orbitSpeed, i, shapeCount);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color neon = neonColors[i % neonColors.Length];

            mat.color = neon;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", neon * emissionIntensity);

            go.GetComponent<Renderer>().material = mat;
            spawnedShapes.Add(go);

            yield return new WaitForSeconds(spawnInterval);
        }

    }


    IEnumerator GrowShapes()
    {
        float elapsed = 0f;

        while (elapsed < growthDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / growthDuration);
            float scale = Mathf.Lerp(startScale, endScale, t);

            foreach (var s in spawnedShapes)
                if (s != null) s.transform.localScale = Vector3.one * scale;

            yield return null;
        }
    }


    IEnumerator FlashLoop()
    {
        while (true)
        {
            foreach (var s in spawnedShapes)
                s.SetActive(!s.activeSelf);
            yield return new WaitForSeconds(flashInterval);
        }
    }


    IEnumerator Strobe()
    {
        yield return new WaitForSeconds(strobeDelay);
        while (true)
        {
            // Blitz AN
            DirectLight.enabled = true;
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.ambientSkyColor = Color.white;

            yield return new WaitForSeconds(flashInterval * 0.3f);

            // Blitz AUS
            DirectLight.enabled = false;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.ambientSkyColor = Color.black;

            yield return new WaitForSeconds(flashInterval * 0.7f);
        }
    }



    public void Deactivate()
    {
        StopAllCoroutines();
        if (flashRoutine != null) StopCoroutine(flashRoutine);

        // Destroy shapes
        foreach (var s in spawnedShapes) Destroy(s);
        spawnedShapes.Clear();

        // Reset lights
        DirectLight.enabled = true;
        RenderSettings.ambientIntensity = 1f;
        RenderSettings.ambientSkyColor = ambientColor;

        gameObject.SetActive(false);
    }


}
