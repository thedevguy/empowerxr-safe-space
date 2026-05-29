using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcFlashController : MonoBehaviour
{
    [Header("Lighting")]
    public float dimDuration = 5f;      // Wie lange das Abdunkeln dauert

    [Header("Shape Settings")]
    public GameObject[] primitivePrefabs;   // Sphere, Cube, etc.
    public int shapeCount = 12;
    public float orbitRadius = 3f;
    public float orbitSpeed = 180f;         // Grad/Sekunde
    public float flashInterval = 0.1f;
    public float emissionIntensity = 2.5f;   // Höher = stärker selbstleuchtend

    [Header("Colors")]
    public Color[] neonColors;

    private List<GameObject> spawnedShapes = new();
    private Coroutine flashRoutine;


    public void Activate()
    {
        gameObject.SetActive(true);
        StartCoroutine(RampAmbient(0f, dimDuration));   // Von aktuellem Wert auf 0
        StartCoroutine(SpawnAndOrbit());
        flashRoutine = StartCoroutine(FlashLoop());
    }

    IEnumerator RampAmbient(float target, float duration)
    {
        float elapsed = 0f;
        float start = RenderSettings.ambientIntensity;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            RenderSettings.ambientIntensity = Mathf.Lerp(start, target, elapsed / duration);
            yield return null;
        }
        RenderSettings.ambientIntensity = target;
    }

    IEnumerator SpawnAndOrbit()
    {
        for (int i = 0; i < shapeCount; i++)
        {
            var go = Instantiate(primitivePrefabs[i % primitivePrefabs.Length], transform);
            go.AddComponent<OrbitBehavior>().Init(transform, orbitRadius, orbitSpeed, i, shapeCount);

            // Neues Material anlegen — nie das shared material verändern
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color neon = neonColors[i % neonColors.Length];

            mat.color = neon;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", neon * emissionIntensity);  // * intensity = HDR-Wert

            go.GetComponent<Renderer>().material = mat;
            spawnedShapes.Add(go);
        }
        yield break;
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

    public void Deactivate()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        // Shapes bleiben — sie fliegen noch durch Chamber 3
        // gameObject bleibt aktiv
    }


    //public void Deactivate()
    //{
    //    if (flashRoutine != null) StopCoroutine(flashRoutine);
    //    foreach (var s in spawnedShapes) Destroy(s);
    //    spawnedShapes.Clear();
    //    gameObject.SetActive(false);
    //}



}
