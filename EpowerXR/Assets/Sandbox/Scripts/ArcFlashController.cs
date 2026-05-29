using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcFlashController : MonoBehaviour
{
    [Header("Shape Settings")]
    public GameObject[] primitivePrefabs;   // Sphere, Cube, etc.
    public int shapeCount = 12;
    public float orbitRadius = 3f;
    public float orbitSpeed = 180f;         // Grad/Sekunde
    public float flashInterval = 0.1f;

    [Header("Colors")]
    public Color[] neonColors;

    private List<GameObject> spawnedShapes = new();
    private Coroutine flashRoutine;

    public void Activate()
    {
        gameObject.SetActive(true);
        RenderSettings.ambientLight = Color.black;
        StartCoroutine(SpawnAndOrbit());
        flashRoutine = StartCoroutine(FlashLoop());
    }

    IEnumerator SpawnAndOrbit()
    {
        for (int i = 0; i < shapeCount; i++)
        {
            var go = Instantiate(primitivePrefabs[i % primitivePrefabs.Length], transform);
            go.AddComponent<OrbitBehavior>().Init(transform, orbitRadius, orbitSpeed, i, shapeCount);
            go.GetComponent<Renderer>().material.color = neonColors[i % neonColors.Length];
            go.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
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
        foreach (var s in spawnedShapes) Destroy(s);
        spawnedShapes.Clear();
        gameObject.SetActive(false);
    }
}
