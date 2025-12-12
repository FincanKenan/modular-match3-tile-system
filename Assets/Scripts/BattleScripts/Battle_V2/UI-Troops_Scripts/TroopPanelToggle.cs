using UnityEngine;

public class TroopPanelToggle : MonoBehaviour
{
    [SerializeField] private GameObject troopPanel;   // Saklayýp göstereceðimiz panel

    private bool _isVisible = true;

    private void Awake()
    {
        if (troopPanel != null)
            _isVisible = troopPanel.activeSelf;
    }

    public void TogglePanel()
    {
        if (troopPanel == null)
        {
            Debug.LogWarning("[TroopPanelToggle] TroopPanel referansý atanmadý.");
            return;
        }

        _isVisible = !_isVisible;
        troopPanel.SetActive(_isVisible);
    }
}
