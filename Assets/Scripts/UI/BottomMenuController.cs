using UnityEngine;

public class BottomMenuController : MonoBehaviour
{
    [Header("Panels")]
    public PanelToggle inventoryToggle;
    public PanelToggle craftingToggle;

    [Header("Shortcuts")]
    public KeyCode inventoryKey = KeyCode.I;
    public KeyCode craftingKey  = KeyCode.C;

    void Update()
    {
        if (Input.GetKeyDown(inventoryKey))
            inventoryToggle?.Toggle();

        if (Input.GetKeyDown(craftingKey))
            craftingToggle?.Toggle();
    }

    // In case you want to register more toggles later from code.
    public void TogglePanel(PanelToggle toggle) => toggle?.Toggle();
    public void ShowPanel(PanelToggle toggle)   => toggle?.Show();
    public void HidePanel(PanelToggle toggle)   => toggle?.Hide();
}
