using System.Collections.Generic;
using UnityEngine;

public class TroopSelectionUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TroopSelectionController selection;   // TroopSelection
    [SerializeField] private RectTransform buttonContainer;        // content
    [SerializeField] private TroopButtonView troopButtonPrefab;
    [SerializeField] private TroopCountProvider countProvider;
    [SerializeField] private TroopPanelDragScroll dragScroll;      // <<< YENÝ: Scroll scripti

    private readonly List<TroopButtonView> _spawnedButtons = new List<TroopButtonView>();

    private void Start()
    {
        BuildButtons();
    }

    public void BuildButtons()
    {
        // Eski butonlarý temizle
        foreach (var b in _spawnedButtons)
        {
            if (b != null)
                Destroy(b.gameObject);
        }
        _spawnedButtons.Clear();

        if (selection == null || selection.availableTroops == null)
        {
            Debug.LogWarning("[TroopSelectionUI] Selection/availableTroops yok.");
            return;
        }

        for (int i = 0; i < selection.availableTroops.Count; i++)
        {
            TroopTypeSO troop = selection.availableTroops[i];
            if (troop == null)
                continue;

            int count = countProvider != null ? countProvider.GetCount(troop) : 0;

            var btn = Instantiate(troopButtonPrefab, buttonContainer);
            btn.name = $"TroopButton_{troop.displayName}";
            btn.Init(troop, i, selection, count);

            _spawnedButtons.Add(btn);
        }

        Debug.Log($"[TroopSelectionUI] BuildButtons, troopCount={selection.availableTroops.Count}");

        // --- ÖNEMLÝ: Layout’u güncelle + scroll sýnýrlarýný hesapla ---
        if (dragScroll != null)
        {
            dragScroll.RecalculateBounds();
        }
        else
        {
            Debug.LogWarning("[TroopSelectionUI] dragScroll referansý atanmadý.");
        }
    }

    /// <summary> Asker sayýlarý deðiþtiðinde (yerleþtirme, ödül, vs) çaðýr. </summary>
    public void RefreshCounts()
    {
        if (countProvider == null || selection == null)
            return;

        for (int i = 0; i < _spawnedButtons.Count && i < selection.availableTroops.Count; i++)
        {
            var troop = selection.availableTroops[i];
            int count = countProvider.GetCount(troop);
            _spawnedButtons[i].RefreshCount(count);
        }
    }
}
