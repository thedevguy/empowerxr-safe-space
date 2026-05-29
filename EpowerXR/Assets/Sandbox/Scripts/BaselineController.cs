using UnityEngine;

public class BaselineController : MonoBehaviour
{
    public Light roomLight;
    public Color softGray = new Color(0.7f, 0.7f, 0.7f);

    public void Activate()
    {
        gameObject.SetActive(true);
        RenderSettings.ambientIntensity = 1.8f;
    }

    public void Deactivate() => gameObject.SetActive(false);
}
