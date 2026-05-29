using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class PassthroughController : MonoBehaviour
{
    [SerializeField] private ARCameraManager arCameraManager;
    private float timer = 0f;
    private bool toggled = false;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer > 5f && !toggled)
        {
            arCameraManager.enabled = !arCameraManager.enabled;
            Debug.Log("Passthrough toggled: " + arCameraManager.enabled);
            toggled = true;
        }
    }
}