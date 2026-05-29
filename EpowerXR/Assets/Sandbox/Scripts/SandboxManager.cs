using System.Collections;
using UnityEngine;

public class SandboxManager : MonoBehaviour
{
    public enum ChamberState { Baseline, ArcFlash, EchoChamber, Done }
    public ChamberState currentState;

    [Header("References")]
    public BaselineController chamber1;
    public ArcFlashController chamber2;
    public EchoChamberController chamber3;
    public WarningUI warningUI;

    [Header("Timing")]
    public float warningLeadTime = 5f;   // Wie viele Sekunden vor Ende die Warning erscheint
    public float chamber1Duration = 5f;
    public float chamber2Duration = 5f;
    public float chamber3Duration = 5f;

    [Header("Warning Messages")]
    public string arcFlashWarning = "Prepare for sudden high-contrast visuals and rapid movement.";
    public string echoChamberWarning = "Prepare for spatial audio overload and claustrophobia.";



    void Start() => StartCoroutine(RunScene());

    IEnumerator RunScene()
    {
        // Chamber 1 – Baseline
        currentState = ChamberState.Baseline;
        chamber1.Activate();
        yield return new WaitForSeconds(chamber1Duration - warningLeadTime);
        StartCoroutine(warningUI.ShowWarning(arcFlashWarning, warningLeadTime));
        yield return new WaitForSeconds(warningLeadTime);

        // Chamber 2 – Arc Flash
        currentState = ChamberState.ArcFlash;
        chamber2.Activate();
        yield return new WaitForSeconds(chamber2Duration - warningLeadTime);
        StartCoroutine(warningUI.ShowWarning(echoChamberWarning, warningLeadTime));
        yield return new WaitForSeconds(warningLeadTime);

        // Chamber 3 – Echo Chamber (kein Warning danach nötig)
        currentState = ChamberState.EchoChamber;
        chamber3.Activate();
        yield return new WaitForSeconds(chamber3Duration);

        currentState = ChamberState.Done;
        ResetAll();
    }

    public void ResetAll()
    {
        StopAllCoroutines();
        chamber1.Deactivate();
        chamber2.Deactivate();
        chamber3.Deactivate();
        warningUI.GetComponent<CanvasGroup>().alpha = 0f;
    }
}