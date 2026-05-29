using UnityEngine;

public class BaselineController : MonoBehaviour
{
    public Light roomLight;
    public Color softGray = new Color(0.7f, 0.7f, 0.7f);

    public void Activate()
    {
        gameObject.SetActive(true);
        roomLight.color = softGray;
        roomLight.intensity = 1.2f;
        RenderSettings.ambientLight = softGray;
    }

    public void Deactivate() => gameObject.SetActive(false);
}
