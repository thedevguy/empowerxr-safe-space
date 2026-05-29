using UnityEngine;

public class OrbitBehavior : MonoBehaviour
{
    private Transform pivot;
    private float speed;
    private float angleOffset;
    private float radius;
    private float verticalOffset;
    private float objectHalfHeight;

    [Header("Floor Detection")]
    public LayerMask floorMask = ~0;        // Standardmäßig alle Layer
    public float minHeightAboveFloor = 0.3f;

    public void Init(Transform pivot, float radius, float speed, int index, int total)
    {
        this.pivot = pivot;
        this.radius = radius;
        this.speed = speed;
        this.angleOffset = (360f / total) * index;
        this.verticalOffset = Mathf.Sin(index) * 1.5f;

        // Halbe Höhe des Objekts ermitteln damit auch große Cubes nicht im Boden stecken
        var renderer = GetComponent<Renderer>();
        objectHalfHeight = renderer != null ? renderer.bounds.extents.y : 0.5f;
    }

    void Update()
    {
        float angle = (Time.time * speed + angleOffset) * Mathf.Deg2Rad;

        Vector3 desiredPosition = pivot.position + new Vector3(
            Mathf.Cos(angle) * radius,
            verticalOffset,
            Mathf.Sin(angle) * radius
        );

        // Boden unter der gewünschten Position suchen
        float floorY = GetFloorY(desiredPosition);
        float minY = floorY + objectHalfHeight + minHeightAboveFloor;

        desiredPosition.y = Mathf.Max(desiredPosition.y, minY);
        transform.position = desiredPosition;
    }

    float GetFloorY(Vector3 position)
    {
        // Von oben nach unten raycasten
        Ray ray = new Ray(position + Vector3.up * 5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f, floorMask))
            return hit.point.y;

        // Fallback falls kein Boden gefunden
        return 0f;
    }
}
