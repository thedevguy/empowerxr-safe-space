using System.Collections;
using TMPro;
using UnityEngine;

public class WarningUI : MonoBehaviour
{
    public TMP_Text warningText;
    public CanvasGroup canvasGroup;

    public IEnumerator ShowWarning(string message, float duration)
    {
        warningText.text = message;
        // Fade in
        for (float t = 0; t < 1f; t += Time.deltaTime * 2)
        {
            canvasGroup.alpha = t;
            yield return null;
        }
        yield return new WaitForSeconds(duration - 1f);
        // Fade out
        for (float t = 1f; t > 0; t -= Time.deltaTime * 2)
        {
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
