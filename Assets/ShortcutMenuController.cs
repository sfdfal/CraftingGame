using UnityEngine;

[System.Serializable]
public class KeyShortcut
{
    [Tooltip("Haupttaste (z. B. I, C, F1, etc.)")]
    public KeyCode key = KeyCode.None;

    [Header("Optionale Modifier")]
    public bool requireShift = false;
    public bool requireCtrl  = false;
    public bool requireAlt   = false;

    public bool IsPressedThisFrame()
    {
        if (key == KeyCode.None) return false;

        // Haupttaste gedrückt?
        bool down = Input.GetKeyDown(key);
        if (!down) return false;

        // Modifier prüfen
        if (requireShift)
        {
            bool s = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!s) return false;
        }
        if (requireCtrl)
        {
            bool c = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                  || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
            if (!c) return false;
        }
        if (requireAlt)
        {
            bool a = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!a) return false;
        }
        return true;
    }
}

public class ShortcutMenuController : MonoBehaviour
{
    [Header("Targets")]
    public PanelToggle inventoryToggle;
    public PanelToggle craftingToggle;

    [Header("Shortcuts (konfigurierbar im Inspector)")]
    public KeyShortcut inventoryShortcut = new KeyShortcut { key = KeyCode.I };
    public KeyShortcut craftingShortcut  = new KeyShortcut { key = KeyCode.C };

    [Header("Debug")]
    public bool logWhenTriggered = false;

    void Update()
    {
        // Game Window muss Fokus haben
        if (!Application.isFocused) return;

        if (inventoryShortcut.IsPressedThisFrame())
        {
            if (logWhenTriggered) Debug.Log("[Shortcuts] Inventory toggle");
            inventoryToggle?.Toggle();
        }

        if (craftingShortcut.IsPressedThisFrame())
        {
            if (logWhenTriggered) Debug.Log("[Shortcuts] Crafting toggle");
            craftingToggle?.Toggle();
        }
    }
}
