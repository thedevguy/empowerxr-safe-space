using UnityEngine;

public class DriftingRotation : MonoBehaviour
{
    [SerializeField] private float speed = 20f;
    [SerializeField] private float directionChangeInterval = 2f;

    private Vector3 currentAxis;
    private Vector3 targetAxis;
    private float timer;

    void Start()
    {
        currentAxis = Random.onUnitSphere;
        targetAxis = Random.onUnitSphere;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= directionChangeInterval)
        {
            currentAxis = targetAxis;
            targetAxis = Random.onUnitSphere;
            timer = 0f;
        }

        Vector3 axis = Vector3.Slerp(currentAxis, targetAxis, timer / directionChangeInterval);
        transform.Rotate(axis * speed * Time.deltaTime, Space.World);
    }
}
