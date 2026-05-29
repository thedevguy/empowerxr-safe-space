using UnityEngine;

public class TestChamberButton : MonoBehaviour
{
    [Header("References")]
    public BaselineController chamber1;
    public ArcFlashController chamber2;
    public EchoChamberController chamber3;
    public WarningUI warningUI;

    [Header("Warning Messages")]
    public string arcFlashWarning = "Prepare for sudden high-contrast visuals and rapid movement.";
    public string echoChamberWarning = "Prepare for spatial audio overload and claustrophobia.";

    // Aktive Chamber tracken, um sauber zu deaktivieren
    private BaselineController activeChamber1;
    private ArcFlashController activeChamber2;
    private EchoChamberController activeChamber3;

    private void DeactivateAll()
    {
        chamber1?.Deactivate();
        chamber2?.Deactivate();
        chamber3?.Deactivate();
    }

    [ContextMenu("▶ Test Chamber 1 – Baseline")]
    public void TestChamber1()
    {
        DeactivateAll();
        chamber1.Activate();
        Debug.Log("[Test] Chamber 1 aktiviert – läuft 15 Sekunden.");
    }

    [ContextMenu("▶ Test Chamber 2 – Arc Flash")]
    public void TestChamber2()
    {
        DeactivateAll();
        chamber2.Activate();
        Debug.Log("[Test] Chamber 2 aktiviert.");
    }

    [ContextMenu("▶ Test Chamber 3 – Echo Chamber")]
    public void TestChamber3()
    {
        DeactivateAll();
        chamber3.Activate();
        Debug.Log("[Test] Chamber 3 aktiviert.");
    }

    [ContextMenu("▶ Test Warning – Arc Flash")]
    public void TestWarningArcFlash()
    {
        StartCoroutine(warningUI.ShowWarning(arcFlashWarning, 5f));
        Debug.Log("[Test] Warning Arc Flash angezeigt.");
    }

    [ContextMenu("▶ Test Warning – Echo Chamber")]
    public void TestWarningEchoChamber()
    {
        StartCoroutine(warningUI.ShowWarning(echoChamberWarning, 5f));
        Debug.Log("[Test] Warning Echo Chamber angezeigt.");
    }

    [ContextMenu("■ Alles deaktivieren")]
    public void StopAll()
    {
        DeactivateAll();
        Debug.Log("[Test] Alle Chambers deaktiviert.");
    }
}