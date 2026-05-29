using UnityEngine;

public class OrbitBehavior : MonoBehaviour
{
    private Transform pivot;
    private float speed;
    private float angleOffset;
    private float radius;
    private float verticalOffset;

    public void Init(Transform pivot, float radius, float speed, int index, int total)
    {
        this.pivot = pivot;
        this.radius = radius;
        this.speed = speed;
        this.angleOffset = (360f / total) * index;
        this.verticalOffset = Mathf.Sin(index) * 1.5f;
    }

    void Update()
    {
        float angle = (Time.time * speed + angleOffset) * Mathf.Deg2Rad;
        transform.position = pivot.position + new Vector3(
            Mathf.Cos(angle) * radius,
            verticalOffset,
            Mathf.Sin(angle) * radius
        );
    }
}
