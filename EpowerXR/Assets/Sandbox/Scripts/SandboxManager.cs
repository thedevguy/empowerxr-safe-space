using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class SandboxManager : MonoBehaviour
{
    public enum ChamberState { Baseline, ArcFlash, EchoChamber, Done }

    [Header("References")]
    public BaselineController chamber1;
    public ArcFlashController chamber2;
    public EchoChamberController chamber3;
    public WarningUI warningUI;

    private ChamberState currentState;

    void Start() => StartCoroutine(RunExperiment());

    IEnumerator RunExperiment()
    {
        // Chamber 1
        currentState = ChamberState.Baseline;
        chamber1.Activate();
        yield return new WaitForSeconds(15f);
        chamber1.Deactivate();

        // Chamber 2
        yield return warningUI.ShowWarning("Prepare for sudden high-contrast visuals and rapid movement.", 5f);
        currentState = ChamberState.ArcFlash;
        chamber2.Activate();
        yield return new WaitForSeconds(30f);
        chamber2.Deactivate();

        // Chamber 3
        yield return warningUI.ShowWarning("Prepare for spatial audio overload and claustrophobia.", 5f);
        currentState = ChamberState.EchoChamber;
        chamber3.Activate();
        yield return new WaitForSeconds(30f);
        chamber3.Deactivate();

        currentState = ChamberState.Done;
    }
}
